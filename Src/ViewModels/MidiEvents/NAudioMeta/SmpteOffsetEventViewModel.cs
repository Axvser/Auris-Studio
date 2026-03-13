using Auris_Studio.Midi;
using NAudio.Midi;
using VeloxDev.Core.MVVM;

namespace Auris_Studio.ViewModels.MidiEvents;

public partial class SmpteOffsetEventViewModel : MetaEventViewModel
{
    [VeloxProperty] private byte _hours = 0;
    [VeloxProperty] private byte _minutes = 0;
    [VeloxProperty] private byte _seconds = 0;
    [VeloxProperty] private byte _frames = 0;
    [VeloxProperty] private byte _subFrames = 0;
    [VeloxProperty] public partial string Description { get; private set; }

    partial void OnHoursChanged(byte oldValue, byte newValue)
    {
        Description = GetFullDescription();
    }

    [VeloxCommand]
    public override void Read(object? parameter)
    {
        if (parameter is SmpteOffsetEvent smpteEvent)
        {
            Hours = (byte)smpteEvent.Hours;
            Minutes = (byte)smpteEvent.Minutes;
            Seconds = (byte)smpteEvent.Seconds;
            Frames = (byte)smpteEvent.Frames;
            SubFrames = (byte)smpteEvent.SubFrames;

            AbsoluteTime = smpteEvent.AbsoluteTime;
        }
    }

    [VeloxCommand]
    public override void Write(object? parameter)
    {
        if (parameter is IList<MidiEvent> midiEvents)
        {
            var smpteEvent = new SmpteOffsetEvent(
                _hours,
                _minutes,
                _seconds,
                _frames,
                _subFrames
            )
            {
                AbsoluteTime = this.AbsoluteTime
            };

            midiEvents.Add(smpteEvent);
        }
    }

    /// <summary>
    /// 获取 SMPTE 帧率类型的描述。
    /// 根据小时的最高位（Bit 5-6）判断，遵循 MIDI 规范。
    /// </summary>
    public SmpteFrameRate GetFrameRate()
    {
        // 在 SMPTE Offset 事件中，帧率信息编码在 `hours` 字节的最高两位 (bits 5-6)
        // 0x00 = 24 fps, 0x01 = 25 fps, 0x10 = 30 drop-frame, 0x11 = 30 non-drop
        int frameRateBits = (_hours >> 5) & 0x03; // 取第5和第6位
        return frameRateBits switch
        {
            0 => SmpteFrameRate.Fps24,
            1 => SmpteFrameRate.Fps25,
            2 => SmpteFrameRate.Fps30Drop,
            3 => SmpteFrameRate.Fps30NonDrop,
            _ => SmpteFrameRate.Unknown
        };
    }

    /// <summary>
    /// 设置 SMPTE 帧率类型。这将相应地修改 `Hours` 属性的帧率位。
    /// </summary>
    /// <param name="frameRate">要设置的帧率</param>
    public void SetFrameRate(SmpteFrameRate frameRate)
    {
        int frameRateBits = frameRate switch
        {
            SmpteFrameRate.Fps24 => 0,
            SmpteFrameRate.Fps25 => 1,
            SmpteFrameRate.Fps30Drop => 2,
            SmpteFrameRate.Fps30NonDrop => 3,
            _ => 0 // 默认为 24 fps
        };

        // 清除 hours 的高两位（帧率位），然后设置新的帧率位
        byte newHours = (byte)((_hours & 0x1F) | (frameRateBits << 5)); // 0x1F = 00011111
        Hours = newHours;
    }

    /// <summary>
    /// 获取格式化的 SMPTE 时间码字符串 (HH:MM:SS:FF)。
    /// </summary>
    public string GetFormattedTimeCode()
    {
        return $"{_hours:D2}:{_minutes:D2}:{_seconds:D2}:{_frames:D2}";
    }

    /// <summary>
    /// 获取完整的描述字符串，包括子帧。
    /// </summary>
    public string GetFullDescription()
    {
        var frameRate = GetFrameRate();
        return $"SMPTE Offset: {GetFormattedTimeCode()}.{_subFrames:D2} @ {frameRate}";
    }
}