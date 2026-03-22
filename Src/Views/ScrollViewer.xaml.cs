using System.Windows;

namespace Auris_Studio.Views
{
    public partial class ScrollViewer : System.Windows.Controls.ScrollViewer
    {
        public ScrollViewer()
        {
            InitializeComponent();
        }

        private void Left(object sender, RoutedEventArgs e)
        {
            ScrollToHorizontalOffset(HorizontalOffset - ViewportWidth * 0.2);
        }
        private void Right(object sender, RoutedEventArgs e)
        {
            ScrollToHorizontalOffset(HorizontalOffset + ViewportWidth * 0.2);
        }
        private void Up(object sender, RoutedEventArgs e)
        {
            ScrollToVerticalOffset(VerticalOffset + ViewportHeight * 0.2);
        }
        private void Down(object sender, RoutedEventArgs e)
        {
            ScrollToVerticalOffset(VerticalOffset - ViewportHeight * 0.2);
        }
    }
}
