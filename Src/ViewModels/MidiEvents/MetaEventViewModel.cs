using NAudio.Midi;
using VeloxDev.Core.MVVM;

namespace Auris_Studio.ViewModels.MidiEvents;

/// <summary>
/// MIDI 元数据事件
/// <para>通道固定为1，这可在 <seealso cref="MetaEvent"/> 构造器源码中得到验证</para>
/// </summary>
public abstract partial class MetaEventViewModel : EventViewModel
{
    [VeloxProperty] private MetaEventType _metaEventType = MetaEventType.TextEvent;
}