using System.Threading;
using System.Windows;

namespace KiteGlance;

public partial class App : System.Windows.Application
{
    // Named mutex, not a process scan: this is race-free and survives
    // the exe being launched from two different paths.
    private const string InstanceKey = "KiteGlance.SingleInstance.v1";

    private Mutex? _instance;
    private MainWindow? _widget;
    private WidgetManager? _manager;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _instance = new Mutex(initiallyOwned: true, InstanceKey, out var isFirst);

        if (!isFirst)
        {
            // Already running. Don't stack a second widget on the desktop;
            // just leave quietly. The tray icon is the way back in.
            Shutdown();
            return;
        }

        _widget = new MainWindow();
        _manager = new WidgetManager(_widget);

        _widget.Show();
        _ = _widget.BootAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _manager?.Dispose();

        if (_instance is not null)
        {
            try { _instance.ReleaseMutex(); } catch { /* not owned */ }
            _instance.Dispose();
        }

        base.OnExit(e);
    }
}
