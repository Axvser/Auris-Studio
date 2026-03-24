using Auris_Studio.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Auris_Studio.Views
{
    public partial class TrackListView : UserControl
    {
        public TrackListView()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MidiEditorViewModel vm)
            {
                vm.Tracks.Add(new MidiTrackViewModel()
                {
                    Name = "Default Track",
                    Channel = 1,
                });
            }
        }

        private void VerticalScrollBar_OffsetChanged(object sender, double e)
        {
            TracksViewer.ScrollToVerticalOffset(e);
        }
    }
}
