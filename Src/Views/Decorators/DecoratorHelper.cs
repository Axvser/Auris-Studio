using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Auris_Studio.Views.Decorators
{
    public static class DecoratorHelper
    {
        // 主开关：控制是否启用（附加）装饰器
        public static readonly DependencyProperty ShowMeasureBeatLinesProperty =
            DependencyProperty.RegisterAttached("ShowMeasureBeatLines", typeof(bool), typeof(DecoratorHelper),
                new PropertyMetadata(false, OnShowMeasureBeatLinesChanged));

        public static bool GetShowMeasureBeatLines(DependencyObject obj)
        {
            return (bool)obj.GetValue(ShowMeasureBeatLinesProperty);
        }

        public static void SetShowMeasureBeatLines(DependencyObject obj, bool value)
        {
            obj.SetValue(ShowMeasureBeatLinesProperty, value);
        }

        // 数据上下文桥接
        public static readonly DependencyProperty DataContextBridgeProperty =
            DependencyProperty.RegisterAttached("DataContextBridge", typeof(object), typeof(DecoratorHelper),
                new PropertyMetadata(null, OnDataContextBridgeChanged));

        public static object GetDataContextBridge(DependencyObject obj)
        {
            return obj.GetValue(DataContextBridgeProperty);
        }

        public static void SetDataContextBridge(DependencyObject obj, object value)
        {
            obj.SetValue(DataContextBridgeProperty, value);
        }

        // 区域显示控制
        public static readonly DependencyProperty ShowHeaderProperty =
            DependencyProperty.RegisterAttached("ShowHeader", typeof(bool), typeof(DecoratorHelper),
                new PropertyMetadata(true, OnDecoratorPropertyChanged));

        public static bool GetShowHeader(DependencyObject obj)
        {
            return (bool)obj.GetValue(ShowHeaderProperty);
        }

        public static void SetShowHeader(DependencyObject obj, bool value)
        {
            obj.SetValue(ShowHeaderProperty, value);
        }

        public static readonly DependencyProperty ShowContentProperty =
            DependencyProperty.RegisterAttached("ShowContent", typeof(bool), typeof(DecoratorHelper),
                new PropertyMetadata(true, OnDecoratorPropertyChanged));

        public static bool GetShowContent(DependencyObject obj)
        {
            return (bool)obj.GetValue(ShowContentProperty);
        }

        public static void SetShowContent(DependencyObject obj, bool value)
        {
            obj.SetValue(ShowContentProperty, value);
        }

        #region 事件处理方法
        private static void OnShowMeasureBeatLinesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                if ((bool)e.NewValue)
                {
                    AttachMeasureBeatDecorator(element);
                }
                else
                {
                    RemoveMeasureBeatDecorator(element);
                }
            }
        }

        private static void OnDataContextBridgeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element && GetShowMeasureBeatLines(element))
            {
                var decorator = GetOrCreateDecorator(element);
                decorator?.DataContext = e.NewValue;
            }
        }

        private static void OnDecoratorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element && GetShowMeasureBeatLines(element))
            {
                var decorator = GetOrCreateDecorator(element);
                if (decorator != null)
                {
                    SyncAttachedPropertiesToDecorator(element, decorator);
                    decorator.InvalidateVisual();
                }
            }
        }
        #endregion

        #region 核心方法
        private static void AttachMeasureBeatDecorator(UIElement element)
        {
            var decorator = GetOrCreateDecorator(element);
            if (decorator != null)
            {
                decorator.DataContext = GetDataContextBridge(element) ?? (element as FrameworkElement)?.DataContext;
                SyncAttachedPropertiesToDecorator(element, decorator);
            }
        }

        private static void SyncAttachedPropertiesToDecorator(UIElement element, MeasureBeatDecorator decorator)
        {
            decorator.ShowHeader = GetShowHeader(element);
            decorator.ShowContent = GetShowContent(element);
        }

        private static MeasureBeatDecorator? GetOrCreateDecorator(UIElement element)
        {
            if (element is Panel panel)
            {
                return GetOrCreateForPanel(panel);
            }
            else if (element is Decorator decorator)
            {
                return GetOrCreateForDecorator(decorator);
            }
            else if (element is ContentControl contentControl)
            {
                return GetOrCreateForContentControl(contentControl);
            }
            else
            {
                return WrapInDecorator(element);
            }
        }

        private static void RemoveMeasureBeatDecorator(UIElement element)
        {
            if (element is Panel panel)
            {
                RemoveFromPanel(panel);
            }
            else if (element is Decorator decorator)
            {
                RemoveFromDecorator(decorator);
            }
            else if (element is ContentControl contentControl)
            {
                RemoveFromContentControl(contentControl);
            }
            else
            {
                RemoveFromWrappedDecorator(element);
            }
        }
        #endregion

        #region 针对不同容器类型的处理
        private static MeasureBeatDecorator? GetOrCreateForPanel(Panel panel)
        {
            foreach (UIElement child in panel.Children)
            {
                if (child is MeasureBeatDecorator existingDecorator)
                {
                    return existingDecorator;
                }
            }

            var decorator = new MeasureBeatDecorator();
            panel.Children.Add(decorator);
            Panel.SetZIndex(decorator, int.MaxValue);
            return decorator;
        }

        private static void RemoveFromPanel(Panel panel)
        {
            for (int i = panel.Children.Count - 1; i >= 0; i--)
            {
                if (panel.Children[i] is MeasureBeatDecorator)
                {
                    panel.Children.RemoveAt(i);
                }
            }
        }

        private static MeasureBeatDecorator? GetOrCreateForDecorator(Decorator decorator)
        {
            if (decorator.Child is MeasureBeatDecorator existingDecorator)
            {
                return existingDecorator;
            }

            var originalChild = decorator.Child;
            var measureDecorator = new MeasureBeatDecorator
            {
                Content = originalChild
            };

            decorator.Child = measureDecorator;
            return measureDecorator;
        }

        private static void RemoveFromDecorator(Decorator decorator)
        {
            if (decorator.Child is MeasureBeatDecorator measureDecorator)
            {
                decorator.Child = measureDecorator.Content as UIElement;
            }
        }

        private static MeasureBeatDecorator? GetOrCreateForContentControl(ContentControl contentControl)
        {
            if (contentControl.Content is MeasureBeatDecorator existingDecorator)
            {
                return existingDecorator;
            }

            var originalContent = contentControl.Content;
            var decorator = new MeasureBeatDecorator
            {
                Content = originalContent
            };

            contentControl.Content = decorator;
            return decorator;
        }

        private static void RemoveFromContentControl(ContentControl contentControl)
        {
            if (contentControl.Content is MeasureBeatDecorator decorator)
            {
                contentControl.Content = decorator.Content;
            }
        }

        private static MeasureBeatDecorator? WrapInDecorator(UIElement element)
        {
            if (VisualTreeHelper.GetParent(element) is Panel parentPanel)
            {
                int index = parentPanel.Children.IndexOf(element);
                if (index >= 0)
                {
                    var decorator = new MeasureBeatDecorator
                    {
                        Content = element
                    };

                    parentPanel.Children[index] = decorator;
                    return decorator;
                }
            }

            return null;
        }

        private static void RemoveFromWrappedDecorator(UIElement element)
        {
            if (VisualTreeHelper.GetParent(element) is Panel parentPanel)
            {
                for (int i = 0; i < parentPanel.Children.Count; i++)
                {
                    if (parentPanel.Children[i] is MeasureBeatDecorator decorator && decorator.Content == element)
                    {
                        parentPanel.Children[i] = element;
                        break;
                    }
                }
            }
        }
        #endregion
    }
}