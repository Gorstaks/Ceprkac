using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;

namespace Ceprkac
{
    /// <summary>
    /// Local HTTPS MITM proxy that strips YouTube ad payloads from responses server-side,
    /// so the pre-roll is gone before the page ever sees the data - without any in-page script
    /// injection (WebView2 has no reliable main-world document-start hook, and CDP injection
    /// black-screens the player). WebView2 is pointed at this proxy via --proxy-server.
    ///
    /// TLS interception is scoped as narrowly as possible: only youtube.com / youtube-nocookie
    /// hosts are decrypted; every other host is tunneled (CONNECT passthrough) untouched, so
    /// banking/login/etc. TLS is never inspected. The proxy trusts its own root CA in the
    /// CurrentUser store so WebView2 accepts the on-the-fly leaf certs.
    /// </summary>
    internal sealed class YtProxy
    {
        private readonly ProxyServer _proxy;
        private ExplicitProxyEndPoint? _endpoint;
        public int Port { get; private set; }
        public bool Running { get; private set; }

        private const string HookMarker = "__ceprkacYtHook";

        // Main-world ad-stripper injected into every YouTube HTML document as the first script,
        // mirroring the GSecurity extension's main-world.js: hook JSON.parse, guard the
        // ytInitial* globals, and strip ad responses from fetch - all before YouTube's scripts
        // run. Deletes only the known ad keys (no array splicing). Because it runs in the page's
        // own main world at document start (delivered inline by the proxy), it catches the ad
        // payload from any source (/player, /get_watch, /next, inline) at parse time.
        private const string MainWorldHookJs =
            "(function(){if(window.__ceprkacYtHook)return;window.__ceprkacYtHook=1;" +
            "var K=['adPlacements','adSlots','playerAds','adBreakHeartbeatParams','ad3Module'," +
            "'adSafetyReason','adLoggingData','showAdSlots','adBreakParams','adBreakStatus'," +
            "'adVideoId','adLayoutLoggingData','instreamAdPlayerOverlayRenderer'," +
            "'adPlacementConfig','adVideoStitcherConfig'];" +
            "function strip(o,d){if(!o||typeof o!=='object'||d>16)return o;" +
            "for(var i=0;i<K.length;i++){if(Object.prototype.hasOwnProperty.call(o,K[i]))delete o[K[i]];}" +
            "var ks=Object.keys(o);for(var j=0;j<ks.length;j++){var v=o[ks[j]];if(v&&typeof v==='object')strip(v,d+1);}return o;}" +
            "var op=JSON.parse;JSON.parse=function(){var r=op.apply(this,arguments);try{if(r&&typeof r==='object')strip(r,0);}catch(e){}return r;};" +
            "function guard(p){var v=window[p];try{Object.defineProperty(window,p,{configurable:true," +
            "get:function(){return v;},set:function(n){if(n&&typeof n==='object'){try{strip(n,0);}catch(e){}}v=n;}});" +
            "if(v)window[p]=v;}catch(e){}}" +
            "guard('ytInitialPlayerResponse');guard('ytInitialData');" +
            "var of=window.fetch;if(of){window.fetch=function(){var a=arguments;return of.apply(this,a).then(function(res){try{" +
            "var u=(typeof a[0]==='string'?a[0]:(a[0]&&a[0].url)||'');" +
            "if(/\\/youtubei\\/v1\\/(player|get_watch|next|reel)/.test(u)){return res.clone().text().then(function(t){try{" +
            "var d=op.call(JSON,t);strip(d,0);return new Response(JSON.stringify(d),{status:res.status,statusText:res.statusText,headers:res.headers});}catch(e){return res;}});}}catch(e){}return res;});};}" +
            "})();";

        // The 15 core YouTube ad keys - same set the GSecurity extension deletes.
        private static readonly string[] AdKeys =
        {
            "adPlacements","adSlots","playerAds","adBreakHeartbeatParams","ad3Module",
            "adSafetyReason","adLoggingData","showAdSlots","adBreakParams","adBreakStatus",
            "adVideoId","adLayoutLoggingData","instreamAdPlayerOverlayRenderer",
            "adPlacementConfig","adVideoStitcherConfig",
        };

