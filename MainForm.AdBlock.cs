using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net.Http;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Ceprkac
{
    public partial class MainForm
    {
        private static readonly HashSet<string> BlockedAdDomains = new(StringComparer.OrdinalIgnoreCase)
        {
            // Google Ads & Analytics
            "doubleclick.net","googleadservices.com","googlesyndication.com","adservice.google.com",
            "ads.google.com","google-analytics.com","googletagmanager.com","googletagservices.com",
            "pagead2.googlesyndication.com","pagead2.googleadservices.com",
            // Major ad networks
            "adnxs.com","taboola.com","outbrain.com","criteo.com","scorecardresearch.com","pubmatic.com",
            "rubiconproject.com","quantserve.com","quantcast.com","omniture.com","comscore.com",
            "krux.com","bluekai.com","exelate.com","adform.com","adroll.com","vungle.com","inmobi.com",
            "flurry.com","mixpanel.com","heap.io","amplitude.com","optimizely.com","bizible.com",
            "pardot.com","hubspot.com","marketo.com","eloqua.com","media.net","appnexus.com","adbrite.com",
            "admob.com","adsonar.com","zergnet.com","revcontent.com","mgid.com","adblade.com","adcolony.com",
            "chartbeat.com","newrelic.com","pingdom.net","kissmetrics.com","tradedesk.com","turn.com",
            "adscale.com","bannerflow.com","nativeads.com","contentad.com","displayads.com",
            "smartadserver.com","openx.net","casalemedia.com","indexww.com","sharethrough.com",
            "33across.com","triplelift.com","sovrn.com","lijit.com","bidswitch.net","yieldmo.com",
            "teads.tv","spotxchange.com","springserve.com","contextweb.com","liveintent.com",
            "adtech.de","adform.net","serving-sys.com","adsafeprotected.com","moatads.com",
            // Facebook / Meta
            "connect.facebook.net","pixel.facebook.com","analytics.facebook.com","ads.facebook.com","an.facebook.com",
            // Twitter / X
            "ads-twitter.com","static.ads-twitter.com","analytics.twitter.com","ads-api.twitter.com","advertising.twitter.com",
            // Reddit
            "pixel.reddit.com","rereddit.com","ads.reddit.com","events.reddit.com","events.redditmedia.com","d.reddit.com",
            // LinkedIn
            "ads.linkedin.com","analytics.pointdrive.linkedin.com",
            // TikTok
            "analytics.tiktok.com","ads.tiktok.com","ads-sg.tiktok.com","analytics-sg.tiktok.com",
            // Pinterest
            "ads.pinterest.com","log.pinterest.com","ads-dev.pinterest.com","analytics.pinterest.com",
            "trk.pinterest.com","trk2.pinterest.com","widgets.pinterest.com",
            // Amazon
            "amazon-adsystem.com","advertising-api-eu.amazon.com","amazonaax.com","amazonclix.com","assoc-amazon.com",
            // YouTube
            "youtubeads.googleapis.com","ads.youtube.com","analytics.youtube.com","video-stats.video.google.com",
            "youtube.cleverads.vn",
            // Yahoo
            "advertising.yahoo.com","ads.yahoo.com","adserver.yahoo.com","global.adserver.yahoo.com",
            "adspecs.yahoo.com","analytics.yahoo.com","analytics.query.yahoo.com","comet.yahoo.com",
            "log.fc.yahoo.com","ganon.yahoo.com","gemini.yahoo.com","beap.gemini.yahoo.com",
            "geo.yahoo.com","marketingsolutions.yahoo.com","pclick.yahoo.com",
            "ads.yap.yahoo.com","m.yap.yahoo.com","partnerads.ysm.yahoo.com",
            // Yandex
            "appmetrica.yandex.com","yandexadexchange.net","adfox.yandex.ru","adsdk.yandex.ru",
            "an.yandex.ru","awaps.yandex.ru","awsync.yandex.ru","bs.yandex.ru","bs-meta.yandex.ru",
            "clck.yandex.ru","informer.yandex.ru","kiks.yandex.ru","mc.yandex.ru","metrika.yandex.ru",
            "share.yandex.ru","offerwall.yandex.net",
            // Hotjar / Session recording
            "hotjar.com","api-hotjar.com","hotjar-analytics.com","fullstory.com","mouseflow.com",
            "luckyorange.com","luckyorange.net","freshmarketer.com",
            // Segment / Analytics
            "segment.io","segment.com","stats.wp.com",
            // Error trackers
            "notify.bugsnag.com","sessions.bugsnag.com","api.bugsnag.com","app.bugsnag.com",
            "browser.sentry-cdn.com","app.getsentry.com",
            // FastClick
            "fastclick.com","fastclick.net",
            // Samsung
            "samsungadhub.com","samsungads.com","smetrics.samsung.com","nmetrics.samsung.com",
            "analytics.samsungknox.com","bigdata.ssp.samsung.com","config.samsungads.com",
            // Apple metrics
            "metrics.apple.com","securemetrics.apple.com","supportmetrics.apple.com",
            "metrics.icloud.com","metrics.mzstatic.com","books-analytics-events.apple.com",
            "stocks-analytics-events.apple.com",
            // Xiaomi
            "api.ad.xiaomi.com","data.mistat.xiaomi.com","sdkconfig.ad.xiaomi.com",
            "globalapi.ad.xiaomi.com","tracking.miui.com","tracking.intl.miui.com",
            // Huawei
            "metrics.data.hicloud.com","logservice.hicloud.com","logbak.hicloud.com",
            // OPPO / Realme / OnePlus
            "adsfs.oppomobile.com","bdapi-in-ads.realmemobile.com",
            "analytics.oneplus.cn","click.oneplus.cn","click.oneplus.com","open.oneplus.net",
            // Missing from d3ward test
            "events.hotjar.io","extmaps-api.yandex.net","metrics2.data.hicloud.com",
            "logservice1.hicloud.com","iot-eu-logser.realme.com","click.googleanalytics.com",
            "grs.hicloud.com","udcm.yahoo.com","auction.unityads.unity3d.com",
            "config.unityads.unity3d.com","adserver.unityads.unity3d.com","webview.unityads.unity3d.com",
            "adfstat.yandex.ru","iadsdk.apple.com","appmetrica.yandex.ru",
            "business-api.tiktok.com","log.byteoversea.com","ads-api.tiktok.com",
            "iot-logser.realme.com","tracking.rus.miui.com","adtech.yahooinc.com",
            "bdapi-ads.realmemobile.com","ck.ads.oppomobile.com","data.ads.oppomobile.com",
            "adx.ads.oppomobile.com","data.mistat.india.xiaomi.com","data.mistat.rus.xiaomi.com",
            "notes-analytics-events.apple.com","weather-analytics-events.apple.com",
            "api-adservices.apple.com","samsung-com.112.2o7.net","analytics-api.samsunghealthcn.com",
            "unityads.unity3d.com","byteoversea.com","yahooinc.com",
            // S3-hosted ad/analytics buckets
            "adtago.s3.amazonaws.com","analyticsengine.s3.amazonaws.com",
            "analytics.s3.amazonaws.com","advice-ads.s3.amazonaws.com",
            // Adult site ad networks
            "trafficjunky.com","trafficjunky.net","trafficstars.com","tsyndicate.com",
            "exoclick.com","exosrv.com","exoticads.com","juicyads.com","realsrv.com",
            "adsrv.org","padsdel.com","tsyndicate.com","syndication.exoclick.com",
            "main.exoclick.com","static.exoclick.com","ads.trafficjunky.net",
            "cdn.trafficjunky.net","adsrv.eacdn.com","a.realsrv.com",
            "mc.yandex.ru","syndication.realsrv.com","s.magsrv.com","magsrv.com",
            // Additional missing
            "sdkconfig.ad.intl.xiaomi.com","iot-eu-logser.realme.com","iot-logser.realme.com",
            "bdapi-ads.realmemobile.com","analytics-api.samsunghealthcn.com",
        };

        private static readonly HashSet<string> AdBlockWhitelist = new(StringComparer.OrdinalIgnoreCase)
        {
            "discord.com", "discordapp.com", "discordapp.net", "discord.gg", "discord.media",
            "cloudflare.com", "challenges.cloudflare.com", "cdnjs.cloudflare.com",
            "youtube-nocookie.com",
            "apple.com", "icloud.com",
            "ebay.com",
            "paypal.com",
            "mediafire.com",
            // Auth/OAuth providers
            "accounts.google.com", "accounts.youtube.com", "myaccount.google.com",
            "google.com", "www.google.com", "google.hr", "google.co.uk",
            "youtube.com", "www.youtube.com",
            "login.microsoftonline.com", "login.live.com", "login.microsoft.com",
            "appleid.apple.com", "idmsa.apple.com",
            "github.com", "auth0.com", "okta.com",
            "apis.google.com", "ssl.gstatic.com",
            "pay.google.com", "payments.google.com",
            "gog.com", "auth.gog.com", "login.gog.com",
            "suno.com", "suno.ai", "clerk.suno.com",
            // AI services
            "openai.com", "chat.openai.com", "chatgpt.com",
            "claude.ai", "anthropic.com",
            "gemini.google.com", "bard.google.com",
            "perplexity.ai", "you.com",
            "midjourney.com", "stability.ai",
            "huggingface.co", "replicate.com",
            "udio.com", "poe.com", "character.ai",
            "copilot.microsoft.com",
            // Banking & financial
            "chase.com", "bankofamerica.com", "wellsfargo.com", "citibank.com",
            "usbank.com", "capitalone.com", "discover.com", "americanexpress.com",
            "hsbc.com", "barclays.com", "natwest.com", "lloydsbank.com",
            "revolut.com", "wise.com", "transferwise.com", "stripe.com",
            "squareup.com", "venmo.com", "zelle.com", "cash.app",
            "ing.com", "raiffeisen.hr", "pbz.hr", "zaba.hr", "erstebank.hr",
            "n26.com", "monzo.com", "starlingbank.com",
            // Gaming clients & stores
            "steampowered.com", "store.steampowered.com", "steamcommunity.com",
            "epicgames.com", "unrealengine.com",
            "gog.com", "gogalaxy.com",
            "ea.com", "origin.com",
            "ubisoft.com", "ubi.com",
            "blizzard.com", "battle.net", "battlenet.com.cn",
            "riotgames.com", "leagueoflegends.com",
            "xbox.com", "xboxlive.com",
            "playstation.com", "sonyentertainmentnetwork.com",
            "nintendo.com", "nintendo.net",
            "humblebundle.com", "itch.io", "indiegala.com",
            "twitch.tv",
        };

        private static string BaseDomain(string host)
        {
            var p = host.Split('.');
            if (p.Length >= 3 && (p[p.Length - 1] == "uk" || p[p.Length - 1] == "au" || p[p.Length - 1] == "jp" || p[p.Length - 1] == "br" || p[p.Length - 1] == "za" || p[p.Length - 1] == "nz" || p[p.Length - 1] == "kr" || p[p.Length - 1] == "in"))
                return string.Join(".", p[p.Length - 3], p[p.Length - 2], p[p.Length - 1]);
            return p.Length >= 2 ? string.Join(".", p[p.Length - 2], p[p.Length - 1]) : host;
        }

        private static bool SameSite(string a, string b) =>
            string.Equals(BaseDomain(a), BaseDomain(b), StringComparison.OrdinalIgnoreCase);

        private static bool IsAdBlockWhitelisted(string host)
        {
            while (host.Contains('.'))
            {
                if (AdBlockWhitelist.Contains(host)) return true;
                int dot = host.IndexOf('.');
                host = host.Substring(dot + 1);
            }
            return false;
        }

        private enum YtRule { None, Allow, Block }

        // YouTube-specific network classifier, ported from the GSecurity Ad Shield extension's
        // rules.json (allow @ priority 100 beats block @ priority 1). Allow the playback + data
        // machinery so the media pipeline never breaks; block only the ad endpoints so the
        // pre-roll/mid-roll never arrives. Returns None for anything not YouTube-related so the
        // caller's generic logic still applies on non-YouTube requests.
        private static YtRule ClassifyYouTubeRequest(string host, string path)
        {
            // ---- ALLOW: playback + data endpoints (priority 100 in the extension) ----
            // Media/content CDNs and image/avatar hosts.
            if (HostIsOrEndsWith(host, "googlevideo.com") ||
                HostIsOrEndsWith(host, "ytimg.com") ||
                HostIsOrEndsWith(host, "ggpht.com") ||
                HostIsOrEndsWith(host, "youtube-nocookie.com") ||
                HostIsOrEndsWith(host, "jnn-pa.googleapis.com"))
                return YtRule.Allow;

            bool isYtHost = host == "youtube.com" || host == "www.youtube.com" ||
                            host == "m.youtube.com" || host == "music.youtube.com" ||
                            host.EndsWith(".youtube.com");

            if (isYtHost)
            {
                // Player / API / stats-watchtime paths the player needs to run.
                if (path.StartsWith("/embed", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/s/", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/youtubei/", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/get_video_info", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/player_ias", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/api/stats/watchtime", StringComparison.OrdinalIgnoreCase) ||
                    path.Contains("/embed/"))
                    return YtRule.Allow;

                // ---- BLOCK: YouTube's own ad/stats-ad endpoints ----
                if (path.StartsWith("/api/stats/ads", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/api/stats/atr", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/pagead/", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/ptracking", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/get_midroll", StringComparison.OrdinalIgnoreCase))
                    return YtRule.Block;

                // Any other youtube.com request on a youtube page: leave alone (Allow) so we
                // never accidentally 403 something the player needs.
                return YtRule.Allow;
            }

            // ---- BLOCK: dedicated YouTube/Google ad hosts (priority 1 in the extension) ----
            if (host == "ads.youtube.com" ||
                host == "analytics.youtube.com" ||
                HostIsOrEndsWith(host, "youtubeads.googleapis.com") ||
                host == "youtubei.googleapis.com" ||
                HostIsOrEndsWith(host, "googlesyndication.com") ||
                HostIsOrEndsWith(host, "googleadservices.com") ||
                HostIsOrEndsWith(host, "doubleclick.net"))
            {
                // static.doubleclick.net/instream/ad_status.js is a probe the player waits on -
                // let it through so the player doesn't stall in its "checking ads" state.
                if (path.IndexOf("/instream/ad_status.js", StringComparison.OrdinalIgnoreCase) >= 0)
                    return YtRule.Allow;
                return YtRule.Block;
            }

            return YtRule.None;
        }

        private static bool HostIsOrEndsWith(string host, string domain) =>
            host == domain || host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if a URL points to a known ad/tracking domain.
        /// Used to block navigations and new windows to ad destinations.
        /// </summary>
        private bool IsAdUrl(string url)
        {
            try
            {
                var uri = new Uri(url.Contains("://") ? url : "https://" + url);
                var host = uri.Host.ToLower();
                // Don't block whitelisted domains
                if (IsAdBlockWhitelisted(host)) return false;
                // Check against blocklist
                var checkHost = host;
                while (checkHost.Contains('.'))
                {
                    if (BlockedAdDomains.Contains(checkHost)) return true;
                    int dot = checkHost.IndexOf('.');
                    checkHost = checkHost.Substring(dot + 1);
                }
                // Check common ad URL patterns
                if (url.Contains("/pagead/") || url.Contains("/adclick") ||
                    url.Contains("/aclk?") || url.Contains("googleadservices.com") ||
                    url.Contains("doubleclick.net") || url.Contains("googlesyndication.com"))
                    return true;
            }
            catch { }
            return false;
        }

        private int adsBlockedCount = 0;

        // Shared client for the YouTube player-response strip. Auto-decompress so we get plain
        // JSON text; no cookie container (we forward the page's Cookie header verbatim instead).
        private static readonly HttpClient YtPlayerHttp = new HttpClient(
            new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip
                    | System.Net.DecompressionMethods.Deflate,
                UseCookies = false,
                AllowAutoRedirect = false,
            })
        { Timeout = TimeSpan.FromSeconds(20) };

        // The 15 core YouTube ad keys - same set the GSecurity extension deletes.
        private static readonly string[] YtAdKeys =
        {
            "adPlacements","adSlots","playerAds","adBreakHeartbeatParams","ad3Module",
            "adSafetyReason","adLoggingData","showAdSlots","adBreakParams","adBreakStatus",
            "adVideoId","adLayoutLoggingData","instreamAdPlayerOverlayRenderer",
            "adPlacementConfig","adVideoStitcherConfig",
        };

        // Recursively delete the ad keys from a parsed player-response node. Mirrors the
        // extension's stripAdKeys: plain key-delete + recurse, no array splicing.
        private static void StripYtAdKeys(System.Text.Json.Nodes.JsonNode? node, int depth)
        {
            if (node == null || depth > 14) return;
            if (node is System.Text.Json.Nodes.JsonObject obj)
            {
                foreach (var key in YtAdKeys)
                    if (obj.ContainsKey(key)) obj.Remove(key);
                foreach (var kv in obj)
                    StripYtAdKeys(kv.Value, depth + 1);
            }
            else if (node is System.Text.Json.Nodes.JsonArray arr)
            {
                foreach (var item in arr)
                    StripYtAdKeys(item, depth + 1);
            }
        }

        // Fetch the /youtubei/v1/player response ourselves, delete ad payloads, and hand the
        // cleaned JSON back to the page. Done in C# at the network layer (not via injected JS),
        // so it needs no early main-world injection (CDP black-screens the player in this SDK)
        // and leaves streamingData byte-intact except for the removed ad keys - which is what
        // actually stops the pre-roll while keeping playback. Returns null on any failure so the
        // caller falls back to letting the original request proceed untouched.
        private async Task<CoreWebView2WebResourceResponse?> BuildStrippedPlayerResponseAsync(
            CoreWebView2 core, CoreWebView2WebResourceRequest request)
        {
            try
            {
                var method = new HttpMethod(request.Method ?? "POST");
                using var msg = new HttpRequestMessage(method, request.Uri);

                // Forward the request body (the player request is a POST with a JSON body).
                string? bodyText = null;
                if (request.Content != null)
                {
                    using var ms = new MemoryStream();
                    request.Content.CopyTo(ms);
                    var bytes = ms.ToArray();
                    if (bytes.Length > 0)
                    {
                        bodyText = Encoding.UTF8.GetString(bytes);
                        msg.Content = new ByteArrayContent(bytes);
                    }
                }

                // Forward request headers (cookies, auth, client context) verbatim. Content-Type
                // goes on the content, everything else on the request.
                string contentType = "application/json";
                foreach (var header in request.Headers)
                {
                    var name = header.Key; var value = header.Value;
                    if (string.Equals(name, "Content-Type", StringComparison.OrdinalIgnoreCase))
                    { contentType = value; continue; }
                    if (string.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase)) continue;
                    if (string.Equals(name, "Accept-Encoding", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!msg.Headers.TryAddWithoutValidation(name, value) && msg.Content != null)
                        msg.Content.Headers.TryAddWithoutValidation(name, value);
                }
                if (msg.Content != null)
                {
                    try { msg.Content.Headers.Remove("Content-Type"); } catch { }
                    try { msg.Content.Headers.TryAddWithoutValidation("Content-Type", contentType); } catch { }
                }

                using var resp = await YtPlayerHttp.SendAsync(msg).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode) return null;
                var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (string.IsNullOrEmpty(json) || json[0] != '{') return null;

                var root = System.Text.Json.Nodes.JsonNode.Parse(json);
                StripYtAdKeys(root, 0);
                var cleaned = root!.ToJsonString();

                var outStream = new MemoryStream(Encoding.UTF8.GetBytes(cleaned));
                // Return 200 with JSON content type; CORS-permissive so the page can read it.
                string headers =
                    "Content-Type: application/json; charset=utf-8\r\n" +
                    "Access-Control-Allow-Origin: https://www.youtube.com\r\n" +
                    "Access-Control-Allow-Credentials: true";
                return core.Environment.CreateWebResourceResponse(outStream, 200, "OK", headers);
            }
            catch { return null; }
        }

        // Strip the ad payloads from the inline ytInitialPlayerResponse / ytInitialData JSON
        // embedded in a YouTube /watch HTML document. The FIRST video's ad data is delivered
        // inline in the page HTML (there is no separate /youtubei/v1/player fetch for it), so
        // intercepting the document is the only way to kill the initial pre-roll at the network
        // layer. We fetch the page ourselves, locate each inline blob, JSON-parse just that blob,
        // delete the ad keys, and splice the cleaned JSON back into the HTML. Returns null on any
        // failure so the original document loads untouched.
        private async Task<CoreWebView2WebResourceResponse?> BuildStrippedWatchDocumentAsync(
            CoreWebView2 core, CoreWebView2WebResourceRequest request)
        {
            try
            {
                using var msg = new HttpRequestMessage(new HttpMethod(request.Method ?? "GET"), request.Uri);
                foreach (var header in request.Headers)
                {
                    var name = header.Key;
                    if (string.Equals(name, "Accept-Encoding", StringComparison.OrdinalIgnoreCase)) continue;
                    msg.Headers.TryAddWithoutValidation(name, header.Value);
                }
                using var resp = await YtPlayerHttp.SendAsync(msg).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode) return null;
                var ct = resp.Content.Headers.ContentType?.MediaType ?? "";
                if (ct.IndexOf("html", StringComparison.OrdinalIgnoreCase) < 0) return null;
                var html = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (string.IsNullOrEmpty(html)) return null;

                bool changed = false;
                html = StripInlineYtBlob(html, "var ytInitialPlayerResponse = ", ref changed);
                html = StripInlineYtBlob(html, "var ytInitialData = ", ref changed);
                if (!changed) return null;

                var outStream = new MemoryStream(Encoding.UTF8.GetBytes(html));
                string headers = "Content-Type: text/html; charset=utf-8";
                return core.Environment.CreateWebResourceResponse(outStream, 200, "OK", headers);
            }
            catch { return null; }
        }

        // Find `<marker>{...};` in the HTML, parse the JSON object, strip ad keys, splice back.
        // Uses brace-depth scanning (string-aware) to find the exact end of the JSON literal.
        private static string StripInlineYtBlob(string html, string marker, ref bool changed)
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
                var node = System.Text.Json.Nodes.JsonNode.Parse(jsonText);
                StripYtAdKeys(node, 0);
                string cleaned = node!.ToJsonString();
                changed = true;
                return html.Substring(0, jsonStart) + cleaned + html.Substring(end);
            }
            catch { return html; }
        }

        private async Task SetupAdBlocker(CoreWebView2 core)
        {
            // Register filters for resource types that serve ads - NOT All, which would
            // intercept upload streams and add IPC overhead on every data chunk
            var adResourceTypes = new[]
            {
                CoreWebView2WebResourceContext.Document,       // top-level HTML (for YouTube watch strip)
                CoreWebView2WebResourceContext.Script,
                CoreWebView2WebResourceContext.Image,
                CoreWebView2WebResourceContext.Stylesheet,
                CoreWebView2WebResourceContext.XmlHttpRequest,  // covers XHR, Fetch, EventSource
                CoreWebView2WebResourceContext.Media,
                CoreWebView2WebResourceContext.Font,
            };
            foreach (var resourceType in adResourceTypes)
                core.AddWebResourceRequestedFilter("*://*", resourceType);
            core.WebResourceRequested += (_, args) =>
            {
                try
                {
                    var uri = new Uri(args.Request.Uri);
                    var host = uri.Host.ToLower();

                    // KILL THE PRE-ROLL AT THE NETWORK LAYER. Intercept the /youtubei/v1/player
                    // response, fetch it ourselves, delete the ad payloads (adPlacements /
                    // playerAds / adSlots / ...), and return the cleaned JSON to the page. This
                    // is the C# equivalent of the extension's JSON strip but done server-side to
                    // the page, so it needs no early main-world injection (CDP black-screens the
                    // player in this SDK) and cannot be defeated by injection timing. streamingData
                    // is preserved, so the video plays - just with no ad to play first.
                    bool ytHost = HostIsOrEndsWith(host, "youtube.com") || HostIsOrEndsWith(host, "youtube-nocookie.com");
                    if (ytHost)
                    {
                        bool isPlayerApi = uri.AbsolutePath.IndexOf("/youtubei/v1/player", StringComparison.OrdinalIgnoreCase) >= 0;
                        // The FIRST video's ad data is inline in the /watch HTML document (no
                        // separate player fetch), so strip the document too. args.ResourceContext
                        // == Document identifies the top-level navigation.
                        bool isWatchDoc = args.ResourceContext == CoreWebView2WebResourceContext.Document
                            && uri.AbsolutePath.StartsWith("/watch", StringComparison.OrdinalIgnoreCase);
                        if (isPlayerApi || isWatchDoc)
                        {
                            var deferral = args.GetDeferral();
                            _ = Task.Run(async () =>
                            {
                                CoreWebView2WebResourceResponse? cleaned = null;
                                try
                                {
                                    cleaned = isWatchDoc
                                        ? await BuildStrippedWatchDocumentAsync(core, args.Request)
                                        : await BuildStrippedPlayerResponseAsync(core, args.Request);
                                }
                                catch { }
                                try
                                {
                                    if (cleaned != null)
                                    {
                                        args.Response = cleaned;
                                        try { Logger.Log("ADBLOCK", isWatchDoc ? "YT watch document stripped" : "YT player response stripped"); } catch { }
                                    }
                                }
                                catch { }
                                finally { try { deferral.Complete(); } catch { } }
                            });
                            return;
                        }
                    }

                    // YouTube ad blocking, network layer - mirrors the GSecurity Ad Shield
                    // extension's declarativeNetRequest rules (allow @ priority 100, block @
                    // priority 1). This is what removes the pre-roll WITHOUT breaking playback:
                    // the player's own media/data endpoints are explicitly allowed, only the ad
                    // endpoints are blocked. Evaluated for requests on ANY page (covers embeds).
                    //   ALLOW (never block): the playback + data machinery.
                    //   BLOCK: the YouTube/Google ad endpoints.
                    var ytBlock = ClassifyYouTubeRequest(host, uri.AbsolutePath);
                    if (ytBlock == YtRule.Allow) return;
                    if (ytBlock == YtRule.Block)
                    {
                        args.Response = core.Environment.CreateWebResourceResponse(null, 403, "Blocked", "");
                        adsBlockedCount++;
                        try { Logger.Log("ADBLOCK", $"YT block ({args.ResourceContext}) {args.Request.Uri}"); } catch { }
                        return;
                    }

                    // On a top-level YouTube page, do not fall through to the generic blocklist
                    // (it would 403 ad-init requests the player waits on and stall playback).
                    // YouTube ads are handled by the allow/block classifier above.
                    try
                    {
                        var topHost = new Uri(core.Source ?? "").Host.ToLower();
                        if (topHost == "www.youtube.com" || topHost == "youtube.com" ||
                            topHost == "m.youtube.com" || topHost == "music.youtube.com" ||
                            topHost.EndsWith(".youtube.com"))
                            return;
                    }
                    catch { }

                    // Skip whitelisted *request* hosts (Discord CDN, Google accounts, etc.)
                    if (IsAdBlockWhitelisted(host)) return;
                    // Same-site (first-party) requests are never blocked
                    try
                    {
                        var pageHost = new Uri(core.Source ?? "").Host.ToLower();
                        if (SameSite(host, pageHost)) return;
                    }
                    catch { }

                    // Do NOT skip blocking just because the *top-level* page is whitelisted
                    // (Discord, etc.). Embedded YouTube iframes still need ad requests cancelled.
                    // First-party SameSite + request-host whitelist above protect the host site.

                    // Check if the host or any parent domain is in the block list
                    var checkHost = host;
                    while (checkHost.Contains('.'))
                    {
                        if (BlockedAdDomains.Contains(checkHost))
                        {
                            args.Response = core.Environment.CreateWebResourceResponse(null, 403, "Blocked", "");
                            adsBlockedCount++;
                            try { Logger.Log("ADBLOCK", $"Blocked ({args.ResourceContext}) {args.Request.Uri}"); } catch { }
                            return;
                        }
                        int dot = checkHost.IndexOf('.');
                        checkHost = checkHost.Substring(dot + 1);
                    }
                }
                catch { }
            };

            // YouTube ads live in ytInitialData / player JSON and must be stripped in the
            // MAIN world before page scripts run. Isolated-world <script> tags are blocked
            // by YouTube CSP, which is why ads came back after 0.6.8.
            //
            // The main-world script is installed ONCE, unconditionally, at tab setup via
            // Page.addScriptToEvaluateOnNewDocument. It is self-guarded: YouTubeMainWorldCode
            // bails immediately on any non-YouTube host and on auth/OAuth pages, so registering
            // it globally never tags Cloudflare forums as a bot. Installing it here (instead of
            // lazily on a cancellable top-level NavigationStarting) means it runs before page
            // scripts on EVERY document - including SPA soft-navigations (clicking a related
            // video), back/forward, and renderer recovery - so ad-blocking no longer depends on
            // the direction the user arrived at the video from.
            //
            // This is AWAITED (callers await SetupAdBlocker before the tab navigates) so the
            // CDP registration is in place BEFORE the first document loads. Previously this was
            // fire-and-forget, which lost a race when a YouTube video was opened directly (e.g.
            // clicked from a search-engine result into a new tab): the first document's
            // ytInitialData/ytInitialPlayerResponse loaded with ads intact because the JSON
            // stripper had not registered yet - hence "ads until you refresh".
            await InstallYouTubeMainWorld(core);

            // Suppress Google One Tap / FedCM prompts in the MAIN world. The isolated-world
            // AddScriptToExecuteOnDocumentCreated registration cannot see the page's real
            // navigator.credentials, so - like the YouTube stripper - this must go through
            // Page.addScriptToEvaluateOnNewDocument to actually patch the page context.
            // This is what stops the address-bar blink storm on pages that show the
            // "Continue with google.com" prompt (e.g. The Guardian). Passkeys are untouched.
            await InstallFedCmSuppressor(core);

            // Intercept custom-scheme (discord:// etc.) launches in the MAIN world. WebView2's
            // LaunchingExternalUriScheme event does not fire for iframe/anchor-initiated launches
            // in this SDK, and an isolated-world script cannot patch the page's window.open /
            // location / iframe.src. So this must run in the main world via CDP, exactly like the
            // FedCM suppressor above. It reports catches through a CustomEvent that the isolated
            // ExternalSchemeBridgeJs relays to the host.
            await InstallExternalSchemeInterceptor(core);

            // Inject fetch/XHR blocker into main world via DevTools Protocol
            core.NavigationCompleted += (_, _) => InjectMainWorldBlocker(core);
        }

        // Install the external-scheme interceptor into the main world, once per CoreWebView2,
        // via Page.addScriptToEvaluateOnNewDocument so it runs before page scripts on every
        // document and in every frame.
        private static async Task InstallExternalSchemeInterceptor(CoreWebView2 core)
        {
            try
            {
                try { await core.CallDevToolsProtocolMethodAsync("Page.enable", "{}"); } catch { }
                string escapedJs = ExternalSchemeMainWorldJs.Replace("\\", "\\\\").Replace("\"", "\\\"");
                string cdpParams = "{\"source\":\"" + escapedJs + "\"}";
                await core.CallDevToolsProtocolMethodAsync("Page.addScriptToEvaluateOnNewDocument", cdpParams);
            }
            catch { }
        }

        // Install the FedCM / One Tap suppressor into the main world, once per CoreWebView2,
        // via Page.addScriptToEvaluateOnNewDocument so it runs before page scripts on every
        // document and in every frame. Only the FedCM ({identity:...}) credential path is
        // neutralised; passkeys ({publicKey:...}) and classic Credential Management pass
        // through. Awaited so the patch is in place before the first document loads.
        private static async Task InstallFedCmSuppressor(CoreWebView2 core)
        {
            try
            {
                try { await core.CallDevToolsProtocolMethodAsync("Page.enable", "{}"); } catch { }
                string escapedJs = FedCmSuppressJs.Replace("\\", "\\\\").Replace("\"", "\\\"");
                string cdpParams = "{\"source\":\"" + escapedJs + "\"}";
                await core.CallDevToolsProtocolMethodAsync("Page.addScriptToEvaluateOnNewDocument", cdpParams);
            }
            catch { }
        }

        // Install the main-world YouTube ad blocker once per CoreWebView2, independent of
        // navigation. Page.addScriptToEvaluateOnNewDocument runs the script in the main world
        // before any page script on every subsequent document - top-level loads, SPA
        // soft-navigations, and back/forward alike. The script self-guards on hostname, so it
        // is inert everywhere except YouTube. Falls back to AddScriptToExecuteOnDocumentCreated
        // (isolated-world wrapper) if CDP is unavailable.
        private static async Task InstallYouTubeMainWorld(CoreWebView2 core)
        {
            // The pre-roll is now stripped server-side by the local YtProxy (it deletes
            // adPlacements/playerAds from the /watch document and /youtubei/v1/player responses
            // before they reach the page). So we do NOT do CDP main-world injection here - that
            // black-screens the player in WebView2 and is no longer needed. We keep only the
            // cosmetic feed scrubber for iframes/embeds (safe, no player tampering).
            try { _ = core.AddScriptToExecuteOnDocumentCreatedAsync(BuildYouTubeEmbedScrubberJs()); } catch { }
            await Task.CompletedTask;
        }

        private static string BuildYouTubeEmbedScrubberJs()
        {
            // Injected into every youtube*/youtube-nocookie* iframe. COSMETIC-ONLY: it injects
            // just the DOM scrubber (YouTubeAdBlockerJs), which hides feed/sidebar ad renderers.
            // It deliberately does NOT install a fetch or JSON.parse interceptor - rewriting the
            // /youtubei/v1/player response corrupted streamingData and tampering with the player
            // data tripped YouTube's anti-adblock enforcement, both of which black-screened the
            // (embedded) video. Ads may play inside embedded YouTube videos; playback works.
            string domScrubber = JsonSerializer.Serialize(YouTubeAdBlockerJs);

            return
                "(function(){" +
                "var h=(location.hostname||'').toLowerCase();" +
                "if(h.indexOf('youtube')===-1)return;" +
                // DOM scrubber injected as a <script> tag into the main world.
                "if(window.__ceprkacYtScrub)return;window.__ceprkacYtScrub=true;" +
                "var s=document.createElement('script');" +
                "s.textContent=" + domScrubber + ";" +
                "(document.documentElement||document.head||document.body).appendChild(s);s.remove();" +
                "})()";
        }

        private const string AdElementHiderJs = @"(function() {
            if (window.__ceprkacAdHider) return;
            window.__ceprkacAdHider = true;
            /* XenForo / Discourse: generic ad CSS and <article> scrapers blank the whole page. */
            var root = document.documentElement;
            if (root && (root.id === 'XF' || root.getAttribute('data-app') === 'public'
                || document.querySelector('.p-pageWrapper, [data-xf-init], #d-splash, .d-header')))
                return;

            var host = (location.hostname || '').toLowerCase();

            /* CSS-based hiding - catches ads before JS runs */
            var css = document.createElement('style');
            css.textContent = [
                'ins.adsbygoogle','[id*=""google_ads""]','[class*=""ad-slot""]','[class*=""advert""]',
                '[class*=""ad-banner""]','[class*=""ad-container""]','[class*=""ad-wrapper""]',
                '[class*=""ad-placement""]',
                '[data-adunit]','[data-ad-slot]','[data-google-query-id]',
                '.sponsored-content','.ad-banner','.ad-container','.ad-wrapper',
                '.native-ad','.ad-unit','.ad-zone','.ad-area','.ad-block','.ad-box','.ad-frame',
                '.ad-header','.ad-footer','.ad-leaderboard','.ad-sidebar','.ad-skyscraper',
                '.ad-rectangle','.ad-interstitial','.ad-overlay','.ad-popup','.ad-modal',
                'div[id*=""taboola""]','div[id*=""outbrain""]','div[class*=""taboola""]',
                'div[class*=""outbrain""]','div[id*=""zergnet""]','div[id*=""revcontent""]',
                'div[id*=""mgid""]','div[class*=""mgid""]',
                'iframe[src*=""doubleclick""]','iframe[src*=""googlesyndication""]',
                'iframe[src*=""googletagmanager""]','iframe[id*=""google_ads""]','iframe[id*=""aswift""]',
                'iframe[src*=""ad""][width]','iframe[data-ad]',
                '.video-ad-overlay','.preroll-ad','.midroll-ad',
                'a[href*=""doubleclick.net""]','a[href*=""googleadservices""]',
                'div[aria-label=""Advertisement""]','div[aria-label=""advertisement""]',
                'section[aria-label=""Sponsored""]',
                /* Pornhub / adult site ads */
                '.adBanner','.ad-banner','#hd-rightColAd','#pb_ad','.advertisement',
                '.mgbox','[class*=""mgbox""]','div[id*=""snigelAdStack""]',
                '.trafficStars','[class*=""trafficStars""]','[id*=""trafficStars""]',
                '[class*=""exoclick""]','[id*=""exoclick""]',
                'iframe[src*=""trafficstars""]','iframe[src*=""exoclick""]',
                'iframe[src*=""trafficjunky""]','iframe[src*=""adsrv""]',
                'iframe[src*=""juicyads""]','iframe[src*=""exosrv""]',
                'iframe[src*=""tsyndicate""]','iframe[src*=""realsrv""]',
                'div[class*=""abovePlayer""]',
                /* DuckDuckGo sponsored results and self-promo */
                '.result--ad','.is-ad','[data-testid=""ad""]','[data-testid=""result--ad""]',
                '.badge--ad','.result__extras__url--ad',
                '.ddg-extension-hide','.js-sidebar-ads','.sidebar-modules--ads',
                '.header-aside',
                /* Google sponsored results */
                '#tads','#tadsb','#bottomads','.commercial-unit-desktop-top',
                '.commercial-unit-desktop-rhs','.cu-container',
                'div[data-text-ad]','div[data-hveid] .uEierd',
                /* Bing sponsored results */
                '.b_ad','.b_adSlug','li.b_ad','#b_results > .b_ad',
                /* Yahoo sponsored results */
                '.searchCenterTopAds','.searchCenterBottomAds','.compDlink',
                /* Reddit promoted posts (GSecurity Ad Shield) */
                'shreddit-ad-post','[data-testid=""ad-post""]','[data-testid=""promoted-post""]',
                'div[data-promoted=""true""]','.promotedlink','.sponsorshipbox','.sponsor-logo',
                'faceplate-tracker[source=""ad""]','faceplate-tracker[noun=""ad""]',
                '[data-testid=""sidebar-ad""]','[data-testid=""subreddit-sidebar-ad""]',
                '.sidebar-ad','div[class*=""promotedlink""]','.premium-banner-outer',
                '[data-testid=""premium-upsell""]',
                'shreddit-experience-tree[bundlename*=""ad""]','shreddit-experience-tree[bundlename*=""Ad""]',
                '.thing.promoted','.thing.stickied.promotedlink',
                /* LinkedIn ads */
                '[data-ad-banner-id]','[data-is-sponsored=""true""]',
                '.ad-banner-container','.ads-container',
                /* Twitch ads */
                '[data-a-target=""video-ad-label""]','.video-ad','.advertisement-banner',
                '[data-test-selector=""ad-banner-default-id""]','.stream-display-ad',
                /* TikTok ads */
                '[class*=""DivAdBanner""]','[data-e2e=""ad""]'
            ].join(',') + '{display:none!important;height:0!important;min-height:0!important;overflow:hidden!important}';
            (document.head || document.documentElement).appendChild(css);

            /* DOM removal selectors */
            var sels = [
                'ins.adsbygoogle','iframe[src*=""doubleclick""]','iframe[src*=""googlesyndication""]',
                'iframe[src*=""googletagmanager""]','iframe[id*=""google_ads""]','iframe[id*=""aswift""]',
                'iframe[src*=""ad""][width]','iframe[data-ad]',
                '[id*=""google_ads""]','[class*=""ad-slot""]','[class*=""advert""]','[class*=""ad-banner""]',
                '[class*=""ad-container""]','[class*=""ad-wrapper""]',
                '[class*=""ad-placement""]',
                '[data-adunit]','[data-ad-slot]','[data-google-query-id]',
                '.sponsored-content','.ad-banner','.ad-container','.ad-wrapper',
                '.native-ad','.ad-unit','.ad-zone','.ad-area','.ad-block','.ad-box','.ad-frame',
                '.ad-header','.ad-footer','.ad-leaderboard','.ad-sidebar','.ad-skyscraper',
                '.ad-rectangle','.ad-interstitial','.ad-overlay','.ad-popup','.ad-modal',
                'div[id*=""taboola""]','div[id*=""outbrain""]','div[class*=""taboola""]',
                'div[class*=""outbrain""]','div[id*=""zergnet""]','div[id*=""revcontent""]',
                'div[id*=""mgid""]','div[class*=""mgid""]',
                '.video-ad-overlay','.preroll-ad','.midroll-ad',
                'div[aria-label=""Advertisement""]','div[aria-label=""advertisement""]',
                /* Search engine sponsored results */
                '.result--ad','.is-ad','[data-testid=""ad""]','[data-testid=""result--ad""]',
                '.badge--ad','.ddg-extension-hide','.js-sidebar-ads','.header-aside',
                '#tads','#tadsb','#bottomads','.commercial-unit-desktop-top',
                '.commercial-unit-desktop-rhs','div[data-text-ad]',
                '.b_ad','.b_adSlug','li.b_ad',
                '.searchCenterTopAds','.searchCenterBottomAds',
                /* Reddit (GSecurity Ad Shield) */
                'shreddit-ad-post','[data-testid=""ad-post""]','[data-testid=""promoted-post""]',
                'div[data-promoted=""true""]','.promotedlink','.sponsorshipbox','.sponsor-logo',
                '#ad-frame','#ad_main',
                'faceplate-tracker[source=""ad""]','faceplate-tracker[noun=""ad""]',
                '[data-testid=""sidebar-ad""]','[data-testid=""subreddit-sidebar-ad""]',
                'shreddit-experience-tree[bundlename*=""ad""]','shreddit-experience-tree[bundlename*=""Ad""]',
                '.premium-banner-outer','[data-testid=""premium-upsell""]',
                /* LinkedIn */
                '[data-ad-banner-id]','[data-is-sponsored=""true""]',
                '.ad-banner-container','.ads-container',
                /* Twitch */
                '[data-a-target=""video-ad-label""]','.video-ad','.advertisement-banner',
                '[data-test-selector=""ad-banner-default-id""]','.stream-display-ad',
                /* TikTok */
                '[class*=""DivAdBanner""]','[data-e2e=""ad""]'
            ];
            function scrub() {
                for (var i = 0; i < sels.length; i++) {
                    try {
                        var els = document.querySelectorAll(sels[i]);
                        for (var j = 0; j < els.length; j++) {
                            if (els[j] && els[j].parentElement) els[j].remove();
                        }
                    } catch(e) {}
                }
                /* Reddit / Facebook / X / Instagram only - XenForo posts are <article> */
                if (/(^|\.)reddit\.com$|(^|\.)redditmedia\.com$/.test(host)) {
                    try {
                        document.querySelectorAll('article, [data-testid=""post-container""], .thing').forEach(function(post) {
                            var badges = post.querySelectorAll('span, faceplate-tracker, [slot=""credit-bar""], .tagline');
                            for (var k = 0; k < badges.length; k++) {
                                var text = (badges[k].textContent || '').trim().toLowerCase();
                                if (text === 'promoted' || text === 'sponsored') { post.remove(); break; }
                            }
                        });
                        document.querySelectorAll('shreddit-post').forEach(function(post) {
                            if (post.hasAttribute('is-promoted') || post.getAttribute('post-type') === 'promoted') post.remove();
                        });
                    } catch(e) {}
                }
                if (/(^|\.)facebook\.com$|(^|\.)fb\.com$/.test(host)) {
                    try {
                        document.querySelectorAll('div[role=""article""], div[role=""feed""] > div').forEach(function(article) {
                            var spans = article.querySelectorAll('span');
                            for (var k = 0; k < spans.length; k++) {
                                if ((spans[k].textContent || '').trim().toLowerCase() === 'sponsored') {
                                    article.style.display = 'none'; break;
                                }
                            }
                        });
                    } catch(e) {}
                }
                if (/(^|\.)twitter\.com$|(^|\.)x\.com$/.test(host)) {
                    try {
                        document.querySelectorAll('article, [data-testid=""placementTracking""]').forEach(function(el) {
                            var text = (el.textContent || '').toLowerCase();
                            if (/\bpromoted\b/.test(text) || /\bad\s*-/.test(text) || el.matches('[data-testid=""placementTracking""]')) {
                                el.style.display = 'none';
                            }
                        });
                    } catch(e) {}
                }
                if (/(^|\.)instagram\.com$/.test(host)) {
                    try {
                        document.querySelectorAll('article').forEach(function(a) {
                            if (/\bsponsored\b/i.test(a.textContent || '')) a.style.display = 'none';
                        });
                        document.querySelectorAll('[data-testid=""reel-ad""]').forEach(function(el) { el.remove(); });
                    } catch(e) {}
                }
            }
            scrub();
            setInterval(scrub, 1500);
            new MutationObserver(scrub).observe(document.documentElement, {childList:true, subtree:true});
        })()";

        private const string YouTubeAdBlockerJs = @"(function() {
            if (window.__ceprkacYtAdBlock) return;
            window.__ceprkacYtAdBlock = true;
            var s = document.createElement('style');
            /* FEED-ONLY CSS. Deliberately excludes ALL player-surface selectors
               (.video-ads, .ytp-ad-module, .ytp-ad-player-overlay, #player-ads,
               .ad-showing .ytp-ad-module, etc.) - those hid the actual <video>/player
               and black-screened playback. We hide only feed/homepage/sidebar/search ad
               *renderers*, mirroring the extension's injectCollapseCss (which is feed-only). */
            s.textContent = 'ytd-display-ad-renderer,ytd-ad-slot-renderer,ytd-promoted-video-renderer,ytd-promoted-sparkles-web-renderer,ytd-promoted-sparkles-text-search-renderer,ytd-banner-promo-renderer,ytd-statement-banner-renderer,ytd-in-feed-ad-layout-renderer,ytd-masthead-ad-renderer,ytd-primetime-promo-renderer,ytd-compact-promoted-video-renderer,ytd-action-companion-ad-renderer,ytd-mealbar-promo-renderer,ytd-search-pyv-renderer,ytd-movie-offer-module-renderer,ytd-enforcement-message-view-model,#masthead-ad,ytd-rich-item-renderer:has(ytd-ad-slot-renderer),ytd-rich-item-renderer:has(ytd-display-ad-renderer),ytd-rich-item-renderer:has(ytd-promoted-video-renderer),ytd-rich-item-renderer:has(ytd-promoted-sparkles-web-renderer),ytd-rich-item-renderer:has(ytd-in-feed-ad-layout-renderer),ytd-rich-item-renderer:has(ytd-banner-promo-renderer),ytd-rich-section-renderer:has(ytd-ad-slot-renderer){display:none!important}';
            (document.head||document.documentElement).appendChild(s);
            /* COSMETIC-ONLY. We intentionally do NOT tamper with the YouTube player:
               no JSON.parse override, no ytInitialPlayerResponse/ytInitialData/ytcfg
               defineProperty hooks, no fetch rewriting of /youtubei/v1/player, and no
               clicking of in-player skip buttons. All of those trip YouTube's anti-adblock
               enforcement (the 'Ad blockers are not allowed on YouTube' screen) and/or
               corrupt streamingData, which black-screened the video. This script now only
               hides feed / homepage / sidebar / search ad *renderers* via CSS + DOM removal,
               which does not touch the media pipeline and does not trigger enforcement. */
            var adS=['ytd-display-ad-renderer','ytd-ad-slot-renderer','ytd-promoted-video-renderer','ytd-promoted-sparkles-web-renderer','ytd-banner-promo-renderer','ytd-in-feed-ad-layout-renderer','ytd-mealbar-promo-renderer','ytd-search-pyv-renderer','ytd-movie-offer-module-renderer','ytd-compact-promoted-video-renderer','ytd-action-companion-ad-renderer','ytd-primetime-promo-renderer','ytd-masthead-ad-renderer','#masthead-ad'];
            /* Localized sponsored/ad badge words - covers major YouTube UI languages */
            var sponsorWords=['sponsored','sponzorirano','gesponsert','sponsoris','patrocinado','sponsorizzato','gesponsord','','','','','reklam','promowane','sponzorovan','szponzorlt','annonce','reklama','hirdets','','commandit','gesponsord','publicidad','pubblicit','anncio','reklame','sponzorovno','sponzorovan','sponzorirane',''];
            function isSponsoredText(t){t=t.trim().toLowerCase();for(var i=0;i<sponsorWords.length;i++){if(t===sponsorWords[i])return true;}return false;}
            function scrub(){for(var i=0;i<adS.length;i++)document.querySelectorAll(adS[i]).forEach(function(e){var p=e.closest('ytd-rich-item-renderer,ytd-rich-section-renderer,ytd-reel-shelf-renderer');if(p)p.remove();else e.remove();});/* Walk homepage rich grid items and remove sponsored cards by badge text */try{document.querySelectorAll('ytd-rich-item-renderer,ytd-rich-section-renderer').forEach(function(item){if(item.querySelector('ytd-ad-slot-renderer,ytd-display-ad-renderer,ytd-promoted-video-renderer,ytd-promoted-sparkles-web-renderer,ytd-in-feed-ad-layout-renderer')){item.remove();return;}var badges=item.querySelectorAll('span.ytd-badge-supported-renderer,ytd-badge-supported-renderer span,div.ytd-badge-supported-renderer,ytd-badge-supported-renderer,[class*=""badge""],.badge,.badge-style-type-ad,span[aria-label]');for(var k=0;k<badges.length;k++){if(isSponsoredText(badges[k].textContent||'')){item.remove();return;}}/* Check inline-block ad metadata text */var metas=item.querySelectorAll('#metadata-line span,#byline-container span,yt-formatted-string.ytd-channel-name');for(var m=0;m<metas.length;m++){if(isSponsoredText(metas[m].textContent||'')){item.remove();return;}}});}catch(e){}/* Walk search results for promoted items */try{document.querySelectorAll('ytd-video-renderer,ytd-compact-video-renderer').forEach(function(item){var badges=item.querySelectorAll('span.ytd-badge-supported-renderer,ytd-badge-supported-renderer span,[class*=""badge""]');for(var k=0;k<badges.length;k++){if(isSponsoredText(badges[k].textContent||'')){item.remove();return;}}});}catch(e){}dismissWall();}
            /* Anti-adblock wall: since we strip ad payloads, YouTube shows an enforcement
               dialog (""Ad blockers are not allowed on YouTube"") and locks page scroll. Port
               of the GSecurity extension's dismissAdblockWall + unlockPageScroll: close/remove
               the enforcement view, remove adblock-wall dialogs, drop the backdrop, and restore
               overflow so the player is usable. */
            function wallText(t){t=String(t||'').toLowerCase();return t.indexOf('ad blocker')>=0||t.indexOf('adblock')>=0||t.indexOf('blokiranje oglasa')>=0||t.indexOf('programi za blokiranje')>=0||t.indexOf('allow ads')>=0||t.indexOf('dopusti ogla')>=0||t.indexOf('nisu dopu')>=0;}
            function unlockScroll(){try{var ns=[document.documentElement,document.body,document.querySelector('ytd-app'),document.querySelector('#content'),document.querySelector('ytd-watch-flexy')];for(var i=0;i<ns.length;i++){var n=ns[i];if(!n||!n.style)continue;if(n.style.overflow==='hidden')n.style.overflow='';if(n.style.overflowY==='hidden')n.style.overflowY='';}}catch(e){}}
            function dismissWall(){try{document.querySelectorAll('ytd-enforcement-message-view-model').forEach(function(el){var c=el.querySelector(""button[aria-label*='Close'],button[aria-label*='Zatvori'],.yt-spec-button-shape-next--icon-only,#dismiss-button"");if(c&&c.click)c.click();el.remove();});document.querySelectorAll('tp-yt-paper-dialog,ytd-modal-with-title-and-button-renderer').forEach(function(d){if(!wallText(d.textContent))return;var c=d.querySelector(""#close-button,#dismiss-button,button[aria-label*='Close'],button[aria-label*='Zatvori'],.yt-spec-button-shape-next--icon-only"");if(c&&c.click)c.click();d.remove();});document.querySelectorAll('tp-yt-iron-overlay-backdrop.opened').forEach(function(b){b.remove();});unlockScroll();}catch(e){}}
            scrub();setInterval(scrub,500);new MutationObserver(scrub).observe(document.documentElement,{childList:true,subtree:true});
        })()";

        // Main-world YouTube ad blocker - built at runtime to handle nested quotes cleanly
        private static readonly string YouTubeMainWorldCode = BuildYouTubeMainWorldCode();
        // Hostname-guarded <script> injector for AddScriptToExecuteOnDocumentCreatedAsync
        private static readonly string YouTubeMainWorldInjectorJs = BuildYouTubeInjector();

        private static string BuildYouTubeMainWorldCode()
        {
            // YouTube in-player ad stripping, mirroring the proven GSecurity Ad Shield
            // Chrome extension exactly. Two rules make this work WITHOUT breaking playback
            // or tripping YouTube's anti-adblock enforcement:
            //   1. Hook ONLY JSON.parse + guard the ytInitialPlayerResponse/ytInitialData/
            //      ytcfg globals, deleting just the 15 core ad keys. No array splicing, no
            //      /^(ad|promoted)/ heuristic - a plain key-delete + recurse.
            //   2. NEVER override window.fetch on YouTube. Rebuilding the /youtubei/v1/player
            //      response (JSON round-trip + reused headers) corrupted streamingData and
            //      black-screened the video; the extension deliberately does fetch hooking
            //      only on non-YouTube hosts. Deleting ad keys off the already-parsed object
            //      via the JSON.parse hook removes pre-rolls without touching the response
            //      bytes the media pipeline needs.
            return
                "(function(){" +
                "var h=location.hostname.toLowerCase();" +
                "if(h.indexOf('youtube.com')===-1&&h.indexOf('youtube-nocookie.com')===-1)return;" +
                // Bail on auth/OAuth pages that might live on a youtube subdomain.
                "if(/accounts\\.google|login\\.microsoft|appleid\\.apple|auth0\\.com|clerk\\.|oauth/.test(h))return;" +
                "if(window.__ceprkacYtMain)return;window.__ceprkacYtMain=true;" +
                "var adKeys=['adPlacements','adSlots','playerAds','adBreakHeartbeatParams','ad3Module'," +
                "'adSafetyReason','adLoggingData','showAdSlots','adBreakParams','adBreakStatus'," +
                "'adVideoId','adLayoutLoggingData','instreamAdPlayerOverlayRenderer'," +
                "'adPlacementConfig','adVideoStitcherConfig'];" +
                // Plain delete-key + recurse. No splicing. depth cap 12.
                "function strip(o,d){if(!o||typeof o!=='object'||d>12)return o;" +
                "for(var i=0;i<adKeys.length;i++)if(Object.prototype.hasOwnProperty.call(o,adKeys[i]))delete o[adKeys[i]];" +
                "var k=Object.keys(o);for(var j=0;j<k.length;j++){if(o[k[j]]&&typeof o[k[j]]==='object')strip(o[k[j]],d+1);}return o;}" +
                // JSON.parse hook only (NO fetch override on YouTube).
                "var op=JSON.parse;JSON.parse=function(){var r=op.apply(this,arguments);" +
                "try{if(r&&typeof r==='object')strip(r,0);}catch(e){}return r;};" +
                // Guard the YouTube globals - strip on assignment.
                "function guard(p){var v=window[p];try{Object.defineProperty(window,p,{configurable:true," +
                "get:function(){return v;},set:function(n){if(n&&typeof n==='object')strip(n,0);v=n;}});" +
                "if(v)window[p]=v;}catch(e){}}" +
                "guard('ytInitialPlayerResponse');guard('ytInitialData');guard('ytcfg');" +
                "})()";
        }

        // Fallback injector - wraps the main world code in a <script> tag for AddScriptToExecuteOnDocumentCreatedAsync
        private static string BuildYouTubeInjector()
        {
            string escaped = YouTubeMainWorldCode.Replace("\\", "\\\\").Replace("'", "\\'");
            // indexOf('youtube') also matches youtube-nocookie.com embeds.
            return "(function(){if(location.hostname.toLowerCase().indexOf('youtube')===-1)return;" +
                   "var sc=document.createElement('script');" +
                   "sc.textContent='" + escaped + "';" +
                   "(document.head||document.documentElement).appendChild(sc);sc.remove();})()";
        }

        private async Task LoadOrUpdateBlocklistAsync()
        {
            // Load bundled blocklist from app directory
            var bundledList = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blocklist.txt");
            if (File.Exists(bundledList))
            {
                int count = 0;
                foreach (var line in File.ReadAllLines(bundledList))
                {
                    var domain = line.Trim();
                    if (!string.IsNullOrEmpty(domain) && !domain.StartsWith("#") && domain.Contains('.'))
                    {
                        BlockedAdDomains.Add(domain);
                        count++;
                    }
                }
                statusLabel.Text = $"Ad blocker: {BlockedAdDomains.Count} domains loaded.";
            }

            // Also try to load/update from appdata (user can drop a custom blocklist.txt there)
            var userList = Path.Combine(appDataFolder, "blocklist.txt");
            if (File.Exists(userList))
            {
                foreach (var line in File.ReadAllLines(userList))
                {
                    var domain = line.Trim();
                    if (!string.IsNullOrEmpty(domain) && !domain.StartsWith("#") && domain.Contains('.'))
                        BlockedAdDomains.Add(domain);
                }
            }
            await Task.CompletedTask;
        }

        private static bool IsChallengePage(CoreWebView2 core)
        {
            try
            {
                var src = (core.Source ?? "").ToLowerInvariant();
                if (src.Contains("cdn-cgi/") || src.Contains("__cf_chl") || src.Contains("challenges.cloudflare"))
                    return true;
                var title = (core.DocumentTitle ?? "").ToLowerInvariant();
                if (title.Contains("just a moment") || title.Contains("attention required") ||
                    title.Contains("checking your browser") || title.Contains("please wait"))
                    return true;
            }
            catch { }
            return false;
        }

        private async void InjectMainWorldBlocker(CoreWebView2 core)
        {
            if (BlockedAdDomains.Count == 0) return;
            if (IsChallengePage(core)) return;
            // Skip YouTube - it gets its own dedicated main-world injection
            try
            {
                var pageHost = new Uri(core.Source ?? "").Host.ToLower();
                if (pageHost == "www.youtube.com" || pageHost == "youtube.com" ||
                    pageHost == "m.youtube.com" || pageHost == "music.youtube.com" ||
                    pageHost.EndsWith(".youtube.com"))
                    return;
            }
            catch { }
            try
            {
                var xf = await core.ExecuteScriptAsync(
                    "(document.documentElement&&(document.documentElement.id==='XF'||document.documentElement.getAttribute('data-app')==='public'||!!document.querySelector('.p-pageWrapper,[data-xf-init]')))");
                if (xf != null && xf.IndexOf("true", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
            }
            catch { }
            try
            {
                // Build the blocker JS
                var topDomains = BlockedAdDomains
                    .Where(d => !d.Contains('*') && d.Length > 3 && d.Length < 60)
                    .Take(15000)
                    .ToList();

                var sb = new System.Text.StringBuilder();
                sb.Append("(function(){if(window.__cFB)return;window.__cFB=1;var b=new Set([");
                bool first = true;
                foreach (var d in topDomains)
                {
                    if (!first) sb.Append(',');
                    sb.Append('"');
                    sb.Append(d.Replace("\"", "").Replace("\\", ""));
                    sb.Append('"');
                    first = false;
                }
                sb.Append("]);");
                sb.Append("var wl=new Set(['google.com','youtube.com','accounts.google.com','apis.google.com','ssl.gstatic.com','gstatic.com','discord.com','discordapp.com','github.com','paypal.com','ebay.com','apple.com','icloud.com','mediafire.com','login.microsoftonline.com','login.live.com','pay.google.com','gog.com','steampowered.com','steamcommunity.com','epicgames.com','ea.com','origin.com','ubisoft.com','blizzard.com','battle.net','riotgames.com','xbox.com','playstation.com','nintendo.com','twitch.tv','chase.com','bankofamerica.com','wellsfargo.com','citibank.com','capitalone.com','revolut.com','wise.com','stripe.com','n26.com','cloudflare.com','challenges.cloudflare.com']);");
                sb.Append("function isWl(h){while(h){if(wl.has(h))return 1;var i=h.indexOf('.');if(i<0)break;h=h.substr(i+1);}return 0};");
                sb.Append("function chk(u){try{if(isWl(location.hostname))return 0;var l=u.toLowerCase();var h=new URL(l).hostname;if(isWl(h))return 0;while(h){if(b.has(h))return 1;var i=h.indexOf('.');if(i<0)break;h=h.substr(i+1);}");
                sb.Append("if(/(\\/ads?\\/|\\/ad[sx]?\\b|\\/pagead\\/|\\/ptracking|\\/advert|\\/sponsored|\\/promotion|\\/tracking\\/|\\/analytics\\/|\\/collect\\?|\\/beacon|\\/pixel|\\/imp\\?|\\/impression|\\/click\\?|ad_banner|ad_frame|sponsored_content|promo_banner|[?&](ad|ads|adunit|adformat|adtag)=)/i.test(l))return 1;");
                sb.Append("if(/(?:\\/(?:adcontent|img\\/adv|web-ad|iframead|contentad|ad\\/image|video-ad|stats\\/event|xtclicks|adscript|bannerad|googlead|adhandler|adimages|adconfig|tracking\\/track|tracker\\/track|adrequest|nativead|adman|advertisement|adframe|adcontrol|adoverlay|adserver|adsense|google-ads|ad-banner|banner-ad|adplacement|adblockdetect|advertising|admanagement|adprovider|adrotation|adunit|adcall|adlog|adcount|adserve|adsrv|adsys|adtrack|adview|adwidget|adzone|sidebar-ads|footer-ads|top-ads|bottom-ads|ads\\.php|ad\\.js|ad\\.css))/i.test(l))return 1;");
                sb.Append("if(/\\/api\\/stats\\/(ads|atr)/i.test(l))return 1;");
                sb.Append("var hh=new URL(l).hostname;");
                sb.Append("if(/^(?:.*[-_.])?(ads?|adv(ert(s|ising)?)?|banners?|track(er|ing|s)?|beacons?|doubleclick|adservice|adnxs|adtech|googleads|gads|adwords|partner|sponsor(ed)?|click(s|bank|tale|through)?|pop(up|under)s?|promo(tion)?|market(ing|er)?|affiliates?|metrics?|stat(s|counter|istics)?|analytics?|pixels?|campaign|traff(ic|iq)|monetize|syndicat(e|ion)|revenue|yield|impress(ion)?s?|conver(sion|t)?|audience|target(ing)?|behavior|profil(e|ing)|telemetry|survey|outbrain|taboola|quantcast|scorecard|omniture|comscore|krux|bluekai|exelate|adform|adroll|rubicon|vungle|inmobi|flurry|mixpanel|heap|amplitude|optimizely|bizible|pardot|hubspot|marketo|eloqua|media(math|net)|criteo|appnexus|turn|adbrite|admob|adsonar|adscale|zergnet|revcontent|mgid|nativeads|contentad|displayads|bannerflow|adblade|adcolony|chartbeat|newrelic|pingdom|kissmetrics|tradedesk|bidder|auction|rtb|programmatic|interstitial|overlay|trafficjunky|trafficstars|exoclick|juicyads|realsrv|magsrv)\\./i.test(hh))return 1;");
                sb.Append("if(/^(?:adcreative(s)?|imageserv|media(mgr)?|stats|switch|track(2|er)?|view|ads?\\d{0,3}|banners?\\d{0,3}|clicks?\\d{0,3}|count(er)?\\d{0,3}|servedby\\d{0,3}|toolbar\\d{0,3}|pageads\\d{0,3}|pops\\d{0,3}|promos?\\d{0,3})\\./i.test(hh))return 1;");
                sb.Append("if(/(?:\\/(1|blank|b|clear|pixel|transp|spacer)\\.gif|\\.swf)$/i.test(l))return 1;");
                sb.Append("return 0}catch(e){return 0}};");
                sb.Append("var F=fetch;window.fetch=function(a,o){var u=typeof a==='string'?a:a&&a.url?a.url:'';if(chk(u))return Promise.reject(new TypeError('blocked'));return F.apply(this,arguments)};");
                sb.Append("var X=XMLHttpRequest.prototype.open;XMLHttpRequest.prototype.open=function(){var u=arguments[1]||'';if(typeof u==='string'&&chk(u)){this.__blk=1;return}return X.apply(this,arguments)};");
                sb.Append("var S=XMLHttpRequest.prototype.send;XMLHttpRequest.prototype.send=function(){if(this.__blk)return;return S.apply(this,arguments)};");
                sb.Append("})()");

                // Use DevTools Protocol to inject into main world - bypasses CSP
                string escapedJs = sb.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"");
                string cdpParams = "{\"expression\":\"" + escapedJs + "\",\"allowUnsafeEvalBlockedByCSP\":true}";
                await core.CallDevToolsProtocolMethodAsync("Runtime.evaluate", cdpParams);
            }
            catch { }
        }

        private async void InjectAdElementHider(BrowserTab tab)
        {
            try
            {
                var core = tab.WebView.CoreWebView2;
                if (core == null) return;
                if (IsChallengePage(core)) return;
                var url = core.Source ?? "";
                string pageHost = "";
                try { pageHost = new Uri(url).Host.ToLower(); } catch { }

                // YouTube gets its own dedicated ad blocking - DevTools main-world injection
                // handles JSON stripping, and YouTubeAdBlockerJs handles DOM scrubbing
                bool isYouTube = pageHost == "www.youtube.com" || pageHost == "youtube.com" ||
                    pageHost == "m.youtube.com" || pageHost == "music.youtube.com" ||
                    pageHost.EndsWith(".youtube.com") || pageHost.EndsWith(".youtube-nocookie.com");

                if (isYouTube)
                {
                    await core.ExecuteScriptAsync(YouTubeAdBlockerJs);
                    return;
                }

                // Skip generic element hiding on whitelisted sites (non-YouTube)
                if (IsAdBlockWhitelisted(pageHost)) return;

                await core.ExecuteScriptAsync(AdElementHiderJs);
            }
            catch { }
        }

        //  Password Manager 
    }
}
