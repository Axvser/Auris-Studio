using NAudio.Midi;

namespace Auris_Studio.Midi;

public class MidiResult
{
    // 文件格式
    public int fileFormat = 1;

    // 时间分辨率
    public int deltaTicksPerQuarterNote = 480;

    // ====================== 特殊事件 (存储为列表，不按通道划分) ======================
    // 系统专有事件 (System Exclusive Events) - 无通道概念，统一收集
    public List<SysexEvent> sysEvs = [];

    // ====================== 全局事件 (存储为列表，不按通道划分) ======================
    // 元事件 (Meta Events) - 固定通道1，统一收集
    // 速度事件
    public List<TempoEvent> tempoEvs = [];
    // 拍号事件
    public List<TimeSignatureEvent> tsEvs = [];
    // 调号事件
    public List<KeySignatureEvent> ksEvs = [];
    // 文本事件
    public List<TextEvent> textEvs = [];
    // 偏移事件
    public List<SmpteOffsetEvent> smpteOffsetEvs = [];
    // 音序器专用事件
    public List<SequencerSpecificEvent> ssEvs = [];

    // ====================== 通道事件 (按通道、音色划分) ======================
    // 通道触后事件
    public Dictionary<int, Dictionary<Patch, List<ChannelAfterTouchEvent>>> catEvs = InitializeEventGroup<ChannelAfterTouchEvent>();

    // 音高弯音事件
    public Dictionary<int, Dictionary<Patch, List<PitchWheelChangeEvent>>> pwcEvs = InitializeEventGroup<PitchWheelChangeEvent>();

    // 音色事件
    public Dictionary<int, Dictionary<Patch, List<PatchChangeEvent>>> patchEvs = InitializeEventGroup<PatchChangeEvent>();

    // 音符事件
    // 音符开始事件
    public Dictionary<int, Dictionary<Patch, List<NoteOnEvent>>> noteOnEvs = InitializeEventGroup<NoteOnEvent>();
    // 音符结束事件
    public Dictionary<int, Dictionary<Patch, List<NoteEvent>>> noteOffEvs = InitializeEventGroup<NoteEvent>();

    // 控制器事件
    public Dictionary<int, Dictionary<Patch, List<ControlChangeEvent>>> cCEvs = InitializeEventGroup<ControlChangeEvent>();

    private static Dictionary<int, Dictionary<Patch, List<T>>> InitializeEventGroup<T>()
    {
        var dict = new Dictionary<int, Dictionary<Patch, List<T>>>();

        for (int i = 1; i <= 16; i++)
        {
            dict[i] = [];
        }

        return dict;
    }
}