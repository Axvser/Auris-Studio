namespace Auris_Studio.Midi;

/// <summary>
/// <para>音色</para>
/// </summary>
public enum Patch : int
{
    // ========== 钢琴 (Piano) ==========
    /// <summary> 大钢琴 </summary>
    AcousticGrandPiano = 0,
    /// <summary> 明亮钢琴 </summary>
    BrightAcousticPiano = 1,
    /// <summary> 电钢琴 </summary>
    ElectricGrandPiano = 2,
    /// <summary> 酒吧钢琴 </summary>
    HonkyTonkPiano = 3,
    /// <summary> 电钢琴1 </summary>
    ElectricPiano1 = 4,
    /// <summary> 电钢琴2 </summary>
    ElectricPiano2 = 5,
    /// <summary> 大键琴 </summary>
    Harpsichord = 6,
    /// <summary> 击弦古钢琴 </summary>
    Clavinet = 7,

    // ========== 色彩打击乐器 (Chromatic Percussion) ==========
    /// <summary> 钢片琴 </summary>
    Celesta = 8,
    /// <summary> 钟琴 </summary>
    Glockenspiel = 9,
    /// <summary> 八音盒 </summary>
    MusicBox = 10,
    /// <summary> 颤音琴 </summary>
    Vibraphone = 11,
    /// <summary> 马林巴琴 </summary>
    Marimba = 12,
    /// <summary> 木琴 </summary>
    Xylophone = 13,
    /// <summary> 管钟 </summary>
    TubularBells = 14,
    /// <summary> 扬琴 </summary>
    Dulcimer = 15,

    // ========== 风琴 (Organ) ==========
    /// <summary> 拉杆风琴 </summary>
    DrawbarOrgan = 16,
    /// <summary> 打击式风琴 </summary>
    PercussiveOrgan = 17,
    /// <summary> 摇滚风琴 </summary>
    RockOrgan = 18,
    /// <summary> 教堂风琴 </summary>
    ChurchOrgan = 19,
    /// <summary> 簧管风琴 </summary>
    ReedOrgan = 20,
    /// <summary> 手风琴 </summary>
    Accordion = 21,
    /// <summary> 口琴 </summary>
    Harmonica = 22,
    /// <summary> 探戈手风琴 </summary>
    TangoAccordion = 23,

    // ========== 吉他 (Guitar) ==========
    /// <summary> 尼龙弦吉他 </summary>
    AcousticGuitarNylon = 24,
    /// <summary> 钢弦吉他 </summary>
    AcousticGuitarSteel = 25,
    /// <summary> 爵士电吉他 </summary>
    ElectricGuitarJazz = 26,
    /// <summary> 清音电吉他 </summary>
    ElectricGuitarClean = 27,
    /// <summary> 闷音电吉他 </summary>
    ElectricGuitarMuted = 28,
    /// <summary> 过载吉他 </summary>
    OverdrivenGuitar = 29,
    /// <summary> 失真吉他 </summary>
    DistortionGuitar = 30,
    /// <summary> 吉他和声 </summary>
    GuitarHarmonics = 31,

    // ========== 贝斯 (Bass) ==========
    /// <summary> 原声贝斯 </summary>
    AcousticBass = 32,
    /// <summary> 指弹电贝斯 </summary>
    ElectricBassFinger = 33,
    /// <summary> 拨片电贝斯 </summary>
    ElectricBassPick = 34,
    /// <summary> 无品贝斯 </summary>
    FretlessBass = 35,
    /// <summary> 击勾贝斯1 </summary>
    SlapBass1 = 36,
    /// <summary> 击勾贝斯2 </summary>
    SlapBass2 = 37,
    /// <summary> 合成贝斯1 </summary>
    SynthBass1 = 38,
    /// <summary> 合成贝斯2 </summary>
    SynthBass2 = 39,

    // ========== 弦乐 (Strings) ==========
    /// <summary> 小提琴 </summary>
    Violin = 40,
    /// <summary> 中提琴 </summary>
    Viola = 41,
    /// <summary> 大提琴 </summary>
    Cello = 42,
    /// <summary> 低音提琴 </summary>
    Contrabass = 43,
    /// <summary> 震音弦乐 </summary>
    TremoloStrings = 44,
    /// <summary> 拨奏弦乐 </summary>
    PizzicatoStrings = 45,
    /// <summary> 竖琴 </summary>
    OrchestralHarp = 46,
    /// <summary> 定音鼓 </summary>
    Timpani = 47,
    /// <summary> 弦乐合奏1 </summary>
    StringEnsemble1 = 48,
    /// <summary> 弦乐合奏2 </summary>
    StringEnsemble2 = 49,
    /// <summary> 合成弦乐1 </summary>
    SynthStrings1 = 50,
    /// <summary> 合成弦乐2 </summary>
    SynthStrings2 = 51,

