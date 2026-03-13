using Auris_Studio.Midi;
using NAudio.Midi;

namespace Auris_Studio.ViewModels.Helpers;

/// <summary>
/// MIDI 数据优化器，默认所有优化全开，可创建 <seealso cref="MidiOptimizerOptions"/> 对象以配置
/// <para><seealso cref="Optimize"/></para>
/// </summary>
public static class MidiOptimizer
{
    /// <summary>
    /// 执行完整的 MIDI 数据优化流程
    /// </summary>
    public static MidiResult Optimize(this MidiResult result, MidiOptimizerOptions? options = null)
    {
        options ??= MidiOptimizerOptions.Default;

        // 1. 全局预排序：确保所有事件按时间严格有序
        SortAllEvents(result);

        // 2. 基本清理：移除完全重复的事件
        if (options.BasicCleaning.Enabled)
        {
            result.Clean(options.BasicCleaning);
        }

        // 3. 连续值优化：基于阈值过滤微小变化
        if (options.ContinuousValue.Enabled)
        {
            result.OptimizeContinuousValues(options.ContinuousValue);
        }

        // 4. 音符修复：配对孤立的 NoteOn/NoteOff
        if (options.NoteFix.Enabled)
        {
            result.Fix(options.NoteFix);
        }

        // 5. 时间线修剪：处理倒流、网格对齐、最小间隔
        if (options.TimelineTrim.Enabled)
        {
            result.ApplyTimelineTrim(options.TimelineTrim);
        }

        return result;
    }

    /// <summary>
    /// 对所有事件列表进行预排序
    /// </summary>
    private static void SortAllEvents(MidiResult result)
    {
        SortList(result.tempoEvs);
        SortList(result.tsEvs);
        SortList(result.ksEvs);
        SortList(result.sysEvs);
        SortList(result.ssEvs);
        SortList(result.smpteOffsetEvs);
        SortList(result.textEvs);

        SortDictionary(result.patchEvs);
        SortDictionary(result.catEvs);
        SortDictionary(result.pwcEvs);
        SortDictionary(result.cCEvs);
        SortDictionary(result.noteOnEvs);
        SortDictionary(result.noteOffEvs);
    }

    private static void SortList<T>(List<T> events) where T : MidiEvent
    {
        if (events == null || events.Count < 2) return;
        events.Sort((a, b) => a.AbsoluteTime.CompareTo(b.AbsoluteTime));
    }

    private static void SortDictionary<T>(Dictionary<int, Dictionary<Patch, List<T>>> eventGroup) where T : MidiEvent
    {
        if (eventGroup == null) return;
        foreach (var channelDict in eventGroup.Values)
        {
            if (channelDict == null) continue;
            foreach (var list in channelDict.Values)
            {
                if (list == null || list.Count < 2) continue;
                list.Sort((a, b) => a.AbsoluteTime.CompareTo(b.AbsoluteTime));
            }
        }
    }

    /// <summary>
    /// 基本清理：移除相邻重复事件
    /// </summary>
    public static void Clean(this MidiResult result, MidiOptimizerOptions.BasicCleaningOptions options)
    {
        if (!options.Enabled) return;

        if (options.CleanGlobalEvents)
        {
            CleanList(result.tempoEvs);
            CleanList(result.tsEvs);
            CleanList(result.ksEvs);
            CleanList(result.sysEvs);
            CleanList(result.ssEvs);
            CleanList(result.smpteOffsetEvs);
            CleanList(result.textEvs);
        }

        if (options.CleanChannelEvents)
        {
            CleanDictionary(result.patchEvs);
            CleanDictionary(result.catEvs);
            CleanDictionary(result.pwcEvs);
            CleanDictionary(result.cCEvs);
            CleanDictionary(result.noteOnEvs);
            CleanDictionary(result.noteOffEvs);
        }
    }

