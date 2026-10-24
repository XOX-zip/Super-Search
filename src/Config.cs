using System.Text.Json;
using System.Text.Json.Serialization;

namespace SuperSearch;

class Config
{
    public bool Configured { get; set; }
    public string? AiEndpoint { get; set; }
    public string? AiKey { get; set; }
    public string? AiModel { get; set; }
    public int DefaultLimit { get; set; } = 10;

    [JsonIgnore]
    public bool HasAi => !string.IsNullOrWhiteSpace(AiEndpoint) && !string.IsNullOrWhiteSpace(AiKey);

    // 按优先级给出的候选目录：Roaming → Local → 程序目录 → 临时目录
    internal static IEnumerable<string> CandidateDirs()
    {
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SS");
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SS");
        yield return Path.Combine(AppContext.BaseDirectory, "SS");
        yield return Path.Combine(Path.GetTempPath(), "SS");
    }

    internal static string Dir => CandidateDirs().First();

    internal static string FilePath => Path.Combine(Dir, "config.json");

    public static Config Load()
    {
        foreach (var dir in CandidateDirs())
        {
            var path = Path.Combine(dir, "config.json");
            try
            {
                if (File.Exists(path))
                    return JsonSerializer.Deserialize<Config>(File.ReadAllText(path)) ?? new Config();
            }
            catch (Exception ex)
            {
                Console.WriteLine("配置读取失败 (" + path + "): " + ex.Message);
            }
        }
        return new Config();
    }

    // 返回是否成功保存。失败不抛异常，调用方自行提示。
    public bool Save()
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

        foreach (var dir in CandidateDirs())
        {
            try
            {
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "config.json"), json);
                return true;
            }
            catch { }
        }
        return false;
    }
}