using Auris_Studio.Midi;
using Auris_Studio.ViewModels.ComponentModel;
using Auris_Studio.ViewModels.MidiEvents;
using NAudio.Midi;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using VeloxDev.Core.MVVM;

namespace Auris_Studio.ViewModels;

public partial class MidiTrackViewModel
{
    private static readonly Patch[] melodicPatches = Enum.GetValues<Patch>()
        .Where(patch => !patch.IsDrum(out _))
        .ToArray();

    private static readonly Patch[] drumPatches = Enum.GetValues<Patch>()
        .Where(patch => patch.IsDrum(out _))
        .ToArray();

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
    [VeloxProperty] private bool _solo = false; // 是否独奏
    [VeloxProperty] private bool _selected = false; // 是否被选中
    [JsonIgnore] public double HeaderHeight { get; internal set; } = 46;
    [JsonIgnore] public double EditorHeight { get; internal set; } = 96;
    [JsonIgnore] public double TrackSpacing { get; internal set; } = 36;

    [JsonIgnore]
    public double TrackHeight => HeaderHeight + EditorHeight + TrackSpacing;

    [JsonIgnore]
    public int Volume
    {
        get => GetPrimaryControlValue(Volumes, 127);
        set => SetPrimaryControlValue(MidiController.MainVolume, value);
    }

    [JsonIgnore]
    public int Pan
    {
        get => GetPrimaryControlValue(Pans, 64);
        set => SetPrimaryControlValue(MidiController.Pan, value);
    }

    [JsonIgnore]
    public bool IsPercussionChannel => Channel == 10;

    [JsonIgnore]
    public IReadOnlyList<Patch> AvailablePatches => IsPercussionChannel ? drumPatches : melodicPatches;

    [JsonIgnore]
    public string PatchDisplayName => GetPatchDisplayName(Patch);

    [JsonIgnore]
    public bool IsAudible => Parent?.IsTrackAudible(this) ?? !Muted;

    public bool IsSynchronizingControlCollections { get; internal set; }

    public MidiTrackViewModel()
    {
        // 音符类
        _notes.CollectionChanged += OnEventsChanged;

        // 通道触后
        _cats.CollectionChanged += OnEventsChanged;

        // 音高弯音
        _pwcs.CollectionChanged += OnEventsChanged;

        // 控制器类
        _ctrls.CollectionChanged += OnEventsChanged;
        _ctrls.CollectionChanged += OnRawControlChangeEventsChanged;
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
        Name = GetPatchDisplayName(newValue);
    }

    partial void OnChannelChanged(int oldValue, int newValue)
    {
        Patch = NormalizePatchForChannel(newValue, Patch);
    }

    partial void OnParentChanged(MidiEditorViewModel? oldValue, MidiEditorViewModel? newValue)
    {
        OnPropertyChanged(nameof(IsAudible));
    }

    partial void OnMutedChanged(bool oldValue, bool newValue)
    {
        OnPropertyChanged(nameof(IsAudible));
    }

    partial void OnSoloChanged(bool oldValue, bool newValue)
    {
        if (newValue && Muted)
        {
            Muted = false;
        }

        OnPropertyChanged(nameof(IsAudible));
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
        Patch.IsDrum(out int programNumber);
        tuple.Item2.Send(MidiMessage.ChangePatch(programNumber, Channel).RawData);
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
    private void TrackSolo()
    {
        if (Parent is not null)
        {
            Parent.SetTrackSoloState(this, !Solo);
            return;
        }

        Solo = !Solo;
        if (Solo)
        {
            Muted = false;
        }
    }

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

    internal void NotifyAudibilityChanged() => OnPropertyChanged(nameof(IsAudible));

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
        if (IsSynchronizingControlCollections) return;

        IsSynchronizingControlCollections = true;
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems != null)
                {
                    foreach (ControlChangeEventViewModel midiEvent in e.NewItems)
                    {
                        if (!Ctrls.Contains(midiEvent))
                        {
                            Ctrls.Add(midiEvent);
                        }
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
        IsSynchronizingControlCollections = false;
    }

    private void OnRawControlChangeEventsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (IsSynchronizingControlCollections) return;

        IsSynchronizingControlCollections = true;
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems is not null)
                {
                    foreach (ControlChangeEventViewModel midiEvent in e.NewItems)
                    {
                        var targetCollection = ResolveControlCollection(midiEvent.MidiController);
                        if (targetCollection is not null && !targetCollection.Contains(midiEvent))
                        {
                            targetCollection.Add(midiEvent);
                        }
                    }
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems is not null)
                {
                    foreach (ControlChangeEventViewModel midiEvent in e.OldItems)
                    {
                        var targetCollection = ResolveControlCollection(midiEvent.MidiController);
                        if (targetCollection is not null && targetCollection.Contains(midiEvent))
                        {
                            targetCollection.Remove(midiEvent);
                        }
                    }
                }
                break;
        }
        IsSynchronizingControlCollections = false;
    }

    private TimeOrderableCollection<ControlChangeEventViewModel>? ResolveControlCollection(MidiController midiController) => midiController switch
    {
        MidiController.MainVolume => Volumes,
        MidiController.Pan => Pans,
        MidiController.Sustain => Sustains,
        MidiController.Expression => Expressions,
        MidiController.Modulation => Modulations,
        _ => null,
    };

    private static int GetPrimaryControlValue(TimeOrderableCollection<ControlChangeEventViewModel> collection, int fallback)
        => collection.OrderBy(x => x.AbsoluteTime).FirstOrDefault()?.Value ?? fallback;

    public static Patch NormalizePatchForChannel(int channel, Patch patch)
    {
        if (channel == 10)
        {
            if (patch.IsDrum(out _))
            {
                return patch;
            }

            int drumValue = (int)patch + 128;
            if (Enum.IsDefined(typeof(Patch), drumValue))
            {
                return (Patch)drumValue;
            }

            return Patch.DrumAcousticBassDrum;
        }

        if (patch.IsDrum(out int programNumber) && Enum.IsDefined(typeof(Patch), programNumber))
        {
            return (Patch)programNumber;
        }

        return patch;
    }

    public static string GetPatchDisplayName(Patch patch)
    {
        string enumString = patch.ToString();
        if (string.IsNullOrEmpty(enumString))
        {
            return string.Empty;
        }

        if (enumString.StartsWith("Drum", StringComparison.Ordinal))
        {
            enumString = enumString[4..];
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

        return result.ToString();
    }

    private void SetPrimaryControlValue(MidiController midiController, int value)
    {
        int clamped = Math.Clamp(value, 0, 127);
        var targetCollection = ResolveControlCollection(midiController);
        var existing = targetCollection?
            .OrderBy(x => x.AbsoluteTime)
            .FirstOrDefault();

        if (existing is not null)
        {
            existing.Value = clamped;
            return;
        }

        var controlVm = new ControlChangeEventViewModel
        {
            AbsoluteTime = 0,
            DeltaTime = 0,
            MidiController = midiController,
            Value = clamped,
        };
        Ctrls.Add(controlVm);
    }

    #endregion
}