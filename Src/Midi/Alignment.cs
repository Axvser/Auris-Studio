namespace Auris_Studio.Midi;

/// <summary>
/// 时刻对齐（可组合的标志位枚举）
/// 组合方式：一个 NoteValue | 一个 Modifier | 一个 Tuplet
/// </summary>
[Flags]
public enum Alignment
{
    // --- 第一部分：音符时值 (NoteValue) ---
    // 标准时值
    DoubleWholeNote = 1 << 0,
    WholeNote = 1 << 1,
    HalfNote = 1 << 2,
    QuarterNote = 1 << 3,
    EighthNote = 1 << 4,
    SixteenthNote = 1 << 5,
    ThirtySecondNote = 1 << 6,
    SixtyFourthNote = 1 << 7,
    OneTwentyEighthNote = 1 << 8, // 一百二十八分音符

    // 时值部分掩码
    NoteValueMask = DoubleWholeNote | WholeNote | HalfNote | QuarterNote |
                    EighthNote | SixteenthNote | ThirtySecondNote |
                    SixtyFourthNote | OneTwentyEighthNote,

    // --- 第二部分：修饰符 (Modifier) ---
    // 附点
    Dot = 1 << 9,             // 单附点
    DoubleDot = 1 << 10,      // 双附点
    // 修饰符部分掩码
    ModifierMask = Dot | DoubleDot,

    // --- 第三部分：连音 (Tuplet) ---
    // 三连音
    Triplet = 1 << 11,
    // 特殊连音
    Quintuplet = 1 << 12,     // 五连音
    Septuplet = 1 << 13,      // 七连音
    // 连音部分掩码
    TupletMask = Triplet | Quintuplet | Septuplet,
}