using System.Windows;

namespace SuperSearch;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var cfg = Config.Load();

        if (!cfg.Configured)
        {
            var setup = new SettingsWindow(cfg, firstRun: true);
            setup.ShowDialog();
            if (!setup.Saved)
            {
                cfg.Configured = true;
                cfg.Save();
            }
        }

        var window = new MainWindow(cfg);
        MainWindow = window;
        window.Show();
    }
}
