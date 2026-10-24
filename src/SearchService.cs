namespace SuperSearch;

record SearchOutcome(
    List<SearchResult> Results,
    bool AiUsed,
    bool AiFailed,
    bool AiConfigured,
    string? Notice,
    string? DebugPath);

class SearchService
{
    readonly Config _cfg;
    bool _aiDown;

    public SearchService(Config cfg) => _cfg = cfg;

    public bool AiDown => _aiDown;

    /// <summary>重新配置 AI 之后清掉本次会话的失败标记。</summary>
    public void ResetAiState() => _aiDown = false;

    public async Task<SearchOutcome> SearchAsync(string query, SearchOptions opt, CancellationToken ct)
    {
        var fetch = await BingSearch.SearchAsync(query, ct);
        var raw = fetch.Results;

        if (raw.Count == 0)
        {
            return new SearchOutcome(
                new List<SearchResult>(), false, _aiDown, _cfg.HasAi,
                "没结果。换个说法，或者等会儿再试（Bing 有时会限流）。",
                fetch.DebugPath);
        }

        Ranker.Score(raw, query);

        if (_cfg.HasAi && !opt.NoAi && !_aiDown)
        {
            var pool = raw.OrderByDescending(r => r.Score)
                          .Take(Math.Max(opt.Limit * 3, 20)).ToList();
            var picked = await AiClient.FilterAsync(_cfg, query, pool, ct);
            if (picked is { Count: > 0 })
            {
                return new SearchOutcome(
                    picked.Take(opt.Limit).ToList(), true, false, true, null, null);
            }

            _aiDown = true;
            return new SearchOutcome(
                LocalTop(raw, opt.Limit), false, true, true,
                "AI 没响应，这次和之后都走本地算法；可在设置里重新配置。", null);
        }

        return new SearchOutcome(
            LocalTop(raw, opt.Limit), false, _aiDown, _cfg.HasAi,
            _cfg.HasAi ? null : "未接 AI，本地算法筛选，相关性排序会糙一些。", null);
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