    /// <summary>
    /// 优化连续值事件：基于智能阈值移除无效变化
    /// </summary>
    public static void OptimizeContinuousValues(this MidiResult result, MidiOptimizerOptions.ContinuousValueOptions options)
    {
        if (!options.Enabled) return;

        // 优化全局连续值 (速度)
        if (options.Tempo.Threshold > 0 && result.tempoEvs.Count > 1)
        {
            result.tempoEvs = OptimizeContinuousValueList(
                result.tempoEvs,
                options.Tempo.Threshold,
                GetTempoValue,
                isPercentage: false);
        }

        // 优化通道连续值
        for (int channel = 1; channel <= 16; channel++)
        {
            if (options.ChannelAfterTouch.Threshold > 0)
            {
                OptimizeContinuousValueDictionary(
                    result.catEvs, channel,
                    options.ChannelAfterTouch.Threshold,
                    GetChannelAfterTouchValue,
                    isPercentage: false,
                    range: 127);
            }

            if (options.PitchWheelChange.Threshold > 0)
            {
                OptimizeContinuousValueDictionary(
                    result.pwcEvs, channel,
                    options.PitchWheelChange.Threshold,
                    GetPitchWheelChangeValue,
                    isPercentage: true,
                    range: 16384);
            }

            if (options.ControlChange.Threshold > 0)
            {
                OptimizeControlChangeDictionary(result.cCEvs, channel, options.ControlChange);
            }

            // 修复：为音色变更事件应用"新值有效"逻辑
            if (options.PatchChange.Threshold >= 0)
            {
                OptimizeContinuousValueDictionary(
                    result.patchEvs, channel,
                    options.PatchChange.Threshold,
                    GetPatchChangeValue,
                    isPercentage: false,
                    range: 128);
            }
        }
    }

    /// <summary>
    /// 应用时间线修剪策略：倒流检查、网格对齐、最小间隔
    /// </summary>
    public static void ApplyTimelineTrim(this MidiResult result, MidiOptimizerOptions.TimelineTrimOptions options)
    {
        if (!options.Enabled) return;

        // 1. 移除时间倒流事件 (防御性检查)
        if (options.RemoveTimeBackwardsEvents)
        {
            RemoveTimeBackwardsEvents(result);
        }

        // 2. 网格对齐
        if (options.AlignToGrid && options.GridSizeTicks > 0)
        {
            AlignEventsToGrid(result, options.GridSizeTicks);
        }

        // 3. 最小时间间隔过滤
        if (options.MinTimeInterval > 0)
        {
            ApplyMinTimeInterval(result, options.MinTimeInterval);
        }
    }

    /// <summary>
    /// 移除时间倒流事件 (确保绝对时间非递减)
    /// </summary>
    private static void RemoveTimeBackwardsEvents(MidiResult result)
    {
        // 全局事件
        result.tempoEvs = FilterTimeBackwards(result.tempoEvs);
        result.tsEvs = FilterTimeBackwards(result.tsEvs);
        result.ksEvs = FilterTimeBackwards(result.ksEvs);
        result.textEvs = FilterTimeBackwards(result.textEvs);
        result.sysEvs = FilterTimeBackwards(result.sysEvs);
        result.ssEvs = FilterTimeBackwards(result.ssEvs);
        result.smpteOffsetEvs = FilterTimeBackwards(result.smpteOffsetEvs);

        // 通道事件
        FilterTimeBackwardsDictionary(result.patchEvs);
        FilterTimeBackwardsDictionary(result.catEvs);
        FilterTimeBackwardsDictionary(result.pwcEvs);
        FilterTimeBackwardsDictionary(result.cCEvs);
        FilterTimeBackwardsDictionary(result.noteOnEvs);
        FilterTimeBackwardsDictionary(result.noteOffEvs);
    }

    private static List<T> FilterTimeBackwards<T>(List<T> events) where T : MidiEvent
    {
        if (events == null || events.Count < 2) return events ?? [];

        var filtered = new List<T> { events[0] };
        long lastTime = events[0].AbsoluteTime;

        for (int i = 1; i < events.Count; i++)
        {
            if (events[i].AbsoluteTime >= lastTime)
            {
                filtered.Add(events[i]);
                lastTime = events[i].AbsoluteTime;
            }
        }
        return filtered;
    }

    private static void FilterTimeBackwardsDictionary<T>(Dictionary<int, Dictionary<Patch, List<T>>> eventGroup) where T : MidiEvent
    {
        if (eventGroup == null) return;
        foreach (var channelDict in eventGroup.Values)
        {
            if (channelDict == null) continue;
            foreach (var patch in channelDict.Keys.ToList())
            {
                var list = channelDict[patch];
                if (list != null)
                {
                    channelDict[patch] = FilterTimeBackwards(list);
                }
            }
        }
    }

