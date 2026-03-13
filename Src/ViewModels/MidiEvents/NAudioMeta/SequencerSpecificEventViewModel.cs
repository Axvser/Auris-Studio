using NAudio.Midi;
using System.Text;
using VeloxDev.Core.MVVM;

namespace Auris_Studio.ViewModels.MidiEvents;

public partial class SequencerSpecificEventViewModel : EventViewModel
{
    [VeloxProperty] private byte[] _data = [];
    [VeloxProperty] public partial string Text { get; private set; }
    [VeloxProperty] public partial string HexData { get; private set; }
    [VeloxProperty] public partial string AsciiData { get; private set; }

    partial void OnDataChanged(byte[] oldValue, byte[] newValue)
    {
        // 当数据变化时，更新相关属性
        UpdateDerivedProperties();
    }

    public override void Read(object? parameter)
    {
        if (parameter is SequencerSpecificEvent sequencerEvent)
        {
            // 设置绝对时间
            AbsoluteTime = sequencerEvent.AbsoluteTime;

            // 设置数据（SequencerSpecificEvent 有公开的 Data 属性）
            Data = sequencerEvent.Data;

            // 更新衍生属性
            UpdateDerivedProperties();
        }
        else
        {
            throw new ArgumentException("Parameter must be of type SequencerSpecificEvent", nameof(parameter));
        }
    }

    public override void Write(object? parameter)
    {
        if (parameter is IList<MidiEvent> events)
        {
            // 创建新的 SequencerSpecificEvent
            var sequencerEvent = new SequencerSpecificEvent(Data, AbsoluteTime);
            events.Add(sequencerEvent);
        }
        else
        {
            throw new ArgumentException("Parameter must be of type IList<MidiEvent>", nameof(parameter));
        }
    }

    /// <summary>
    /// 更新衍生属性（十六进制和ASCII表示）
    /// </summary>
    private void UpdateDerivedProperties()
    {
        if (_data == null || _data.Length == 0)
        {
            Text = "Empty Sequencer Specific";
            HexData = string.Empty;
            AsciiData = string.Empty;
            return;
        }

        // 生成可读的文本描述
        Text = $"Sequencer Specific [{_data.Length} bytes]";

        // 生成十六进制字符串
        var hexBuilder = new StringBuilder();
        for (int i = 0; i < _data.Length; i++)
        {
            hexBuilder.Append($"{_data[i]:X2}");
            if (i < _data.Length - 1)
                hexBuilder.Append(' ');
        }
        HexData = hexBuilder.ToString();

        // 生成ASCII字符串（可打印字符）
        var asciiBuilder = new StringBuilder();
        foreach (byte b in _data)
        {
            if (b >= 32 && b <= 126) // 可打印ASCII字符
            {
                asciiBuilder.Append((char)b);
            }
            else
            {
                asciiBuilder.Append('.');
            }
        }
        AsciiData = asciiBuilder.ToString();
    }

    /// <summary>
    /// 尝试从文本中解析出数据
    /// 这通常用于从ToString()输出中提取数据
    /// </summary>
    public bool TryParseDataFromText(string text)
    {
        try
        {
            // 检查是否为SequencerSpecificEvent的ToString()输出
            if (text.Contains("SequencerSpecific:"))
            {
                // 格式示例: "0 SequencerSpecific: 8 0F 1E 2D 3C 4B 5A 69 78"
                // 我们需要提取十六进制部分

                int colonIndex = text.IndexOf(':');
                if (colonIndex < 0)
                    return false;

                string hexPart = text[(colonIndex + 1)..].Trim();
                var bytes = ParseHexString(hexPart);

                if (bytes.Length > 0)
                {
                    Data = bytes;
                    return true;
                }
            }
        }
        catch
        {
            // 解析失败
        }

        return false;
    }

