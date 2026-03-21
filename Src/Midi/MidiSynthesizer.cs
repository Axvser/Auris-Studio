using NAudio.Midi;
using System.IO;

namespace Auris_Studio.Midi;

/// <summary>
/// 基于NAudio的 <seealso cref="MidiFile"/> 合成
/// <para><seealso cref="ExportAsync"/></para>
/// <para><seealso cref="Export"/></para>
/// </summary>
public static class MidiSynthesizer
{
    /// <summary>
    /// 异步导出 MIDI 文件
    /// </summary>
    /// <param name="result">MIDI 解析结果</param>
    /// <param name="path">MIDI 文件路径</param>
    public static async Task ExportAsync(this MidiResult result, string path)
    {
        if (string.IsNullOrEmpty(path)) throw new ArgumentException("Path cannot be null or empty", nameof(path));
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await Task.Run(() =>
        {
            Export(result, path);
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// 同步导出 MIDI 文件
    /// </summary>
    /// <param name="result">MIDI 解析结果</param>
    /// <param name="path">MIDI 文件路径</param>
    public static void Export(this MidiResult result, string path)
    {
        // 1. 参数检查
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 2. 创建 MidiEventCollection
        var events = new MidiEventCollection(result.fileFormat, result.deltaTicksPerQuarterNote);

        // 3. 轨道1：包含所有全局事件
        var track1 = new List<MidiEvent>();
        long globalMaxTick = 0;

        // 3.1 添加所有全局事件到轨道1
        track1.AddRange(result.tempoEvs);
        track1.AddRange(result.tsEvs);
        track1.AddRange(result.ksEvs);
        track1.AddRange(result.sysEvs);
        track1.AddRange(result.ssEvs);
        track1.AddRange(result.smpteOffsetEvs);
        track1.AddRange(result.textEvs);

        // 3.2 为通道1添加通道事件
        AddChannelEventsToTrack(track1, result, 1, ref globalMaxTick);

        // 4. 为通道2-16创建独立轨道
        var channelTracks = new Dictionary<int, List<MidiEvent>>();

        for (int channel = 2; channel <= 16; channel++)
        {
            var track = new List<MidiEvent>();
            channelTracks[channel] = track;
            AddChannelEventsToTrack(track, result, channel, ref globalMaxTick);
        }

        // 5. 对轨道1事件按时间排序
        track1.Sort((a, b) => a.AbsoluteTime.CompareTo(b.AbsoluteTime));

        // 6. 添加轨道结束事件
        long endTime = globalMaxTick;
        track1.Add(new MetaEvent(MetaEventType.EndTrack, 0, 0) { AbsoluteTime = endTime });

        // 7. 添加轨道1
        events.AddTrack(track1);

        // 8. 为通道2-16添加轨道
        for (int channel = 2; channel <= 16; channel++)
        {
            // 检查通道是否为空
            bool channelIsEmpty = IsChannelEmpty(result, channel);
            if (channelIsEmpty) continue;

            var track = channelTracks[channel];

            // 对每个轨道进行排序
            track.Sort((a, b) => a.AbsoluteTime.CompareTo(b.AbsoluteTime));

            // 为每个轨道添加结束事件
            track.Add(new MetaEvent(MetaEventType.EndTrack, 0, 0) { AbsoluteTime = endTime });

            events.AddTrack(track);
        }

        // 9. 导出MIDI文件
        events.PrepareForExport();
        MidiFile.Export(path, events);
    }

    private static bool IsChannelEmpty(MidiResult result, int channel)
    {
        bool HasEventsInDictionary<T>(Dictionary<int, Dictionary<Patch, List<T>>> dict) where T : MidiEvent
        {
            if (!dict.TryGetValue(channel, out var patchDict)) return false;
            return patchDict.Any(kvp => kvp.Value.Count > 0);
        }

        return !HasEventsInDictionary(result.patchEvs) &&
               !HasEventsInDictionary(result.cCEvs) &&
               !HasEventsInDictionary(result.noteOnEvs) &&
               !HasEventsInDictionary(result.noteOffEvs) &&
               !HasEventsInDictionary(result.pwcEvs) &&
               !HasEventsInDictionary(result.catEvs);
    }

    private static void AddChannelEventsToTrack(
        List<MidiEvent> track,
        MidiResult result,
        int channel,
        ref long globalMaxTick)
    {
        // 添加音色变化事件
        if (result.patchEvs.TryGetValue(channel, out var patchDict))
        {
            foreach (var kvp in patchDict)
            {
                foreach (var patchEvent in kvp.Value)
                {
                    track.Add(patchEvent);
                }
            }
        }

        // 添加所有控制器事件
        if (result.cCEvs.TryGetValue(channel, out var ccDict))
        {
            foreach (var kvp in ccDict)
            {
                foreach (var controlEvent in kvp.Value)
                {
                    track.Add(controlEvent);
                }
            }
        }

        // 添加音符开始事件
        if (result.noteOnEvs.TryGetValue(channel, out var noteOnDict))
        {
            foreach (var kvp in noteOnDict)
            {
                foreach (var noteOn in kvp.Value)
                {
                    track.Add(noteOn);
                    if (noteOn.OffEvent?.AbsoluteTime > globalMaxTick)
                    {
                        globalMaxTick = noteOn.OffEvent.AbsoluteTime;
                    }
                }
            }
        }

        // 添加音符结束事件
        if (result.noteOffEvs.TryGetValue(channel, out var noteOffDict))
        {
            foreach (var kvp in noteOffDict)
            {
                foreach (var noteOff in kvp.Value)
                {
                    track.Add(noteOff);
                    if (noteOff.AbsoluteTime > globalMaxTick)
                    {
                        globalMaxTick = noteOff.AbsoluteTime;
                    }
                }
            }
        }

        // 添加音高弯音事件
        if (result.pwcEvs.TryGetValue(channel, out var pwcDict))
        {
            foreach (var kvp in pwcDict)
            {
                foreach (var pwcEvent in kvp.Value)
                {
                    track.Add(pwcEvent);
                }
            }
        }

        // 添加通道触后事件
        if (result.catEvs.TryGetValue(channel, out var catDict))
        {
            foreach (var kvp in catDict)
            {
                foreach (var catEvent in kvp.Value)
                {
                    track.Add(catEvent);
                }
            }
        }
    }
}