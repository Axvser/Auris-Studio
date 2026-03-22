using Auris_Studio.Midi;
using Auris_Studio.ViewModels.Workflows.Helpers;
using System.Diagnostics;
using System.IO;
using VeloxDev.Core.MVVM;
using VeloxDev.Core.WorkflowSystem;

namespace Auris_Studio.ViewModels.Workflows;

[WorkflowBuilder.ViewModel.Node<BasicPitchConfigHelper>]
public partial class BasicPitchConfigViewModel
{
    // 程序.exe所在目录应确保存在下述位置
    // ① Python依赖、basic-pitch依赖
    private static readonly string ENV_Path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".venv");
    private static readonly string Python_Script_Path = Path.Combine(ENV_Path, "audio_to_midi.exe");
    private static readonly string Python_Models_Path = Path.Combine(ENV_Path, "icassp_2022");
    private static readonly string Python_Model_npm_Path = Path.Combine(Python_Models_Path, "nmp");

    // ② 文件缓存
    private static readonly string TMP_Path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".tmp");
    private static readonly string EXF_Path = Path.Combine(TMP_Path, ".exchange_files");

    public BasicPitchConfigViewModel()
    {
        InitializeWorkflow();

        // 确保临时目录存在
        EnsureTempDirectories();
    }

    // 输入参数属性
    [VeloxProperty] private string _audioFilePath = string.Empty;
    [VeloxProperty] private string _outputDirectory = EXF_Path;  // 默认使用交换文件路径
    [VeloxProperty] private string _modelPath = Python_Model_npm_Path;  // 默认使用Python模型npm路径

    // 阈值参数
    [VeloxProperty] private double _onsetThreshold = 0.5;
    [VeloxProperty] private double _frameThreshold = 0.3;
    [VeloxProperty] private double _minimumNoteLength = 127.7;
    [VeloxProperty] private double _minimumFrequency = 8.18;
    [VeloxProperty] private double _maximumFrequency = 12543.85;
    [VeloxProperty] private double _samplerate = 44100;
    [VeloxProperty] private double _tempo = 120.0;

    // 程序内部编辑所需参数
    [VeloxProperty] private int _defChannel = 1;
    [VeloxProperty] private int _defPatch = 0;

    // 可执行文件路径
    [VeloxProperty] private string _exePath = Python_Script_Path;  // 默认使用Python脚本路径

    // 进程引用
    private Process? _currentProcess;

    #region 私有方法

    private static void EnsureTempDirectories()
    {
        if (!Directory.Exists(TMP_Path))
        {
            Directory.CreateDirectory(TMP_Path);
        }
        if (!Directory.Exists(EXF_Path))
        {
            Directory.CreateDirectory(EXF_Path);
        }
    }

    private async Task StartConversionProcessAsync(CancellationToken ct)
    {
        // 1. 基本验证
        if (string.IsNullOrWhiteSpace(_audioFilePath) || !File.Exists(_audioFilePath))
        {
            return;  // 不返回任何状态，直接退出
        }

        if (string.IsNullOrWhiteSpace(_exePath) || !File.Exists(_exePath))
        {
            return;
        }

        // 2. 确保输出目录存在
        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }

        // 3. 构建命令行参数
        string arguments = BuildArguments();

        // 4. 准备进程启动信息
        var startInfo = new ProcessStartInfo
        {
            FileName = _exePath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = false,  // 不重定向输出
            RedirectStandardError = false,   // 不重定向错误
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            ErrorDialog = false  // 禁止显示错误对话框
        };

        // 5. 启动进程
        using (_currentProcess = new Process { StartInfo = startInfo })
        {
            _currentProcess.EnableRaisingEvents = true;

            // 直接启动，不处理结果
            _currentProcess.Start();

            // 简单等待进程完成，不检查退出代码
            await Task.Run(() => _currentProcess.WaitForExit(), ct);
        }

        _currentProcess = null;
    }

    private string BuildArguments()
    {
        // 构建Python列表格式的音频路径
        string audioPathJson = $"\"[\\\"{_audioFilePath.Replace("\\", "\\\\")}\\\"]\"";

        var args = new List<string>
        {
            audioPathJson,                     // 音频路径列表：Python列表格式的音频文件路径
            $"\"{_outputDirectory}\"",         // 输出目录：模型输出结果（MIDI文件、音频预览等）的保存路径
            "True",                            // 是否保存MIDI：True表示保存MIDI文件
            "False",                           // 是否生成音频预览：True表示从MIDI生成音频文件
            "False",                           // 是否保存模型输出：True保存原始模型输出（轮廓、起始点、音符等）
            "False",                           // 是否保存音符事件：True保存音符事件数据
            $"\"{_modelPath}\"",               // 模型路径：训练好的模型文件或模型目录路径
            _onsetThreshold.ToString("F2"),    // 起始检测阈值：音符起始检测的最小能量阈值（范围0.0-1.0）
            _frameThreshold.ToString("F2"),    // 帧阈值：每帧的最小能量阈值（范围0.0-1.0）
            _minimumNoteLength.ToString("F1"), // 最小音符长度：允许的音符最小持续时间（单位：毫秒）
            _minimumFrequency.ToString(),      // 最小频率：允许输出的最低频率（单位：Hz）
            _maximumFrequency.ToString(),      // 最大频率：允许输出的最高频率（单位：Hz）
            "True",                            // 多重音高弯曲：True允许MIDI文件中的重叠音符具有音高弯曲
            "True",                            // Melodia技巧：使用Melodia后处理步骤
            "None",                            // 调试文件：调试数据输出路径，用于测试/验证
            _samplerate.ToString("F1"),        // 音频采样率：从MIDI渲染音频时的采样率
            _tempo.ToString("F0")              // MIDI速度：生成的MIDI文件的默认速度
        };

        return string.Join(" ", args);
    }

    #endregion

    #region 命令

    [VeloxCommand]
    private void BrowseAudioFile()
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "音频文件 (*.wav;*.mp3;*.ogg;*.flac;*.m4a)|*.wav;*.mp3;*.ogg;*.flac;*.m4a|所有文件 (*.*)|*.*",
            Title = "选择音频文件"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            AudioFilePath = openFileDialog.FileName;
        }
    }

    [VeloxCommand]
    private void BrowseOutputDirectory()
    {
        var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "选择输出目录",
            ShowNewFolderButton = true
        };

        if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            OutputDirectory = folderBrowserDialog.SelectedPath;
        }
    }

    [VeloxCommand]
    private async Task Convert(object? parameter, CancellationToken ct)
    {
        await StartConversionProcessAsync(ct);

        if (parameter is Action<MidiResult, int, int> action)
        {
            var rs = await MidiParser.ImportAsync(Path.Combine(
                OutputDirectory,
                $"{Path.GetFileNameWithoutExtension(AudioFilePath)}_basic_pitch{".mid"}"));
            action.Invoke(rs, DefChannel, DefPatch);
        }
    }

    [VeloxCommand]
    private void Cancel()
    {
        if (_currentProcess != null && !_currentProcess.HasExited)
        {
            ConvertCommand.Clear();
        }
    }

    [VeloxCommand]
    private void SetDefaultOutputDirectory()
    {
        _outputDirectory = EXF_Path;
    }

    [VeloxCommand]
    private void SetDefaultModelPath()
    {
        _modelPath = Python_Model_npm_Path;
    }

    [VeloxCommand]
    private void SetDefaultExePath()
    {
        _exePath = Python_Script_Path;
    }

    #endregion
}