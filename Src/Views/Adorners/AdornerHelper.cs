using System.Windows;
using System.Windows.Documents;

namespace Auris_Studio.Views.Adorners
{
    public class AdornerHelper : DependencyObject
    {
        public static readonly DependencyProperty ShowMeasureBeatLinesProperty =
            DependencyProperty.RegisterAttached("ShowMeasureBeatLines", typeof(bool), typeof(AdornerHelper),
                new PropertyMetadata(false, OnShowMeasureBeatLinesChanged));

        public static bool GetShowMeasureBeatLines(UIElement element)
        {
            return (bool)element.GetValue(ShowMeasureBeatLinesProperty);
        }

        public static void SetShowMeasureBeatLines(UIElement element, bool value)
        {
            element.SetValue(ShowMeasureBeatLinesProperty, value);
        }

        private static void OnShowMeasureBeatLinesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                if ((bool)e.NewValue)
                {
                    AttachMeasureBeatAdorner(element);
                }
                else
                {
                    RemoveMeasureBeatAdorner(element);
                }
            }
        }

        private static void AttachMeasureBeatAdorner(UIElement element)
        {
            if (AdornerLayer.GetAdornerLayer(element) is AdornerLayer layer)
            {
                // 检查是否已存在
                var adorners = layer.GetAdorners(element);
                if (adorners != null)
                {
                    foreach (var adorner in adorners)
                    {
                        if (adorner is MeasureBeatAdorner)
                            return; // 已存在
                    }
                }

                // 创建并添加Adorner
                var measureAdorner = new MeasureBeatAdorner(element);
                layer.Add(measureAdorner);
            }
        }

        private static void RemoveMeasureBeatAdorner(UIElement element)
        {
            if (AdornerLayer.GetAdornerLayer(element) is AdornerLayer layer)
            {
                var adorners = layer.GetAdorners(element);
                if (adorners != null)
                {
                    foreach (var adorner in adorners)
                    {
                        if (adorner is MeasureBeatAdorner measureAdorner)
                        {
                            layer.Remove(measureAdorner);
                        }
                    }
                }
            }
        }

        public static readonly DependencyProperty DataContextBridgeProperty =
        DependencyProperty.RegisterAttached("DataContextBridge", typeof(object), typeof(AdornerHelper),
            new PropertyMetadata(null, OnDataContextBridgeChanged));

        public static object GetDataContextBridge(DependencyObject obj) =>
            obj.GetValue(DataContextBridgeProperty);

        public static void SetDataContextBridge(DependencyObject obj, object value) =>
            obj.SetValue(DataContextBridgeProperty, value);

        private static void OnDataContextBridgeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element && GetShowMeasureBeatLines(element))
            {
                var adorner = GetOrCreateAdorner(element);
                adorner?.DataContextBridge = e.NewValue;
            }
        }

        private static MeasureBeatAdorner? GetOrCreateAdorner(UIElement element)
        {
            var layer = AdornerLayer.GetAdornerLayer(element);
            if (layer == null) return null;

            var adorners = layer.GetAdorners(element);
            if (adorners != null)
            {
                foreach (var adorner in adorners)
                {
                    if (adorner is MeasureBeatAdorner measureAdorner)
                        return measureAdorner;
                }
            }

            var newAdorner = new MeasureBeatAdorner(element);
            layer.Add(newAdorner);
            return newAdorner;
        }
    }
}