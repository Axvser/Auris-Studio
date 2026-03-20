using VeloxDev.Core.WorkflowSystem;

namespace Auris_Studio.ViewModels.Workflows;

[WorkflowBuilder.ViewModel.Slot
    <WorkflowHelper.ViewModel.Slot>]
public partial class SlotViewModel
{
    public SlotViewModel() => InitializeWorkflow();

    // …… 自由扩展您的输入/输出口视图模型
}