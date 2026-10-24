using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SuperSearch;

static class AiClient
{
    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };

    public static async Task<bool> PingAsync(Config cfg, CancellationToken ct = default)
    {
        try
        {
            var req = Build(cfg, new object[]
            {
                new { role = "user", content = "hi" }
            }, 5);
            using var res = await Http.SendAsync(req, ct);
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<List<SearchResult>?> FilterAsync(Config cfg, string query, List<SearchResult> pool, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine("搜索词: " + query);
        sb.AppendLine("下面是候选结果。挑出真正相关的，按相关度从高到低排序，最多 10 条。");
        sb.AppendLine("只输出 JSON: {\"order\":[0,3,1]}，数字是下面方括号里的索引。无关的直接不要。");
        sb.AppendLine();

        for (int i = 0; i < pool.Count; i++)
        {
            var r = pool[i];
            sb.AppendLine($"[{i}] {r.Title}");
            sb.AppendLine("    " + r.Url);
            if (r.Snippet.Length > 0)
                sb.AppendLine("    " + (r.Snippet.Length > 150 ? r.Snippet[..150] + "..." : r.Snippet));
        }

        try
        {
            var req = Build(cfg, new object[]
            {
                new { role = "system", content = "你是搜索结果筛选器。只输出 JSON，不要解释。" },
                new { role = "user", content = sb.ToString() }
            }, 600);

            using var res = await Http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";

            using var inner = JsonDocument.Parse(ExtractJson(content));
            var order = inner.RootElement.GetProperty("order");

            var picked = new List<SearchResult>();
            var seen = new HashSet<int>();
            foreach (var el in order.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Number) continue;
                var idx = el.GetInt32();
                if (idx >= 0 && idx < pool.Count && seen.Add(idx))
                    picked.Add(pool[idx]);
            }

            return picked;
        }
        catch
        {
            return null;
        }
    }

    static string ExtractJson(string s)
    {
        var a = s.IndexOf('{');
        var b = s.LastIndexOf('}');
        return a >= 0 && b > a ? s[a..(b + 1)] : s;
    }

    static HttpRequestMessage Build(Config cfg, object[] messages, int maxTokens)
    {
        var url = cfg.AiEndpoint!.TrimEnd('/') + "/chat/completions";
        var json = JsonSerializer.Serialize(new
        {
            model = cfg.AiModel,
            messages,
            max_tokens = maxTokens,
            temperature = 0
        });

        var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cfg.AiKey);
        req.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return req;
    }
}