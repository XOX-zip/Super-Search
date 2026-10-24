using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SuperSearch;

public partial class SettingsWindow : Window
{
    readonly Config _cfg;
    readonly bool _firstRun;

    public bool Saved { get; private set; }

    internal SettingsWindow(Config cfg, bool firstRun)
    {
        _cfg = cfg;
        _firstRun = firstRun;
        InitializeComponent();

        MaxHeight = SystemParameters.WorkArea.Height - 40;
        TitleText.Text = firstRun ? "首次设置" : "设置";
        EpBox.Text = cfg.AiEndpoint ?? "";
        KeyBox.Password = cfg.AiKey ?? "";
        ModelBox.Text = cfg.AiModel ?? "";
        LimitBox.Text = cfg.DefaultLimit.ToString();
        ConfigPathText.Text = "配置文件：" + Config.FilePath;

        FirstRunBanner.Visibility = firstRun ? Visibility.Visible : Visibility.Collapsed;
        SkipButton.Visibility = firstRun ? Visibility.Visible : Visibility.Collapsed;
        SaveButton.Content = firstRun ? "开始使用" : "保存";

        SetStatus("", false);
    }

    // ---------- AI 配置 ----------

    async void Test_Click(object sender, RoutedEventArgs e)
    {
        if (!TryBuildAi(out var temp, out var error, out var localOnly))
        {
            SetStatus(error, true);
            return;
        }

        if (localOnly)
        {
            SetStatus("没填 AI 信息，将只用本地算法。", false);
            return;
        }

        TestButton.IsEnabled = false;
        SetStatus("正在测试连通…", false);
        var ok = await AiClient.PingAsync(temp);
        TestButton.IsEnabled = true;

        SetStatus(ok
            ? "连通正常，可以用 AI 做相关性精筛。"
            : "连接失败，检查地址 / Key / 网络。保存后仍会自动退回本地算法。", !ok);
    }

    bool TryBuildAi(out Config temp, out string error, out bool localOnly)
    {
        temp = _cfg;
        error = "";
        localOnly = false;

        var endpoint = EpBox.Text.Trim();
        var key = KeyBox.Password;
        var model = ModelBox.Text.Trim();

        if (endpoint.Length == 0 && key.Length == 0 && model.Length == 0)
        {
            localOnly = true;
            return true;
        }
        if (key.Length == 0)
        {
            error = "填了 AI 信息，但 API Key 是空的。";
            return false;
        }
        if (endpoint.Length == 0)
        {
            error = "填了 AI 信息，但接口地址是空的。";
            return false;
        }

        temp = new Config
        {
            AiEndpoint = endpoint,
            AiKey = key,
            AiModel = model.Length == 0 ? "gpt-4o-mini" : model
        };
        return true;
    }

    // ---------- 保存 ----------

    void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!TryBuildAi(out var temp, out var error, out var localOnly))
        {
            SetStatus(error, true);
            return;
        }

        if (!int.TryParse(LimitBox.Text.Trim(), out var limit))
            limit = _cfg.DefaultLimit;
        _cfg.DefaultLimit = Math.Clamp(limit, 1, 50);
        LimitBox.Text = _cfg.DefaultLimit.ToString();

        if (localOnly)
        {
            _cfg.AiEndpoint = null;
            _cfg.AiKey = null;
            _cfg.AiModel = null;
        }
        else
        {
            _cfg.AiEndpoint = temp.AiEndpoint;
            _cfg.AiKey = temp.AiKey;
            _cfg.AiModel = temp.AiModel;
        }

        _cfg.Configured = true;
        Saved = true;
        if (!_cfg.Save()) ShowSaveWarning();
        Close();
    }

    void Skip_Click(object sender, RoutedEventArgs e)
    {
        // 和原来的控制台流程一致：明确选择不用 AI 时清掉旧配置。
        _cfg.AiEndpoint = null;
        _cfg.AiKey = null;
        _cfg.AiModel = null;
        _cfg.Configured = true;
        Saved = true;
        if (!_cfg.Save()) ShowSaveWarning();
        Close();
    }

    void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!_firstRun || Saved) return;

        // 首次运行时直接关窗口 = 先不配 AI，之后可以再进设置。
        _cfg.Configured = true;
        _cfg.Save();
        Saved = true;
    }

    void ShowSaveWarning()
    {
        MessageBox.Show(this,
            "配置保存失败（权限问题）。本次会话照常能用，但下次要重配。\n\n" +
            "检查 " + Config.Dir + " 的权限，或把它删掉让程序重建。",
            "保存失败", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    // ---------- 界面 ----------

    void SetStatus(string text, bool warn)
    {
        StatusText.Text = text;
        StatusText.Foreground = warn
            ? (Brush)FindResource("WarnBrush")
            : (Brush)FindResource("MutedBrush");
        StatusText.Visibility = text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    void Close_Click(object sender, RoutedEventArgs e) => Close();
}
