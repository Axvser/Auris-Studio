using Auris_Studio.Midi;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VeloxDev.Core.DynamicTheme;
using VeloxDev.WPF.PlatformAdapters;

namespace Auris_Studio.Views
{
    [ThemeConfig<ObjectConverter, Dark, Light>(nameof(Background), ["#111318"], ["#F5F6FA"])]
    [ThemeConfig<ObjectConverter, Dark, Light>(nameof(Foreground), [nameof(Brushes.White)], [nameof(Brushes.Black)])]
    [ThemeConfig<ObjectConverter, Dark, Light>(nameof(CardBackground), ["#16181D"], ["#FFFFFF"])]
    [ThemeConfig<ObjectConverter, Dark, Light>(nameof(PanelBackground), ["#22000000"], ["#08000000"])]
    [ThemeConfig<ObjectConverter, Dark, Light>(nameof(SecondaryForeground), ["#AAFFFFFF"], ["#99000000"])]
    public partial class MidiEditorSettingsWindow : Window
    {
        public int SelectedBpm { get; private set; }
        public int SelectedNumerator { get; private set; }
        public int SelectedDenominator { get; private set; }
        public Alignment SelectedAlignment { get; private set; }

        public MidiEditorSettingsWindow(int bpm, int numerator, int denominator, Alignment alignment)
        {
            InitializeComponent();
            InitializeTheme();

            SelectedBpm = bpm;
            SelectedNumerator = numerator;
            SelectedDenominator = denominator;
            SelectedAlignment = alignment;

            NumeratorComboBox.ItemsSource = Enumerable.Range(1, 16).ToArray();
            DenominatorComboBox.ItemsSource = new[] { 1, 2, 4, 8, 16, 32 };

            BpmSlider.Value = SelectedBpm;
            NumeratorComboBox.SelectedItem = SelectedNumerator;
            DenominatorComboBox.SelectedItem = SelectedDenominator;

            RefreshTimeSignaturePreview();
            RefreshAlignmentPreview();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            CaptureCurrentSelections();
            base.OnClosing(e);
        }

        public Brush CardBackground
        {
            get => (Brush)GetValue(CardBackgroundProperty);
            set => SetValue(CardBackgroundProperty, value);
        }
        public static readonly DependencyProperty CardBackgroundProperty =
            DependencyProperty.Register(nameof(CardBackground), typeof(Brush), typeof(MidiEditorSettingsWindow), new PropertyMetadata(Brushes.Transparent));

        public Brush PanelBackground
        {
            get => (Brush)GetValue(PanelBackgroundProperty);
            set => SetValue(PanelBackgroundProperty, value);
        }
        public static readonly DependencyProperty PanelBackgroundProperty =
            DependencyProperty.Register(nameof(PanelBackground), typeof(Brush), typeof(MidiEditorSettingsWindow), new PropertyMetadata(Brushes.Transparent));

        public Brush SecondaryForeground
        {
            get => (Brush)GetValue(SecondaryForegroundProperty);
            set => SetValue(SecondaryForegroundProperty, value);
        }
        public static readonly DependencyProperty SecondaryForegroundProperty =
            DependencyProperty.Register(nameof(SecondaryForeground), typeof(Brush), typeof(MidiEditorSettingsWindow), new PropertyMetadata(Brushes.Gray));

        private void TimeSignatureComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NumeratorComboBox.SelectedItem is int numerator)
            {
                SelectedNumerator = numerator;
            }

            if (DenominatorComboBox.SelectedItem is int denominator)
            {
                SelectedDenominator = denominator;
            }

