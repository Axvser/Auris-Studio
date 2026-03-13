using Auris_Studio.ViewModels.ComponentModel;
using VeloxDev.Core.MVVM;

namespace Auris_Studio.ViewModels.MidiEvents;

public abstract partial class EventViewModel : ITimeOrderable, IMidiFormatable
{
    [VeloxProperty] private MidiTrackViewModel? _parent; // 所属音轨
    [VeloxProperty] private long _absoluteTime = 0; // 起始时间(Tick)
    [VeloxProperty] private int _deltaTime = 0; // 持续时间(Tick)

    public abstract void Read(object? parameter); // 从NAudio的Midi事件解析数据
    public abstract void Write(object? parameter); // 输出NAudio的Midi事件到 IList<MidiEvents>
}
