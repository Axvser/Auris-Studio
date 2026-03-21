using Auris_Studio.ViewModels.Workflows.Helpers;
using System.Collections.ObjectModel;
using System.IO;
using VeloxDev.Core.Extension;
using VeloxDev.Core.Interfaces.WorkflowSystem;
using VeloxDev.Core.MVVM;
using VeloxDev.Core.WorkflowSystem;

namespace Auris_Studio.ViewModels.Workflows;

[WorkflowBuilder.ViewModel.Tree<AIPipelineHelper>]
public partial class AIPipelineViewModel
{
    public AIPipelineViewModel() => InitializeWorkflow();

    // …… 自由扩展您的工作流树视图模型

    [VeloxProperty] private CanvasLayout layout = new();

    [VeloxProperty] private ObservableCollection<IWorkflowViewModel> _visibleItems = [];

    [VeloxCommand]
    private async Task Save(object? parameter)
    {
        if (parameter is not string path) return;
        await Helper.CloseAsync();
        var json = this.Serialize();
        await File.WriteAllTextAsync(path, json);
    }
}
