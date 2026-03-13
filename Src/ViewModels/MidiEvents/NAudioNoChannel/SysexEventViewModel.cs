using NAudio.Midi;
using System.Text;
using VeloxDev.Core.MVVM;

namespace Auris_Studio.ViewModels.MidiEvents;

public partial class SysexEventViewModel : EventViewModel
{
    [VeloxProperty] private byte[] _data = [];
    [VeloxProperty] public partial string Text { get; private set; }

    partial void OnDataChanged(byte[] oldValue, byte[] newValue)
    {
        // 当Data变化时，重新解析并更新Text属性
        if (newValue != null && newValue.Length > 0)
        {
            Text = ParseSysexData(newValue);
        }
        else
        {
            Text = string.Empty;
        }
    }

    [VeloxCommand]
    public override void Read(object? parameter)
    {
        if (parameter is SysexEvent sysexEvent)
        {
            // 设置绝对时间
            AbsoluteTime = sysexEvent.AbsoluteTime;

            // 获取原始数据
            // 由于SysexEvent的data字段是私有的，只能通过ToString()方法来解析
            var data = ParseDataFromToString(sysexEvent.ToString());

            if (data != null)
            {
                Data = data;
            }
        }
    }

    [VeloxCommand]
    public override void Write(object? parameter)
    {
        if (parameter is IList<MidiEvent> events)
        {
            // 创建新的SysexEvent
            // 注意：由于无法直接访问SysexEvent的构造函数和数据字段，
            // 我们需要通过反射或创建自定义的SysexEvent
            var sysexEvent = CreateSysexEvent();
            if (sysexEvent != null)
            {
                events.Add(sysexEvent);
            }
        }
    }

    /// <summary>
    /// 从ToString()输出中解析出原始字节数据
    /// </summary>
    private static byte[] ParseDataFromToString(string toString)
    {
        try
        {
            // SysexEvent.ToString()的输出格式为：
            // "[绝对时间] Sysex: [字节数] bytes\r\n[十六进制数据]"
            // 示例: "0 Sysex: 8 bytes\r\nF0 7E 7F 09 01 F7 "

            if (string.IsNullOrEmpty(toString))
                return [];

            // 找到换行符位置
            int newLineIndex = toString.IndexOf("\r\n");
            if (newLineIndex < 0)
                return [];

            // 提取十六进制数据部分
            string hexData = toString[(newLineIndex + 2)..].Trim();

            // 解析十六进制字符串为字节数组
            return ParseHexStringToBytes(hexData);
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// 解析十六进制字符串为字节数组
    /// </summary>
    private static byte[] ParseHexStringToBytes(string hexString)
    {
        if (string.IsNullOrEmpty(hexString))
            return [];

        // 移除空格，将十六进制字符串分割
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
                // 如果转换失败，设置为0
                bytes[i] = 0;
            }
        }

        return bytes;
    }

