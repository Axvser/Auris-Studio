using Auris_Studio.Midi;
using Auris_Studio.ViewModels.ComponentModel;
using Auris_Studio.ViewModels.MidiEvents;
using Auris_Studio.ViewModels.Workflows;
using NAudio.Midi;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using VeloxDev.Core.MVVM;

namespace Auris_Studio.ViewModels;

public partial class MidiEditorViewModel : IMidiFormatable
{
    // 分辨率
    [VeloxProperty] private int _pPQN = 480;

    // 音轨
    [VeloxProperty] private ObservableCollection<MidiTrackViewModel> _tracks = [];

    // 元数据
    [VeloxProperty] private TimeOrderableCollection<TempoEventViewModel> _tempos = [];
    [VeloxProperty] private TimeOrderableCollection<TimeSignatureEventViewModel> _tss = [];
    [VeloxProperty] private TimeOrderableCollection<KeySignatureEventViewModel> _kss = [];
    [VeloxProperty] private TimeOrderableCollection<SmpteOffsetEventViewModel> _offsets = [];
    [VeloxProperty] private TimeOrderableCollection<SysexEventViewModel> _syss = [];
    [VeloxProperty] private TimeOrderableCollection<SequencerSpecificEventViewModel> _sss = [];

    // 文本总集
    [VeloxProperty] private TimeOrderableCollection<TextEventViewModel> _texts = [];
    // 分集
    [VeloxProperty] private TimeOrderableCollection<TextEventViewModel> _lyrics = [];
    [VeloxProperty] private TimeOrderableCollection<TextEventViewModel> _markers = [];
    [VeloxProperty] private TimeOrderableCollection<TextEventViewModel> _cues = [];

    // 编辑音符时长所采用的对齐策略
    [VeloxProperty] private Alignment _alignment = Alignment.EighthNote;

    // AI处理管道
    [VeloxProperty] private ObservableCollection<AIPipelineViewModel> _aIPipelines = [];
    [VeloxProperty] private AIPipelineViewModel? _currentAIPipeline;

    // [数据层] 计算属性
    [VeloxProperty] public partial long NowTime { get; internal set; } // 当前时间
    [VeloxProperty] public partial long MaxTime { get; internal set; } // 最大时间
    [VeloxProperty] public partial int BPM { get; internal set; } // 当前速度
    [VeloxProperty] public partial int Numerator { get; internal set; } // 拍号分子
    [VeloxProperty] public partial int Denominator { get; internal set; } // 拍号分母（用户可见，不是协议中的原值）
    [VeloxProperty] public partial double TickTime { get; private set; } // 每Tick物理时间
    [VeloxProperty] public partial int SharpsFlats { get; internal set; } // 升降号
    [VeloxProperty] public partial int MajorMinor { get; internal set; } // 调号
    [VeloxProperty] public partial string Lyric { get; internal set; } // 歌词
    [VeloxProperty] public partial MidiTrackViewModel? CurrentSelectedTrack { get; internal set; } // 当前选中的音轨
    [VeloxProperty] public partial MidiTrackViewModel? CurrentSoloTrack { get; internal set; } // 当前独奏的音轨
    [VeloxProperty] public partial HashSet<MidiTrackViewModel> ActiveTracks { get; internal set; } // 当前发声的音轨
    [VeloxProperty] public partial TimeOrderableCollection<NoteEventViewModel> CurrentNotes { get; internal set; } // 当前可见的音符

    // [视图层] 计算属性
    [VeloxProperty] public partial TimeSpan CurrentTimeSpan { get; internal set; } // 当前时分秒
    [VeloxProperty] public partial double CanvasWidth { get; internal set; } // [共享]画布宽度
    [VeloxProperty] public partial double NotesCanvasHeight { get; internal set; } // [音符]画布高度
    [VeloxProperty] public partial double ControlCanvasHeight { get; internal set; } // [控制器]画布高度
    [VeloxProperty] public partial double ViewportLeft { get; internal set; } // [可见区域]距离父容器左边界的距离
    [VeloxProperty] public partial double ViewportTop { get; internal set; } // [可见区域]距离父容器上边界的距离
    [VeloxProperty] public partial double ViewportWidth { get; internal set; } // [可见区域]宽度
    [VeloxProperty] public partial double ViewportHeight { get; internal set; } // [可见区域]高度
    [VeloxProperty] public partial long ViewportStartTime { get; internal set; } // [可见区域]起始时间
    [VeloxProperty] public partial long ViewportEndTime { get; internal set; } // [可见区域]结束时间
    [VeloxProperty] public partial double WidthPerQuarterNote { get; internal set; } // 每四分音符长度
    [VeloxProperty] public partial double WidthPerTick { get; internal set; } // 每Tick长度
    // 卷帘门绘制
    [VeloxProperty] public partial double HeightPerLine { get; internal set; } // 每行高度
    [VeloxProperty] public partial double BlackNoteHeight { get; internal set; } // 黑键高度（53个=10*5+3）
    [VeloxProperty] public partial double WhiteNoteHeight { get; internal set; } // 小白键高度（44个=10*4+4）
    [VeloxProperty] public partial double HugeWhiteNoteHeight { get; internal set; } // 大白键高度（31个=10*3+1）
    [VeloxProperty] public partial ObservableCollection<PianoKeyViewModel> PianoKeys { get; internal set; } // 所有按键
    [VeloxProperty] public partial ObservableCollection<VisualTrackViewModel> VisualTracks { get; internal set; } // 视觉编辑轨
    [VeloxProperty] public partial Dictionary<int, PianoKeyViewModel> PianoKeysMap { get; internal set; } // 音符映射钢琴键
    // 编辑音符所需要的
    [VeloxProperty] public partial NoteSpatialGridHashMap NoteSpatialIndex { get; internal set; } // 哈希空间索引
    [VeloxProperty] public partial NoteEventViewModel? CapturedNote { get; internal set; } // 被捕获的音符
    [VeloxProperty] public partial double PointerLeft { get; internal set; } // 鼠标对于Canvas左侧
    [VeloxProperty] public partial double PointerTop { get; internal set; } // 鼠标对于Canvas顶侧
    [VeloxProperty] public partial int PointerNote { get; internal set; } // 鼠标所处音高
    [VeloxProperty] public partial int PointerOperation { get; internal set; } // 1 → AbsTime / 2 → DelTime / 3 → 整体移动

