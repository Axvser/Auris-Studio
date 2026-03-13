namespace Auris_Studio.ViewModels.Helpers;

/// <summary>
/// MIDI 优化配置选项，支持 Fluent API 配置
/// 默认启用所有优化，使用推荐的阈值设置
/// </summary>
public class MidiOptimizerOptions
{
    /// <summary>
    /// 基本清理配置
    /// 默认：启用全局和通道事件清理
    /// </summary>
    public BasicCleaningOptions BasicCleaning { get; } = new BasicCleaningOptions();

    /// <summary>
    /// 连续值优化配置
    /// 默认：启用所有连续值优化，控制器阈值=1，弯音阈值=5（百分比），速度阈值=100
    /// </summary>
    public ContinuousValueOptions ContinuousValue { get; } = new ContinuousValueOptions();

    /// <summary>
    /// 音符修复配置
    /// 默认：启用音符修复，修复孤立音符，最大音符时长=960 ticks（2拍）
    /// </summary>
    public NoteFixOptions NoteFix { get; } = new NoteFixOptions();

    /// <summary>
    /// 时间线修剪配置
    /// 默认：启用时间线修剪，最小时间间隔=1 tick，移除超长持续事件
    /// </summary>
    public TimelineTrimOptions TimelineTrim { get; } = new TimelineTrimOptions();

    /// <summary>
    /// 创建默认优化配置
    /// </summary>
    public static MidiOptimizerOptions Default => new();

    #region Fluent API 配置方法

    /// <summary>
    /// 启用基本清理
    /// 默认：启用
    /// </summary>
    public MidiOptimizerOptions UseBasicCleaning(bool enabled = true)
    {
        BasicCleaning.Enabled = enabled;
        return this;
    }

    /// <summary>
    /// 启用连续值优化
    /// 默认：启用
    /// </summary>
    public MidiOptimizerOptions UseContinuousValueOptimization(bool enabled = true)
    {
        ContinuousValue.Enabled = enabled;
        return this;
    }

    /// <summary>
    /// 启用音符修复
    /// 默认：启用
    /// </summary>
    public MidiOptimizerOptions UseNoteFix(bool enabled = true)
    {
        NoteFix.Enabled = enabled;
        return this;
    }

    /// <summary>
    /// 启用时间线修剪
    /// 默认：启用
    /// </summary>
    public MidiOptimizerOptions UseTimelineTrim(bool enabled = true)
    {
        TimelineTrim.Enabled = enabled;
        return this;
    }

    /// <summary>
    /// 设置控制器变化阈值
    /// 默认：1 (0-127)
    /// </summary>
    public MidiOptimizerOptions SetControlChangeThreshold(int threshold)
    {
        if (threshold >= 0 && threshold <= 127)
        {
            ContinuousValue.ControlChange.Threshold = threshold;
        }
        return this;
    }

    /// <summary>
    /// 设置弯音变化阈值（百分比模式）
    /// 默认：5 (0-100)
    /// </summary>
    public MidiOptimizerOptions SetPitchWheelChangeThreshold(int threshold)
    {
        if (threshold >= 0 && threshold <= 100)
        {
            ContinuousValue.PitchWheelChange.Threshold = threshold;
        }
        return this;
    }

    /// <summary>
    /// 设置速度变化阈值
    /// 默认：100 微秒/四分音符
    /// </summary>
    public MidiOptimizerOptions SetTempoChangeThreshold(int threshold)
    {
        if (threshold >= 0)
        {
            ContinuousValue.Tempo.Threshold = threshold;
        }
        return this;
    }

    /// <summary>
    /// 设置音色变更阈值
    /// 默认：0 (任何变化都保留)
    /// </summary>
    public MidiOptimizerOptions SetPatchChangeThreshold(int threshold)
    {
        if (threshold >= 0 && threshold <= 127)
        {
            ContinuousValue.PatchChange.Threshold = threshold;
        }
        return this;
    }

    /// <summary>
    /// 设置是否修复孤立的音符开始事件
    /// 默认：启用
    /// </summary>
    public MidiOptimizerOptions SetFixOrphanedNoteOns(bool fix = true)
    {
        NoteFix.FixOrphanedNoteOns = fix;
        return this;
    }

    /// <summary>
    /// 设置是否修复孤立的音符结束事件
    /// 默认：启用
    /// </summary>
    public MidiOptimizerOptions SetFixOrphanedNoteOffs(bool fix = true)
    {
        NoteFix.FixOrphanedNoteOffs = fix;
        return this;
    }

    /// <summary>
    /// 设置最大音符时长（tick）
    /// 默认：960 (2拍，基于480 PPQN)
    /// </summary>
    public MidiOptimizerOptions SetMaxNoteDuration(int ticks)
    {
        if (ticks > 0)
        {
            NoteFix.MaxNoteDurationTicks = ticks;
        }
        return this;
    }

