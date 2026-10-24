using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace SuperSearch;

record SearchFetch(List<SearchResult> Results, string? DebugPath);

static class BingSearch
{
    static readonly HttpClient Http = CreateClient();

    static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip
                                   | DecompressionMethods.Deflate
                                   | DecompressionMethods.Brotli
        };
        var c = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(20) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
        c.DefaultRequestHeaders.Accept.ParseAdd(
            "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        c.DefaultRequestHeaders.AcceptLanguage.ParseAdd("zh-CN,zh;q=0.9,en;q=0.8");
        return c;
    }

    static readonly Regex BlockRx = new(
        "<li class=\"(b_algo[^\"]*)\"[^>]*>(.*?)</li>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    static readonly Regex H2Rx = new(
        "<h2[^>]*>(.*?)</h2>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    static readonly Regex ARx = new(
        "<a[^>]+href=\"([^\"]+)\"[^>]*>(.*?)</a>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    static readonly Regex PRx = new(
        "<p[^>]*>(.*?)</p>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    static readonly Regex TagRx = new("<[^>]+>", RegexOptions.Compiled);

    public static async Task<SearchFetch> SearchAsync(string query, CancellationToken ct = default)
    {
        var url = "https://www.bing.com/search?q=" + Uri.EscapeDataString(query) + "&mkt=en-US&setlang=en";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Referrer = new Uri("https://cn.bing.com/");

        using var res = await Http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"Bing 返回 HTTP {(int)res.StatusCode}");

        var html = await res.Content.ReadAsStringAsync(ct);

        var results = new List<SearchResult>();
        var blocks = BlockRx.Matches(html);

        foreach (Match block in blocks)
        {
            var cls = block.Groups[1].Value;
            var b = block.Groups[2].Value;
            if (cls.Contains("b_ad")) continue;

            var h2 = H2Rx.Match(b);
            if (!h2.Success) continue;

            var a = ARx.Match(h2.Groups[1].Value);
            if (!a.Success) continue;

            var title = Clean(a.Groups[2].Value);
            if (title.Length == 0) continue;

            var href = WebUtility.HtmlDecode(a.Groups[1].Value);
            if (href.StartsWith("/")) href = "https://cn.bing.com" + href;
            href = UnwrapBingRedirect(href);
            if (href.Length == 0) continue;

            string snippet = "";
            var p = PRx.Match(b);
            if (p.Success) snippet = Clean(p.Groups[1].Value);

            results.Add(new SearchResult(title, href, snippet));
        }

        var debugPath = results.Count == 0 ? SaveDebug(html) : null;
        return new SearchFetch(results, debugPath);
    }

    static string UnwrapBingRedirect(string href)
    {
        if (!href.Contains("bing.com/ck/a")) return href;

        var m = Regex.Match(href, "[?&]u=a1([^&]+)");
        string enc;
        if (m.Success) enc = m.Groups[1].Value;
        else
        {
            var m2 = Regex.Match(href, "[?&]u=([^&]+)");
            if (!m2.Success) return "";
            enc = m2.Groups[1].Value;
        }

        try
        {
            enc = enc.Replace('-', '+').Replace('_', '/');
            var pad = enc.Length % 4 == 0 ? "" : new string('=', 4 - enc.Length % 4);
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(enc + pad));
        }
        catch { return ""; }
    }

    static string? SaveDebug(string html)
    {
        try
        {
            var path = Path.Combine(Path.GetTempPath(), "bing_debug.html");
            File.WriteAllText(path, html);
            return path;
        }
        catch { return null; }
    }

    static string Clean(string s)
    {
        s = TagRx.Replace(s, "");
        s = WebUtility.HtmlDecode(s);
        return Regex.Replace(s, @"\s+", " ").Trim();
    }
}