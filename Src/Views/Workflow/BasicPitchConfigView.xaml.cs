using Auris_Studio.ViewModels.Workflows;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VeloxDev.Core.DynamicTheme;
using VeloxDev.Core.WorkflowSystem;
using VeloxDev.WPF.PlatformAdapters;

namespace Auris_Studio.Views.Workflow
{
    [ThemeConfig<ObjectConverter, Dark, Light>(nameof(Background), ["#1e1e1e"], ["White"])]
    [ThemeConfig<ObjectConverter, Dark, Light>(nameof(Foreground), ["White"], ["#1e1e1e"])]
    public partial class BasicPitchConfigView : UserControl
    {
        public BasicPitchConfigView()
        {
            InitializeComponent();
        }

        private bool _isDragging;
        private Point _lastPosition;
        private Canvas? _parentCanvas;

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);
            _parentCanvas = FindVisualParent<Canvas>(this);
        }

        private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;

            if (parentObject is T parent) return parent;
            return FindVisualParent<T>(parentObject);
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _lastPosition = e.GetPosition(_parentCanvas);
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging || _parentCanvas == null) return;

            var currentPosition = e.GetPosition(_parentCanvas);
            var delta = currentPosition - _lastPosition;

            if (DataContext is BasicPitchConfigViewModel nodeContext)
            {
                nodeContext.MoveCommand.Execute(new Offset(delta.X, delta.Y));
            }

            _lastPosition = currentPosition;
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            _isDragging = false;
        }
    }
}