    /// <summary>
    /// 将所有事件对齐到最近的网格点
    /// </summary>
    private static void AlignEventsToGrid(MidiResult result, int gridSize)
    {
        AlignList(result.tempoEvs, gridSize);
        AlignList(result.tsEvs, gridSize);
        AlignList(result.ksEvs, gridSize);
        AlignList(result.textEvs, gridSize);
        AlignList(result.sysEvs, gridSize);
        AlignList(result.ssEvs, gridSize);
        AlignList(result.smpteOffsetEvs, gridSize);

        AlignDictionary(result.patchEvs, gridSize);
        AlignDictionary(result.catEvs, gridSize);
        AlignDictionary(result.pwcEvs, gridSize);
        AlignDictionary(result.cCEvs, gridSize);
        AlignDictionary(result.noteOnEvs, gridSize);
        AlignDictionary(result.noteOffEvs, gridSize);

        // 对齐后需要重新排序，因为时间可能发生变化导致顺序错乱
        SortAllEvents(result);
    }

    private static void AlignList<T>(List<T> events, int gridSize) where T : MidiEvent
    {
        if (events == null) return;
        foreach (var ev in events)
        {
            ev.AbsoluteTime = (int)Math.Round((double)ev.AbsoluteTime / gridSize) * gridSize;
        }
    }

    private static void AlignDictionary<T>(Dictionary<int, Dictionary<Patch, List<T>>> eventGroup, int gridSize) where T : MidiEvent
    {
        if (eventGroup == null) return;
        foreach (var channelDict in eventGroup.Values)
        {
            if (channelDict == null) continue;
            foreach (var list in channelDict.Values)
            {
                if (list != null) AlignList(list, gridSize);
            }
        }
    }

    /// <summary>
    /// 应用最小时间间隔过滤：移除过于密集的事件
    /// </summary>
    private static void ApplyMinTimeInterval(MidiResult result, int minInterval)
    {
        // 全局事件
        result.tempoEvs = FilterMinInterval(result.tempoEvs, minInterval);
        result.tsEvs = FilterMinInterval(result.tsEvs, minInterval);
        result.ksEvs = FilterMinInterval(result.ksEvs, minInterval);
        result.textEvs = FilterMinInterval(result.textEvs, minInterval);
        result.sysEvs = FilterMinInterval(result.sysEvs, minInterval);
        result.ssEvs = FilterMinInterval(result.ssEvs, minInterval);
        result.smpteOffsetEvs = FilterMinInterval(result.smpteOffsetEvs, minInterval);

        // 通道事件
        FilterMinIntervalDictionary(result.patchEvs, minInterval);
        FilterMinIntervalDictionary(result.catEvs, minInterval);
        FilterMinIntervalDictionary(result.pwcEvs, minInterval);
        FilterMinIntervalDictionary(result.cCEvs, minInterval);
    }

    private static List<T> FilterMinInterval<T>(List<T> events, int minInterval) where T : MidiEvent
    {
        if (events == null || events.Count < 2) return events ?? [];

        var filtered = new List<T> { events[0] };
        long lastTime = events[0].AbsoluteTime;

        for (int i = 1; i < events.Count; i++)
        {
            if (events[i].AbsoluteTime - lastTime >= minInterval)
            {
                filtered.Add(events[i]);
                lastTime = events[i].AbsoluteTime;
            }
        }
        return filtered;
    }

    private static void FilterMinIntervalDictionary<T>(Dictionary<int, Dictionary<Patch, List<T>>> eventGroup, int minInterval) where T : MidiEvent
    {
        if (eventGroup == null) return;
        foreach (var channelDict in eventGroup.Values)
        {
            if (channelDict == null) continue;
            foreach (var patch in channelDict.Keys.ToList())
            {
                var list = channelDict[patch];
                if (list != null)
                {
                    channelDict[patch] = FilterMinInterval(list, minInterval);
                }
            }
        }
    }

