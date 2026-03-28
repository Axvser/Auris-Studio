using Auris_Studio.Midi;
using Auris_Studio.ViewModels;
using Auris_Studio.ViewModels.Workflows;
using Auris_Studio.Views.Decorators;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using VeloxDev.Core.Extension;

namespace Auris_Studio.Views
{
    public partial class MidiEditorView : UserControl
    {
        private const string logo_auto = "M343.134964 724.45563a44.688952 44.688952 0 0 0 11.721692-12.362723 115.38541 115.38541 0 0 0 9.157572-17.033084c2.56412-6.135573 5.12824-12.454298 7.783937-19.04775l32.051502-81.410817h206.961132l42.949014 108.242503a44.872104 44.872104 0 0 0 12.087995 19.505629 28.571625 28.571625 0 0 0 21.520295 6.685028c13.553207 0 22.161325-3.937756 25.824354-11.721693a36.630289 36.630289 0 0 0-0.824182-28.571625l-164.8363-433.611044a35.165077 35.165077 0 0 0-14.102661-18.315145 47.344648 47.344648 0 0 0-26.556959-6.685028 43.681619 43.681619 0 0 0-25.366475 6.685028 35.439804 35.439804 0 0 0-13.736359 18.315145L303.665827 687.275886c-5.677695 16.025751-5.677695 27.472717 0 32.875685a32.143078 32.143078 0 0 0 24.267567 9.157572 26.190657 26.190657 0 0 0 15.20157-4.853513zM507.696536 302.199975l89.011602 242.584088H418.593359z M754.401532 910.629073a462.640548 462.640548 0 0 1-657.42211-191.576411 468.501395 468.501395 0 0 1 2.380969-426.926017v-0.732605a19.68878 19.68878 0 0 0 1.556787-6.776604 22.619203 22.619203 0 0 0-22.619203-22.527628 21.795022 21.795022 0 0 0-21.062416 15.018419A526.1941 526.1941 0 0 0 0.000733 503.666563V511.999954a509.344167 509.344167 0 0 0 109.341412 316.210969l4.578786 5.76927a511.084105 511.084105 0 0 0 397.988089 190.019624H523.081258a509.985197 509.985197 0 0 0 253.481599-74.817365l30.128412 51.923434L836.728106 888.284597l-112.546562-29.853686z m160.165938-714.840087c-1.465212-1.92309-3.021999-3.754605-4.578786-5.677695A511.084105 511.084105 0 0 0 511.90902 0.000092h-11.263814A509.06944 509.06944 0 0 0 247.255182 75.000608l-30.128412-52.01501-29.945261 112.546562 112.546562 30.036837-30.219988-52.289737a462.548973 462.548973 0 0 1 657.422109 191.667986 468.318243 468.318243 0 0 1-2.014666 426.834441V732.605869a20.696113 20.696113 0 0 0-1.556787 6.776603 22.710779 22.710779 0 0 0 22.619204 22.619204 21.886598 21.886598 0 0 0 21.062416-15.109994 526.1941 526.1941 0 0 0 57.14325-226.558337v-0.64103-7.600785a509.06944 509.06944 0 0 0-109.616139-316.302544z";
        private const string logo_hand = "M51.186933 307.254945C23.03397 307.254945 0 284.732975 0 256.068012s23.03397-51.185933 51.186933-51.185932h611.679193c28.151963 0 51.185933 23.03297 51.185933 51.185932s-23.03397 51.186933-51.186933 51.186933H51.185933zM351.139537 716.746405h688.459092c28.151963 0 51.185933 23.03397 51.185933 51.186933s-23.03397 51.185933-51.186933 51.185932H351.139537l83.43489 124.895835c15.867979 23.545969 9.725987 55.280927-13.307982 71.149907-23.545969 15.866979-55.281927 9.724987-71.149907-13.309983 0-0.510999-0.511999-0.510999-0.511999-1.022998l-155.606795-232.899693 155.094796-232.897693c15.35598-23.545969 47.091938-30.19996 70.637907-14.843981 23.545969 15.35598 30.19996 47.091938 14.84398 70.636907 0 0.511999-0.511999 0.511999-0.511999 1.023999L351.139537 716.746405z m526.709306-409.49146c-28.152963 0-51.186933-23.03397-51.186933-51.186933s23.03397-51.185933 51.186933-51.185932h161.749786c28.151963 0 51.185933 23.03297 51.185933 51.185932s-23.03397 51.186933-51.186933 51.186933H877.848843zM51.186933 818.608271C23.03397 818.608271 0 795.574301 0 767.420338s23.03397-51.186933 51.186933-51.186932H76.779899c28.151963 0 51.185933 23.03397 51.185932 51.186932s-23.03397 51.186933-51.185932 51.186933H51.186933z m586.085227-562.539259L520.055314 79.985245c-15.867979-23.545969-9.725987-55.280927 13.307983-71.149907 23.545969-15.866979 55.281927-9.724987 71.149906 13.309983 0 0.510999 0.510999 0.510999 0.510999 1.022998l155.607795 232.899693-155.095795 232.897693c-15.35598 23.545969-47.090938 30.19996-70.636907 14.843981s-30.19996-47.091938-14.843981-70.636907c0-0.511999 0.511999-0.511999 0.512-1.023999l116.704846-176.081768z";

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
            oldValue?.PropertyChanged -= IsPlaying_PropertyChanged;
            if (e.NewValue is MidiEditorViewModel newValue)
            {
                if (oldValue?.AIPipeline is not null) newValue.AIPipeline = oldValue.AIPipeline;
                if (oldValue is not null)
                {
                    newValue.ProgressFollow = oldValue.ProgressFollow;
                    newValue.UseSnap = oldValue.UseSnap;
                    newValue.DragBehavior = oldValue.DragBehavior;
                }
                newValue.PropertyChanged += IsPlaying_PropertyChanged;
                PlayButton.Command = newValue.PlayCommand;
            }
        }

