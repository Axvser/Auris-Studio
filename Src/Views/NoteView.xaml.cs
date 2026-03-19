using Auris_Studio.ViewModels.MidiEvents;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VeloxDev.Core.DynamicTheme;
using VeloxDev.WPF.PlatformAdapters;

namespace Auris_Studio.Views
{
    [ThemeConfig<ObjectConverter, Dark, Light>(nameof(Background), ["#00FFFF"], ["#FFA500"])]
    public partial class NoteView : UserControl
    {
        public NoteView()
        {
            InitializeComponent();
            InitializeTheme();
        }

        private void LeftArea_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is UIElement ui) ui.CaptureMouse();
            if (DataContext is NoteEventViewModel vm)
            {
                vm.CaptureCommand.Execute(null);
                vm.SetOperationModeCommand.Execute(1);
            }
        }

        private void CenterArea_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is UIElement ui) ui.CaptureMouse();
            if (DataContext is NoteEventViewModel vm)
            {
                vm.CaptureCommand.Execute(null);
                vm.SetOperationModeCommand.Execute(3);
            }
        }

        private void RightArea_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is UIElement ui) ui.CaptureMouse();
            if (DataContext is NoteEventViewModel vm)
            {
                vm.CaptureCommand.Execute(null);
                vm.SetOperationModeCommand.Execute(2);
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            // 释放所有可能捕获鼠标的元素
            if (LeftArea.IsMouseCaptured) LeftArea.ReleaseMouseCapture();
            if (CenterArea.IsMouseCaptured) CenterArea.ReleaseMouseCapture();
            if (RightArea.IsMouseCaptured) RightArea.ReleaseMouseCapture();

            // 执行释放命令
            if (DataContext is NoteEventViewModel vm)
            {
                vm.ReleaseCommand.Execute(null);
                vm.SetOperationModeCommand.Execute(0);
            }
        }
    }
}