    /// <summary>
    /// 修复未配对的音符开始/结束事件
    /// </summary>
    public static void Fix(this MidiResult result, MidiOptimizerOptions.NoteFixOptions options)
    {
        if (!options.Enabled) return;

        for (int channel = 1; channel <= 16; channel++)
        {
            if (!result.noteOnEvs.TryGetValue(channel, out var noteOnDict) ||
                !result.noteOffEvs.TryGetValue(channel, out var noteOffDict))
            {
                continue;
            }

            var allNoteOns = new List<NoteOnEvent>();
            var allNoteOffs = new List<NoteEvent>();

            foreach (var kvp in noteOnDict)
            {
                if (kvp.Value != null) allNoteOns.AddRange(kvp.Value);
            }
            foreach (var kvp in noteOffDict)
            {
                if (kvp.Value != null) allNoteOffs.AddRange(kvp.Value);
            }

            var noteOnGroups = allNoteOns
                .GroupBy(n => n.NoteNumber)
                .ToDictionary(g => g.Key, g => g.OrderBy(n => n.AbsoluteTime).ToList());

            var noteOffGroups = allNoteOffs
                .GroupBy(n => n.NoteNumber)
                .ToDictionary(g => g.Key, g => g.OrderBy(n => n.AbsoluteTime).ToList());

            foreach (var noteNumber in noteOnGroups.Keys.Concat(noteOffGroups.Keys).Distinct())
            {
                noteOnGroups.TryGetValue(noteNumber, out var noteOns);
                noteOffGroups.TryGetValue(noteNumber, out var noteOffs);

                noteOns ??= [];
                noteOffs ??= [];

                int onIndex = 0, offIndex = 0;
                var unmatchedNoteOns = new Stack<NoteOnEvent>();

                while (onIndex < noteOns.Count || offIndex < noteOffs.Count)
                {
                    if (onIndex < noteOns.Count &&
                        (offIndex >= noteOffs.Count || noteOns[onIndex].AbsoluteTime <= noteOffs[offIndex].AbsoluteTime))
                    {
                        unmatchedNoteOns.Push(noteOns[onIndex]);
                        onIndex++;
                    }
                    else
                    {
                        if (unmatchedNoteOns.Count > 0)
                        {
                            var noteOn = unmatchedNoteOns.Pop();
                            noteOn.OffEvent = noteOffs[offIndex];
                        }
                        else if (options.FixOrphanedNoteOffs)
                        {
                            var orphanedNoteOff = noteOffs[offIndex];
                            var dummyNoteOn = new NoteOnEvent(
                                Math.Max(0, orphanedNoteOff.AbsoluteTime - options.MaxNoteDurationTicks),
                                channel,
                                orphanedNoteOff.NoteNumber,
                                64,
                                0)
                            {
                                OffEvent = orphanedNoteOff
                            };
                            AddEventToDictionary(result.noteOnEvs, channel, Patch.AcousticGrandPiano, dummyNoteOn);
                        }
                        offIndex++;
                    }
                }

                if (options.FixOrphanedNoteOns && unmatchedNoteOns.Count > 0)
                {
                    foreach (var orphanedNoteOn in unmatchedNoteOns)
                    {
                        var dummyNoteOff = new NoteEvent(
                            orphanedNoteOn.AbsoluteTime + options.MaxNoteDurationTicks,
                            channel,
                            MidiCommandCode.NoteOff,
                            orphanedNoteOn.NoteNumber,
                            0);
                        orphanedNoteOn.OffEvent = dummyNoteOff;
                        AddEventToDictionary(result.noteOffEvs, channel, Patch.AcousticGrandPiano, dummyNoteOff);
                    }
                }
            }
        }
    }

    #region 辅助方法

    private static void CleanList<T>(List<T> events) where T : MidiEvent
    {
        if (events == null || events.Count < 2) return;

        var cleaned = new List<T> { events[0] };
        for (int i = 1; i < events.Count; i++)
        {
            var current = events[i];
            var previous = cleaned[^1];
            if (current.AbsoluteTime != previous.AbsoluteTime || !AreEventsEqual(current, previous))
            {
                cleaned.Add(current);
            }
        }
        events.Clear();
        events.AddRange(cleaned);
    }

    private static void CleanDictionary<T>(Dictionary<int, Dictionary<Patch, List<T>>> eventGroup) where T : MidiEvent
    {
        if (eventGroup == null) return;

        for (int channel = 1; channel <= 16; channel++)
        {
            if (!eventGroup.TryGetValue(channel, out var patchDict)) continue;
            if (patchDict == null) continue;

            foreach (var patch in patchDict.Keys.ToList())
            {
                var events = patchDict[patch];
                if (events == null || events.Count < 2) continue;

                var cleaned = new List<T> { events[0] };
                for (int i = 1; i < events.Count; i++)
                {
                    var current = events[i];
                    var previous = cleaned[^1];
                    if (current.AbsoluteTime != previous.AbsoluteTime || !AreEventsEqual(current, previous))
                    {
                        cleaned.Add(current);
                    }
                }
                patchDict[patch] = cleaned;
            }
        }
    }

