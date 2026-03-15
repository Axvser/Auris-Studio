using Auris_Studio.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using VeloxDev.Core.DynamicTheme;
using VeloxDev.WPF.PlatformAdapters;

namespace Auris_Studio.Views
{
    [ThemeConfig<ObjectConverter, Dark, Light>(nameof(WhiteTrackBrush), ["#25262C"], ["#FFFFFF"])]
    [ThemeConfig<ObjectConverter, Dark, Light>(nameof(BlackTrackBrush), ["#1E1F24"], ["#F4F4F6"])]
    public partial class TrackBackgroundView : UserControl
    {
        public TrackBackgroundView()
        {
            InitializeComponent();
            InitializeTheme();
        }

        public Brush WhiteTrackBrush
        {
            get { return (Brush)GetValue(WhiteTrackBrushProperty); }
            set { SetValue(WhiteTrackBrushProperty, value); }
        }
        public static readonly DependencyProperty WhiteTrackBrushProperty =
            DependencyProperty.Register(nameof(WhiteTrackBrush), typeof(Brush), typeof(TrackBackgroundView), new PropertyMetadata());

        public Brush BlackTrackBrush
        {
            get { return (Brush)GetValue(BlackTrackBrushProperty); }
            set { SetValue(BlackTrackBrushProperty, value); }
        }
        public static readonly DependencyProperty BlackTrackBrushProperty =
            DependencyProperty.Register(nameof(BlackTrackBrush), typeof(Brush), typeof(TrackBackgroundView), new PropertyMetadata());

        public bool IsHuge
        {
            get { return (bool)GetValue(IsHugeProperty); }
            set { SetValue(IsHugeProperty, value); }
        }
        public static readonly DependencyProperty IsHugeProperty =
            DependencyProperty.Register(nameof(IsHuge), typeof(bool), typeof(TrackBackgroundView), new PropertyMetadata(false));

        public PianoKeyType Type
        {
            get { return (PianoKeyType)GetValue(TypeProperty); }
            set { SetValue(TypeProperty, value); }
        }
        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register(nameof(Type), typeof(PianoKeyType), typeof(TrackBackgroundView), new PropertyMetadata(PianoKeyType.White));
    }
}
