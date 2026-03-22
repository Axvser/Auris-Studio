using System.Windows.Controls;
using System.Windows.Input;
using VeloxDev.Core.Interfaces.WorkflowSystem;
using VeloxDev.Core.WorkflowSystem;

namespace Auris_Studio.Views.Workflow
{
    public partial class AIPipelineView : UserControl
    {
        public AIPipelineView()
        {
            InitializeComponent();
        }

        private void OnPointerMoved(object sender, MouseEventArgs e)
        {
            if (DataContext is not IWorkflowTreeViewModel tree) return;
            var point = e.GetPosition(this);
            tree.SetPointerCommand.Execute(new Anchor(point.X, point.Y, 0));
        }

        private void OnPointerReleased(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not IWorkflowTreeViewModel tree) return;
            tree.GetHelper().ResetVirtualLink();
        }
    }
}
