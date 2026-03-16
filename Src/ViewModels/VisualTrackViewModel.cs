using Auris_Studio.Midi;
using VeloxDev.Core.MVVM;

namespace Auris_Studio.ViewModels;

public partial class VisualTrackViewModel
{
    [VeloxProperty] private int _note = -1;
    [VeloxProperty] public partial PianoKeyType Type { get; internal set; }
    [VeloxProperty] public partial double Bottom { get; internal set; }
    [VeloxProperty] public partial double Height { get; internal set; }
    [VeloxProperty] public partial bool Huge { get; internal set; }
    [VeloxProperty] public partial int Layer { get; internal set; }

    partial void OnNoteChanged(int oldValue, int newValue)
    {
        if (newValue < (int)Pitch.C_minus1 || newValue > (int)Pitch.G9) return;

        Pitch pitch = (Pitch)newValue;
        string pitchName = pitch.ToString();

        bool isSharp = pitchName.Contains("Sharp");

        if (isSharp)
        {
            Type = PianoKeyType.Black;
            Layer = 2;
            Huge = false;
        }
        else
        {
            Type = PianoKeyType.White;
            Layer = 1;
            bool prevIsSharp = (newValue - 1) >= 0 && ((Pitch)(newValue - 1)).ToString().Contains("Sharp");
            bool nextIsSharp = (newValue + 1) <= 127 && ((Pitch)(newValue + 1)).ToString().Contains("Sharp");
            Huge = prevIsSharp && nextIsSharp;
        }
    }
}
