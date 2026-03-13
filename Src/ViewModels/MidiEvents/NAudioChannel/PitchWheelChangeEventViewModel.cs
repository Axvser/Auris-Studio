using NAudio.Midi;
using VeloxDev.Core.MVVM;

namespace Auris_Studio.ViewModels.MidiEvents;

public partial class PitchWheelChangeEventViewModel : EventViewModel
{
    [VeloxProperty] private int _pitch = 8192; // 默认值 (0x2000)
    [VeloxProperty] public partial int PitchOffset { get; private set; }
    [VeloxProperty] public partial double PitchSemitones { get; private set; }
    [VeloxProperty] public partial double PitchPercentage { get; private set; }

    partial void OnPitchChanged(int oldValue, int newValue)
    {
        PitchOffset = newValue - 8192;
    }
    partial void OnPitchOffsetChanged(int oldValue, int newValue)
    {
        PitchSemitones = newValue / 4096.0;
        PitchPercentage = newValue / 8191.0 * 100.0;
    }

    [VeloxCommand]
    public override void Read(object? parameter)
    {
        if (parameter is PitchWheelChangeEvent pitchEvent)
        {
            AbsoluteTime = pitchEvent.AbsoluteTime;
            Pitch = pitchEvent.Pitch;
        }
    }

    [VeloxCommand]
    public override void Write(object? parameter)
    {
        if (parameter is IList<MidiEvent> events)
        {
            var pitchEvent = new PitchWheelChangeEvent(
                AbsoluteTime,
                channel: Parent?.Channel ?? 1,
                Pitch);
            events.Add(pitchEvent);
        }
    }

    /// <summary>
    /// 设置弯音偏移量 (-8192 到 8191)
    /// </summary>
    public void SetPitchOffset(int offset)
    {
        int newPitch = 8192 + offset;
        if (newPitch < 0 || newPitch >= 16384)
            throw new ArgumentOutOfRangeException(nameof(offset), "Resulting pitch must be in the range 0 - 0x3FFF");

        Pitch = newPitch;
    }

    /// <summary>
    /// 设置弯音半音数
    /// </summary>
    /// <param name="semitones">半音数 (±2 标准范围)</param>
    public void SetPitchSemitones(double semitones)
    {
        int offset = (int)(semitones * 4096.0);
        SetPitchOffset(offset);
    }

    /// <summary>
    /// 设置弯音百分比
    /// </summary>
    /// <param name="percentage">百分比 (±100%)</param>
    public void SetPitchPercentage(double percentage)
    {
        int offset = (int)(percentage / 100.0 * 8191.0);
        SetPitchOffset(offset);
    }

    /// <summary>
    /// 重置弯音到中心位置
    /// </summary>
    public void ResetToCenter()
    {
        Pitch = 8192;
    }

    /// <summary>
    /// 设置最大上弯音
    /// </summary>
    public void SetMaxUp()
    {
        Pitch = 16383; // 0x3FFF
    }

    /// <summary>
    /// 设置最大下弯音
    /// </summary>
    public void SetMaxDown()
    {
        Pitch = 0;
    }

    /// <summary>
    /// 获取弯音方向描述
    /// </summary>
    public string GetPitchDirectionDescription()
    {
        int offset = PitchOffset;

        if (offset == 0)
            return "Center";
        else if (offset > 0)
        {
            double percent = Math.Abs(PitchPercentage);
            if (percent < 10)
                return $"Slightly up ({percent:F1}%)";
            else if (percent < 50)
                return $"Moderately up ({percent:F1}%)";
            else
                return $"Fully up ({percent:F1}%)";
        }
        else
        {
            double percent = Math.Abs(PitchPercentage);
            if (percent < 10)
                return $"Slightly down ({percent:F1}%)";
            else if (percent < 50)
                return $"Moderately down ({percent:F1}%)";
            else
                return $"Fully down ({percent:F1}%)";
        }
    }

    /// <summary>
    /// 获取弯音值描述
    /// </summary>
    public string GetPitchValueDescription()
    {
        int offset = PitchOffset;

        if (offset == 0)
            return "Center (no bend)";

        string direction = offset > 0 ? "up" : "down";
        double semitones = Math.Abs(PitchSemitones);
        double percent = Math.Abs(PitchPercentage);

        return $"Pitch bend {direction} {semitones:F2} semitones ({percent:F1}%)";
    }
}