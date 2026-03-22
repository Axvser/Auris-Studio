using Auris_Studio.ViewModels.Workflows;
using System.Windows;
using System.Windows.Controls;

namespace Auris_Studio.Views.Workflow
{
    public class CustomTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? BasicPitch { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            return item switch
            {
                BasicPitchConfigViewModel => BasicPitch ?? throw new ArgumentNullException($"Failed to find the [ {BasicPitch} ] template"),
                _ => throw new InvalidOperationException("Unknown Data Type")
            };
        }
    }
}
