using System.Windows;
using VeloxDev.Core.DynamicTheme;
using VeloxDev.WPF.PlatformAdapters;

namespace Auris_Studio
{
    [ThemeConfig<ObjectConverter, Dark, Light>(nameof(Background), ["#1e1e1e"], ["White"])]
    [ThemeConfig<ObjectConverter, Dark, Light>(nameof(Foreground), ["White"], ["#1e1e1e"])]
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeTheme();
        }


        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {

        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }
    }
}