    /// <summary>
    /// 改进的连续值优化：支持绝对值和百分比两种阈值模式
    /// </summary>
    private static List<T> OptimizeContinuousValueList<T>(
        List<T> events,
        int threshold,
        Func<T, int> valueGetter,
        bool isPercentage = false,
        int range = 128) where T : MidiEvent
    {
        if (events == null || events.Count < 2) return events ?? [];

        var optimized = new List<T>();
        T? lastKeptEvent = null;

        foreach (var currentEvent in events)
        {
            if (lastKeptEvent == null)
            {
                optimized.Add(currentEvent);
                lastKeptEvent = currentEvent;
                continue;
            }

            // 规则1: 同一时间点的重复事件
            if (currentEvent.AbsoluteTime == lastKeptEvent.AbsoluteTime &&
                AreEventsEqual(currentEvent, lastKeptEvent))
            {
                continue;
            }

            int currentValue = valueGetter(currentEvent);
            int lastValue = valueGetter(lastKeptEvent);

            // 规则2: 智能阈值检查
            if (IsChangeInsignificant(currentValue, lastValue, threshold, isPercentage, range))
            {
                continue;
            }

            optimized.Add(currentEvent);
            lastKeptEvent = currentEvent;
        }
        return optimized;
    }

    /// <summary>
    /// 判断变化是否不显著
    /// </summary>
    private static bool IsChangeInsignificant(int currentValue, int lastValue, int threshold,
        bool isPercentage, int range)
    {
        int absoluteChange = Math.Abs(currentValue - lastValue);

        if (isPercentage)
        {
            // 百分比模式：threshold 解释为百分比（0-100）
            double percentageChange = (absoluteChange * 100.0) / range;
            return percentageChange < threshold;
        }
        else
        {
            // 绝对值模式
            return absoluteChange < threshold;
        }
    }

    private static void OptimizeContinuousValueDictionary<T>(
        Dictionary<int, Dictionary<Patch, List<T>>> eventGroup,
        int channel,
        int threshold,
        Func<T, int> valueGetter,
        bool isPercentage = false,
        int range = 128) where T : MidiEvent
    {
        if (eventGroup == null) return;
        if (!eventGroup.TryGetValue(channel, out var patchDict)) return;
        if (patchDict == null) return;

        foreach (var patch in patchDict.Keys.ToList())
        {
            var events = patchDict[patch];
            if (events == null || events.Count < 2) continue;
            patchDict[patch] = OptimizeContinuousValueList(events, threshold, valueGetter, isPercentage, range);
        }
    }

    private static void OptimizeControlChangeDictionary(
        Dictionary<int, Dictionary<Patch, List<ControlChangeEvent>>> eventGroup,
        int channel, MidiOptimizerOptions.ControlChangeOptions options)
    {
        if (eventGroup == null) return;
        if (!eventGroup.TryGetValue(channel, out var patchDict)) return;
        if (patchDict == null) return;
        if (options.Threshold <= 0) return;

        foreach (var patch in patchDict.Keys.ToList())
        {
            var events = patchDict[patch];
            if (events == null || events.Count < 2) continue;

            var eventsByController = events
                .GroupBy(cc => cc.Controller)
                .ToDictionary(g => g.Key, g => g.OrderBy(e => e.AbsoluteTime).ToList());

            var optimizedEvents = new List<ControlChangeEvent>();

            foreach (var controllerGroup in eventsByController)
            {
                var controller = controllerGroup.Key;
                if (!ShouldOptimizeController(controller, options))
                {
                    optimizedEvents.AddRange(controllerGroup.Value);
                    continue;
                }

                var controllerEvents = controllerGroup.Value;
                var optimizedForController = new List<ControlChangeEvent>();
                ControlChangeEvent? lastKeptEvent = null;

                // 为不同控制器设置不同的优化策略
                bool usePercentage = ShouldUsePercentageForController(controller);
                int range = GetControllerRange();

                foreach (var currentEvent in controllerEvents)
                {
                    if (lastKeptEvent == null)
                    {
                        optimizedForController.Add(currentEvent);
                        lastKeptEvent = currentEvent;
                        continue;
                    }

                    if (currentEvent.AbsoluteTime == lastKeptEvent.AbsoluteTime &&
                        AreEventsEqual(currentEvent, lastKeptEvent))
                    {
                        continue;
                    }

                    int currentValue = currentEvent.ControllerValue;
                    int lastValue = lastKeptEvent.ControllerValue;
                    int absoluteChange = Math.Abs(currentValue - lastValue);

                    bool isInsignificant = usePercentage
                        ? (absoluteChange * 100.0 / range) < options.Threshold
                        : absoluteChange < options.Threshold;

                    if (isInsignificant)
                    {
                        continue;
                    }

                    optimizedForController.Add(currentEvent);
                    lastKeptEvent = currentEvent;
                }
                optimizedEvents.AddRange(optimizedForController);
            }
            patchDict[patch] = [.. optimizedEvents.OrderBy(e => e.AbsoluteTime)];
        }
    }

