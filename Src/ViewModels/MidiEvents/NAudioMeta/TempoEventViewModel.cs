using NAudio.Midi;
using VeloxDev.Core.MVVM;

namespace Auris_Studio.ViewModels.MidiEvents;

public partial class TempoEventViewModel : EventViewModel
{
    [VeloxProperty] private int _bPM = 0;

    [VeloxCommand]
    public override void Read(object? parameter)
    {
        if (parameter is TempoEvent tempoEvent)
        {
            BPM = 60000000 / tempoEvent.MicrosecondsPerQuarterNote;
            AbsoluteTime = tempoEvent.AbsoluteTime;
        }
    }

    [VeloxCommand]
    public override void Write(object? parameter)
    {
        if (parameter is IList<MidiEvent> midiEvents)
        {
            midiEvents.Add(new TempoEvent(60000000 / BPM, AbsoluteTime));
        }
    }
}
