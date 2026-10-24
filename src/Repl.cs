namespace SuperSearch;

static class Repl
{
    public static async Task RunAsync(Config cfg)
    {
        var searcher = new Searcher(cfg);

        Console.WriteLine();
        Console.WriteLine("SS - Super Search");
        Console.WriteLine("直接输入搜索词回车。-n 数量 / -site 域名 / -noai / -v");
        Console.WriteLine("命令: /ai /config /exit");
        Console.WriteLine(cfg.HasAi ? "模式: 本地筛选 + AI 辅助" : "模式: 仅本地筛选（未接 AI）");
        Console.WriteLine();

        while (true)
        {
            Console.Write("SS> ");
            var line = Console.ReadLine();
            if (line == null) break;
            line = line.Trim();
            if (line.Length == 0) continue;

            if (line is "/exit" or "exit" or "quit") break;

            if (line is "/config")
            {
                Console.WriteLine("  配置文件  " + Config.FilePath);
                Console.WriteLine("  AI        " + (cfg.HasAi ? $"{cfg.AiModel} @ {cfg.AiEndpoint}" : "未配置"));
                Console.WriteLine("  默认条数  " + cfg.DefaultLimit);
                continue;
            }

            if (line is "/ai")
            {
                await Setup.RunAsync(cfg);
                if (!cfg.Save())
                    Console.WriteLine("⚠ 保存失败，本次会话有效，下次要重配。");
                continue;
            }

            var (query, opt) = Parse(line, cfg);
            if (query.Length == 0)
            {
                Console.WriteLine("没看到搜索词。");
                continue;
            }
            if (opt.Site != null) query += " site:" + opt.Site;

            try
            {
                await searcher.RunAsync(query, opt);
            }
            catch (Exception ex)
            {
                Console.WriteLine("出错: " + ex.Message);
            }
            Console.WriteLine();
        }
    }

    static (string, SearchOptions) Parse(string line, Config cfg)
    {
        var opt = new SearchOptions { Limit = cfg.DefaultLimit };
        var words = Split(line);
        var query = new List<string>();

        for (int i = 0; i < words.Count; i++)
        {
            switch (words[i])
            {
                case "-n":
                    if (i + 1 < words.Count && int.TryParse(words[i + 1], out var n))
                    {
                        opt.Limit = Math.Clamp(n, 1, 50);
                        i++;
                    }
                    break;
                case "-site":
                    if (i + 1 < words.Count) { opt.Site = words[i + 1]; i++; }
                    break;
                case "-noai":
                    opt.NoAi = true;
                    break;
                case "-v":
                    opt.Verbose = true;
                    break;
                default:
                    query.Add(words[i]);
                    break;
            }
        }

        return (string.Join(' ', query), opt);
    }

    static List<string> Split(string line)
    {
        var list = new List<string>();
        var sb = new System.Text.StringBuilder();
        bool quoted = false;

        foreach (var ch in line)
        {
            if (ch == '"') { quoted = !quoted; continue; }
            if (ch == ' ' && !quoted)
            {
                if (sb.Length > 0) { list.Add(sb.ToString()); sb.Clear(); }
                continue;
            }
            sb.Append(ch);
        }
        if (sb.Length > 0) list.Add(sb.ToString());
        return list;
    }
}