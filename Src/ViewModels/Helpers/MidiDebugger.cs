using Auris_Studio.Midi;
using NAudio.Midi;
using System.Reflection;
using System.Text;

namespace Auris_Studio.ViewModels.Helpers;

public static class MidiDebugger
{
    public static async Task BuildMarkdownAsync(this MidiResult result, string path)
    {
        await Task.Run(() => BuildMarkdown(result, path));
    }

    public static void BuildMarkdown(this MidiResult result, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MIDI 文件调试报告");
        sb.AppendLine();

        sb.AppendLine("## 文件信息");
        sb.AppendLine($"- 文件格式: {result.fileFormat}");
        sb.AppendLine($"- 分辨率 (PPQN): {result.deltaTicksPerQuarterNote}");
        sb.AppendLine();

        sb.AppendLine("## 全局事件统计");
        AppendEventListInfo(sb, "速度事件 (Tempo)", result.tempoEvs, ev => $"{ev.MicrosecondsPerQuarterNote} µs/qnote");
        AppendEventListInfo(sb, "拍号事件 (Time Signature)", result.tsEvs, ev => $"{ev.Numerator}/{Math.Pow(2, ev.Denominator)}");
        AppendEventListInfo(sb, "调号事件 (Key Signature)", result.ksEvs, ev => $"{GetKeySignatureText(ev.SharpsFlats, ev.MajorMinor)}");
        AppendEventListInfo(sb, "SMPTE偏移事件", result.smpteOffsetEvs, ev => $"{ev.Hours:D2}:{ev.Minutes:D2}:{ev.Seconds:D2}:{ev.Frames:D2}.{ev.SubFrames:D2}");
        AppendEventListInfo(sb, "系统专有事件 (SysEx)", result.sysEvs, ev => GetSysExInfo(ev));
        AppendEventListInfo(sb, "音序器专有事件", result.ssEvs, ev => GetSequencerSpecificInfo(ev));
        AppendEventListInfo(sb, "文本事件 (Text)", result.textEvs, ev => $"{ev.MetaEventType}: \"{ev.Text}\"");

        sb.AppendLine("## 通道事件统计");
        for (int channel = 1; channel <= 16; channel++)
        {
            bool channelHasEvents = false;
            var eventCounts = new List<string>();

            int CountEventsInDict<T>(Dictionary<int, Dictionary<Patch, List<T>>> dict) where T : MidiEvent
            {
                if (dict.TryGetValue(channel, out var patchDict))
                {
                    return patchDict.Sum(kvp => kvp.Value.Count);
                }
                return 0;
            }

            int patchCount = CountEventsInDict(result.patchEvs);
            int catCount = CountEventsInDict(result.catEvs);
            int pwcCount = CountEventsInDict(result.pwcEvs);
            int ccCount = CountEventsInDict(result.cCEvs);
            int noteOnCount = CountEventsInDict(result.noteOnEvs);
            int noteOffCount = CountEventsInDict(result.noteOffEvs);

            if (patchCount > 0) { channelHasEvents = true; eventCounts.Add($"音色: {patchCount}"); }
            if (catCount > 0) { channelHasEvents = true; eventCounts.Add($"触后: {catCount}"); }
            if (pwcCount > 0) { channelHasEvents = true; eventCounts.Add($"弯音: {pwcCount}"); }
            if (ccCount > 0) { channelHasEvents = true; eventCounts.Add($"控制: {ccCount}"); }
            if (noteOnCount > 0) { channelHasEvents = true; eventCounts.Add($"开始: {noteOnCount}"); }
            if (noteOffCount > 0) { channelHasEvents = true; eventCounts.Add($"结束: {noteOffCount}"); }

            if (channelHasEvents)
            {
                sb.AppendLine($"### 通道 {channel}");
                sb.AppendLine($"- 事件分布: {string.Join("; ", eventCounts)}");

                // 按音色细分
                AppendPatchDetail(sb, "音色事件", result.patchEvs, channel);
                AppendPatchDetail(sb, "通道触后", result.catEvs, channel);
                AppendPatchDetail(sb, "音高弯音", result.pwcEvs, channel);
                AppendPatchDetail(sb, "控制器", result.cCEvs, channel);
                AppendPatchDetail(sb, "音符开始", result.noteOnEvs, channel);
                AppendPatchDetail(sb, "音符结束", result.noteOffEvs, channel);
                sb.AppendLine();
            }
        }

        sb.AppendLine("## 全局事件详情");
        AppendEventListDetail(sb, "速度事件", result.tempoEvs, ev => $"{ev.AbsoluteTime} ticks -> {ev.MicrosecondsPerQuarterNote} µs/qnote");
        AppendEventListDetail(sb, "拍号事件", result.tsEvs, ev => $"{ev.AbsoluteTime} ticks -> {ev.Numerator}/{Math.Pow(2, ev.Denominator)}");
        AppendEventListDetail(sb, "调号事件", result.ksEvs, ev => $"{ev.AbsoluteTime} ticks -> {GetKeySignatureText(ev.SharpsFlats, ev.MajorMinor)}");
        AppendEventListDetail(sb, "SMPTE偏移事件", result.smpteOffsetEvs, ev => $"{ev.AbsoluteTime} ticks -> {ev.Hours:D2}:{ev.Minutes:D2}:{ev.Seconds:D2}:{ev.Frames:D2}.{ev.SubFrames:D2}");
        AppendEventListDetail(sb, "系统专有事件", result.sysEvs, ev => $"{ev.AbsoluteTime} ticks -> {GetSysExInfo(ev)}");
        AppendEventListDetail(sb, "音序器专有事件", result.ssEvs, ev => $"{ev.AbsoluteTime} ticks -> {GetSequencerSpecificInfo(ev)}");
        AppendEventListDetail(sb, "文本事件", result.textEvs, ev => $"{ev.AbsoluteTime} ticks -> {ev.MetaEventType}: \"{ev.Text}\"");

        sb.AppendLine("## 通道事件详情");
        for (int channel = 1; channel <= 16; channel++)
        {
            bool hasDetail = false;
            var channelSb = new StringBuilder();

            void AppendDetail<T>(string name, Dictionary<int, Dictionary<Patch, List<T>>> dict, Func<T, string> formatter) where T : MidiEvent
            {
                if (dict.TryGetValue(channel, out var patchDict))
                {
                    foreach (var kvp in patchDict)
                    {
                        if (kvp.Value.Count > 0)
                        {
                            hasDetail = true;
                            channelSb.AppendLine($"#### {name} (通道 {channel}, 音色 {kvp.Key})");
                            foreach (var ev in kvp.Value.OrderBy(e => e.AbsoluteTime))
                            {
                                channelSb.AppendLine($"- {formatter(ev)}");
                            }
                            channelSb.AppendLine();
                        }
                    }
                }
            }

            AppendDetail("音色事件", result.patchEvs, ev => $"{ev.AbsoluteTime} ticks -> Patch {ev.Patch}");
            AppendDetail("通道触后事件", result.catEvs, ev => $"{ev.AbsoluteTime} ticks -> 压力 {ev.AfterTouchPressure}");
            AppendDetail("音高弯音事件", result.pwcEvs, ev => $"{ev.AbsoluteTime} ticks -> 值 {ev.Pitch} (0-16383)");
            AppendDetail("控制器事件", result.cCEvs, ev => $"{ev.AbsoluteTime} ticks -> {ev.Controller} = {ev.ControllerValue}");
            AppendDetail("音符开始事件", result.noteOnEvs, ev => $"{ev.AbsoluteTime} ticks -> 音符 {ev.NoteNumber} ({GetNoteName(ev.NoteNumber)}), 力度 {ev.Velocity}");
            AppendDetail("音符结束事件", result.noteOffEvs, ev => $"{ev.AbsoluteTime} ticks -> 音符 {ev.NoteNumber} ({GetNoteName(ev.NoteNumber)})");

            if (hasDetail)
            {
                sb.AppendLine($"### 通道 {channel}");
                sb.Append(channelSb);
            }
        }

        System.IO.File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static void AppendEventListInfo<T>(StringBuilder sb, string name, List<T> events, Func<T, string> formatter) where T : MidiEvent
    {
        sb.AppendLine($"- {name}: {events.Count} 个");
        if (events.Count > 0)
        {
            var first = events.OrderBy(e => e.AbsoluteTime).First();
            var last = events.OrderByDescending(e => e.AbsoluteTime).First();
            sb.AppendLine($"  - 首个: {first.AbsoluteTime} ticks -> {formatter(first)}");
            sb.AppendLine($"  - 最后: {last.AbsoluteTime} ticks -> {formatter(last)}");
        }
    }

    private static void AppendEventListDetail<T>(StringBuilder sb, string name, List<T> events, Func<T, string> formatter) where T : MidiEvent
    {
        if (events.Count == 0) return;

        sb.AppendLine($"### {name}");
        foreach (var ev in events.OrderBy(e => e.AbsoluteTime))
        {
            sb.AppendLine($"- {formatter(ev)}");
        }
        sb.AppendLine();
    }

    private static void AppendPatchDetail<T>(StringBuilder sb, string eventType, Dictionary<int, Dictionary<Patch, List<T>>> dict, int channel) where T : MidiEvent
    {
        if (dict.TryGetValue(channel, out var patchDict))
        {
            foreach (var kvp in patchDict.Where(kvp => kvp.Value.Count > 0))
            {
                sb.AppendLine($"  - {eventType} (音色 {kvp.Key}): {kvp.Value.Count} 个事件");
            }
        }
    }

    private static string GetSysExInfo(SysexEvent ev)
    {
        // 使用 ToString() 获取安全的信息，或尝试通过反射获取 Data
        try
        {
            // 方法1: 使用 ToString()
            string str = ev.ToString();
            if (str.Contains("SysEx") && str.Length < 100)
            {
                return str;
            }
            // 方法2: 尝试获取 Data 属性 (如果可用)
            var prop = typeof(SysexEvent).GetProperty("Data", BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.GetValue(ev) is byte[] data)
            {
                return $"[{data.Length} bytes]";
            }
        }
        catch { }
        return "[SysEx Data]";
    }

    private static string GetSequencerSpecificInfo(SequencerSpecificEvent ev)
    {
        try
        {
            // 类似地处理 SequencerSpecificEvent
            string str = ev.ToString();
            if (str.Contains("Sequencer") && str.Length < 100)
            {
                return str;
            }
            var prop = typeof(SequencerSpecificEvent).GetProperty("Data", BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.GetValue(ev) is byte[] data)
            {
                return $"[{data.Length} bytes]";
            }
        }
        catch { }
        return "[Sequencer Specific Data]";
    }

    private static string GetKeySignatureText(int sharpsFlats, int majorMinor)
    {
        string[] majorKeys = ["C", "G", "D", "A", "E", "B", "F#", "C#"];
        string[] minorKeys = ["A", "E", "B", "F#", "C#", "G#", "D#", "A#"];

        int index = sharpsFlats + 7;
        if (index < 0 || index >= 8) return $"异常值: {sharpsFlats}/{majorMinor}";

        return majorMinor == 0 ? $"{majorKeys[index]} 大调" : $"{minorKeys[index]} 小调";
    }

    private static string GetNoteName(int noteNumber)
    {
        string[] noteNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
        int octave = (noteNumber / 12) - 1;
        return $"{noteNames[noteNumber % 12]}{octave}";
    }
}