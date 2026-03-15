using Auris_Studio.ViewModels;
using Auris_Studio.ViewModels.Helpers;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using VeloxDev.Core.DynamicTheme;
using VeloxDev.Core.Extension;
using VeloxDev.WPF.PlatformAdapters;

namespace Auris_Studio.Views
{
    public partial class MidiEditorView : UserControl
    {
        public MidiEditorView()
        {
            InitializeComponent();
        }

        private async void SelectMidi_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "MIDI和JSON文件 (*.mid;*.json)|*.mid;*.json|所有文件 (*.*)|*.*",
                Title = "请选择MIDI文件"
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
                result.Optimize();
                var context = new MidiEditorViewModel();
                context.ReadCommand.Execute(result);
                DataContext = context;
            }
            else if (fileExtension == ".json")
            {
                var stream = File.OpenRead(openFileDialog.FileName);
                var context = await stream.TryDeserializeFromStreamAsync<MidiEditorViewModel>();
                DataContext = context;
            }
            else
            {
                MessageBox.Show($"⚠ *{fileExtension} 不受支持!");
            }
        }

        private async void Diagnose_Click(object sender, RoutedEventArgs e)
        {
            // 1. 让用户选择原始 MIDI 文件
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
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
            string baseDirectory = Path.GetDirectoryName(originalMidiPath);
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
                result.Optimize();
                await result.ExportAsync(debugMidiPath);
                await result.BuildMarkdownAsync(debugMdPath);

                // 步骤3: 通过编辑器处理
                var editor = new MidiEditorViewModel();
                editor.Read(result);
                var newResult = new MidiResult();
                editor.Write(newResult);

                // 步骤4: 优化并导出第二个调试文件
                newResult.Optimize();
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
                    newResult1.Optimize();
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
        
        private void Theme_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Jump(
                ThemeManager.Current == typeof(Dark) ? typeof(Light) : typeof(Dark));
        }
    }
}
