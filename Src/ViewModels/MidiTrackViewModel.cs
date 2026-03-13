using Auris_Studio.Midi;
using Auris_Studio.ViewModels.ComponentModel;
using Auris_Studio.ViewModels.MidiEvents;
using System.Collections.Specialized;
using VeloxDev.Core.MVVM;

namespace Auris_Studio.ViewModels;

public partial class MidiTrackViewModel
{
    [VeloxProperty] private MidiEditorViewModel? _parent = null;

    [VeloxProperty] private string _name = string.Empty; // 音轨名称
    [VeloxProperty] private int _channel = 1; // MIDI通道
    [VeloxProperty] private Patch _patch = Patch.AcousticGrandPiano; // 轨道音色

    [VeloxProperty] private TimeOrderableCollection<NoteEventViewModel> _notes = []; // 音符
    [VeloxProperty] private TimeOrderableCollection<ChannelAfterTouchEventViewModel> _cats = []; // 通道触后
    [VeloxProperty] private TimeOrderableCollection<PitchWheelChangeEventViewModel> _pwcs = []; // 音高弯音

    // 所有可能在[0,127]取值的控制器事件，完整存储Mdii解析结果
    [VeloxProperty] private TimeOrderableCollection<ControlChangeEventViewModel> _ctrls = [];
    // 分类，用于软件内部编辑支持
    [VeloxProperty] private TimeOrderableCollection<ControlChangeEventViewModel> _volumes = []; // 音量
    [VeloxProperty] private TimeOrderableCollection<ControlChangeEventViewModel> _pans = []; // 声相
    [VeloxProperty] private TimeOrderableCollection<ControlChangeEventViewModel> _sustains = []; // 延音踏板
    [VeloxProperty] private TimeOrderableCollection<ControlChangeEventViewModel> _expressions = []; // 表情/动态音量
    [VeloxProperty] private TimeOrderableCollection<ControlChangeEventViewModel> _modulations = []; // 调制轮

    [VeloxProperty] private bool _muted = false; // 是否被静音
    [VeloxProperty] private bool _selected = false; // 是否被选中

    partial void OnSelectedChanged(bool oldValue, bool newValue)
    {
        if (Parent is not null && Parent.CurrentTrack != this)
        {
            Parent.CurrentTrack = this;
        }

        foreach (var note in _notes)
        {
            note.IsHighlighted = newValue;
        }
    }

    public void Restore()
    {
        // 音符类
        Notes.Restore();

        // 通道触后
        Cats.Restore();

        // 音高弯音
        Pwcs.Restore();

        // 控制器类
        Volumes.Restore();
        Pans.Restore();
        Sustains.Restore();
        Expressions.Restore();
        Modulations.Restore();
    }

    public MidiTrackViewModel()
    {
        // 跟踪音符可见性
        _notes.ItemRestored += OnNoteRestored;
        _notes.ItemVirtualized += OnNoteVirtualized;

        // 音符类
        _notes.CollectionChanged += OnEventsChanged;

        // 通道触后
        _cats.CollectionChanged += OnEventsChanged;

        // 音高弯音
        _pwcs.CollectionChanged += OnEventsChanged;

        // 控制器类
        _volumes.CollectionChanged += OnEventsChanged;
        _pans.CollectionChanged += OnEventsChanged;
        _sustains.CollectionChanged += OnEventsChanged;
        _expressions.CollectionChanged += OnEventsChanged;
        _modulations.CollectionChanged += OnEventsChanged;

        _volumes.CollectionChanged += OnControlChangeEventsChanged;
        _pans.CollectionChanged += OnControlChangeEventsChanged;
        _sustains.CollectionChanged += OnControlChangeEventsChanged;
        _expressions.CollectionChanged += OnControlChangeEventsChanged;
        _modulations.CollectionChanged += OnControlChangeEventsChanged;
    }

    private void OnEventsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems != null)
                {
                    foreach (EventViewModel midiEvent in e.NewItems)
                    {
                        midiEvent.Parent = this;
                    }
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems != null)
                {
                    foreach (EventViewModel midiEvent in e.OldItems)
                    {
                        midiEvent.Parent = null;
                    }
                }
                break;
        }
    }

    private void OnControlChangeEventsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems != null)
                {
                    foreach (ControlChangeEventViewModel midiEvent in e.NewItems)
                    {
                        Ctrls.Add(midiEvent);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems != null)
                {
                    foreach (ControlChangeEventViewModel midiEvent in e.OldItems)
                    {
                        Ctrls.Remove(midiEvent);
                    }
                }
                break;
        }
    }

    private static void OnNoteRestored(NoteEventViewModel note)
    {
        note.IsVisible = true;
    }
    private static void OnNoteVirtualized(NoteEventViewModel note)
    {
        note.IsVisible = false;
    }
}