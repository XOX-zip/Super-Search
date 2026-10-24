namespace SuperSearch;

class Searcher
{
    readonly Config _cfg;
    bool _aiDown;

    public Searcher(Config cfg) => _cfg = cfg;

    public async Task RunAsync(string query, SearchOptions opt)
    {
        Console.WriteLine($"\n> {query}");

        List<SearchResult> raw;
        try
        {
            raw = await BingSearch.SearchAsync(query);
        }
        catch (Exception ex)
        {
            Console.WriteLine("抓取失败: " + ex.Message);
            return;
        }

        if (raw.Count == 0)
        {
            Console.WriteLine("没结果。换个说法，或者等会儿再试（DDG 有时会限流）。");
            return;
        }

        Ranker.Score(raw, query);

        List<SearchResult> shown;
        bool aiUsed = false;

        if (_cfg.HasAi && !opt.NoAi && !_aiDown)
        {
            var pool = raw.OrderByDescending(r => r.Score).Take(Math.Max(opt.Limit * 3, 20)).ToList();
            var picked = await AiClient.FilterAsync(_cfg, query, pool);
            if (picked is { Count: > 0 })
            {
                shown = picked.Take(opt.Limit).ToList();
                aiUsed = true;
            }
            else
            {
                _aiDown = true;
                Console.WriteLine("(AI 没响应，这次和之后都走本地算法；用 /ai 可以重配)");
                shown = LocalTop(raw, opt.Limit);
            }
        }
        else
        {
            shown = LocalTop(raw, opt.Limit);
        }

        int i = 1;
        foreach (var r in shown)
        {
            Console.WriteLine();
            Console.WriteLine($"{i++}. {r.Title}");
            Console.WriteLine($"   {r.Url}");
            if (r.Snippet.Length > 0)
                Console.WriteLine("   " + (r.Snippet.Length > 220 ? r.Snippet[..220] + "..." : r.Snippet));
            if (opt.Verbose) Console.WriteLine($"   [score {r.Score:F1}]");
        }

        if (!aiUsed && !_cfg.HasAi)
            Console.WriteLine("\n(本地算法筛选，没接 AI，相关性排序会糙一些)");
    }

    static List<SearchResult> LocalTop(List<SearchResult> list, int n)
    {
        var pq = new PriorityQueue<SearchResult, double>();
        foreach (var r in list) pq.Enqueue(r, -r.Score);

        var res = new List<SearchResult>();
        while (pq.Count > 0 && res.Count < n) res.Add(pq.Dequeue());
        return res;
    }
}