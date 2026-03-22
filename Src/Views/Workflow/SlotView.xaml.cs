using System.Windows.Controls;
using System.Windows.Input;
using VeloxDev.Core.DynamicTheme;
using VeloxDev.Core.Interfaces.WorkflowSystem;
using VeloxDev.WPF.PlatformAdapters;

namespace Auris_Studio.Views.Workflow;

[ThemeConfig<ObjectConverter, Dark, Light>(nameof(Background), ["#1e1e1e"], ["White"])]
public partial class SlotView : UserControl
{
    public SlotView()
    {
        InitializeComponent();
        InitializeTheme();
    }

    private void OnPointerPressed(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not IWorkflowSlotViewModel context) return;

        context.SendConnectionCommand.Execute(null);

        e.Handled = true;
    }

    private void OnPointerReleased(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not IWorkflowSlotViewModel context) return;

        context.ReceiveConnectionCommand.Execute(null);

        e.Handled = true;
    }
}
