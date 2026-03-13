namespace Auris_Studio.Midi;

/// <summary>
/// 时刻对齐
/// </summary>
public enum Alignment
{
    // 标准时值
    DoubleWholeNote,
    WholeNote,
    HalfNote,
    QuarterNote,
    EighthNote,
    SixteenthNote,
    ThirtySecondNote,
    SixtyFourthNote,
    OneTwentyEighthNote,       // 一百二十八分音符

    // 附点音符
    DottedDoubleWholeNote,
    DottedWholeNote,
    DottedHalfNote,
    DottedQuarterNote,
    DottedEighthNote,
    DottedSixteenthNote,
    DottedThirtySecondNote,

    // 双附点音符
    DoubleDottedHalfNote,
    DoubleDottedQuarterNote,
    DoubleDottedEighthNote,

    // 三连音
    TripletWholeNote,
    TripletHalfNote,
    TripletQuarterNote,
    TripletEighthNote,
    TripletSixteenthNote,

    // 特殊组合
    QuintupletEighthNote,       // 五连音
    SeptupletEighthNote,        // 七连音
    TripletQuarterNoteDotted    // 附点四分音符三连音
}

public static class MidiDurationCalculator
{
    private static readonly Dictionary<Alignment, double> DurationRatioMap = new()
    {
        // 标准时值（相对于四分音符的倍数）
        [Alignment.DoubleWholeNote] = 8.0,
        [Alignment.WholeNote] = 4.0,
        [Alignment.HalfNote] = 2.0,
        [Alignment.QuarterNote] = 1.0,
        [Alignment.EighthNote] = 0.5,
        [Alignment.SixteenthNote] = 0.25,
        [Alignment.ThirtySecondNote] = 0.125,
        [Alignment.SixtyFourthNote] = 0.0625,
        [Alignment.OneTwentyEighthNote] = 0.03125,

        // 附点音符（基础时值的1.5倍）
        [Alignment.DottedDoubleWholeNote] = 12.0,    // 8 * 1.5
        [Alignment.DottedWholeNote] = 6.0,           // 4 * 1.5
        [Alignment.DottedHalfNote] = 3.0,            // 2 * 1.5
        [Alignment.DottedQuarterNote] = 1.5,         // 1 * 1.5
        [Alignment.DottedEighthNote] = 0.75,         // 0.5 * 1.5
        [Alignment.DottedSixteenthNote] = 0.375,     // 0.25 * 1.5
        [Alignment.DottedThirtySecondNote] = 0.1875, // 0.125 * 1.5

        // 双附点音符（基础时值的1.75倍）
        [Alignment.DoubleDottedHalfNote] = 3.5,      // 2 * 1.75
        [Alignment.DoubleDottedQuarterNote] = 1.75,  // 1 * 1.75
        [Alignment.DoubleDottedEighthNote] = 0.875,  // 0.5 * 1.75

        // 三连音（三个音符占据两个基础时值）
        [Alignment.TripletWholeNote] = 8.0 / 3.0,    // 三个音符占据一个二全音符时长
        [Alignment.TripletHalfNote] = 4.0 / 3.0,     // 三个音符占据一个全音符时长
        [Alignment.TripletQuarterNote] = 2.0 / 3.0,  // 三个音符占据一个二分音符时长
        [Alignment.TripletEighthNote] = 1.0 / 3.0,   // 三个音符占据一个四分音符时长
        [Alignment.TripletSixteenthNote] = 0.5 / 3.0,// 三个音符占据一个八分音符时长

        // 特殊组合
        [Alignment.QuintupletEighthNote] = 0.4,      // 五个音符占据两个四分音符时长
        [Alignment.SeptupletEighthNote] = 2.0 / 7.0, // 七个音符占据两个四分音符时长
        [Alignment.TripletQuarterNoteDotted] = 1.0   // 三个附点四分音符占据两个全音符时长
    };

    public static long GetTicks(Alignment durationType, int ppqn)
    {
        if (DurationRatioMap.TryGetValue(durationType, out double ratio))
        {
            return (long)Math.Round(ppqn * ratio);
        }

        return ppqn;
    }

    public static double GetRatio(Alignment durationType)
    {
        return DurationRatioMap.TryGetValue(durationType, out double ratio) ? ratio : 1.0;
    }
}