    public MidiEditorViewModel()
    {
        CurrentNotes = [];
        ActiveTracks = [];
        VisualTracks = [];
        PianoKeysMap = [];
        PianoKeys = [];
        NoteSpatialIndex = new();

        BPM = 120;
        Numerator = 4;
        Denominator = 4;
        SharpsFlats = 0;
        MajorMinor = 0;
        Lyric = string.Empty;
        WidthPerQuarterNote = 40;
        HeightPerLine = 10;
        CanvasWidth = 0;
        ControlCanvasHeight = 100;

        Tracks.CollectionChanged += OnTracksChanged;
        Texts.CollectionChanged += OnTextsChanged;
        CurrentNotes.CollectionChanged += OnSpatialndexNotesChanged;

        Tempos.CollectionChanged += OnTempoEventsChanged;
        Tss.CollectionChanged += OnTimeSignatureEventsChanged;
        Kss.CollectionChanged += OnKeySignatureEventsChanged;
        Lyrics.CollectionChanged += OnLyricsEventsChanged;

        UpdateTickTime();
        UpdateNotesCanvasHeight();
        LoadPianoKeys();
        LoadVisualTracks();
        UpdatePianoKeys();
        UpdateVisualTracks();
    }

    #region [Dispatcher] 属性回调

    partial void OnMaxTimeChanged(long oldValue, long newValue)
    {
        CanvasWidth = WidthPerTick * newValue;
    }

    partial void OnBPMChanged(int oldValue, int newValue) => UpdateTickTime();

    partial void OnPPQNChanged(int oldValue, int newValue)
    {
        UpdateTickTime();
        UpdateWidthPerTick();
    }

    partial void OnNowTimeChanged(long oldValue, long newValue)
    {
        UpdateCurrentLyric(newValue);
        UpdateCurrentTempo(newValue);
        UpdateCurrentTimeSignature(newValue);
        UpdateCurrentKeySignature(newValue);
        CurrentTimeSpan = TimeSpan.FromMilliseconds(newValue * TickTime);
    }

    partial void OnWidthPerTickChanged(double oldValue, double newValue)
    {
        CanvasWidth = MaxTime * newValue;
        foreach (var note in CurrentNotes)
        {
            UpdateNote(note);
        }
    }

    partial void OnWidthPerQuarterNoteChanged(double oldValue, double newValue)
    {
        UpdateWidthPerTick();
    }

    partial void OnHeightPerLineChanged(double oldValue, double newValue)
    {
        BlackNoteHeight = newValue * 2 * 0.618;
        WhiteNoteHeight = newValue + BlackNoteHeight / 2;
        HugeWhiteNoteHeight = newValue + BlackNoteHeight;
        UpdateNotesCanvasHeight();
        UpdatePianoKeys();
        UpdateVisualTracks();
        UpdateCurrentNotes();
    }

    partial void OnViewportStartTimeChanged(long oldValue, long newValue)
    {
        CurrentNotes.Virtualize(newValue, ViewportEndTime);
    }

    partial void OnViewportEndTimeChanged(long oldValue, long newValue)
    {
        CurrentNotes.Virtualize(ViewportStartTime, newValue);
    }

    partial void OnViewportLeftChanged(double oldValue, double newValue)
    {
        UpdateViewportTimeRange();
        ExtendMaxTime();
    }

    partial void OnViewportWidthChanged(double oldValue, double newValue)
    {
        UpdateViewportTimeRange();
        ExtendMaxTime();
    }

    partial void OnPointerLeftChanged(double oldValue, double newValue)
    {
        if (CapturedNote == null || PointerOperation == 0) return;

        long pointerTick = (long)(newValue / WidthPerTick);
        long minNoteLength = GetMinNoteLength(); // 使用统一的最小长度

        switch (PointerOperation)
        {
            case 1: // 调整起始时间
                HandleAbsoluteTimeAdjustment(pointerTick, minNoteLength);
                break;

            case 2: // 调整持续时间
                HandleDeltaTimeAdjustment(pointerTick, minNoteLength);
                break;

            case 3: // 整体移动
                HandleNoteMove(pointerTick);
                break;
        }

        if (CapturedNote != null)
        {
            UpdateNote(CapturedNote);
        }
    }

    partial void OnPointerTopChanged(double oldValue, double newValue)
    {
        PointerNote = FindPitchByTop(NotesCanvasHeight - newValue);
    }

    partial void OnPointerNoteChanged(int oldValue, int newValue)
    {
        if (CapturedNote is not null &&
            PointerOperation == 3)
        {
            CapturedNote.Note = (Pitch)newValue;
        }
    }

    #endregion

    #region 画布计算

    [VeloxCommand]
    private void HitTest()
    {
        // 0. 安全检查
        if (PointerLeft < 0 || PointerTop < 0) return;

        // 1. 使用空间索引进行高性能点查询
        var hitNote = NoteSpatialIndex.PointQuery(PointerLeft, NotesCanvasHeight - PointerTop);

        if (hitNote != null)
        {
            return;
        }
        else
        {
            // 3.1 没有命中任何音符
            CapturedNote = null;
            PointerOperation = 0;

            // 3.2 根据PointerTop计算当前音高
            int currentNotePitch = FindPitchByTop(NotesCanvasHeight - PointerTop);
            PointerNote = currentNotePitch;

            // 3.3 如果有选中的音轨，创建新音符
            if (CurrentSelectedTrack != null)
            {
                CreateNoteAtPosition((long)(PointerLeft / WidthPerTick), currentNotePitch);
            }
        }
    }

    private void CreateNoteAtPosition(long absoluteTime, int pitch)
    {
        if (CurrentSelectedTrack == null) return;

        // 1. 对齐到最近的对齐点
        long alignedTime = AlignTimeForward(absoluteTime);

        // 2. 获取最小音符长度
        long minNoteLength = GetMinNoteLength();

        // 3. 创建默认长度的音符
        int defaultDeltaTime = (int)Math.Max(GetAlignmentStep(), minNoteLength);
        defaultDeltaTime = (int)AlignTimeForward(defaultDeltaTime);

        // 4. 创建MIDI事件
        var noteOnEvent = new NoteOnEvent(
            absoluteTime: alignedTime,
            channel: CurrentSelectedTrack.Channel,
            noteNumber: pitch,
            velocity: 100, // 默认力度
            duration: defaultDeltaTime);

        var noteOffEvent = new NoteEvent(
            absoluteTime: alignedTime + defaultDeltaTime,
            channel: CurrentSelectedTrack.Channel,
            commandCode: MidiCommandCode.NoteOff,
            noteNumber: pitch,
            velocity: 0);

        noteOnEvent.OffEvent = noteOffEvent;

        // 5. 创建视图模型
        var noteVm = new NoteEventViewModel();
        noteVm.Read(Tuple.Create(noteOnEvent, noteOffEvent));

        // 6. 设置父轨道
        noteVm.Parent = CurrentSelectedTrack;

        // 7. 添加到轨道
        CurrentSelectedTrack.Notes.Add(noteVm);

        // 8. 自动选中新创建的音符
        CapturedNote = noteVm;

        // 9. 根据点击位置确定操作类型
        PointerOperation = 3;

        UpdateNote(noteVm);
    }

