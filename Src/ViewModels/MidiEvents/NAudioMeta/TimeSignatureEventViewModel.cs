using NAudio.Midi;
using VeloxDev.Core.MVVM;

namespace Auris_Studio.ViewModels.MidiEvents;

public partial class TimeSignatureEventViewModel : EventViewModel
{
    [VeloxProperty] private int _numerator = 4;
    [VeloxProperty] private int _denominator = 4;

    [VeloxCommand]
    public override void Read(object? parameter)
    {
        if (parameter is TimeSignatureEvent tsEvent)
        {
            Numerator = tsEvent.Numerator;
            Denominator = (int)Math.Pow(2, tsEvent.Denominator);
            AbsoluteTime = tsEvent.AbsoluteTime;
        }
    }

    [VeloxCommand]
    public override void Write(object? parameter)
    {
        if (parameter is IList<MidiEvent> midiTrack)
        {
            int denominatorPower = 0;
            double d = Denominator;
            while (d > 1)
            {
                d /= 2;
                denominatorPower++;
            }
            midiTrack.Add(new TimeSignatureEvent(
                AbsoluteTime,
                Numerator,
                denominatorPower,
                24,
                8));
        }
    }
}
