using NAudio.Midi;
using VeloxDev.Core.MVVM;

namespace Auris_Studio.ViewModels.MidiEvents;

public partial class ChannelAfterTouchEventViewModel : EventViewModel
{
    [VeloxProperty] private int _afterTouchPressure = 0;

    [VeloxCommand]
    public override void Read(object? parameter)
    {
        if (parameter is ChannelAfterTouchEvent afterTouchEvent)
        {
            AbsoluteTime = afterTouchEvent.AbsoluteTime;
            AfterTouchPressure = afterTouchEvent.AfterTouchPressure;
        }
    }

    [VeloxCommand]
    public override void Write(object? parameter)
    {
        if (parameter is IList<MidiEvent> events)
        {
            var afterTouchEvent = new ChannelAfterTouchEvent(
                AbsoluteTime,
                channel: Parent?.Channel ?? 1,
                AfterTouchPressure);
            events.Add(afterTouchEvent);
        }
    }

    /// <summary>
    /// 获取压力描述文本
    /// </summary>
    public string GetPressureDescription()
    {
        if (_afterTouchPressure <= 0)
            return "No pressure";
        else if (_afterTouchPressure < 32)
            return "Very light pressure";
        else if (_afterTouchPressure < 64)
            return "Light pressure";
        else if (_afterTouchPressure < 96)
            return "Moderate pressure";
        else
            return "Heavy pressure";
    }

    /// <summary>
    /// 获取标准化压力值 (0.0 - 1.0)
    /// </summary>
    public double GetNormalizedPressure()
    {
        return _afterTouchPressure / 127.0;
    }

    /// <summary>
    /// 从标准化值设置压力
    /// </summary>
    public void SetFromNormalizedPressure(double normalizedPressure)
    {
        if (normalizedPressure < 0.0 || normalizedPressure > 1.0)
            throw new ArgumentOutOfRangeException(nameof(normalizedPressure), "Normalized pressure must be between 0.0 and 1.0");

        AfterTouchPressure = (int)(normalizedPressure * 127.0);
    }
}