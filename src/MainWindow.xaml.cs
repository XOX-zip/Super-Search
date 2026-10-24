using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SuperSearch;

public partial class MainWindow : Window
{
    enum StatusKind { Info, Warn, Success, Danger }

    readonly Config _cfg;
    readonly SearchService _svc;
    readonly ObservableCollection<ResultRow> _rows = new();

    CancellationTokenSource? _cts;
    bool _busy;
    bool _hadAi;
    ResultRow? _selected;

    internal MainWindow(Config cfg)
    {
        _cfg = cfg;
        InitializeComponent();

        _svc = new SearchService(cfg);
        _hadAi = cfg.HasAi;

        var work = SystemParameters.WorkArea;
        Width = Math.Min(Width, work.Width - 40);
        Height = Math.Min(Height, work.Height - 40);

        ResultsList.ItemsSource = _rows;
        LimitBox.Text = cfg.DefaultLimit.ToString();
        AiToggle.IsChecked = cfg.HasAi;

        UpdateAiState();
        SetStatus("就绪 · 输入关键词开始", StatusKind.Info);
        if (Config.LastWarning is { } warn)
            SetStatus(warn, StatusKind.Warn);

        Loaded += (_, _) => QueryBox.Focus();
    }

    // ---------- 搜索 ----------

    async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        if (_busy)
        {
            _cts?.Cancel();
            return;
        }

        var query = QueryBox.Text.Trim();
        if (query.Length == 0)
        {
            SetStatus("先输入搜索词。", StatusKind.Warn);
            QueryBox.Focus();
            return;
        }

        if (!int.TryParse(LimitBox.Text.Trim(), out var limit))
            limit = _cfg.DefaultLimit;
        limit = Math.Clamp(limit, 1, 50);
        LimitBox.Text = limit.ToString();

        var site = SiteBox.Text.Trim();
        var opt = new SearchOptions
        {
            Limit = limit,
            NoAi = AiToggle.IsChecked != true,
            Verbose = VerboseToggle.IsChecked == true,
            Site = site.Length > 0 ? site : null
        };

        var fullQuery = query;
        if (opt.Site != null) fullQuery += " site:" + opt.Site;

        var cts = new CancellationTokenSource();
        _cts = cts;
        _busy = true;
        SetBusy(true);
        _rows.Clear();
        ClearDetail();
        CountText.Text = "";
        ShowEmpty("正在搜索…", "已发出请求，稍等片刻。");
        SetStatus(opt.NoAi || !_cfg.HasAi ? "检索中 · 本地算法" : "检索中 · 本地评分，等待 AI 精筛", StatusKind.Info);