        private void IsPlaying_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is MidiEditorViewModel vm)
            {
                if (e.PropertyName == nameof(MidiEditorViewModel.IsPlaying))
                {
                    PlayButton.ButtonContent = vm.IsPlaying ?
                        "M128 256c0-70.6 57.4-128 128-128h512c70.6 0 128 57.4 128 128v512c0 70.6-57.4 128-128 128H256c-70.6 0-128-57.4-128-128V256z"
                        :
                        "M912.724884 429.355681L208.797545 13.198638C151.603449-20.597874 64.01249 12.198741 64.01249 95.790112V927.904219c0 74.992259 81.391599 120.187594 144.785055 82.591475l703.927339-415.957064c62.793518-36.996181 62.993498-128.186768 0-165.182949z";
                    PlayButton.Command = vm.IsPlaying ?
                        vm.StopCommand
                        :
                        vm.PlayCommand;
                }
                if (e.PropertyName == nameof(MidiEditorViewModel.ProgressFollow))
                {
                    FollowButton.ButtonContent = vm.ProgressFollow ? logo_hand : logo_auto;
                }
            }
        }

        private async void Tracks_Click(object sender, RoutedEventArgs e)
        {
            if (TrackView.Visibility == Visibility.Visible)
            {
                TrackView.Visibility = Visibility.Hidden;
                TrackView.Width = 0;
                TracksButton.ButtonContent = "▶ Tracks";
            }
            else
            {
                TrackView.Visibility = Visibility.Visible;
                TrackView.Width = ActualWidth * 0.2;
                TracksButton.ButtonContent = "▼ Tracks";
            }
        }

        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            WaitingView.Instance?.IsWaiting = true;
            WaitingView.Instance?.Text = "Select File";
            ImportButton.IsEnabled = false;
            ExportButton.IsEnabled = false;
            DiagnoseButton.IsEnabled = false;
            PianoSlidingDoorView.IsEnabled = false;

            if (DataContext is MidiEditorViewModel vm) vm.StopCommand.Execute(null);

            var openFileDialog = new OpenFileDialog
            {
                Filter = "Audio Files (*.mid;*.json;*.mp3;*.wav;*.ogg;*.flac;*.m4a;)|*.mid;*.json;*.mp3;*.wav;*.ogg;*.flac;*.m4a;|所有文件 (*.*)|*.*",
                Title = "Midi Source Import"
            };

            if (openFileDialog.ShowDialog() != true)
            {
                WaitingView.Instance?.IsWaiting = false;
                WaitingView.Instance?.Text = string.Empty;
                ImportButton.IsEnabled = true;
                ExportButton.IsEnabled = true;
                DiagnoseButton.IsEnabled = true;
                PianoSlidingDoorView.IsEnabled = true;
                return;
            }

            // 获取文件扩展名
            string fileExtension = Path.GetExtension(openFileDialog.FileName).ToLower();

            // 根据文件扩展名判断文件类型
            if (fileExtension == ".mid" || fileExtension == ".midi")
            {
                WaitingView.Instance?.Text = "Load Midi";
                // 处理 MIDI 文件
                var result = await MidiParser.ImportAsync(openFileDialog.FileName);
                var context = new MidiEditorViewModel();
                context.Read(result);
                DataContext = context;
                WaitingView.Instance?.IsWaiting = false;
                WaitingView.Instance?.Text = string.Empty;
                ImportButton.IsEnabled = true;
                ExportButton.IsEnabled = true;
                DiagnoseButton.IsEnabled = true;
                PianoSlidingDoorView.IsEnabled = true;
            }
            else if (fileExtension == ".json")
            {
                WaitingView.Instance?.Text = "Load Json";
                // 处理 MidiEditorViewModel
                var stream = File.OpenRead(openFileDialog.FileName);
                var (Success, Result) = await stream.TryDeserializeFromStreamAsync<MidiEditorViewModel>();
                if (Success)
                {
                    var pipeline = Result?.AIPipeline;
                    DataContext = Result;
                    if (pipeline is not null) Result?.AIPipeline = pipeline;
                }
                WaitingView.Instance?.IsWaiting = false;
                WaitingView.Instance?.Text = string.Empty;
                ImportButton.IsEnabled = true;
                ExportButton.IsEnabled = true;
                DiagnoseButton.IsEnabled = true;
                PianoSlidingDoorView.IsEnabled = true;
            }
            else if (fileExtension == ".mp3" ||
                    fileExtension == ".wav" ||
                    fileExtension == ".ogg" ||
                    fileExtension == ".flac" ||
                    fileExtension == ".m4a")
            {
                WaitingView.Instance?.Text = "AI Converting";
                // Basic-Pitch 扒谱
                // 测试临时用
                var ai = ((DataContext as MidiEditorViewModel)?.AIPipeline?.Nodes?.FirstOrDefault() as BasicPitchConfigViewModel)
                    ?? new BasicPitchConfigViewModel();
                ai.AudioFilePath = openFileDialog.FileName;
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
                    WaitingView.Instance?.IsWaiting = false;
                    WaitingView.Instance?.Text = string.Empty;
                    ImportButton.IsEnabled = true;
                    ExportButton.IsEnabled = true;
                    DiagnoseButton.IsEnabled = true;
                    PianoSlidingDoorView.IsEnabled = true;
                });
            }
            else
            {
                WaitingView.Instance?.Text = "Error";
                MessageBox.Show($"⚠ *{fileExtension} 不受支持!");
                WaitingView.Instance?.IsWaiting = false;
                WaitingView.Instance?.Text = string.Empty;
                ImportButton.IsEnabled = true;
                ExportButton.IsEnabled = true;
                DiagnoseButton.IsEnabled = true;
                PianoSlidingDoorView.IsEnabled = true;
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

        private void Root_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (TrackView.Visibility == Visibility.Visible)
            {
                TrackView.Width = e.NewSize.Width * 0.2;
            }
        }

        private void FollowMode_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MidiEditorViewModel vm)
            {
                vm.ProgressFollow = !vm.ProgressFollow;
                FollowButton.ButtonContent = vm.ProgressFollow ? logo_hand : logo_auto;
            }
        }
    }
}
