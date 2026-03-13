using NAudio.Midi;
using VeloxDev.Core.MVVM;

namespace Auris_Studio.ViewModels.MidiEvents;

public partial class TextEventViewModel : EventViewModel
{
    [VeloxProperty] private string _text = string.Empty;
    [VeloxProperty] private MetaEventType _metaEventType = MetaEventType.TextEvent;

    [VeloxCommand]
    public override void Read(object? parameter)
    {
        if (parameter is TextEvent textEvent)
        {
            Text = textEvent.Text;
            MetaEventType = textEvent.MetaEventType;
            AbsoluteTime = textEvent.AbsoluteTime;
        }
    }

    [VeloxCommand]
    public override void Write(object? parameter)
    {
        if (parameter is IList<MidiEvent> midiTrack)
        {
            midiTrack.Add(new TextEvent(
                Text,
                MetaEventType,
                AbsoluteTime));
        }
    }
}