    // ========== 合唱/人声 (Choir/Voice) ==========
    /// <summary> 人声“啊” </summary>
    ChoirAahs = 52,
    /// <summary> 人声“哦” </summary>
    VoiceOohs = 53,
    /// <summary> 合成人声 </summary>
    SynthVoice = 54,
    /// <summary> 管弦乐强音 </summary>
    OrchestraHit = 55,

    // ========== 铜管 (Brass) ==========
    /// <summary> 小号 </summary>
    Trumpet = 56,
    /// <summary> 长号 </summary>
    Trombone = 57,
    /// <summary> 大号 </summary>
    Tuba = 58,
    /// <summary> 弱音小号 </summary>
    MutedTrumpet = 59,
    /// <summary> 圆号 </summary>
    FrenchHorn = 60,
    /// <summary> 铜管乐组 </summary>
    BrassSection = 61,
    /// <summary> 合成铜管1 </summary>
    SynthBrass1 = 62,
    /// <summary> 合成铜管2 </summary>
    SynthBrass2 = 63,

    // ========== 簧乐 (Reed) ==========
    /// <summary> 高音萨克斯 </summary>
    SopranoSax = 64,
    /// <summary> 中音萨克斯 </summary>
    AltoSax = 65,
    /// <summary> 次中音萨克斯 </summary>
    TenorSax = 66,
    /// <summary> 上低音萨克斯 </summary>
    BaritoneSax = 67,
    /// <summary> 双簧管 </summary>
    Oboe = 68,
    /// <summary> 英国管 </summary>
    EnglishHorn = 69,
    /// <summary> 巴松管 </summary>
    Bassoon = 70,
    /// <summary> 单簧管 </summary>
    Clarinet = 71,

    // ========== 管乐 (Pipe) ==========
    /// <summary> 短笛 </summary>
    Piccolo = 72,
    /// <summary> 长笛 </summary>
    Flute = 73,
    /// <summary> 竖笛 </summary>
    Recorder = 74,
    /// <summary> 排箫 </summary>
    PanFlute = 75,
    /// <summary> 吹瓶声 </summary>
    BlownBottle = 76,
    /// <summary> 尺八 </summary>
    Shakuhachi = 77,
    /// <summary> 哨声 </summary>
    Whistle = 78,
    /// <summary> 陶笛 </summary>
    Ocarina = 79,

    // ========== 合成主音 (Synth Lead) ==========
    /// <summary> 主音1（方波） </summary>
    Lead1Square = 80,
    /// <summary> 主音2（锯齿波） </summary>
    Lead2Sawtooth = 81,
    /// <summary> 主音3（汽笛风琴） </summary>
    Lead3Calliope = 82,
    /// <summary> 主音4（啾啾声） </summary>
    Lead4Chiff = 83,
    /// <summary> 主音5（电吉他声） </summary>
    Lead5Charang = 84,
    /// <summary> 主音6（人声） </summary>
    Lead6Voice = 85,
    /// <summary> 主音7（五度） </summary>
    Lead7Fifths = 86,
    /// <summary> 主音8（贝斯+主音） </summary>
    Lead8BassAndLead = 87,

    // ========== 合成铺底 (Synth Pad) ==========
    /// <summary> 铺底音1（新世纪） </summary>
    Pad1NewAge = 88,
    /// <summary> 铺底音2（温暖） </summary>
    Pad2Warm = 89,
    /// <summary> 铺底音3（复音合成） </summary>
    Pad3Polysynth = 90,
    /// <summary> 铺底音4（合唱） </summary>
    Pad4Choir = 91,
    /// <summary> 铺底音5（弓弦） </summary>
    Pad5Bowed = 92,
    /// <summary> 铺底音6（金属） </summary>
    Pad6Metallic = 93,
    /// <summary> 铺底音7（光晕） </summary>
    Pad7Halo = 94,
    /// <summary> 铺底音8（扫掠） </summary>
    Pad8Sweep = 95,

