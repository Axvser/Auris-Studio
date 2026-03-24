# MIDI 事件 - 视图模型

基于 NAudio 提供的 MidiEvent 及其子类，项目在 MVVM 模式下重建对应视图模型

基于 VeloxDev.Core.Extension 及其依赖的 Newtonsoft.Json，所有 MIDI 上下文可以轻松实现本地化

## MIDI 事件概述

MIDI 协议允许你同时操作16个通道，每个通道理解为一个独立的"演奏家"，NAudio对其取 [1,16] 的编程值

按种类可划分成 `通道事件` `元数据事件` `无通道数据事件`

---

## MIDI 事件分类详述

### 1. 通道事件 (Channel Events)
**定义**：与特定MIDI通道相关联的事件，控制音符、控制器、音色等演奏参数。

| 事件类型 | NAudio 对应类 | 视图模型类 | 功能描述 | 通道范围 |
|---------|--------------|-----------|---------|---------|
| **音符事件** | `NoteOnEvent`<br>`NoteEvent` (NoteOff) | `NoteEventViewModel` | 音符开始/结束，包含音高、力度、时长 | 1-16 |
| **音色变化** | `PatchChangeEvent` | 通过`MidiTrackViewModel.Patch`属性 | 切换乐器音色（Program Change） | 1-16 |
| **控制器事件** | `ControlChangeEvent` | `ControlChangeEventViewModel` | 音量、声相、表情、延音踏板等控制 | 1-16 |
| **通道触后** | `ChannelAfterTouchEvent` | `ChannelAfterTouchEventViewModel` | 通道压力变化（Channel Aftertouch） | 1-16 |
| **音高弯音** | `PitchWheelChangeEvent` | `PitchWheelChangeEventViewModel` | 弯音轮控制（Pitch Bend） | 1-16 |

**特性**：
- 每个事件必须指定通道号（1-16）
- 同一通道内的事件按时间顺序处理
- 可通过通道隔离不同乐器声部

### 2. 元数据事件 (Meta Events)
**定义**：描述乐曲结构、速度、拍号等全局信息的非音频数据。

| 事件类型 | NAudio 对应类 | 视图模型类 | 功能描述 | 通道处理 |
|---------|--------------|-----------|---------|---------|
| **速度事件** | `TempoEvent` | `TempoEventViewModel` | 设置乐曲速度（BPM） | 程序收集并统一至通道1 |
| **拍号事件** | `TimeSignatureEvent` | `TimeSignatureEventViewModel` | 设置拍号（如4/4, 3/4） |  |
| **调号事件** | `KeySignatureEvent` | `KeySignatureEventViewModel` | 设置调性（如C大调，G小调） |  |
| **文本事件** | `TextEvent` | `TextEventViewModel` | 歌词、标记、版权等文本信息 |  |
| **轨道名称** | `TextEvent` (SequenceTrackName) | `TextEventViewModel` | 音轨名称标识 |  |
| **音序器专有** | `SequencerSpecificEvent` | `SequencerSpecificEventViewModel` | DAW专属数据 |  |

**特性**：
- 无通道概念，但通常存储在第一个轨道
- 影响整个MIDI序列的播放
- 可在乐曲中任意时间点变化

### 3. 无通道数据事件 (System Events)
**定义**：系统级消息，不归属于任何通道，用于设备控制和扩展功能。

| 事件类型 | NAudio 对应类 | 视图模型类 | 功能描述 | 通道处理 |
|---------|--------------|-----------|---------|---------|
| **系统专有** | `SysexEvent` | `SysexEventViewModel` | 厂商自定义系统消息 | 实际没有值，程序处理为固定0 |

**特性**：
- 不关联特定MIDI通道
- 用于设备识别、参数查询、系统重置
- 通常具有厂商特定的数据格式

---

## NAudio MidiEvent 继承关系图

```
MidiEvent (抽象基类)
├── MetaEvent (元数据事件)
│   ├── TempoEvent (速度)
│   ├── TimeSignatureEvent (拍号)
│   ├── KeySignatureEvent (调号)
│   ├── TextEvent (文本)
│   │   ├── 普通文本 (TextEvent)
│   │   ├── 歌词 (Lyric)
│   │   ├── 标记 (Marker)
│   │   ├── 提示点 (CuePoint)
│   │   ├── 轨道名称 (SequenceTrackName)
│   │   ├── 乐器名称 (TrackInstrumentName)
│   │   ├── 版权信息 (Copyright)
│   │   └── 设备名称 (DeviceName)
│   └── SequencerSpecificEvent (音序器专有)
│
├── ChannelEvent (通道事件，抽象)
│   ├── NoteEvent (音符事件，抽象)
│   │   ├── NoteOnEvent (音符开始)
│   │   └── NoteEvent (NoteOff) (音符结束)
│   ├── PatchChangeEvent (音色变化)
│   ├── ControlChangeEvent (控制器变化)
│   ├── PitchWheelChangeEvent (音高弯音)
│   └── ChannelAfterTouchEvent (通道触后)
│
└── SystemExclusiveEvent (系统专有事件，抽象)
    └── SysexEvent (系统专有消息)
```

## 视图模型与NAudio事件映射

### 映射关系表

| NAudio 事件类 | 视图模型类 | 项目中的文件 | 主要属性映射 |
|--------------|-----------|------------|-------------|
| `NoteOnEvent` + `NoteEvent` | `NoteEventViewModel` | NoteEventViewModel.cs | `AbsoluteTime`, `Note`(Pitch), `OnVelocity`, `OffVelocity`, `DeltaTime` |
| `ControlChangeEvent` | `ControlChangeEventViewModel` | ControlChangeEventViewModel.cs | `AbsoluteTime`, `MidiController`, `Value` |
| `TempoEvent` | `TempoEventViewModel` | TempoEventViewModel.cs | `AbsoluteTime`, `BPM` |
| `TimeSignatureEvent` | `TimeSignatureEventViewModel` | TimeSignatureEventViewModel.cs | `AbsoluteTime`, `Numerator`, `Denominator` |
| `KeySignatureEvent` | `KeySignatureEventViewModel` | KeySignatureEventViewModel.cs | `AbsoluteTime`, `SharpsFlats`, `MajorMinor` |
| `TextEvent` | `TextEventViewModel` | TextEventViewModel.cs | `AbsoluteTime`, `Text`, `MetaEventType` |
| `ChannelAfterTouchEvent` | `ChannelAfterTouchEventViewModel` | ChannelAfterTouchEventViewModel.cs | `AbsoluteTime`, `AfterTouchPressure`, `Channel` |
| `PitchWheelChangeEvent` | `PitchWheelChangeEventViewModel` | PitchWheelChangeEventViewModel.cs | `AbsoluteTime`, `Pitch`, `Channel`, `PitchOffset`, `PitchSemitones` |
| `SysexEvent` | `SysexEventViewModel` | SysexEventViewModel.cs | `AbsoluteTime`, `Data`, `Text`(解析后) |
| `SequencerSpecificEvent` | `SequencerSpecificEventViewModel` | SequencerSpecificEventViewModel.cs | `AbsoluteTime`, `Data`, `Text`, `HexData`, `AsciiData` |

### 数据流图

> 关于导入与导出，系统遵循下述流程

```
MIDI文件 (.mid)
    ↓
MidiParser.Import() → origin MidiResult
    ↓
MidiEditorViewModel.Read() → 加载视图模型
    ↓
UI绑定与用户编辑
    ↓
MidiEditorViewModel.Write() → new MidiResult
    ↓
MidiSynthesizer.Export() → 导出 MIDI 文件
```