    /// <summary>
    /// 解析十六进制字符串为字节数组
    /// </summary>
    private static byte[] ParseHexString(string hexString)
    {
        if (string.IsNullOrEmpty(hexString))
            return [];

        hexString = hexString.Trim();
        string[] hexValues = hexString.Split([' '], StringSplitOptions.RemoveEmptyEntries);

        var bytes = new byte[hexValues.Length];
        for (int i = 0; i < hexValues.Length; i++)
        {
            try
            {
                bytes[i] = Convert.ToByte(hexValues[i], 16);
            }
            catch
            {
                bytes[i] = 0;
            }
        }

        return bytes;
    }

    /// <summary>
    /// 检查是否为特定制造商或格式的Sequencer Specific数据
    /// </summary>
    public string? IdentifyDataFormat()
    {
        if (_data == null || _data.Length == 0)
            return null;

        // 检查是否为SMF (Standard MIDI File) 格式标识
        if (_data.Length >= 4)
        {
            // MIDI文件类型通常以"MThd"或"MTrk"开头
            if (_data[0] == 0x4D && _data[1] == 0x54 &&
                _data[2] == 0x68 && _data[3] == 0x64) // "MThd"
            {
                return "SMF Header Chunk";
            }
            else if (_data[0] == 0x4D && _data[1] == 0x54 &&
                     _data[2] == 0x72 && _data[3] == 0x6B) // "MTrk"
            {
                return "SMF Track Chunk";
            }
        }

        // 检查是否为XML格式
        if (_data.Length >= 5)
        {
            string start = Encoding.ASCII.GetString(_data, 0, Math.Min(5, _data.Length));
            if (start.StartsWith("<?xml") || start.StartsWith("<xml>"))
            {
                return "XML Data";
            }
        }

        // 检查是否为JSON格式
        if (_data.Length >= 2)
        {
            if (_data[0] == 0x7B && _data[1] == 0x22) // "{"
            {
                return "JSON Data";
            }
        }

        // 检查是否为DAW特定格式
        if (_data.Length >= 8)
        {
            // Cubase 格式示例检查
            if (_data[0] == 0x43 && _data[1] == 0x75 && _data[2] == 0x62) // "Cub"
            {
                return "Steinberg Cubase Data";
            }

            // Logic 格式检查
            if (_data[0] == 0x4C && _data[1] == 0x6F && _data[2] == 0x67 && _data[3] == 0x69) // "Logi"
            {
                return "Apple Logic Data";
            }
        }

        return "Unknown Format";
    }

    /// <summary>
    /// 尝试从ASCII字符串设置数据
    /// </summary>
    public bool SetDataFromAscii(string asciiText)
    {
        try
        {
            var bytes = Encoding.ASCII.GetBytes(asciiText);
            Data = bytes;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 尝试从十六进制字符串设置数据
    /// </summary>
    public bool SetDataFromHex(string hexText)
    {
        try
        {
            var bytes = ParseHexString(hexText);
            if (bytes.Length > 0)
            {
                Data = bytes;
                return true;
            }
        }
        catch
        {
            // 解析失败
        }

        return false;
    }

    /// <summary>
    /// 获取数据的摘要信息
    /// </summary>
    public string GetDataSummary()
    {
        if (_data == null || _data.Length == 0)
            return "Empty";

        var sb = new StringBuilder();
        sb.AppendLine($"Size: {_data.Length} bytes");

        // 计算校验和
        int checksum = 0;
        foreach (byte b in _data)
        {
            checksum += b;
        }
        sb.AppendLine($"Checksum: 0x{(checksum & 0xFF):X2}");

        // 统计可打印字符
        int printableChars = 0;
        foreach (byte b in _data)
        {
            if (b >= 32 && b <= 126)
                printableChars++;
        }
        sb.AppendLine($"Printable characters: {printableChars}/{_data.Length}");

        // 识别可能的格式
        string? format = IdentifyDataFormat();
        if (format != null)
        {
            sb.AppendLine($"Format: {format}");
        }

        return sb.ToString();
    }
}