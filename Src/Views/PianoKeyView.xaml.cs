using Auris_Studio.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Auris_Studio.Views
{
    public partial class PianoKeyView : UserControl
    {
        public PianoKeyView()
        {
            InitializeComponent();
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(PianoKeyView), new PropertyMetadata(string.Empty, UpdateVisual));

        public PianoKeyType Type
        {
            get { return (PianoKeyType)GetValue(TypeProperty); }
            set { SetValue(TypeProperty, value); }
        }
        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register(nameof(Type), typeof(PianoKeyType), typeof(PianoKeyView), new PropertyMetadata(PianoKeyType.White, UpdateVisual));

        public PianoKeySideBolded Bolded
        {
            get { return (PianoKeySideBolded)GetValue(BoldedProperty); }
            set { SetValue(BoldedProperty, value); }
        }
        public static readonly DependencyProperty BoldedProperty =
            DependencyProperty.Register(nameof(Bolded), typeof(PianoKeySideBolded), typeof(PianoKeyView), new PropertyMetadata(PianoKeySideBolded.None));

        private static void UpdateVisual(object? sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is PianoKeyView view)
            {
                view.Background = view.Type switch
                {
                    PianoKeyType.Black => Brushes.Black,
                    _ => Brushes.White
                };

                view.Width = view.Type switch
                {
                    PianoKeyType.Black => 80 * 0.618,
                    _ => 80
                };

                view.BorderThickness = view.Bolded switch
                {
                    PianoKeySideBolded.None => new Thickness(0, 0.5, 0, 0.5),
                    PianoKeySideBolded.Top => new Thickness(0, 1, 0, 0.5),
                    PianoKeySideBolded.Bottom => new Thickness(0, 0.5, 0, 1),
                    _ => new Thickness(0)
                };
            }
        }
    }
}