    // ========== 合成效果 (Synth Effects) ==========
    /// <summary> 效果音1（雨声） </summary>
    FX1Rain = 96,
    /// <summary> 效果音2（音轨） </summary>
    FX2Soundtrack = 97,
    /// <summary> 效果音3（水晶） </summary>
    FX3Crystal = 98,
    /// <summary> 效果音4（氛围） </summary>
    FX4Atmosphere = 99,
    /// <summary> 效果音5（明亮） </summary>
    FX5Brightness = 100,
    /// <summary> 效果音6（地精） </summary>
    FX6Goblins = 101,
    /// <summary> 效果音7（回声） </summary>
    FX7Echoes = 102,
    /// <summary> 效果音8（科幻） </summary>
    FX8SciFi = 103,

    // ========== 民族乐器 (Ethnic) ==========
    /// <summary> 西塔琴 </summary>
    Sitar = 104,
    /// <summary> 班卓琴 </summary>
    Banjo = 105,
    /// <summary> 三味线 </summary>
    Shamisen = 106,
    /// <summary> 日本筝 </summary>
    Koto = 107,
    /// <summary> 拇指琴 </summary>
    Kalimba = 108,
    /// <summary> 风笛 </summary>
    BagPipe = 109,
    /// <summary> 民间小提琴 </summary>
    Fiddle = 110,
    /// <summary> 唢呐（萨奈） </summary>
    Shanai = 111,

    // ========== 打击乐 (Percussive) ==========
    /// <summary> 叮当铃 </summary>
    TinkleBell = 112,
    /// <summary> 阿哥哥铃 </summary>
    Agogo = 113,
    /// <summary> 钢鼓 </summary>
    SteelDrums = 114,
    /// <summary> 木鱼 </summary>
    Woodblock = 115,
    /// <summary> 太鼓 </summary>
    TaikoDrum = 116,
    /// <summary> 旋律通通鼓 </summary>
    MelodicTom = 117,
    /// <summary> 合成鼓 </summary>
    SynthDrum = 118,
    /// <summary> 反镲 </summary>
    ReverseCymbal = 119,

    // ========== 音效 (Sound Effects) ==========
    /// <summary> 吉他品丝噪音 </summary>
    GuitarFretNoise = 120,
    /// <summary> 呼吸声 </summary>
    BreathNoise = 121,
    /// <summary> 海浪声 </summary>
    Seashore = 122,
    /// <summary> 鸟鸣 </summary>
    BirdTweet = 123,
    /// <summary> 电话铃声 </summary>
    TelephoneRing = 124,
    /// <summary> 直升机声 </summary>
    Helicopter = 125,
    /// <summary> 掌声 </summary>
    Applause = 126,
    /// <summary> 枪声 </summary>
    Gunshot = 127,