    /// <summary>
    /// 设置最小时间间隔（tick）
    /// 默认：1 tick
    /// </summary>
    public MidiOptimizerOptions SetMinTimeInterval(int ticks)
    {
        if (ticks >= 0)
        {
            TimelineTrim.MinTimeInterval = ticks;
        }
        return this;
    }

    /// <summary>
    /// 设置是否移除时间倒流事件
    /// 默认：启用
    /// </summary>
    public MidiOptimizerOptions SetRemoveTimeBackwards(bool remove = true)
    {
        TimelineTrim.RemoveTimeBackwardsEvents = remove;
        return this;
    }

    /// <summary>
    /// 设置是否对齐事件时间到网格
    /// 默认：禁用
    /// </summary>
    public MidiOptimizerOptions SetAlignToGrid(bool align = true, int gridSize = 120)
    {
        TimelineTrim.AlignToGrid = align;
        TimelineTrim.GridSizeTicks = gridSize;
        return this;
    }

    #endregion

    #region 内部配置类

    /// <summary>
    /// 基本清理配置
    /// 默认：Enabled=true, CleanGlobalEvents=true, CleanChannelEvents=true
    /// </summary>
    public class BasicCleaningOptions
    {
        public bool Enabled { get; set; } = true;
        public bool CleanGlobalEvents { get; set; } = true;
        public bool CleanChannelEvents { get; set; } = true;
    }

    /// <summary>
    /// 连续值优化配置
    /// 默认：启用所有连续值优化
    /// </summary>
    public class ContinuousValueOptions
    {
        public bool Enabled { get; set; } = true;
        public ControlChangeOptions ControlChange { get; } = new ControlChangeOptions();
        public ChannelAfterTouchOptions ChannelAfterTouch { get; } = new ChannelAfterTouchOptions();
        public PitchWheelChangeOptions PitchWheelChange { get; } = new PitchWheelChangeOptions();
        public TempoOptions Tempo { get; } = new TempoOptions();
        public PatchChangeOptions PatchChange { get; } = new PatchChangeOptions();
    }

    /// <summary>
    /// 音符修复配置
    /// 默认：Enabled=true, FixOrphanedNoteOns=true, FixOrphanedNoteOffs=true, MaxNoteDurationTicks=960
    /// </summary>
    public class NoteFixOptions
    {
        public bool Enabled { get; set; } = true;
        public bool FixOrphanedNoteOns { get; set; } = true;
        public bool FixOrphanedNoteOffs { get; set; } = true;
        public int MaxNoteDurationTicks { get; set; } = 960;
    }

    /// <summary>
    /// 时间线修剪配置
    /// 默认：Enabled=true, MinTimeInterval=1, RemoveTimeBackwardsEvents=true, AlignToGrid=false
    /// </summary>
    public class TimelineTrimOptions
    {
        public bool Enabled { get; set; } = true;
        public int MinTimeInterval { get; set; } = 1;
        public bool RemoveTimeBackwardsEvents { get; set; } = true;
        public bool AlignToGrid { get; set; } = false;
        public int GridSizeTicks { get; set; } = 120;
    }

    /// <summary>
    /// 控制器事件配置
    /// 默认：Threshold=1, OptimizeVolume=true, OptimizePan=true, OptimizeExpression=true, OptimizeSustain=true, OptimizeModulation=true
    /// </summary>
    public class ControlChangeOptions
    {
        public int Threshold { get; set; } = 1;
        public bool OptimizeVolume { get; set; } = true;
        public bool OptimizePan { get; set; } = true;
        public bool OptimizeExpression { get; set; } = true;
        public bool OptimizeSustain { get; set; } = true;
        public bool OptimizeModulation { get; set; } = true;
    }

    /// <summary>
    /// 通道触后事件配置
    /// 默认：Threshold=1
    /// </summary>
    public class ChannelAfterTouchOptions
    {
        public int Threshold { get; set; } = 1;
    }

    /// <summary>
    /// 音高弯音事件配置
    /// 默认：Threshold=5（百分比）
    /// </summary>
    public class PitchWheelChangeOptions
    {
        public int Threshold { get; set; } = 5;
    }

    /// <summary>
    /// 速度事件配置
    /// 默认：Threshold=100
    /// </summary>
    public class TempoOptions
    {
        public int Threshold { get; set; } = 100;
    }

    /// <summary>
    /// 音色变更事件配置
    /// 默认：Threshold=1 (任何变化都保留)
    /// </summary>
    public class PatchChangeOptions
    {
        public int Threshold { get; set; } = 1;
    }

    #endregion
}