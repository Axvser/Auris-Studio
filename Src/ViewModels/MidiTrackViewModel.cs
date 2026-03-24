using Auris_Studio.Midi;
using Auris_Studio.ViewModels.ComponentModel;
using Auris_Studio.ViewModels.MidiEvents;
using NAudio.Midi;
using System.Collections.Specialized;
using System.Text;
using VeloxDev.Core.MVVM;

namespace Auris_Studio.ViewModels;

public partial class MidiTrackViewModel
{
    [VeloxProperty] private MidiEditorViewModel? _parent = null;

    [VeloxProperty] private string _name = "Acoustic Grand Piano"; // 音轨名称
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

    public MidiTrackViewModel()
    {
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

    partial void OnPatchChanged(Patch oldValue, Patch newValue)
    {
        string enumString = newValue.ToString();
        if (string.IsNullOrEmpty(enumString))
        {
            Name = string.Empty;
            return;
        }

        var result = new StringBuilder();
        for (int i = 0; i < enumString.Length; i++)
        {
            char currentChar = enumString[i];
            if (i > 0 && char.IsUpper(currentChar))
            {
                result.Append(' ');
            }
            result.Append(currentChar);
        }
        Name = result.ToString();
    }

    [VeloxCommand]
    private void ExecuteMidi(object? parameter)
    {
        if (parameter is not Tuple<long, MidiOut> tuple) return;
        var ends = Notes.QueryAtEnd(tuple.Item1);
        var starts = Notes.QueryAtStart(tuple.Item1);
        var volume = Volumes.FindFirstAtOrBefore(tuple.Item1);
        var pan = Pans.FindFirstAtOrBefore(tuple.Item1);
        var sustains = Sustains.FindFirstAtOrBefore(tuple.Item1);
        var expression = Expressions.FindFirstAtOrBefore(tuple.Item1);
        var modulation = Modulations.FindFirstAtOrBefore(tuple.Item1);
        foreach (var endNote in ends)
        {
            endNote.StopNoteCommand.Execute(tuple.Item2);
        }
        tuple.Item2.Send(MidiMessage.ChangePatch((int)Patch, Channel).RawData);
        if (volume is not null) tuple.Item2.Send(MidiMessage.ChangeControl((int)volume.MidiController, volume.Value, Channel).RawData);
        if (pan is not null) tuple.Item2.Send(MidiMessage.ChangeControl((int)pan.MidiController, pan.Value, Channel).RawData);
        if (sustains is not null) tuple.Item2.Send(MidiMessage.ChangeControl((int)sustains.MidiController, sustains.Value, Channel).RawData);
        if (expression is not null) tuple.Item2.Send(MidiMessage.ChangeControl((int)expression.MidiController, expression.Value, Channel).RawData);
        if (modulation is not null) tuple.Item2.Send(MidiMessage.ChangeControl((int)modulation.MidiController, modulation.Value, Channel).RawData);
        foreach (var startsNote in starts)
        {
            startsNote.StartNoteCommand.Execute(tuple.Item2);
        }
    }

    #region 音轨功能开关

    [VeloxCommand]
    private void TrackMuted() => Muted = !Muted;
    [VeloxCommand]
    private void TrackSelect()
    {
        if (Parent is not null)
        {
            if (Parent.CurrentSelectedTrack == this)
            {
                Selected = true;
            }
            else
            {
                Parent.CurrentSelectedTrack?.Selected = false;
                Parent.CurrentSelectedTrack = this;
                Selected = true;
            }
        }
    }

    #endregion

    #region 集合处理

    [VeloxCommand]
    private void Restore()
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

    #endregion
}