    // ========== 通道10 独占打击乐 (Channel 10 Exclusive Drums) ==========
    // 注意：以下值为原始 Note Number + 128
    /// <summary> 原声低音鼓 (Acoustic Bass Drum) </summary>
    DrumAcousticBassDrum = 35 + 128,
    /// <summary> 低音鼓 (Bass Drum 1) </summary>
    DrumBassDrum1 = 36 + 128,
    /// <summary> 侧边击 (Side Stick) </summary>
    DrumSideStick = 37 + 128,
    /// <summary> 原声军鼓 (Acoustic Snare) </summary>
    DrumAcousticSnare = 38 + 128,
    /// <summary> 拍手 (Hand Clap) </summary>
    DrumHandClap = 39 + 128,
    /// <summary> 电子军鼓 (Electric Snare) </summary>
    DrumElectricSnare = 40 + 128,
    /// <summary> 落地嗵鼓 (Low Floor Tom) </summary>
    DrumLowFloorTom = 41 + 128,
    /// <summary> 闭合踩镲 (Closed Hi-Hat) </summary>
    DrumClosedHiHat = 42 + 128,
    /// <summary> 高架嗵鼓 (High Floor Tom) </summary>
    DrumHighFloorTom = 43 + 128,
    /// <summary> 踏板踩镲 (Pedal Hi-Hat) </summary>
    DrumPedalHiHat = 44 + 128,
    /// <summary> 低嗵鼓 (Low Tom) </summary>
    DrumLowTom = 45 + 128,
    /// <summary> 开放踩镲 (Open Hi-Hat) </summary>
    DrumOpenHiHat = 46 + 128,
    /// <summary> 中低嗵鼓 (Low-Mid Tom) </summary>
    DrumLowMidTom = 47 + 128,
    /// <summary> 中高嗵鼓 (Hi-Mid Tom) </summary>
    DrumHiMidTom = 48 + 128,
    /// <summary> 吊镲 (Crash Cymbal 1) </summary>
    DrumCrashCymbal1 = 49 + 128,
    /// <summary> 高嗵鼓 (High Tom) </summary>
    DrumHighTom = 50 + 128,
    /// <summary> 节奏镲 (Ride Cymbal 1) </summary>
    DrumRideCymbal1 = 51 + 128,
    /// <summary> 中国镲 (Chinese Cymbal) </summary>
    DrumChineseCymbal = 52 + 128,
    /// <summary> 叮叮镲 (Ride Bell) </summary>
    DrumRideBell = 53 + 128,
    /// <summary> 手铃 (Tambourine) </summary>
    DrumTambourine = 54 + 128,
    /// <summary> 飞溅镲 (Splash Cymbal) </summary>
    DrumSplashCymbal = 55 + 128,
    /// <summary> 牛铃 (Cowbell) </summary>
    DrumCowbell = 56 + 128,
    /// <summary> 吊镲2 (Crash Cymbal 2) </summary>
    DrumCrashCymbal2 = 57 + 128,
    /// <summary> 振动拍 (Vibra-slap) </summary>
    DrumVibraSlap = 58 + 128,
    /// <summary> 节奏镲2 (Ride Cymbal 2) </summary>
    DrumRideCymbal2 = 59 + 128,
    /// <summary> 高音邦戈鼓 (High Bongo) </summary>
    DrumHighBongo = 60 + 128,
    /// <summary> 低音邦戈鼓 (Low Bongo) </summary>
    DrumLowBongo = 61 + 128,
    /// <summary> 哑高音康加鼓 (Mute High Conga) </summary>
    DrumMuteHighConga = 62 + 128,
    /// <summary> 开放高音康加鼓 (Open High Conga) </summary>
    DrumOpenHighConga = 63 + 128,
    /// <summary> 低音康加鼓 (Low Conga) </summary>
    DrumLowConga = 64 + 128,
    /// <summary> 高音天巴鼓 (High Timbale) </summary>
    DrumHighTimbale = 65 + 128,
    /// <summary> 低音天巴鼓 (Low Timbale) </summary>
    DrumLowTimbale = 66 + 128,
    /// <summary> 高音阿果果铃 (High Agogo) </summary>
    DrumHighAgogo = 67 + 128,
    /// <summary> 低音阿果果铃 (Low Agogo) </summary>
    DrumLowAgogo = 68 + 128,
    /// <summary> 卡巴萨 (Cabasa) </summary>
    DrumCabasa = 69 + 128,
    /// <summary> 沙锤 (Maracas) </summary>
    DrumMaracas = 70 + 128,
    /// <summary> 短哨 (Short Whistle) </summary>
    DrumShortWhistle = 71 + 128,
    /// <summary> 长哨 (Long Whistle) </summary>
    DrumLongWhistle = 72 + 128,
    /// <summary> 短刮葫 (Short Guiro) </summary>
    DrumShortGuiro = 73 + 128,
    /// <summary> 长刮葫 (Long Guiro) </summary>
    DrumLongGuiro = 74 + 128,
    /// <summary> 响棒 (Claves) </summary>
    DrumClaves = 75 + 128,
    /// <summary> 高音木鱼 (High Wood Block) </summary>
    DrumHighWoodBlock = 76 + 128,
    /// <summary> 低音木鱼 (Low Wood Block) </summary>
    DrumLowWoodBlock = 77 + 128,
    /// <summary> 静音库伊卡 (Mute Cuica) </summary>
    DrumMuteCuica = 78 + 128,
    /// <summary> 开放库伊卡 (Open Cuica) </summary>
    DrumOpenCuica = 79 + 128,
    /// <summary> 静音三角铁 (Mute Triangle) </summary>
    DrumMuteTriangle = 80 + 128,
    /// <summary> 开放三角铁 (Open Triangle) </summary>
    DrumOpenTriangle = 81 + 128
}

public static class PathchEx
{
    public static bool IsDrum(this Patch patch, out int programeNumber)
    {
        var isDrum = patch >= Patch.DrumAcousticBassDrum;
        programeNumber = isDrum ? (int)patch - 128 : (int)patch;
        return isDrum;
    }
}