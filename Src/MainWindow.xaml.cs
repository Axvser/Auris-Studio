using Auris_Studio.ViewModels;
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

        protected override void OnClosed(EventArgs e)
        {
            if (Editor.DataContext is MidiEditorViewModel editor) editor.StopCommand.Execute(null);
            base.OnClosed(e);
        }

        private void CloseApp(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ReSizeApp(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
        }

        private void HideApp(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private bool IsDragging { get; set; }

        private void Grid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            IsDragging = true;
        }

        private void Grid_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            IsDragging = false;
        }

        private void Grid_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (IsDragging) DragMove();
        }

        private void Grid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            IsDragging = false;
        }
    }
}