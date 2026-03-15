using Auris_Studio.Midi;
using VeloxDev.Core.MVVM;

namespace Auris_Studio.ViewModels;

public enum PianoKeyType : int
{
    White,
    Black,
}

public enum PianoKeySideBolded : int
{
    None,
    Top,
    Bottom,
}

public partial class PianoKeyViewModel
{
    [VeloxProperty] private int _note = -1;
    [VeloxProperty] public partial double Bottom { get; internal set; }
    [VeloxProperty] public partial double Height { get; internal set; }
    [VeloxProperty] public partial bool Huge { get; internal set; }
    [VeloxProperty] public partial PianoKeyType Type { get; internal set; }
    [VeloxProperty] public partial PianoKeySideBolded Bolded { get; internal set; }
    [VeloxProperty] public partial string Text { get; internal set; }
    [VeloxProperty] public partial int Layer { get; internal set; }

    partial void OnNoteChanged(int oldValue, int newValue)
    {
        if (newValue < (int)Pitch.C_minus1 || newValue > (int)Pitch.G9) return;

        Pitch pitch = (Pitch)newValue;
        int octave = (newValue / 12) - 1;
        string pitchName = pitch.ToString();

        // 判断是否为升号（黑键）
        bool isSharp = pitchName.Contains("Sharp");

        if (isSharp)
        {
            Type = PianoKeyType.Black;
            Layer = 2;
            Huge = false;
            Bolded = PianoKeySideBolded.None;
        }
        else
        {
            Type = PianoKeyType.White;

            int currentIndexInOctave = newValue % 12;
            bool prevIsSharp = (newValue - 1) >= 0 && ((Pitch)(newValue - 1)).ToString().Contains("Sharp");
            bool nextIsSharp = (newValue + 1) <= 127 && ((Pitch)(newValue + 1)).ToString().Contains("Sharp");
            Huge = prevIsSharp && nextIsSharp;

            if (currentIndexInOctave == 0) // C
            {
                Bolded = PianoKeySideBolded.Bottom;
                Text = $"{pitchName[0]}{octave}";
            }
            else if (currentIndexInOctave == 5 || currentIndexInOctave == 11) // F 或 B
            {
                Bolded = PianoKeySideBolded.Top;
            }
            else
            {
                Bolded = PianoKeySideBolded.None;
            }
        }
    }
}