    public long GetAlignmentStep()
    {
        // 1. 提取组合后的对齐值
        Alignment alignment = this._alignment;
        long ppqn = this._pPQN;
        int denominator = this.Denominator;

        // 2. 分离并验证基础音符时值部分
        Alignment noteValue = alignment & Alignment.NoteValueMask;
        if (noteValue == 0)
        {
            // 没有选择有效的音符时值，返回一个安全默认值（如八分音符）
            return ppqn / 2;
        }

        // 3. 根据音符时值计算基础Tick数
        long baseTicks = noteValue switch
        {
            Alignment.DoubleWholeNote => ppqn * 8,
            Alignment.WholeNote => ppqn * 4,
            Alignment.HalfNote => ppqn * 2,
            Alignment.QuarterNote => ppqn,
            Alignment.EighthNote => ppqn / 2,
            Alignment.SixteenthNote => ppqn / 4,
            Alignment.ThirtySecondNote => ppqn / 8,
            Alignment.SixtyFourthNote => ppqn / 16,
            Alignment.OneTwentyEighthNote => ppqn / 32,
            _ => ppqn / 2, // 默认八分音符
        };
        // 确保基础Tick数为正整数
        if (baseTicks <= 0) baseTicks = ppqn / 2;

        // 4. 应用附点系数
        double dotMultiplier = 1.0;
        Alignment modifier = alignment & Alignment.ModifierMask;
        switch (modifier)
        {
            case Alignment.Dot:
                // 单附点：时长增加1/2
                dotMultiplier = 1.5;
                break;
            case Alignment.DoubleDot:
                // 双附点：时长增加1/2 + 1/4
                dotMultiplier = 1.75;
                break;
        }

        // 5. 应用连音系数
        double tupletMultiplier = 1.0;
        Alignment tuplet = alignment & Alignment.TupletMask;
        switch (tuplet)
        {
            case Alignment.Triplet:
                // 三连音：3个音符占原本2个音符的时长，所以每个音符时长为 2/3
                tupletMultiplier = 2.0 / 3.0;
                break;
            case Alignment.Quintuplet:
                // 五连音：5个音符占原本4个音符的时长，所以每个音符时长为 4/5
                tupletMultiplier = 4.0 / 5.0;
                break;
            case Alignment.Septuplet:
                // 七连音：7个音符占原本4个音符的时长，所以每个音符时长为 4/7
                tupletMultiplier = 4.0 / 7.0;
                break;
        }

        // 6. 计算最终Tick数
        double finalTicksDouble = baseTicks * dotMultiplier * tupletMultiplier;
        long finalTicks = (long)Math.Round(finalTicksDouble);

        // 7. 新增：基于拍号的最小步长限制
        long minStepByTimeSignature = CalculateMinStepByTimeSignature(ppqn, denominator);
        if (finalTicks < minStepByTimeSignature)
        {
            finalTicks = minStepByTimeSignature;
        }

        // 8. 最终验证，确保结果为合理正数
        if (finalTicks <= 0)
        {
            finalTicks = ppqn / 2; // 兜底为八分音符
        }

        return finalTicks;
    }

    private static long CalculateMinStepByTimeSignature(long ppqn, int denominator)
    {
        long beatTicks = ppqn * 4 / denominator;
        long minStep = beatTicks / 2;
        return Math.Max(1, minStep);
    }

    public long AlignTimeForward(long time)
    {
        long step = GetAlignmentStep();
        if (step <= 0) return time;
        // 向上取整
        return ((time + step - 1) / step) * step;
    }

    public long AlignTimeBackward(long time)
    {
        long step = GetAlignmentStep();
        if (step <= 0) return time;
        // 向下取整
        return (time / step) * step;
    }

    private void HandleNoteMove(long pointerTick)
    {
        if (CapturedNote == null) return;

        // 计算鼠标指针相对于音符原中心点的偏移
        long originalCenter = CapturedNote.AbsoluteTime + CapturedNote.DeltaTime / 2;
        long centerOffset = pointerTick - originalCenter;

        // 优先对齐起始时间
        long targetAbsoluteTime = AlignTimeForward(CapturedNote.AbsoluteTime + centerOffset);

        // 确保新起始时间不为负数
        if (targetAbsoluteTime < 0)
        {
            targetAbsoluteTime = 0;
        }

        // 根据对齐后的起始时间，计算结束时间
        long targetEndTime = targetAbsoluteTime + CapturedNote.DeltaTime;

        // 重新计算最终的时长
        long newDeltaTime = targetEndTime - targetAbsoluteTime;

        // 应用更新
        CapturedNote.AbsoluteTime = targetAbsoluteTime;
        CapturedNote.DeltaTime = (int)newDeltaTime;
    }

    private void HandleAbsoluteTimeAdjustment(long pointerTick, long minNoteLength)
    {
        if (CapturedNote == null) return;

        long alignedTime = AlignTimeForward(pointerTick);
        long maxValidStartTime = AlignTimeBackward(
            CapturedNote.AbsoluteTime + CapturedNote.DeltaTime - minNoteLength
        );

        if (alignedTime <= maxValidStartTime)
        {
            long originalEndTime = CapturedNote.AbsoluteTime + CapturedNote.DeltaTime;
            CapturedNote.AbsoluteTime = alignedTime;
            CapturedNote.DeltaTime = (int)(originalEndTime - alignedTime);
        }
        else
        {
            // 自动对齐到最大允许起始时间
            CapturedNote.AbsoluteTime = maxValidStartTime;
            CapturedNote.DeltaTime = (int)((CapturedNote.AbsoluteTime + CapturedNote.DeltaTime) - maxValidStartTime);
        }
    }

