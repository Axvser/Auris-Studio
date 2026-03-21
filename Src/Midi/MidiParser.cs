using NAudio.Midi;

namespace Auris_Studio.Midi;

/// <summary>
/// 基于NAudio的 <seealso cref="MidiFile"/> 解析
/// <para><seealso cref="ImportAsync"/></para>
/// <para><seealso cref="Import"/></para>
/// </summary>
public static class MidiParser
{
    /// <summary>
    /// 异步导入 MIDI 文件
    /// </summary>
    /// <param name="path">Midi 文件路径</param>
    /// <returns><seealso cref="MidiResult"/></returns>
    public static async Task<MidiResult> ImportAsync(string path)
    {
        return await Task.Run(() => Import(path));
    }

    /// <summary>
    /// 同步导入 MIDI 文件
    /// </summary>
    /// <param name="path">MIDI 文件路径</param>
    /// <returns><seealso cref="MidiResult"/></returns>
    public static MidiResult Import(string path)
    {
        var midiFile = new MidiFile(path, strictChecking: false);
        var midiResult = new MidiResult
        {
            fileFormat = midiFile.FileFormat,
            deltaTicksPerQuarterNote = midiFile.DeltaTicksPerQuarterNote
        };

        var activePatches = new Dictionary<int, Patch>();

        for (int track = 0; track < midiFile.Tracks; track++)
        {
            var events = midiFile.Events[track];

            foreach (var midiEvent in events)
            {
                if (midiEvent is TempoEvent tempoEvent)
                {
                    midiResult.tempoEvs.Add(tempoEvent);
                }
                else if (midiEvent is TimeSignatureEvent timeSignatureEvent)
                {
                    midiResult.tsEvs.Add(timeSignatureEvent);
                }
                else if (midiEvent is KeySignatureEvent keySignatureEvent)
                {
                    midiResult.ksEvs.Add(keySignatureEvent);
                }
                else if (midiEvent is SmpteOffsetEvent smpteOffsetEvent)
                {
                    midiResult.smpteOffsetEvs.Add(smpteOffsetEvent);
                }
                else if (midiEvent is SequencerSpecificEvent sequencerSpecificEvent)
                {
                    midiResult.ssEvs.Add(sequencerSpecificEvent);
                }
                else if (midiEvent is SysexEvent sysexEvent)
                {
                    midiResult.sysEvs.Add(sysexEvent);
                }
                else if (midiEvent is TextEvent textEvent)
                {
                    midiResult.textEvs.Add(textEvent);
                }
                else
                {
                    int channel = midiEvent.Channel;
                    if (channel >= 1 && channel <= 16)
                    {
                        var currentPatch = GetCurrentPatch(channel, activePatches);

                        if (midiEvent is PatchChangeEvent patchChangeEvent)
                        {
                            var patch = (Patch)patchChangeEvent.Patch;
                            activePatches[channel] = patch;
                            AddEventToGroup(midiResult.patchEvs, channel, patch, patchChangeEvent);
                        }
                        else if (midiEvent is ChannelAfterTouchEvent channelAfterTouchEvent)
                        {
                            AddEventToGroup(midiResult.catEvs, channel, currentPatch, channelAfterTouchEvent);
                        }
                        else if (midiEvent is PitchWheelChangeEvent pitchWheelChangeEvent)
                        {
                            AddEventToGroup(midiResult.pwcEvs, channel, currentPatch, pitchWheelChangeEvent);
                        }
                        else if (midiEvent is ControlChangeEvent controlChangeEvent)
                        {
                            AddEventToGroup(midiResult.cCEvs, channel, currentPatch, controlChangeEvent);
                        }
                        else if (midiEvent is NoteOnEvent noteOnEvent)
                        {
                            AddEventToGroup(midiResult.noteOnEvs, channel, currentPatch, noteOnEvent);
                        }
                        else if (midiEvent is NoteEvent noteEvent && noteEvent.CommandCode == MidiCommandCode.NoteOff)
                        {
                            AddEventToGroup(midiResult.noteOffEvs, channel, currentPatch, noteEvent);
                        }
                    }
                }
            }
        }

        return midiResult;
    }

    private static Patch GetCurrentPatch(int channel, Dictionary<int, Patch> activePatches)
    {
        if (activePatches.TryGetValue(channel, out var patch))
        {
            return patch;
        }
        return Patch.AcousticGrandPiano;
    }

    private static void AddEventToGroup<T>(Dictionary<int, Dictionary<Patch, List<T>>> eventGroup, int channel, Patch patch, T midiEvent)
    {
        var channelDict = eventGroup[channel];
        if (!channelDict.TryGetValue(patch, out List<T>? value))
        {
            value = [];
            channelDict[patch] = value;
        }

        value.Add(midiEvent);
    }
}