        public YtProxy()
        {
            _proxy = new ProxyServer(userTrustRootCertificate: true);
            // Root cert used to sign per-host leaf certs. Kept in CurrentUser.
            _proxy.CertificateManager.RootCertificateName = RootCertName;
            _proxy.CertificateManager.RootCertificateIssuerName = "Ceprkac";
        }

        public void Start()
        {
            if (Running) return;
            try
            {
                // Latency reduction: reuse upstream sockets and pool connections so each
                // YouTube request doesn't pay a fresh TCP+TLS handshake through the proxy.
                try { _proxy.EnableConnectionPool = true; } catch { }
                try { _proxy.ReuseSocket = true; } catch { }
                try { _proxy.TcpTimeWaitSeconds = 10; } catch { }
                try { _proxy.ConnectionTimeOutSeconds = 30; } catch { }
                // Cache leaf certs on disk so the first hit to each host isn't slowed by
                // generating a cert every launch.
                try { _proxy.CertificateManager.SaveFakeCertificates = true; } catch { }

                // Ensure a root cert exists, then trust it SILENTLY. Titanium's
                // TrustRootCertificate can trigger the Windows "install this certificate?"
                // confirmation dialog; adding it to the CurrentUser Root store directly via
                // X509Store does not prompt (no admin needed either). We only add it if it's
                // not already present, so normal launches touch nothing.
                try { _proxy.CertificateManager.CreateRootCertificate(persistToFile: true); } catch { }
                try { EnsureRootTrustedSilently(_proxy.CertificateManager.RootCertificate); } catch { }

                Port = FindFreePort();
                _endpoint = new ExplicitProxyEndPoint(IPAddress.Loopback, Port, decryptSsl: true);

                // Only MITM-decrypt YouTube hosts; tunnel everything else untouched.
                _endpoint.BeforeTunnelConnectRequest += (sender, e) =>
                {
                    try
                    {
                        var host = (e.HttpClient.Request.RequestUri.Host ?? "").ToLowerInvariant();
                        e.DecryptSsl = IsYouTubeHost(host);
                    }
                    catch { e.DecryptSsl = false; }
                    return Task.CompletedTask;
                };

                _proxy.AddEndPoint(_endpoint);
                _proxy.BeforeResponse += OnBeforeResponse;
                _proxy.Start();
                Running = true;
                Logger.Log("PROXY", $"YouTube filter proxy started on 127.0.0.1:{Port}");
            }
            catch (Exception ex)
            {
                Logger.Error("PROXY", "start failed", ex);
                Running = false;
            }
        }

        public void Stop()
        {
            try
            {
                if (_endpoint != null) _endpoint.BeforeTunnelConnectRequest -= null;
                _proxy.BeforeResponse -= OnBeforeResponse;
                if (Running) _proxy.Stop();
            }
            catch { }
            Running = false;
        }

        private const string RootCertName = "Ceprkac YouTube Filter";

        // True if our root cert is already trusted in either the LocalMachine or CurrentUser
        // Root store. When true we skip adding it, so no Windows confirmation dialog appears.
        private static bool IsRootAlreadyTrusted(X509Certificate2? root)
        {
            if (root == null) return false;
            foreach (var loc in new[] { StoreLocation.LocalMachine, StoreLocation.CurrentUser })
            {
                try
                {
                    using var store = new X509Store(StoreName.Root, loc);
                    store.Open(OpenFlags.ReadOnly);
                    var found = store.Certificates.Find(X509FindType.FindByThumbprint, root.Thumbprint, false);
                    store.Close();
                    if (found.Count > 0) return true;
                }
                catch { }
            }
            return false;
        }

