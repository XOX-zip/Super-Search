namespace SuperSearch;

static class Setup
{
    public static async Task RunAsync(Config cfg)
    {
        Console.WriteLine("首次运行，先做点配置。");
        Console.WriteLine();

        Console.Write("你有 AI API 吗？(y/N) ");
        var ans = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();

        if (ans is "y" or "yes")
        {
            var epDefault = string.IsNullOrWhiteSpace(cfg.AiEndpoint) ? "https://api.openai.com/v1" : cfg.AiEndpoint;
            Console.Write($"接口地址 [{epDefault}]: ");
            var ep = (Console.ReadLine() ?? "").Trim();
            if (ep.Length > 0) cfg.AiEndpoint = ep;
            else if (string.IsNullOrWhiteSpace(cfg.AiEndpoint)) cfg.AiEndpoint = epDefault;

            Console.Write("API Key: ");
            var key = ReadSecret();
            if (key.Length > 0) cfg.AiKey = key;

            var mDefault = string.IsNullOrWhiteSpace(cfg.AiModel) ? "gpt-4o-mini" : cfg.AiModel;
            Console.Write($"模型 [{mDefault}]: ");
            var model = (Console.ReadLine() ?? "").Trim();
            if (model.Length > 0) cfg.AiModel = model;
            else if (string.IsNullOrWhiteSpace(cfg.AiModel)) cfg.AiModel = mDefault;

            Console.WriteLine();
            Console.Write("测试连通性...");
            var ok = await AiClient.PingAsync(cfg);
            Console.WriteLine(ok ? " OK" : " 失败");
            Console.WriteLine(ok
                ? "搜索时会用 AI 做相关性筛选。"
                : "先存着，搜索时会自动退回本地算法。之后可以用 /ai 重新配。");
        }
        else
        {
            if (cfg.HasAi)
            {
                cfg.AiEndpoint = null;
                cfg.AiKey = null;
                cfg.AiModel = null;
                Console.WriteLine("已清掉原来的 AI 配置。");
            }
            else
            {
                Console.WriteLine("好，只用本地算法。");
            }
            Console.WriteLine("提前说一声：没有 AI 参与时，筛选和排序只能靠关键词、域名权重这些，效果会打折。");
        }

        cfg.Configured = true;
    }

    static string ReadSecret()
    {
        var sb = new System.Text.StringBuilder();
        while (true)
        {
            var k = Console.ReadKey(true);
            if (k.Key == ConsoleKey.Enter) { Console.WriteLine(); break; }
            if (k.Key == ConsoleKey.Backspace)
            {
                if (sb.Length > 0) { sb.Length--; Console.Write("\b \b"); }
                continue;
            }
            if (char.IsControl(k.KeyChar)) continue;
            sb.Append(k.KeyChar);
            Console.Write('*');
        }
        return sb.ToString();
    }
}