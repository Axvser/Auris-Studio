using NAudio.Midi;
using VeloxDev.Core.MVVM;

namespace Auris_Studio.ViewModels.MidiEvents;

public partial class KeySignatureEventViewModel : EventViewModel
{
    [VeloxProperty] private int _sharpsFlats = 0;
    [VeloxProperty] private int _majorMinor = 0;

    [VeloxCommand]
    public override void Read(object? parameter)
    {
        if (parameter is KeySignatureEvent ksEvent)
        {
            SharpsFlats = ksEvent.SharpsFlats;
            MajorMinor = ksEvent.MajorMinor;
            AbsoluteTime = ksEvent.AbsoluteTime;
        }
    }

    [VeloxCommand]
    public override void Write(object? parameter)
    {
        if (parameter is IList<MidiEvent> midiEvents)
        {
            midiEvents.Add(new KeySignatureEvent(
                SharpsFlats,
                MajorMinor,
                AbsoluteTime));
        }
    }
}