        // Trust the root without prompting IF it's already installed (e.g. by the elevated
        // installer). Only as a last-resort fallback (dev builds run without the installer) do
        // we add it to the CurrentUser store, which shows Windows' one-time confirmation dialog.
        private static void EnsureRootTrustedSilently(X509Certificate2? root)
        {
            if (root == null) return;
            if (IsRootAlreadyTrusted(root)) return; // installed by setup -> no prompt
            try
            {
                using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);
                store.Add(root);
                store.Close();
            }
            catch { }
        }

        // Installer entry point (run elevated): generate the proxy root cert if needed and add
        // it to the LocalMachine Trusted Root store. Silent because setup is elevated.
        public static void InstallRootCertToLocalMachine()
        {
            try
            {
                var proxy = new ProxyServer(userTrustRootCertificate: false);
                proxy.CertificateManager.RootCertificateName = RootCertName;
                proxy.CertificateManager.RootCertificateIssuerName = "Ceprkac";
                proxy.CertificateManager.CreateRootCertificate(persistToFile: true);
                var root = proxy.CertificateManager.RootCertificate;
                if (root == null) return;
                using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadWrite);
                var found = store.Certificates.Find(X509FindType.FindByThumbprint, root.Thumbprint, false);
                if (found.Count == 0) store.Add(root);
                store.Close();
            }
            catch { }
        }

        // Installer uninstall entry point (run elevated): remove our root(s) from LocalMachine.
        public static void UninstallRootCertFromLocalMachine()
        {
            try
            {
                using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadWrite);
                foreach (var c in store.Certificates.Find(X509FindType.FindBySubjectName, RootCertName, false))
                    store.Remove(c);
                store.Close();
            }
            catch { }
        }

        private static bool IsYouTubeHost(string host) =>
            host == "youtube.com" || host.EndsWith(".youtube.com", StringComparison.Ordinal) ||
            host == "youtube-nocookie.com" || host.EndsWith(".youtube-nocookie.com", StringComparison.Ordinal) ||
            // The desktop/mobile player often fetches /youtubei/v1/player from the googleapis
            // host rather than www.youtube.com; must be decrypted + stripped too or the ad
            // survives. Narrowly scoped to the youtubei API host only.
            host == "youtubei.googleapis.com";

        private async Task OnBeforeResponse(object sender, SessionEventArgs e)
        {
            try
            {
                var req = e.HttpClient.Request;
                var host = (req.RequestUri.Host ?? "").ToLowerInvariant();
                if (!IsYouTubeHost(host)) return;

                var path = req.RequestUri.AbsolutePath ?? "";
                var resp = e.HttpClient.Response;
                var ct = (resp.ContentType ?? "").ToLowerInvariant();
                if (resp.StatusCode < 200 || resp.StatusCode >= 300) return;
                if (!resp.HasBody) return;

                bool isHtml = ct.IndexOf("html", StringComparison.OrdinalIgnoreCase) >= 0;
                bool isJson = ct.IndexOf("json", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!isHtml && !isJson) return;

                // Remove Content-Security-Policy on YouTube HTML documents. YouTube's CSP blocks
                // inline <script> without a nonce, which would silently prevent our injected
                // main-world hook from executing (Chrome extensions are exempt from page CSP; a
                // proxy-injected inline script is not). Stripping CSP lets the hook run.
                if (isHtml)
                {
                    try
                    {
                        foreach (var name in new[] { "content-security-policy", "content-security-policy-report-only" })
                            while (resp.Headers.HeaderExists(name)) resp.Headers.RemoveHeader(name);
                    }
                    catch { }
                }

                string body;
                try { body = await e.GetResponseBodyAsString().ConfigureAwait(false); }
                catch { return; }
                if (string.IsNullOrEmpty(body)) return;

                string modified;
                if (isHtml)
                {
                    // Inject the JSON.parse ad-stripper as the FIRST <script> so it installs in
                    // the page's main world before YouTube's own scripts - like the GSecurity
                    // extension's main-world.js at document_start. Catches the ad payload from any
                    // source at parse time. Injected once per document (guarded by HookMarker).
                    if (body.IndexOf(HookMarker, StringComparison.Ordinal) >= 0) return;
                    string inject = "<script>" + MainWorldHookJs + "</script>";
                    int headIdx = body.IndexOf("<head>", StringComparison.OrdinalIgnoreCase);
                    if (headIdx >= 0)
                        modified = body.Substring(0, headIdx + 6) + inject + body.Substring(headIdx + 6);
                    else
                    {
                        int htmlIdx = body.IndexOf("<html", StringComparison.OrdinalIgnoreCase);
                        int insertAt = htmlIdx >= 0 ? body.IndexOf('>', htmlIdx) + 1 : 0;
                        if (insertAt <= 0) return;
                        modified = body.Substring(0, insertAt) + inject + body.Substring(insertAt);
                    }
                    try { Logger.Log("PROXY", $"injected JSON hook into document: {path}"); } catch { }
                }
                else // JSON response: belt-and-suspenders server-side strip of ad payloads
                {
                    if (body.IndexOf("\"adPlacements\"", StringComparison.Ordinal) < 0 &&
                        body.IndexOf("\"playerAds\"", StringComparison.Ordinal) < 0 &&
                        body.IndexOf("\"adSlots\"", StringComparison.Ordinal) < 0 &&
                        body.IndexOf("\"adBreakParams\"", StringComparison.Ordinal) < 0)
                        return;
                    modified = StripJsonDocument(body);
                    try { Logger.Log("PROXY", $"stripped ad payload: {path}"); } catch { }
                }

                if (!ReferenceEquals(modified, body) && modified != body)
                {
                    e.SetResponseBodyString(modified);
                }
            }
            catch (Exception ex)
            {
                try { Logger.Error("PROXY", "response strip failed", ex); } catch { }
            }
        }

        private static string StripJsonDocument(string json)
        {
            try
            {
                if (string.IsNullOrEmpty(json) || json[0] != '{') return json;
                var node = JsonNode.Parse(json);
                StripAdKeys(node, 0);
                return node!.ToJsonString();
            }
            catch (Exception ex) { try { Logger.Error("PROXY", "StripJsonDocument", ex); } catch { } return json; }
        }

        private static string StripInlineBlob(string html, string marker, ref bool changed)
        {
            try
            {
                int start = html.IndexOf(marker, StringComparison.Ordinal);
                if (start < 0) return html;
                int jsonStart = start + marker.Length;
                if (jsonStart >= html.Length || html[jsonStart] != '{') return html;

                int depth = 0; bool inStr = false; bool esc = false; int end = -1;
                for (int i = jsonStart; i < html.Length; i++)
                {
                    char c = html[i];
                    if (inStr)
                    {
                        if (esc) esc = false;
                        else if (c == '\\') esc = true;
                        else if (c == '"') inStr = false;
                    }
                    else
                    {
                        if (c == '"') inStr = true;
                        else if (c == '{') depth++;
                        else if (c == '}') { depth--; if (depth == 0) { end = i + 1; break; } }
                    }
                }
                if (end < 0) return html;

                string jsonText = html.Substring(jsonStart, end - jsonStart);
                var node = JsonNode.Parse(jsonText);
                StripAdKeys(node, 0);
                string cleaned = node!.ToJsonString();
                changed = true;
                return html.Substring(0, jsonStart) + cleaned + html.Substring(end);
            }
            catch { return html; }
        }

        // Plain key-delete + recurse (no array splicing) - matches the GSecurity extension.
        private static void StripAdKeys(JsonNode? node, int depth)
        {
            if (node == null || depth > 14) return;
            if (node is JsonObject obj)
            {
                foreach (var key in AdKeys)
                    if (obj.ContainsKey(key)) obj.Remove(key);
                foreach (var kv in obj)
                    StripAdKeys(kv.Value, depth + 1);
            }
            else if (node is JsonArray arr)
            {
                foreach (var item in arr)
                    StripAdKeys(item, depth + 1);
            }
        }

        private static int FindFreePort()
        {
            var l = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
    }
}