            RefreshTimeSignaturePreview();
        }

        private void AlignmentNoteValueButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Parameter is not string value || !TryParseNoteValue(value, out var noteValue))
            {
                return;
            }

            SelectedAlignment = noteValue | GetAlignmentModifierPart(SelectedAlignment) | GetAlignmentTupletPart(SelectedAlignment);
            RefreshAlignmentPreview();
        }

        private void AlignmentModifierButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Parameter is not string value || !TryParseModifier(value, out var modifier))
            {
                return;
            }

            SelectedAlignment = GetAlignmentNoteValuePart(SelectedAlignment) | modifier | GetAlignmentTupletPart(SelectedAlignment);
            RefreshAlignmentPreview();
        }

        private void AlignmentTupletButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Parameter is not string value || !TryParseTuplet(value, out var tuplet))
            {
                return;
            }

            SelectedAlignment = GetAlignmentNoteValuePart(SelectedAlignment) | GetAlignmentModifierPart(SelectedAlignment) | tuplet;
            RefreshAlignmentPreview();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            CaptureCurrentSelections();
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CaptureCurrentSelections();
            Close();
        }

        private void CaptureCurrentSelections()
        {
            SelectedBpm = (int)System.Math.Round(BpmSlider.Value);

            if (NumeratorComboBox.SelectedItem is int numerator)
            {
                SelectedNumerator = numerator;
            }

            if (DenominatorComboBox.SelectedItem is int denominator)
            {
                SelectedDenominator = denominator;
            }
        }

        private void RefreshTimeSignaturePreview()
        {
            TimeSignaturePreviewText.Text = $"{SelectedNumerator}/{SelectedDenominator}";
        }

        private void RefreshAlignmentPreview()
        {
            AlignmentPreviewText.Text = GetAlignmentText(SelectedAlignment);

            Alignment noteValue = GetAlignmentNoteValuePart(SelectedAlignment);
            Alignment modifier = GetAlignmentModifierPart(SelectedAlignment);
            Alignment tuplet = GetAlignmentTupletPart(SelectedAlignment);

            UpdateSelectionButton(AlignmentNoteDoubleWholeButton, noteValue == Alignment.DoubleWholeNote, "2/1");
            UpdateSelectionButton(AlignmentNoteWholeButton, noteValue == Alignment.WholeNote, "1/1");
            UpdateSelectionButton(AlignmentNoteHalfButton, noteValue == Alignment.HalfNote, "1/2");
            UpdateSelectionButton(AlignmentNoteQuarterButton, noteValue == Alignment.QuarterNote, "1/4");
            UpdateSelectionButton(AlignmentNoteEighthButton, noteValue == Alignment.EighthNote, "1/8");
            UpdateSelectionButton(AlignmentNoteSixteenthButton, noteValue == Alignment.SixteenthNote, "1/16");
            UpdateSelectionButton(AlignmentNoteThirtySecondButton, noteValue == Alignment.ThirtySecondNote, "1/32");
            UpdateSelectionButton(AlignmentNoteSixtyFourthButton, noteValue == Alignment.SixtyFourthNote, "1/64");
            UpdateSelectionButton(AlignmentNoteOneTwentyEighthButton, noteValue == Alignment.OneTwentyEighthNote, "1/128");

            UpdateSelectionButton(AlignmentModifierNoneButton, modifier == 0, "Off");
            UpdateSelectionButton(AlignmentModifierDotButton, modifier == Alignment.Dot, "Dot");
            UpdateSelectionButton(AlignmentModifierDoubleDotButton, modifier == Alignment.DoubleDot, "2Dot");

            UpdateSelectionButton(AlignmentTupletNoneButton, tuplet == 0, "Off");
            UpdateSelectionButton(AlignmentTupletTripletButton, tuplet == Alignment.Triplet, "Tri");
            UpdateSelectionButton(AlignmentTupletQuintupletButton, tuplet == Alignment.Quintuplet, "Quint");
            UpdateSelectionButton(AlignmentTupletSeptupletButton, tuplet == Alignment.Septuplet, "Sept");
        }

        private static void UpdateSelectionButton(Auris_Studio.Views.Button button, bool isSelected, string text)
        {
            button.ButtonContent = isSelected ? $"* {text}" : text;
        }

        private static Alignment GetAlignmentNoteValuePart(Alignment alignment)
        {
            var noteValue = alignment & Alignment.NoteValueMask;
            return noteValue == 0 ? Alignment.EighthNote : noteValue;
        }

        private static Alignment GetAlignmentModifierPart(Alignment alignment) => alignment & Alignment.ModifierMask;

        private static Alignment GetAlignmentTupletPart(Alignment alignment) => alignment & Alignment.TupletMask;

        private static bool TryParseNoteValue(string value, out Alignment alignment)
        {
            alignment = value switch
            {
                "DoubleWhole" => Alignment.DoubleWholeNote,
                "Whole" => Alignment.WholeNote,
                "Half" => Alignment.HalfNote,
                "Quarter" => Alignment.QuarterNote,
                "Eighth" => Alignment.EighthNote,
                "Sixteenth" => Alignment.SixteenthNote,
                "ThirtySecond" => Alignment.ThirtySecondNote,
                "SixtyFourth" => Alignment.SixtyFourthNote,
                "OneTwentyEighth" => Alignment.OneTwentyEighthNote,
                _ => 0,
            };

            return alignment != 0;
        }

        private static bool TryParseModifier(string value, out Alignment alignment)
        {
            alignment = value switch
            {
                "None" => 0,
                "Dot" => Alignment.Dot,
                "DoubleDot" => Alignment.DoubleDot,
                _ => (Alignment)int.MinValue,
            };

            return alignment != (Alignment)int.MinValue;
        }

        private static bool TryParseTuplet(string value, out Alignment alignment)
        {
            alignment = value switch
            {
                "None" => 0,
                "Triplet" => Alignment.Triplet,
                "Quintuplet" => Alignment.Quintuplet,
                "Septuplet" => Alignment.Septuplet,
                _ => (Alignment)int.MinValue,
            };

            return alignment != (Alignment)int.MinValue;
        }

        private static string GetAlignmentText(Alignment alignment)
        {
            string noteValueText = GetAlignmentNoteValuePart(alignment) switch
            {
                Alignment.DoubleWholeNote => "2/1",
                Alignment.WholeNote => "1/1",
                Alignment.HalfNote => "1/2",
                Alignment.QuarterNote => "1/4",
                Alignment.EighthNote => "1/8",
                Alignment.SixteenthNote => "1/16",
                Alignment.ThirtySecondNote => "1/32",
                Alignment.SixtyFourthNote => "1/64",
                Alignment.OneTwentyEighthNote => "1/128",
                _ => "1/8",
            };

            var parts = new System.Collections.Generic.List<string> { noteValueText };

            switch (GetAlignmentModifierPart(alignment))
            {
                case Alignment.Dot:
                    parts.Add("Dot");
                    break;
                case Alignment.DoubleDot:
                    parts.Add("2Dot");
                    break;
            }

            switch (GetAlignmentTupletPart(alignment))
            {
                case Alignment.Triplet:
                    parts.Add("Tri");
                    break;
                case Alignment.Quintuplet:
                    parts.Add("Quint");
                    break;
                case Alignment.Septuplet:
                    parts.Add("Sept");
                    break;
            }

            return string.Join(" + ", parts);
        }
    }
}
