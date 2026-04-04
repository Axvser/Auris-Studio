using NAudio.Midi;
using VeloxDev.Core.MVVM;

namespace Auris_Studio.ViewModels.MidiEvents;

/// <summary>
/// 存储控制器事件，取值范围[0,127]
/// </summary>
public partial class ControlChangeEventViewModel : EventViewModel
{
    public ControlChangeEventViewModel()
    {
        DeltaTime = 1;
    }

    [VeloxProperty] private int _value = 127; // 控制值
    [VeloxProperty] private MidiController midiController = MidiController.AllNotesOff; // 控制器类型

    [VeloxCommand]
    public override void Read(object? parameter)
    {
        if (parameter is ControlChangeEvent controlEvent)
        {
            Value = controlEvent.ControllerValue;
            MidiController = controlEvent.Controller;
            AbsoluteTime = controlEvent.AbsoluteTime;
            DeltaTime = 1;
        }
    }

    [VeloxCommand]
    public override void Write(object? parameter)
    {
        if (parameter is IList<MidiEvent> midiEvents)
        {
            midiEvents.Add(new ControlChangeEvent(
                AbsoluteTime,
                Parent?.Channel ?? 1,
                MidiController,
                Value));
        }
    }
}