    private void HandleDeltaTimeAdjustment(long pointerTick, long minNoteLength)
    {
        if (CapturedNote == null) return;

        long alignedEndTime = AlignTimeForward(pointerTick);
        long minValidEndTime = AlignTimeForward(CapturedNote.AbsoluteTime + minNoteLength);

        if (alignedEndTime >= minValidEndTime)
        {
            CapturedNote.DeltaTime = (int)(alignedEndTime - CapturedNote.AbsoluteTime);
        }
        else
        {
            CapturedNote.DeltaTime = (int)(minValidEndTime - CapturedNote.AbsoluteTime);
        }
    }

    public long GetMinNoteLength()
    {
        long alignmentStep = GetAlignmentStep();
        long timeSignatureMinStep = CalculateMinStepByTimeSignature(PPQN, Denominator);
        return Math.Max(alignmentStep, timeSignatureMinStep);
    }

    private int FindPitchByTop(double pointerTop)
    {
        if (PianoKeys == null || PianoKeys.Count == 0)
            return 0; // 返回默认值

        // 在钢琴键中查找包含pointerTop的键
        foreach (var pianoKey in PianoKeys)
        {
            double keyTop = pianoKey.Bottom;
            double keyBottom = keyTop + pianoKey.Height;

            if (pointerTop >= keyTop && pointerTop < keyBottom)
            {
                return pianoKey.Note;
            }
        }

        // 如果没有找到，返回最近的音高
        return FindNearestPitch(pointerTop);
    }

    private int FindNearestPitch(double pointerTop)
    {
        if (PianoKeys == null || PianoKeys.Count == 0)
            return 60; // 返回中央C（C4）

        PianoKeyViewModel? nearestKey = null;
        double minDistance = double.MaxValue;

        foreach (var pianoKey in PianoKeys)
        {
            double keyCenter = pianoKey.Bottom + pianoKey.Height / 2;
            double distance = Math.Abs(pointerTop - keyCenter);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearestKey = pianoKey;
            }
        }

