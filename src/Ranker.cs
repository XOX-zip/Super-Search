namespace SuperSearch;

static class Ranker
{
    static readonly string[] Junk =
    {
        "pinterest.", "quora.", "answers.", "wikihow.", "zhidao.baidu.",
        "wenwen.sogou.", "tieba.baidu.", "douban.com/group"
    };

    static readonly string[] Good =
    {
        "github.com", "stackoverflow.com", "docs.", "developer.mozilla.org",
        "wikipedia.org", "learn.microsoft.com", "arxiv.org", "medium.com"
    };

    public static void Score(List<SearchResult> list, string query)
    {
        var q = query.ToLowerInvariant();
        var terms = q.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var r in list)
        {
            var title = r.Title.ToLowerInvariant();
            var snip = r.Snippet.ToLowerInvariant();
            var url = r.Url.ToLowerInvariant();
            var host = Host(url);
            double s = 0;

            foreach (var t in terms)
            {
                if (title.Contains(t)) s += 3;
                if (snip.Contains(t)) s += 1;
                if (url.Contains(t)) s += 1.5;
            }

            if (title.Contains(q)) s += 4;
            if (snip.Contains(q)) s += 2;

            foreach (var g in Good)
                if (host.Contains(g) || url.Contains(g)) { s += 1.5; break; }
            foreach (var j in Junk)
                if (url.Contains(j)) { s -= 6; break; }

            if (host.EndsWith(".edu") || host.EndsWith(".gov")) s += 2;
            if (r.Snippet.Length < 30) s -= 1;

            r.Score = s;
        }
    }

    static string Host(string url)
    {
        try { return new Uri(url).Host.ToLowerInvariant(); }
        catch { return ""; }
    }
}