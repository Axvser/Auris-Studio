using Auris_Studio.Midi;
using NAudio.Midi;
using VeloxDev.Core.MVVM;

namespace Auris_Studio.ViewModels.MidiEvents;

public partial class NoteEventViewModel : EventViewModel
{
    [VeloxProperty] private Pitch _note = Pitch.Error; // 音高
    [VeloxProperty] private int _onVelocity = 127; // 开始力度
    [VeloxProperty] private int _offVelocity = 0; // 结束力度

    [VeloxProperty] public partial bool IsHighlighted { get; internal set; } // 是否高亮
    [VeloxProperty] public partial double Left { get; internal set; } // 距离画布左边界
    [VeloxProperty] public partial double Bottom { get; internal set; } // 距离画布下边界
    [VeloxProperty] public partial double Width { get; internal set; } // 宽度
    [VeloxProperty] public partial double Height { get; internal set; } // 高度

    [VeloxCommand]
    public override void Read(object? parameter)
    {
        // 已配对音符事件
        if (parameter is Tuple<NoteOnEvent, NoteEvent> noteEventPair)
        {
            Note = (Pitch)noteEventPair.Item1.NoteNumber;
            OnVelocity = noteEventPair.Item1.Velocity;
            OffVelocity = noteEventPair.Item2.Velocity;
            AbsoluteTime = noteEventPair.Item1.AbsoluteTime;
            DeltaTime = (int)(noteEventPair.Item2.AbsoluteTime - noteEventPair.Item1.AbsoluteTime);
        }
    }

    [VeloxCommand]
    public override void Write(object? parameter)
    {
        if (parameter is IList<MidiEvent> midiEvents)
        {
            var noteOnEvent = new NoteOnEvent(
                absoluteTime: AbsoluteTime,
                channel: Parent?.Channel ?? 1,
                noteNumber: (int)Note,
                velocity: OnVelocity,
                duration: DeltaTime);
            var noteOffEvent = new NoteEvent(
                absoluteTime: AbsoluteTime + DeltaTime,
                channel: Parent?.Channel ?? 1,
                commandCode: MidiCommandCode.NoteOff,
                noteNumber: (int)Note,
                velocity: OffVelocity);
            noteOnEvent.OffEvent = noteOffEvent;
            midiEvents.Add(noteOnEvent);
            midiEvents.Add(noteOffEvent);
        }
    }
}