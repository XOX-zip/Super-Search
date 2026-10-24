using SuperSearch;

try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { }
Console.Title = "SS - Super Search";

var cfg = Config.Load();
if (!cfg.Configured)
{
    await Setup.RunAsync(cfg);
    if (!cfg.Save())
    {
        Console.WriteLine();
        Console.WriteLine("⚠ 配置保存失败（权限问题），本次会话照常能用，但下次要重配。");
        Console.WriteLine("  检查 " + Config.Dir + " 的权限，或把它删掉让程序重建。");
    }
}

await Repl.RunAsync(cfg);