    private static bool ShouldUsePercentageForController(MidiController controller)
    {
        // 为具有宽动态范围的控制器使用百分比模式
        return controller switch
        {
            MidiController.Modulation => true,
            MidiController.Expression => true,
            _ => false
        };
    }

    private static int GetControllerRange()
    {
        return 128;
    }

    private static bool ShouldOptimizeController(MidiController controller, MidiOptimizerOptions.ControlChangeOptions options)
    {
        return controller switch
        {
            MidiController.MainVolume => options.OptimizeVolume,
            MidiController.Pan => options.OptimizePan,
            MidiController.Expression => options.OptimizeExpression,
            MidiController.Sustain => options.OptimizeSustain,
            MidiController.Modulation => options.OptimizeModulation,
            _ => true
        };
    }

    private static void AddEventToDictionary<T>(
        Dictionary<int, Dictionary<Patch, List<T>>> dict,
        int channel, Patch patch, T midiEvent) where T : MidiEvent
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

    private static int GetTempoValue(TempoEvent ev) => ev.MicrosecondsPerQuarterNote;
    private static int GetChannelAfterTouchValue(ChannelAfterTouchEvent ev) => ev.AfterTouchPressure;
    private static int GetPitchWheelChangeValue(PitchWheelChangeEvent ev) => ev.Pitch;
    private static int GetPatchChangeValue(PatchChangeEvent ev) => ev.Patch;

    private static bool AreEventsEqual(MidiEvent a, MidiEvent b)
    {
        if (a.GetType() != b.GetType()) return false;
        return a switch
        {
            TempoEvent t1 when b is TempoEvent t2 => t1.MicrosecondsPerQuarterNote == t2.MicrosecondsPerQuarterNote,
            TimeSignatureEvent ts1 when b is TimeSignatureEvent ts2 => ts1.Numerator == ts2.Numerator && ts1.Denominator == ts2.Denominator && ts1.TicksInMetronomeClick == ts2.TicksInMetronomeClick && ts1.No32ndNotesInQuarterNote == ts2.No32ndNotesInQuarterNote,
            KeySignatureEvent ks1 when b is KeySignatureEvent ks2 => ks1.SharpsFlats == ks2.SharpsFlats && ks1.MajorMinor == ks2.MajorMinor,
            TextEvent te1 when b is TextEvent te2 => te1.Text == te2.Text && te1.MetaEventType == te2.MetaEventType,
            SysexEvent sx1 when b is SysexEvent sx2 => sx1.ToString() == sx2.ToString(),
            SequencerSpecificEvent ss1 when b is SequencerSpecificEvent ss2 => ss1.ToString() == ss2.ToString(),
            SmpteOffsetEvent so1 when b is SmpteOffsetEvent so2 => so1.Hours == so2.Hours && so1.Minutes == so2.Minutes && so1.Seconds == so2.Seconds && so1.Frames == so2.Frames && so1.SubFrames == so2.SubFrames,
            ChannelAfterTouchEvent ca1 when b is ChannelAfterTouchEvent ca2 => ca1.AfterTouchPressure == ca2.AfterTouchPressure,
            PitchWheelChangeEvent pw1 when b is PitchWheelChangeEvent pw2 => pw1.Pitch == pw2.Pitch,
            PatchChangeEvent pc1 when b is PatchChangeEvent pc2 => pc1.Patch == pc2.Patch,
            ControlChangeEvent cc1 when b is ControlChangeEvent cc2 => cc1.Controller == cc2.Controller && cc1.ControllerValue == cc2.ControllerValue,
            NoteOnEvent no1 when b is NoteOnEvent no2 => no1.NoteNumber == no2.NoteNumber && no1.Velocity == no2.Velocity,
            NoteEvent ne1 when b is NoteEvent ne2 => ne1.NoteNumber == ne2.NoteNumber && ne1.Velocity == ne2.Velocity,
            _ => false
        };
    }

    #endregion
}