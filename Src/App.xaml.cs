using System.Windows;
using VeloxDev.Core.DynamicTheme;
using VeloxDev.WPF.PlatformAdapters;

namespace Auris_Studio
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ThemeManager.SetPlatformInterpolator(new Interpolator());
        }
    }
}
