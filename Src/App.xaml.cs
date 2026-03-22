using System.Windows;
using VeloxDev.Core.DynamicTheme;
using VeloxDev.Core.TimeLine;
using VeloxDev.WPF.PlatformAdapters;

namespace Auris_Studio
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ThemeManager.SetPlatformInterpolator(new Interpolator());
            MonoBehaviourManager.SetTargetFPS(30);
            MonoBehaviourManager.Start();
        }

        protected async override void OnExit(ExitEventArgs e)
        {
            await MonoBehaviourManager.StopAsync();
            base.OnExit(e);
        }
    }
}