    /// <summary>
    /// 解析SysEx数据为可读文本
    /// </summary>
    private static string ParseSysexData(byte[] data)
    {
        if (data == null || data.Length == 0)
            return "Empty SysEx";

        try
        {
            var sb = new StringBuilder();
            sb.Append("SysEx: ");

            // 检查是否为通用SysEx消息
            if (data.Length >= 3)
            {
                byte manufacturerId = data[0];

                if (manufacturerId == 0x7D) // 教育用途
                {
                    sb.Append("Educational Use");
                }
                else if (manufacturerId == 0x7E) // 非实时通用SysEx
                {
                    sb.Append("Universal Non-Realtime");

                    if (data.Length >= 2)
                    {
                        byte subId1 = data[1];
                        sb.Append($" Sub-ID1: 0x{subId1:X2}");
                    }
                }
                else if (manufacturerId == 0x7F) // 实时通用SysEx
                {
                    sb.Append("Universal Realtime");

                    if (data.Length >= 2)
                    {
                        byte subId1 = data[1];
                        sb.Append($" Sub-ID1: 0x{subId1:X2}");
                    }
                }
                else if (manufacturerId == 0x00) // 扩展制造商ID
                {
                    if (data.Length >= 3)
                    {
                        byte manufacturerByte1 = data[1];
                        byte manufacturerByte2 = data[2];
                        sb.Append($"Extended Manufacturer: 0x{manufacturerByte1:X2} 0x{manufacturerByte2:X2}");
                    }
                }
                else // 标准制造商ID
                {
                    sb.Append($"Manufacturer ID: 0x{manufacturerId:X2}");

                    // 常见制造商ID
                    var manufacturerNames = new Dictionary<byte, string>
                    {
                        { 0x01, "Sequential Circuits" },
                        { 0x06, "Kawai" },
                        { 0x07, "Roland" },
                        { 0x0E, "Yamaha" },
                        { 0x10, "Korg" },
                        { 0x11, "Kurzweil" },
                        { 0x1B, "Akai" },
                        { 0x24, "Casio" },
                        { 0x2C, "Fender" },
                        { 0x3F, "M-Audio" },
                        { 0x40, "DigiTech" },
                        { 0x41, "Ibanez" },
                        { 0x42, "Fostex" },
                        { 0x43, "Zoom" },
                        { 0x44, "Peavey" },
                        { 0x45, "360 Systems" },
                        { 0x46, "Lexicon" },
                        { 0x47, "DOD" },
                        { 0x48, "Studiologic" },
                        { 0x49, "Moog" }
                    };

                    if (manufacturerNames.TryGetValue(manufacturerId, out string? manufacturerName))
                    {
                        sb.Append($" ({manufacturerName})");
                    }
                }
            }

            // 添加字节数信息
            sb.Append($" [{data.Length} bytes]");

            // 显示前几个字节的十六进制
            int showBytes = Math.Min(data.Length, 10);
            sb.Append(" Data: ");
            for (int i = 0; i < showBytes; i++)
            {
                sb.Append($"{data[i]:X2} ");
            }
            if (data.Length > showBytes)
            {
                sb.Append("...");
            }

            return sb.ToString();
        }
        catch
        {
            return $"SysEx Data [{data.Length} bytes]";
        }
    }

    /// <summary>
    /// 创建SysEx事件
    /// 由于无法直接访问SysexEvent的构造函数，这里提供替代方案
    /// </summary>
    private SysexEvent? CreateSysexEvent()
    {
        try
        {
            if (_data == null || _data.Length == 0)
                return null;

            // 由于SysexEvent没有公开的构造函数，我们需要通过反射创建
            // 或者使用其他方法
            var sysexEvent = (SysexEvent?)Activator.CreateInstance(typeof(SysexEvent), true);

            if (sysexEvent != null)
            {
                // 通过反射设置数据
                var dataField = typeof(SysexEvent).GetField("data",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                dataField?.SetValue(sysexEvent, _data);

                // 设置绝对时间
                sysexEvent.AbsoluteTime = AbsoluteTime;
            }

            return sysexEvent;
        }
        catch
        {
            // 如果无法创建，返回null
            return null;
        }
    }

    /// <summary>
    /// 获取十六进制格式的数据字符串
    /// </summary>
    public string GetHexDataString()
    {
        if (_data == null || _data.Length == 0)
            return string.Empty;

        var sb = new StringBuilder();
        for (int i = 0; i < _data.Length; i++)
        {
            sb.Append($"{_data[i]:X2}");
            if (i < _data.Length - 1)
                sb.Append(' ');
        }
        return sb.ToString();
    }

    /// <summary>
    /// 获取ASCII格式的数据字符串（适用于文本类型的SysEx消息）
    /// </summary>
    public string GetAsciiDataString()
    {
        if (_data == null || _data.Length == 0)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (byte b in _data)
        {
            if (b >= 32 && b <= 126) // 可打印ASCII字符
            {
                sb.Append((char)b);
            }
            else
            {
                sb.Append('.');
            }
        }
        return sb.ToString();
    }
}