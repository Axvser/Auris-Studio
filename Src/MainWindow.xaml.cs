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
        private const string logo_mon = "M716.8 2.048q67.584 27.648 124.416 74.24t97.28 106.496 63.488 131.072 23.04 148.992q0 68.608-17.92 132.096t-50.176 118.784-77.824 100.864-100.864 77.824-119.296 50.176-132.608 17.92q-77.824 0-148.992-22.528t-131.072-63.488-106.496-97.792-74.24-124.416q64.512 73.728 153.6 115.712t194.56 41.984q95.232 0 179.2-36.352t146.432-98.816 98.816-146.432 36.352-179.2q0-104.448-42.496-194.048t-115.2-153.088zM220.16 289.792l-89.088-86.016 122.88-18.432 54.272-110.592 55.296 110.592 122.88 18.432-89.088 86.016 21.504 122.88-110.592-58.368-109.568 58.368z";
        private const string logo_sun = "M473.386667 241.984a27.861333 27.861333 0 0 1-30.08-25.429333l-4.693334-56.128a27.861333 27.861333 0 0 1 55.488-4.693334l4.714667 56.149334c1.28 15.338667-10.090667 28.8-25.408 30.08zM666.752 273.557333a27.84 27.84 0 0 1-1.493333-39.36l38.314666-41.322666a27.84 27.84 0 0 1 40.832 37.866666L706.133333 272.064c-10.453333 11.285333-28.074667 11.946667-39.36 1.493333zM773.034667 415.786667a27.861333 27.861333 0 0 1 27.690666-28.010667l56.32-0.362667a27.84 27.84 0 1 1 0.362667 55.701334l-56.32 0.362666a27.861333 27.861333 0 0 1-28.053333-27.669333zM764.245333 616.810667a27.861333 27.861333 0 0 1 37.504-12.053334l50.133334 25.728a27.861333 27.861333 0 0 1-25.450667 49.557334l-50.133333-25.728a27.861333 27.861333 0 0 1-12.053334-37.504zM662.250667 729.6a27.861333 27.861333 0 0 1 36.970666 13.589333l23.68 51.136a27.861333 27.861333 0 1 1-50.56 23.381334l-23.68-51.136a27.861333 27.861333 0 0 1 13.589334-36.970667zM308.544 337.28c9.024-12.458667 6.229333-29.866667-6.250667-38.890667l-45.653333-33.024a27.861333 27.861333 0 0 0-32.64 45.141334l45.653333 33.024c12.458667 9.002667 29.866667 6.208 38.890667-6.250667zM263.509333 513.024a27.861333 27.861333 0 0 0-30.357333-25.109333l-56.106667 5.333333a27.84 27.84 0 1 0 5.290667 55.466667l56.064-5.333334a27.84 27.84 0 0 0 25.109333-30.357333zM318.677333 677.269333a27.861333 27.861333 0 0 0-38.506666-8.213333L232.896 699.733333a27.861333 27.861333 0 0 0 30.293333 46.72l47.274667-30.634666c12.906667-8.362667 16.597333-25.6 8.213333-38.506667zM480.064 760.234667a27.84 27.84 0 0 0-33.237333 21.12l-12.245334 54.997333a27.84 27.84 0 1 0 54.357334 12.096l12.245333-54.976a27.84 27.84 0 0 0-21.12-33.237333zM756.842667 499.2c0 130.773333-105.984 236.736-236.714667 236.736-130.752 0-236.736-105.984-236.736-236.714667 0-130.752 105.984-236.736 236.736-236.736 130.730667 0 236.714667 105.984 236.714667 236.736z m-236.714667 194.069333a194.069333 194.069333 0 1 0 0-388.117333 194.069333 194.069333 0 0 0 0 388.117333z";

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

        private void Theme_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Jump(
                ThemeManager.Current == typeof(Dark) ? typeof(Light) : typeof(Dark));
            ThemeButton.ButtonContent = ThemeManager.Current == typeof(Dark) ? logo_sun : logo_mon;
        }
    }
}