        return nearestKey?.Note ?? 60;
    }

    private void ExtendMaxTime()
    {
        if (MaxTime >= ViewportEndTime)
        {
            if (MaxTime - ViewportEndTime < PPQN)
            {
                MaxTime += PPQN;
            }
        }
        else
        {
            MaxTime += (ViewportEndTime - MaxTime) + PPQN;
        }
    }

    #endregion

    #region [Helper] 数据更新

    private void LoadPianoKeys()
    {
        // 批次创建，用于初始化
        for (int i = (int)Pitch.C_minus1; i <= (int)Pitch.G9; i++)
        {
            var key = new PianoKeyViewModel() { Note = i };
            PianoKeys.Add(key);
            PianoKeysMap.Add(i, key);
        }
    }

    private void UpdatePianoKeys()
    {
        double currentBottom = 0.0;
        // 1. 首先更新所有白键的位置和高度
        foreach (var key in PianoKeys)
        {
            if (key.Type == PianoKeyType.White)
            {
                double height = key.Huge ? HugeWhiteNoteHeight : WhiteNoteHeight;
                key.Height = height;
                key.Bottom = currentBottom;
                currentBottom += height;
            }
        }

        // 2. 然后更新黑键的位置和高度
        foreach (var key in PianoKeys)
        {
            if (key.Type == PianoKeyType.Black)
            {
                key.Height = BlackNoteHeight;
                int leftWhiteNote = key.Note - 1;
                int rightWhiteNote = key.Note + 1;

                // 查找左侧和右侧的白键
                var leftWhiteKey = PianoKeys.FirstOrDefault(k => k.Type == PianoKeyType.White && k.Note == leftWhiteNote);
                var rightWhiteKey = PianoKeys.FirstOrDefault(k => k.Type == PianoKeyType.White && k.Note == rightWhiteNote);

                if (leftWhiteKey != null && rightWhiteKey != null)
                {
                    double leftWhiteKeyBottom = leftWhiteKey.Bottom + leftWhiteKey.Height;
                    double rightWhiteKeyBottom = rightWhiteKey.Bottom;

                    double midPoint = (leftWhiteKeyBottom + rightWhiteKeyBottom) / 2.0;
                    key.Bottom = midPoint - (key.Height / 2.0);
                }
                else
                {
                    var baseWhiteKey = PianoKeys.FirstOrDefault(k => k.Type == PianoKeyType.White && k.Note == leftWhiteNote);
                    if (baseWhiteKey != null)
                    {
                        double offsetRatio = 0.5;
                        key.Bottom = baseWhiteKey.Bottom + (baseWhiteKey.Height * offsetRatio);
                    }
                    else
                    {
                        key.Bottom = 0;
                    }
                }
            }
        }
    }

    private void LoadVisualTracks()
    {
        // 创建与钢琴键对应的视觉轨道
        for (int i = (int)Pitch.C_minus1; i <= (int)Pitch.G9; i++)
        {
            var visual = new VisualTrackViewModel() { Note = i };
            VisualTracks.Add(visual);
        }
    }

    private void UpdateVisualTracks()
    {
        if (VisualTracks == null || PianoKeys == null || VisualTracks.Count != PianoKeys.Count)
        {
            return;
        }

        for (int i = 0; i < PianoKeys.Count; i++)
        {
            var pianoKey = PianoKeys[i];
            var visualTrack = VisualTracks[i];

            // 同步Note属性会触发OnNoteChanged，从而自动设置Type和Huge
            if (visualTrack.Note != pianoKey.Note)
            {
                visualTrack.Note = pianoKey.Note;
            }
            // 直接复制布局属性
            visualTrack.Bottom = pianoKey.Bottom;
            visualTrack.Height = pianoKey.Height;
        }
    }

    private void UpdateNotesCanvasHeight()
    {
        NotesCanvasHeight = (44 * WhiteNoteHeight) + (31 * HugeWhiteNoteHeight);
        UpdateCurrentNotes();
    }

    private void UpdateCurrentNotes()
    {
        CurrentNotes.Virtualize(0, MaxTime);
        foreach (var note in CurrentNotes)
        {
            UpdateNote(note);
        }
    }

    internal void UpdateNote(NoteEventViewModel noteEventVm)
    {
        if (noteEventVm == null || PianoKeysMap == null) return;

        // 计算新的位置
        double newLeft = noteEventVm.AbsoluteTime * WidthPerTick;
        double newWidth = noteEventVm.DeltaTime * WidthPerTick;

        // 获取音高对应的钢琴键
        int targetNoteNumber = (int)noteEventVm.Note;

        if (PianoKeysMap.TryGetValue(targetNoteNumber, out var pianoKey))
        {
            bool hasDownBlack = false;
            bool hasUpBlack = false;
            if (PianoKeysMap.TryGetValue(targetNoteNumber - 1, out var upKey) && upKey.Type == PianoKeyType.Black)
            {
                hasDownBlack = true;
            }
            if (PianoKeysMap.TryGetValue(targetNoteNumber + 1, out var downKey) && downKey.Type == PianoKeyType.Black)
            {
                hasUpBlack = true;
            }

            double newHeight = (hasDownBlack, hasUpBlack, pianoKey.Type) switch
            {
                (true, true, PianoKeyType.White) => pianoKey.Height - BlackNoteHeight,
                (true, false, PianoKeyType.White) => pianoKey.Height - BlackNoteHeight / 2,
                (false, true, PianoKeyType.White) => pianoKey.Height - BlackNoteHeight / 2,
                (_, _, PianoKeyType.Black) => pianoKey.Height,
                _ => 0
            };

            double newBottom = (hasDownBlack, pianoKey.Type) switch
            {
                (true, PianoKeyType.White) => pianoKey.Bottom + BlackNoteHeight / 2,
                _ => pianoKey.Bottom
            };

            // 更新音符的UI属性
            // NoteEventViewModel的Left/Bottom/Width/Height属性变化会自动触发空间索引更新
            noteEventVm.Left = newLeft;
            noteEventVm.Bottom = newBottom;
            noteEventVm.Width = newWidth;
            noteEventVm.Height = newHeight;
        }
    }

    private void UpdateWidthPerTick()
    {
        WidthPerTick = WidthPerQuarterNote / PPQN;
    }

    private void UpdateMaxTime()
    {
        long calculatedMaxTime = 0;

        foreach (var track in Tracks)
        {
            var trackNotes = track?.Notes;
            if (trackNotes != null && trackNotes.MaxTime > calculatedMaxTime)
            {
                calculatedMaxTime = trackNotes.MaxTime;
            }
        }

        this.MaxTime = calculatedMaxTime;
    }

    private void UpdateTickTime() => TickTime = (60000.0d / BPM) / PPQN;

    private void UpdateCurrentLyric(long currentTime)
    {
        var currentLyric = Lyrics
            .Where(t => t.AbsoluteTime <= currentTime)
            .OrderByDescending(t => t.AbsoluteTime)
            .FirstOrDefault();

        if (currentLyric != null)
        {
            Lyric = currentLyric.Text;
        }
    }

    private void UpdateCurrentTempo(long currentTime)
    {
        var currentTempo = Tempos
            .Where(t => t.AbsoluteTime <= currentTime)
            .OrderByDescending(t => t.AbsoluteTime)
            .FirstOrDefault();

        if (currentTempo != null)
        {
            BPM = currentTempo.BPM;
        }
    }

    private void UpdateCurrentTimeSignature(long currentTime)
    {
        var currentTimeSignature = Tss
            .Where(t => t.AbsoluteTime <= currentTime)
            .OrderByDescending(t => t.AbsoluteTime)
            .FirstOrDefault();

        if (currentTimeSignature != null)
        {
            Numerator = currentTimeSignature.Numerator;
            Denominator = currentTimeSignature.Denominator;
        }
    }

    private void UpdateCurrentKeySignature(long currentTime)
    {
        var currentKeySignature = Kss
            .Where(k => k.AbsoluteTime <= currentTime)
            .OrderByDescending(k => k.AbsoluteTime)
            .FirstOrDefault();

        if (currentKeySignature != null)
        {
            SharpsFlats = currentKeySignature.SharpsFlats;
            MajorMinor = currentKeySignature.MajorMinor;
        }
    }

    private void UpdateViewportTimeRange()
    {
        ViewportStartTime = (long)(ViewportLeft / WidthPerTick);
        ViewportEndTime = (long)((ViewportLeft + ViewportWidth) / WidthPerTick);
    }

    #endregion

    #region [Helper] 事件管理

    private void OnTracksChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems is not null)
                {
                    foreach (MidiTrackViewModel track in e.NewItems)
                    {
                        track.Parent = this;
                        track.Notes.PropertyChanged += OnTrackMaxTimeChanged;
                        track.Notes.VisibleItems.CollectionChanged += OnNoteCollectionChanged;
                        UpdateMaxTime();
                        foreach (var note in track.Notes)
                        {
                            CurrentNotes.Add(note);
                            NoteSpatialIndex.Insert(note);
                        }
                        UpdateCurrentNotes();
                    }
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems is not null)
                {
                    foreach (MidiTrackViewModel track in e.OldItems)
                    {
                        track.Parent = null;
                        track.Notes.PropertyChanged -= OnTrackMaxTimeChanged;
                        track.Notes.VisibleItems.CollectionChanged -= OnNoteCollectionChanged;
                        track.RestoreCommand.Execute(null);
                        foreach (var note in track.Notes)
                        {
                            CurrentNotes.Remove(note);
                            NoteSpatialIndex.Remove(note);
                        }
                    }
                }
                break;
        }
    }

    private void OnNoteCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems is not null)
                {
                    foreach (NoteEventViewModel note in e.NewItems)
                    {
                        CurrentNotes.Add(note);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems is not null)
                {
                    foreach (NoteEventViewModel note in e.OldItems)
                    {
                        CurrentNotes.Remove(note);
                    }
                }
                break;
        }
    }

    private void OnSpatialndexNotesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems is not null)
                {
                    foreach (NoteEventViewModel note in e.NewItems)
                    {
                        NoteSpatialIndex.Insert(note);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems is not null)
                {
                    foreach (NoteEventViewModel note in e.OldItems)
                    {
                        NoteSpatialIndex.Remove(note);
                    }
                }
                break;
        }
    }

    private void OnTrackMaxTimeChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not TimeOrderableCollection<NoteEventViewModel> track) return;

        if (e.PropertyName != nameof(track.MaxTime)) return;

        // 对于编辑器而言，最大时长可以只增不减，格式互转时会依据事件集合动态计算而不会用到MaxTime
        if (track.MaxTime > this.MaxTime) this.MaxTime = track.MaxTime;
    }

    private void OnTextsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems is not null)
                {
                    foreach (TextEventViewModel textEvent in e.NewItems)
                    {
                        switch (textEvent.MetaEventType)
                        {
                            case MetaEventType.Lyric:
                                Lyrics.Add(textEvent);
                                break;
                            case MetaEventType.Marker:
                                Markers.Add(textEvent);
                                break;
                            case MetaEventType.CuePoint:
                                Cues.Add(textEvent);
                                break;
                        }
                    }
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems is not null)
                {
                    foreach (TextEventViewModel textEvent in e.OldItems)
                    {
                        switch (textEvent.MetaEventType)
                        {
                            case MetaEventType.Lyric:
                                Lyrics.Remove(textEvent);
                                break;
                            case MetaEventType.Marker:
                                Markers.Remove(textEvent);
                                break;
                            case MetaEventType.CuePoint:
                                Cues.Remove(textEvent);
                                break;
                        }
                    }
                }
                break;
        }
    }

    private void OnLyricsEventsChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateCurrentLyric(NowTime);

    private void OnTempoEventsChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateCurrentTempo(NowTime);

    private void OnTimeSignatureEventsChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateCurrentTimeSignature(NowTime);

    private void OnKeySignatureEventsChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateCurrentKeySignature(NowTime);

    #endregion

    #region 播放

    [VeloxCommand]
    private async Task Play(object? parameter, CancellationToken ct)
    {
        if (NowTime >= MaxTime) NowTime = 0;

        // 创建用于取消独立线程的CancellationTokenSource
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        // 使用ManualResetEventSlim同步线程状态
        using var playbackCompleted = new ManualResetEventSlim(false);
        var playbackException = (Exception?)null;

        // 在线程中运行播放逻辑
        await Task.Run(async () =>
        {
            using var midiOut = new MidiOut(0);
            var playbackStopwatch = Stopwatch.StartNew();
            long playbackStartTick = NowTime;

            try
            {
                while (!cts.IsCancellationRequested && NowTime < MaxTime)
                {
                    long currentLogicalTick = NowTime;

                    foreach (var track in Tracks)
                    {
                        if (track.Muted) continue;
                        track.ExecuteMidiCommand.Execute(Tuple.Create(currentLogicalTick, midiOut));
                    }

                    long nextLogicalTick = currentLogicalTick + 1;
                    double targetPhysicalTimeMs = (nextLogicalTick - playbackStartTick) * TickTime;
                    double elapsedPhysicalTimeMs = playbackStopwatch.Elapsed.TotalMilliseconds;
                    double waitTimeMs = targetPhysicalTimeMs - elapsedPhysicalTimeMs;

                    if (waitTimeMs > 0)
                    {
                        await MicrosecondDelay.Delay(waitTimeMs * 1000.0, cts.Token);
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        NowTime = nextLogicalTick;
                    });
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            catch (Exception ex)
            {
                playbackException = ex;
            }
            finally
            {
                foreach (var track in Tracks)
                {
                    midiOut.Send(new ControlChangeEvent(0, track.Channel, MidiController.AllNotesOff, 0).GetAsShortMessage());
                }
                playbackStopwatch.Stop();
                playbackCompleted.Set();
            }
        }, cts.Token);

        try
        {
            // 等待播放完成或取消
            await Task.Run(() => playbackCompleted.Wait(cts.Token), cts.Token);

            // 如果有异常则重新抛出
            if (playbackException != null)
            {
                throw new Exception("Playback thread failed", playbackException);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // 外部取消请求
            cts.Cancel();
            throw;
        }
    }

    [VeloxCommand]
    private void Stop()
    {
        PlayCommand.Clear();
    }

    [VeloxCommand]
    private async Task MoveTick(object? parameter)
    {
        await PlayCommand.LockAsync();
        await PlayCommand.ClearAsync();
        if (parameter is double value)
        {
            NowTime = (long)(value / WidthPerTick);
        }
        await PlayCommand.UnLockAsync();
    }

    #endregion

    #region [Helper] 格式互转

    [VeloxCommand]
    public void Read(object? parameter)
    {
        if (parameter is not MidiResult midiResult) return;

        midiResult.Optimize();

        Tracks.Clear();
        Tempos.Clear();
        Tss.Clear();
        Kss.Clear();
        Offsets.Clear();
        Syss.Clear();
        Sss.Clear();
        Texts.Clear();
        Lyrics.Clear();
        Markers.Clear();
        Cues.Clear();
        PPQN = midiResult.deltaTicksPerQuarterNote;

        // 1. 读取全局事件
        foreach (var tempo in midiResult.tempoEvs.OrderBy(e => e.AbsoluteTime))
        {
            var tempoVm = new TempoEventViewModel();
            tempoVm.Read(tempo);
            Tempos.Add(tempoVm);
        }
        if (midiResult.tempoEvs.Count > 0)
        {
            var firstTempo = midiResult.tempoEvs.OrderBy(e => e.AbsoluteTime).First();
            BPM = (int)(60000000.0 / firstTempo.MicrosecondsPerQuarterNote);
        }

        foreach (var ts in midiResult.tsEvs.OrderBy(e => e.AbsoluteTime))
        {
            var tsVm = new TimeSignatureEventViewModel();
            tsVm.Read(ts);
            Tss.Add(tsVm);
        }
        if (midiResult.tsEvs.Count > 0)
        {
            var firstTs = midiResult.tsEvs.OrderBy(e => e.AbsoluteTime).First();
            Numerator = firstTs.Numerator;
            Denominator = (int)Math.Pow(2, firstTs.Denominator);
        }

        foreach (var ks in midiResult.ksEvs.OrderBy(e => e.AbsoluteTime))
        {
            var ksVm = new KeySignatureEventViewModel();
            ksVm.Read(ks);
            Kss.Add(ksVm);
        }
        if (midiResult.ksEvs.Count > 0)
        {
            var firstKs = midiResult.ksEvs.OrderBy(e => e.AbsoluteTime).First();
            SharpsFlats = firstKs.SharpsFlats;
            MajorMinor = firstKs.MajorMinor;
        }

        foreach (var offset in midiResult.smpteOffsetEvs.OrderBy(e => e.AbsoluteTime))
        {
            var offsetVm = new SmpteOffsetEventViewModel();
            offsetVm.Read(offset);
            Offsets.Add(offsetVm);
        }

        foreach (var sysEx in midiResult.sysEvs.OrderBy(e => e.AbsoluteTime))
        {
            var sysExVm = new SysexEventViewModel();
            sysExVm.Read(sysEx);
            Syss.Add(sysExVm);
        }

        foreach (var seqEvent in midiResult.ssEvs.OrderBy(e => e.AbsoluteTime))
        {
            var seqVm = new SequencerSpecificEventViewModel();
            seqVm.Read(seqEvent);
            Sss.Add(seqVm);
        }

        // 2. 读取全局文本事件，并分发到分类集合
        var allTextEvents = midiResult.textEvs.OrderBy(e => e.AbsoluteTime).ToList();
        foreach (var textEvent in allTextEvents)
        {
            var textVm = new TextEventViewModel();
            textVm.Read(textEvent);
            Texts.Add(textVm);
        }

        // 3. 读取通道事件
        for (int channel = 1; channel <= 16; channel++)
        {
            // 检查通道是否有任何事件
            bool hasEvents = false;
            var eventCheckers = new Func<MidiResult, int, bool>[]
            {
                (r, c) => HasEventsInDict(r.noteOnEvs, c),
                (r, c) => HasEventsInDict(r.patchEvs, c),
                (r, c) => HasEventsInDict(r.cCEvs, c),
                (r, c) => HasEventsInDict(r.catEvs, c),
                (r, c) => HasEventsInDict(r.pwcEvs, c)
            };
            foreach (var checker in eventCheckers)
            {
                if (checker(midiResult, channel))
                {
                    hasEvents = true;
                    break;
                }
            }
            if (!hasEvents) continue;

            var trackVm = new MidiTrackViewModel
            {
                Parent = this,
                Channel = channel,
                Name = $"Track {channel}"
            };

            // 读取该通道最后一个音色事件，作为轨道默认音色
            Patch? lastPatch = null;
            if (midiResult.patchEvs.TryGetValue(channel, out var patchDict))
            {
                foreach (var kvp in patchDict)
                {
                    var lastEvent = kvp.Value.OrderByDescending(e => e.AbsoluteTime).FirstOrDefault();
                    if (lastEvent != null)
                    {
                        lastPatch = (Patch)lastEvent.Patch;
                    }
                }
            }
            trackVm.Patch = lastPatch ?? Patch.AcousticGrandPiano;

            // 读取通道触后事件
            ReadEventsFromDict(trackVm.Cats, midiResult.catEvs, channel, catEvent =>
            {
                var vm = new ChannelAfterTouchEventViewModel();
                vm.Read(catEvent);
                return vm;
            });

            // 读取音高弯音事件
            ReadEventsFromDict(trackVm.Pwcs, midiResult.pwcEvs, channel, pwcEvent =>
            {
                var vm = new PitchWheelChangeEventViewModel();
                vm.Read(pwcEvent);
                return vm;
            });

            // 读取音符事件
            if (midiResult.noteOnEvs.TryGetValue(channel, out var noteOnDict))
            {
                var allNoteOns = new List<NoteOnEvent>();
                foreach (var kvp in noteOnDict)
                {
                    allNoteOns.AddRange(kvp.Value);
                }
                // 假设优化器已配对，找到每个NoteOn的OffEvent
                foreach (var noteOn in allNoteOns.OrderBy(n => n.AbsoluteTime))
                {
                    var noteOff = noteOn.OffEvent;
                    if (noteOff != null)
                    {
                        var noteVm = new NoteEventViewModel();
                        noteVm.Read(Tuple.Create(noteOn, noteOff));
                        trackVm.Notes.Add(noteVm);
                    }
                }
            }

            // 读取控制器事件
            if (midiResult.cCEvs.TryGetValue(channel, out var ccDict))
            {
                var allCCEvents = new List<ControlChangeEvent>();
                foreach (var kvp in ccDict)
                {
                    allCCEvents.AddRange(kvp.Value);
                }
                foreach (var cc in allCCEvents.OrderBy(e => e.AbsoluteTime))
                {
                    var vm = new ControlChangeEventViewModel();
                    vm.Read(cc);
                    trackVm.Ctrls.Add(vm);

                    // 分发到分类集合
                    switch (cc.Controller)
                    {
                        case MidiController.MainVolume:
                            trackVm.Volumes.Add(vm);
                            break;
                        case MidiController.Pan:
                            trackVm.Pans.Add(vm);
                            break;
                        case MidiController.Sustain:
                            trackVm.Sustains.Add(vm);
                            break;
                        case MidiController.Expression:
                            trackVm.Expressions.Add(vm);
                            break;
                        case MidiController.Modulation:
                            trackVm.Modulations.Add(vm);
                            break;
                    }
                }
            }

            Tracks.Add(trackVm);
        }

        Tracks.FirstOrDefault()?.TrackSelectCommand.Execute(null);

        UpdateCurrentTempo(NowTime);
        UpdateCurrentTimeSignature(NowTime);
        UpdateCurrentKeySignature(NowTime);
    }

    [VeloxCommand]
    public void Write(object? parameter)
    {
        if (parameter is not MidiResult midiResult) return;

        midiResult.deltaTicksPerQuarterNote = PPQN;

        // 1. 清空现有结果
        midiResult.tempoEvs.Clear();
        midiResult.tsEvs.Clear();
        midiResult.ksEvs.Clear();
        midiResult.smpteOffsetEvs.Clear();
        midiResult.sysEvs.Clear();
        midiResult.ssEvs.Clear();
        midiResult.textEvs.Clear();

        for (int channel = 1; channel <= 16; channel++)
        {
            ClearDictForChannel(midiResult.patchEvs, channel);
            ClearDictForChannel(midiResult.catEvs, channel);
            ClearDictForChannel(midiResult.pwcEvs, channel);
            ClearDictForChannel(midiResult.cCEvs, channel);
            ClearDictForChannel(midiResult.noteOnEvs, channel);
            ClearDictForChannel(midiResult.noteOffEvs, channel);
        }

        // 2. 写入全局事件
        foreach (var tempoVm in Tempos)
        {
            var eventsList = new List<MidiEvent>();
            tempoVm.Write(eventsList);
            if (eventsList.FirstOrDefault() is TempoEvent tempoEvent)
            {
                midiResult.tempoEvs.Add(tempoEvent);
            }
        }
        if (Tempos.Count == 0)
        {
            var microseconds = 60000000 / BPM;
            midiResult.tempoEvs.Add(new TempoEvent(microseconds, 0));
        }

        foreach (var tsVm in Tss)
        {
            var eventsList = new List<MidiEvent>();
            tsVm.Write(eventsList);
            if (eventsList.FirstOrDefault() is TimeSignatureEvent tsEvent)
            {
                midiResult.tsEvs.Add(tsEvent);
            }
        }
        if (Tss.Count == 0)
        {
            int denomPower = 0;
            double d = Denominator;
            while (d > 1) { d /= 2; denomPower++; }
            int ticksInMetronomeClick = (int)(PPQN * (4.0 / Math.Pow(2, denomPower)));
            ticksInMetronomeClick = Math.Max(1, ticksInMetronomeClick);
            int no32ndNotesInQuarterNote = 8;
            midiResult.tsEvs.Add(new TimeSignatureEvent(0, Numerator, denomPower, ticksInMetronomeClick, no32ndNotesInQuarterNote));
        }

        foreach (var ksVm in Kss)
        {
            var eventsList = new List<MidiEvent>();
            ksVm.Write(eventsList);
            if (eventsList.FirstOrDefault() is KeySignatureEvent ksEvent)
            {
                midiResult.ksEvs.Add(ksEvent);
            }
        }

        foreach (var offsetVm in Offsets)
        {
            var eventsList = new List<MidiEvent>();
            offsetVm.Write(eventsList);
            if (eventsList.FirstOrDefault() is SmpteOffsetEvent offsetEvent)
            {
                midiResult.smpteOffsetEvs.Add(offsetEvent);
            }
        }

        foreach (var sysExVm in Syss)
        {
            var eventsList = new List<MidiEvent>();
            sysExVm.Write(eventsList);
            if (eventsList.FirstOrDefault() is SysexEvent sysExEvent)
            {
                midiResult.sysEvs.Add(sysExEvent);
            }
        }

        foreach (var seqVm in Sss)
        {
            var eventsList = new List<MidiEvent>();
            seqVm.Write(eventsList);
            if (eventsList.FirstOrDefault() is SequencerSpecificEvent seqEvent)
            {
                midiResult.ssEvs.Add(seqEvent);
            }
        }

        // 3. 写入全局文本事件
        foreach (var textVm in Texts)
        {
            var textEvent = new TextEvent(textVm.Text, textVm.MetaEventType, textVm.AbsoluteTime);
            midiResult.textEvs.Add(textEvent);
        }

        // 4. 写入通道事件
        foreach (var track in Tracks)
        {
            int channel = track.Channel;
            track.RestoreCommand.Execute(null);

            // 写入音色事件
            var patchEvent = new PatchChangeEvent(0, channel, (int)track.Patch);
            AddEventToDict(midiResult.patchEvs, channel, track.Patch, patchEvent);

            // 写入通道触后事件
            foreach (var catVm in track.Cats)
            {
                var eventsList = new List<MidiEvent>();
                catVm.Write(eventsList);
                if (eventsList.FirstOrDefault() is ChannelAfterTouchEvent catEvent)
                {
                    AddEventToDict(midiResult.catEvs, channel, track.Patch, catEvent);
                }
            }

            // 写入音高弯音事件
            foreach (var pwcVm in track.Pwcs)
            {
                var eventsList = new List<MidiEvent>();
                pwcVm.Write(eventsList);
                if (eventsList.FirstOrDefault() is PitchWheelChangeEvent pwcEvent)
                {
                    AddEventToDict(midiResult.pwcEvs, channel, track.Patch, pwcEvent);
                }
            }

            // 写入音符事件
            foreach (var noteVm in track.Notes)
            {
                var eventsList = new List<MidiEvent>();
                noteVm.Write(eventsList);
                foreach (var midiEvent in eventsList)
                {
                    if (midiEvent is NoteOnEvent noteOnEvent)
                    {
                        noteOnEvent.Channel = channel;
                        AddEventToDict(midiResult.noteOnEvs, channel, track.Patch, noteOnEvent);
                    }
                    else if (midiEvent is NoteEvent noteEvent && noteEvent.CommandCode == MidiCommandCode.NoteOff)
                    {
                        noteEvent.Channel = channel;
                        AddEventToDict(midiResult.noteOffEvs, channel, track.Patch, noteEvent);
                    }
                }
            }

            // 写入控制器事件
            foreach (var ccVm in track.Ctrls)
            {
                var ccEvent = new ControlChangeEvent(ccVm.AbsoluteTime, channel, ccVm.MidiController, ccVm.Value);
                AddEventToDict(midiResult.cCEvs, channel, track.Patch, ccEvent);
            }
        }
    }

    private static bool HasEventsInDict<T>(Dictionary<int, Dictionary<Patch, List<T>>> dict, int channel) where T : MidiEvent
    {
        if (dict.TryGetValue(channel, out var patchDict))
        {
            return patchDict.Any(kvp => kvp.Value.Count > 0);
        }
        return false;
    }

    private static void ReadEventsFromDict<TEvent, TViewModel>(
        ICollection<TViewModel> targetCollection,
        Dictionary<int, Dictionary<Patch, List<TEvent>>> sourceDict,
        int channel,
        Func<TEvent, TViewModel> converter)
        where TEvent : MidiEvent
        where TViewModel : EventViewModel
    {
        if (sourceDict.TryGetValue(channel, out var patchDict))
        {
            var allEvents = new List<TEvent>();
            foreach (var kvp in patchDict)
            {
                allEvents.AddRange(kvp.Value);
            }
            foreach (var ev in allEvents.OrderBy(e => e.AbsoluteTime))
            {
                targetCollection.Add(converter(ev));
            }
        }
    }

    private static void ClearDictForChannel<T>(Dictionary<int, Dictionary<Patch, List<T>>> dict, int channel) where T : MidiEvent
    {
        if (dict.TryGetValue(channel, out var patchDict))
        {
            patchDict.Clear();
        }
    }

    private static void AddEventToDict<T>(Dictionary<int, Dictionary<Patch, List<T>>> dict, int channel, Patch patch, T midiEvent) where T : MidiEvent
    {
        if (!dict.TryGetValue(channel, out var patchDict))
        {
            patchDict = [];
            dict[channel] = patchDict;
        }
        if (!patchDict.TryGetValue(patch, out var eventList))
        {
            eventList = [];
            patchDict[patch] = eventList;
        }
        eventList.Add(midiEvent);
    }

    #endregion
}