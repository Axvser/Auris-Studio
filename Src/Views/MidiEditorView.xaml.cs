using Auris_Studio.Midi;
using Auris_Studio.ViewModels;
using Auris_Studio.ViewModels.Workflows;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using VeloxDev.Core.DynamicTheme;
using VeloxDev.Core.Extension;

namespace Auris_Studio.Views
{
    public partial class MidiEditorView : UserControl
    {
        public MidiEditorView()
        {
            InitializeComponent();
            DataContextChanged += MidiEditorView_DataContextChanged;
            DataContext = new MidiEditorViewModel();
        }

        private void MidiEditorView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var oldValue = e.OldValue as MidiEditorViewModel;
            oldValue?.CurrentNotes.Clear();
            if (e.NewValue is MidiEditorViewModel viewModel1)
            {
                if (oldValue?.AIPipeline is not null) viewModel1.AIPipeline = oldValue.AIPipeline;
            }
        }

        private void ToMostLeft_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ToMostRight_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MidiEditorViewModel vm) vm.StopCommand.Execute(null);

            var openFileDialog = new OpenFileDialog
            {
                Filter = "Auris Studio Support (*.mid;*.json;*.mp3;*.wav;*.ogg;*.flac;*.m4a;)|*.mid;*.json;*.mp3;*.wav;*.ogg;*.flac;*.m4a;|所有文件 (*.*)|*.*",
                Title = "Auris Studio / Import Midi"
            };

            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            // 获取文件扩展名
            string fileExtension = Path.GetExtension(openFileDialog.FileName).ToLower();

            // 根据文件扩展名判断文件类型
            if (fileExtension == ".mid" || fileExtension == ".midi")
            {
                // 处理 MIDI 文件
                var result = await MidiParser.ImportAsync(openFileDialog.FileName);
                var context = new MidiEditorViewModel();
                context.Read(result);
                DataContext = context;
            }
            else if (fileExtension == ".json")
            {
                // 处理 MidiEditorViewModel
                var stream = File.OpenRead(openFileDialog.FileName);
                var (Success, Result) = await stream.TryDeserializeFromStreamAsync<MidiEditorViewModel>();
                if (Success)
                {
                    var pipeline = Result?.AIPipeline;
                    DataContext = Result;
                    if (pipeline is not null) Result?.AIPipeline = pipeline;
                }
            }
            else if (fileExtension == ".mp3" ||
                    fileExtension == ".wav" ||
                    fileExtension == ".ogg" ||
                    fileExtension == ".flac" ||
                    fileExtension == ".m4a")
            {
                // Basic-Pitch 扒谱
                // 测试临时用
                var ai = ((DataContext as MidiEditorViewModel)?.AIPipeline?.Nodes?.FirstOrDefault() as BasicPitchConfigViewModel)
                    ?? new BasicPitchConfigViewModel();
                ai.AudioFilePath = openFileDialog.FileName;
                var context = new MidiEditorViewModel();
                await ai.ConvertCommand.ExecuteAsync((MidiResult rs, int channel, int patch) =>
                {
                    var context = new MidiEditorViewModel();
                    context.Read(rs);
                    foreach (var track in context.Tracks)
                    {
                        track.Channel = channel;
                        track.Patch = (Patch)patch;
                    }
                    DataContext = context;
                });
            }
            else
            {
                MessageBox.Show($"⚠ *{fileExtension} 不受支持!");
            }
        }

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MidiEditorViewModel vm)
            {
                vm.StopCommand.Execute(null);
                var openFolderDialog = new OpenFolderDialog()
                {
                    Title = "save as midi file"
                };

                if (openFolderDialog.ShowDialog() != true)
                {
                    return;
                }

                var path = Path.Combine(openFolderDialog.FolderName, $"output.mid");
                var midi = new MidiResult();
                vm.WriteCommand.Execute(midi);
                await MidiSynthesizer.ExportAsync(midi, path);
            }
        }

        private void Component_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MidiEditorViewModel vm) vm.StopCommand.Execute(null);

            if (AIPipelineView.Visibility == Visibility.Visible)
            {
                Component.ButtonContent = "AI Pipeline";
                AIPipelineView.Visibility = Visibility.Hidden;
                PianoSlidingDoorView.Visibility = Visibility.Visible;
            }
            else
            {
                Component.ButtonContent = "Piano Sliding Door";
                AIPipelineView.Visibility = Visibility.Visible;
                PianoSlidingDoorView.Visibility = Visibility.Hidden;
            }
        }

        private void Theme_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Jump(
                ThemeManager.Current == typeof(Dark) ? typeof(Light) : typeof(Dark));
        }

        private async void Diagnose_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MidiEditorViewModel vm) vm.StopCommand.Execute(null);

            // 1. 让用户选择原始 MIDI 文件
            var openFileDialog = new OpenFileDialog
            {
                Filter = "MIDI 文件 (*.mid)|*.mid|所有文件 (*.*)|*.*",
                Title = "选择要诊断的 MIDI 文件",
                DefaultExt = ".mid"
            };

            if (openFileDialog.ShowDialog() != true)
            {
                return; // 用户取消了选择
            }

            string originalMidiPath = openFileDialog.FileName;
            string? baseDirectory = Path.GetDirectoryName(originalMidiPath);
            if (baseDirectory == null)
            {
                MessageBox.Show($"error path : {originalMidiPath}");
                return;
            }
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalMidiPath);

            // 2. 在用户选择的目录中创建测试文件
            string debugDir = Path.Combine(baseDirectory, "MidiDebug");
            Directory.CreateDirectory(debugDir);

            // 构建所有输出文件的路径
            string debugMidiPath = Path.Combine(debugDir, $"{fileNameWithoutExt}_Debug.mid");
            string debugMdPath = Path.Combine(debugDir, $"{fileNameWithoutExt}_Debug.md");
            string debugMidi1Path = Path.Combine(debugDir, $"{fileNameWithoutExt}_Debug1.mid");
            string debugMd1Path = Path.Combine(debugDir, $"{fileNameWithoutExt}_Debug1.md");
            string jsonPath = Path.Combine(debugDir, $"{fileNameWithoutExt}.json");
            string debugMidi2Path = Path.Combine(debugDir, $"{fileNameWithoutExt}_Debug2.mid");
            string debugMd2Path = Path.Combine(debugDir, $"{fileNameWithoutExt}_Debug2.md");

            try
            {
                // 3. 执行完整的诊断流程
                // 步骤1: 导入原始文件
                var result = await MidiParser.ImportAsync(originalMidiPath);

                // 步骤2: 优化并导出第一个调试文件
                await result.ExportAsync(debugMidiPath);
                await result.BuildMarkdownAsync(debugMdPath);

                // 步骤3: 通过编辑器处理
                var editor = new MidiEditorViewModel();
                editor.Read(result);
                var newResult = new MidiResult();
                editor.Write(newResult);

                // 步骤4: 优化并导出第二个调试文件
                await newResult.ExportAsync(debugMidi1Path);
                await newResult.BuildMarkdownAsync(debugMd1Path);

                // 步骤5: 序列化/反序列化测试
                var json = await editor.SerializeAsync();
                await File.WriteAllTextAsync(jsonPath, json);
                var (Success, Result) = await json.TryDeserializeAsync<MidiEditorViewModel>();

                if (Success && Result != null)
                {
                    // 步骤6: 导出第三个调试文件
                    var newResult1 = new MidiResult();
                    Result.Write(newResult1);
                    await newResult1.ExportAsync(debugMidi2Path);
                    await newResult1.BuildMarkdownAsync(debugMd2Path);
                }

                // 4. 显示完成信息
                MessageBox.Show(
                    $"诊断完成！\n" +
                    $"原始文件: {Path.GetFileName(originalMidiPath)}\n" +
                    $"输出目录: {debugDir}\n" +
                    $"生成的文件:\n" +
                    $"  - {Path.GetFileName(debugMidiPath)}\n" +
                    $"  - {Path.GetFileName(debugMdPath)}\n" +
                    $"  - {Path.GetFileName(debugMidi1Path)}\n" +
                    $"  - {Path.GetFileName(debugMd1Path)}\n" +
                    $"  - {Path.GetFileName(jsonPath)}\n" +
                    (Success ? $"  - {Path.GetFileName(debugMidi2Path)}\n  - {Path.GetFileName(debugMd2Path)}" : ""),
                    "MIDI 诊断完成",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // 5. 可选：打开输出目录
                Process.Start("explorer.exe", debugDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"诊断过程中发生错误：\n{ex.Message}\n\n堆栈跟踪：\n{ex.StackTrace}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