        try
        {
            var outcome = await _svc.SearchAsync(fullQuery, opt, cts.Token);
            Fill(outcome);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            SetStatus("已取消。", StatusKind.Warn);
            ShowEmpty("已取消", "换个关键词再试试。");
        }
        catch (Exception ex)
        {
            var msg = ex is TaskCanceledException ? "请求超时（20 秒），网络可能不通。" : "抓取失败：" + ex.Message;
            SetStatus(msg, StatusKind.Danger);
            ShowEmpty("抓取失败", msg);
        }
        finally
        {
            _busy = false;
            _cts = null;
            cts.Dispose();
            SetBusy(false);
            UpdateAiState();
        }
    }

    void Fill(SearchOutcome outcome)
    {
        _rows.Clear();

        if (outcome.Results.Count == 0)
        {
            CountText.Text = "";
            ClearDetail();
            var desc = outcome.Notice ?? "换个说法再试。";
            if (outcome.DebugPath is { } path)
                desc += "\n原始响应已保存到 " + path;
            ShowEmpty("没有找到结果", desc);
            SetStatus(outcome.Notice ?? "没有结果。", StatusKind.Warn);
            return;
        }

        var showScore = VerboseToggle.IsChecked == true;
        foreach (var r in outcome.Results)
            _rows.Add(new ResultRow(r) { ShowScore = showScore });

        HideEmpty();
        CountText.Text = $"共 {outcome.Results.Count} 条";
        ResultsList.SelectedIndex = 0;

        var mode = outcome.AiUsed ? "AI 精筛" : (outcome.AiConfigured ? "本地算法" : "本地算法 · 未接 AI");
        if (outcome.Notice != null)
            SetStatus(outcome.Notice, outcome.AiConfigured && !outcome.AiUsed ? StatusKind.Warn : StatusKind.Info);
        else
            SetStatus($"完成 · {mode} · {outcome.Results.Count} 条", StatusKind.Success);

        ResultsList.BeginAnimation(OpacityProperty,
            new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(200)));
    }

    // ---------- 结果 / 详情 ----------

    void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ResultsList.SelectedItem is not ResultRow row)
        {
            ClearDetail();
            return;
        }

        _selected = row;
        DetailEmpty.Visibility = Visibility.Collapsed;
        DetailBody.Visibility = Visibility.Visible;
        DetailTitle.Text = row.Title;
        DetailHost.Text = row.Host;
        DetailUrl.Text = row.Url;
        DetailSnippet.Text = row.Snippet;
        DetailScore.Text = $"评分 {row.Result.Score:F1}";
        DetailScore.Visibility = VerboseToggle.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    void ResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ResultsList.SelectedItem is ResultRow) OpenSelected();
    }

    void DetailOpen_Click(object sender, RoutedEventArgs e) => OpenSelected();

    void OpenSelected()
    {
        if (_selected is null) return;

        try
        {
            Process.Start(new ProcessStartInfo(_selected.Url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            SetStatus("打开链接失败：" + ex.Message, StatusKind.Danger);
        }
    }

    void DetailCopy_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null) return;

        try
        {
            Clipboard.SetText(_selected.Url);
            SetStatus("链接已复制到剪贴板。", StatusKind.Success);
        }
        catch (Exception ex)
        {
            SetStatus("复制失败：" + ex.Message, StatusKind.Danger);
        }
    }

    void ClearDetail()
    {
        _selected = null;
        DetailBody.Visibility = Visibility.Collapsed;
        DetailEmpty.Visibility = Visibility.Visible;
    }

    // ---------- 选项 ----------

    void VerboseToggle_Changed(object sender, RoutedEventArgs e)
    {
        var show = VerboseToggle.IsChecked == true;
        foreach (var row in _rows) row.ShowScore = show;
        if (_selected != null)
            DetailScore.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
    }

    void QueryBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            SearchButton_Click(SearchButton, new RoutedEventArgs());
        }
    }

    // ---------- 设置 ----------

    void Settings_Click(object sender, RoutedEventArgs e) => OpenSettings();

    void AiPill_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) => OpenSettings();

    void OpenSettings()
    {
        var dialog = new SettingsWindow(_cfg, firstRun: false) { Owner = this };
        dialog.ShowDialog();
        if (!dialog.Saved) return;

        if (_cfg.HasAi && !_hadAi) AiToggle.IsChecked = true;
        _hadAi = _cfg.HasAi;

        _svc.ResetAiState();
        UpdateAiState();
        LimitBox.Text = _cfg.DefaultLimit.ToString();
        SetStatus("设置已保存。", StatusKind.Success);
    }

    void UpdateAiState()
    {
        AiToggle.IsEnabled = _cfg.HasAi;
        AiToggle.ToolTip = _cfg.HasAi ? "关闭后本次搜索只用本地算法" : "未接入 AI，点右上角设置配置";
        if (!_cfg.HasAi) AiToggle.IsChecked = false;

        if (!_cfg.HasAi)
        {
            AiPillText.Text = "本地算法";
            AiDot.Fill = (Brush)FindResource("FaintBrush");
            AiPill.ToolTip = "未接入 AI，点击配置";
        }
        else if (_svc.AiDown)
        {
            AiPillText.Text = "本地算法 · AI 无响应";
            AiDot.Fill = (Brush)FindResource("WarnBrush");
            AiPill.ToolTip = "AI 本次会话不可用，点击重新配置";
        }
        else
        {
            AiPillText.Text = "AI · " + (_cfg.AiModel ?? "已配置");
            AiDot.Fill = (Brush)FindResource("SuccessBrush");
            AiPill.ToolTip = "已接入 AI 精筛";
        }
    }

    // ---------- 界面状态 ----------

    void SetBusy(bool busy)
    {
        SearchLabel.Text = busy ? "取消" : "搜索";
        BusyDot.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;

        if (busy)
        {
            BusyDot.BeginAnimation(OpacityProperty,
                new DoubleAnimation(1.0, 0.15, TimeSpan.FromMilliseconds(700))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                });
        }
        else
        {
            BusyDot.BeginAnimation(OpacityProperty, null);
        }
    }

    void SetStatus(string text, StatusKind kind)
    {
        StatusText.Text = text;
        StatusText.Foreground = kind switch
        {
            StatusKind.Warn => (Brush)FindResource("WarnBrush"),
            StatusKind.Danger => (Brush)FindResource("DangerBrush"),
            StatusKind.Success => (Brush)FindResource("SuccessBrush"),
            _ => (Brush)FindResource("MutedBrush")
        };
        StatusDot.Fill = kind switch
        {
            StatusKind.Warn => (Brush)FindResource("WarnBrush"),
            StatusKind.Danger => (Brush)FindResource("DangerBrush"),
            StatusKind.Success => (Brush)FindResource("SuccessBrush"),
            _ => (Brush)FindResource("FaintBrush")
        };
    }

    void ShowEmpty(string title, string desc)
    {
        EmptyTitle.Text = title;
        EmptyDesc.Text = desc;
        EmptyPanel.Visibility = Visibility.Visible;
        ResultsList.Visibility = Visibility.Collapsed;
    }

    void HideEmpty()
    {
        EmptyPanel.Visibility = Visibility.Collapsed;
        ResultsList.Visibility = Visibility.Visible;
    }

    // ---------- 窗口 ----------

    void Window_StateChanged(object sender, EventArgs e)
    {
        var maximized = WindowState == WindowState.Maximized;
        MaxIcon.Text = maximized ? "\uE923" : "\uE922";
        RootBorder.Margin = maximized ? new Thickness(6) : new Thickness(0);
    }

    void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _busy)
        {
            _cts?.Cancel();
            e.Handled = true;
        }
        else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            QueryBox.Focus();
            QueryBox.SelectAll();
            e.Handled = true;
        }
    }

    void Min_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    void Max_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    void Close_Click(object sender, RoutedEventArgs e) => Close();

    void Window_Closed(object? sender, EventArgs e) => Application.Current.Shutdown();
}
