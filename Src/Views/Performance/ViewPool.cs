using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace Auris_Studio.Views.Performance;

/// <summary>
/// 为Canvas提供视图对象池管理，优化大量动态UI项的创建和销毁性能。
/// 通过<see cref="TemplateProperty"/>和<see cref="DataContextProperty"/>附加属性与Canvas关联。
/// </summary>
public static class ViewPool
{
    // 使用弱引用字典避免静态字典导致的内存泄漏
    private static readonly ConditionalWeakTable<Canvas, PoolInfo> CanvasPools = [];
    private static readonly ConditionalWeakTable<INotifyCollectionChanged, Canvas> DataContextToCanvas = [];

    /// <summary>
    /// 标识用于创建子控件的DataTemplate的附加属性
    /// </summary>
    public static readonly DependencyProperty TemplateProperty =
        DependencyProperty.RegisterAttached(
            "Template",
            typeof(DataTemplate),
            typeof(ViewPool),
            new PropertyMetadata(null, OnTemplateChanged));

    /// <summary>
    /// 标识绑定的集合数据源的附加属性
    /// </summary>
    public static readonly DependencyProperty DataContextProperty =
        DependencyProperty.RegisterAttached(
            "DataContext",
            typeof(INotifyCollectionChanged),
            typeof(ViewPool),
            new PropertyMetadata(null, OnDataContextChanged));

    public static DataTemplate GetTemplate(DependencyObject obj)
    {
        return obj is null ? throw new ArgumentNullException(nameof(obj)) : (DataTemplate)obj.GetValue(TemplateProperty);
    }

    public static void SetTemplate(DependencyObject obj, DataTemplate value)
    {
        ArgumentNullException.ThrowIfNull(obj);
        obj.SetValue(TemplateProperty, value);
    }

    public static INotifyCollectionChanged GetDataContext(DependencyObject obj)
    {
        return obj is null ? throw new ArgumentNullException(nameof(obj)) : (INotifyCollectionChanged)obj.GetValue(DataContextProperty);
    }

    public static void SetDataContext(DependencyObject obj, INotifyCollectionChanged value)
    {
        ArgumentNullException.ThrowIfNull(obj);
        obj.SetValue(DataContextProperty, value);
    }

    /// <summary>
    /// 视图池信息
    /// </summary>
    private class PoolInfo
    {
        public Queue<Control> AvailableControls { get; } = new();
        public List<Control> ActiveControls { get; } = [];
        public DataTemplate? Template { get; set; }
        public Canvas? Canvas { get; set; }
    }

    private static void OnTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Canvas canvas) return;

        // 清理旧的关联
        if (CanvasPools.TryGetValue(canvas, out _))
        {
            CleanupCanvas(canvas);
        }

        if (e.NewValue is DataTemplate template)
        {
            InitializeCanvasPool(canvas, template);
        }
    }

    private static void OnDataContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Canvas canvas) return;

        var newCollection = e.NewValue as INotifyCollectionChanged;

        // 从旧集合取消订阅
        if (e.OldValue is INotifyCollectionChanged oldCollection)
        {
            oldCollection.CollectionChanged -= OnCollectionChanged;
            DataContextToCanvas.Remove(oldCollection);
        }

        // 附加到新集合
        if (newCollection != null)
        {
            newCollection.CollectionChanged += OnCollectionChanged;
            DataContextToCanvas.AddOrUpdate(newCollection, canvas);
        }

        // 如果已有模板，初始化显示
        if (CanvasPools.TryGetValue(canvas, out var pool) && newCollection != null)
        {
            InitializeCollectionItems(canvas, newCollection);
        }
    }

    private static void InitializeCanvasPool(Canvas canvas, DataTemplate template, int initialPoolSize = 30)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(template);

        var pool = new PoolInfo
        {
            Template = template,
            Canvas = canvas
        };

        // 初始化可用控件池
        for (int i = 0; i < initialPoolSize; i++)
        {
            try
            {
                if (template.LoadContent() is Control control)
                {
                    control.Visibility = Visibility.Collapsed;
                    canvas.Children.Add(control);
                    pool.AvailableControls.Enqueue(control);
                }
            }
            catch (Exception ex)
            {
                // 记录日志
                System.Diagnostics.Debug.WriteLine($"初始化控件池时创建控件失败: {ex.Message}");
                // 单个控件创建失败不应阻止整个池的初始化
            }
        }

        CanvasPools.Add(canvas, pool);

        // 如果已设置DataContext，初始化显示
        var collection = GetDataContext(canvas);
        if (collection != null)
        {
            DataContextToCanvas.AddOrUpdate(collection, canvas);
            collection.CollectionChanged += OnCollectionChanged;
            InitializeCollectionItems(canvas, collection);
        }
    }

    private static void InitializeCollectionItems(Canvas canvas, INotifyCollectionChanged collection)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(collection);

        try
        {
            if (!CanvasPools.TryGetValue(canvas, out var pool))
            {
                return; // 没有对应的池，无法初始化
            }

            if (collection is System.Collections.IEnumerable enumerable)
            {
                int index = 0;
                foreach (var item in enumerable)
                {
                    if (item is null) continue;
                    AddItemToCanvas(canvas, item, index);
                    index++;
                }
            }
        }
        catch (Exception ex)
        {
            // 记录日志
            System.Diagnostics.Debug.WriteLine($"初始化集合项时出错: {ex.Message}");
        }
    }

    private static void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is not INotifyCollectionChanged collection) return;

        // 空值安全处理
        if (!DataContextToCanvas.TryGetValue(collection, out var canvas)) return;
        if (!CanvasPools.TryGetValue(canvas, out var pool)) return;

        // 处理集合变更
        try
        {
            HandleCollectionChanged(canvas, pool, e);
        }
        catch (Exception ex)
        {
            // 记录日志
            System.Diagnostics.Debug.WriteLine($"处理集合变更时出错: {ex.Message}");
        }
    }

    private static void HandleCollectionChanged(Canvas canvas, PoolInfo pool, NotifyCollectionChangedEventArgs e)
    {
        if (canvas is null || pool is null) return;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems is null) break;
                for (int i = 0; i < e.NewItems.Count; i++)
                {
                    var newItem = e.NewItems[i];
                    if (newItem is null) continue;
                    int index = e.NewStartingIndex + i;
                    AddItemToCanvas(canvas, newItem, index);
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems is null) break;
                foreach (var oldItem in e.OldItems)
                {
                    if (oldItem is null) continue;
                    RemoveItemFromCanvas(canvas, oldItem);
                }
                break;

            case NotifyCollectionChangedAction.Replace:
                if (e.OldItems is null || e.NewItems is null) break;
                for (int i = 0; i < Math.Min(e.OldItems.Count, e.NewItems.Count); i++)
                {
                    var oldItem = e.OldItems[i];
                    var newItem = e.NewItems[i];
                    if (oldItem is null || newItem is null) continue;

                    var control = pool.ActiveControls.Find(c => c.DataContext == oldItem);
                    if (control != null)
                    {
                        control.DataContext = newItem;
                        UpdateControlPosition(control, newItem);
                        // 更新ZIndex
                        Canvas.SetZIndex(control, e.NewStartingIndex + i);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Move:
                // 处理项移动
                if (e.OldItems is null || e.NewItems is null) break;
                for (int i = 0; i < Math.Min(e.OldItems.Count, e.NewItems.Count); i++)
                {
                    var item = e.NewItems[i];
                    var control = pool.ActiveControls.Find(c => c.DataContext == item);
                    if (control != null)
                    {
                        Canvas.SetZIndex(control, e.NewStartingIndex + i);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                ClearCanvas(pool);
                // 重置后重新初始化
                var collection = GetDataContext(canvas);
                if (collection != null)
                {
                    InitializeCollectionItems(canvas, collection);
                }
                break;
        }
    }

    private static Control? GetOrCreateControl(Canvas canvas, PoolInfo pool)
    {
        if (canvas is null || pool is null) return null;

        // 从可用池获取控件
        if (pool.AvailableControls.Count > 0)
        {
            return pool.AvailableControls.Dequeue();
        }

        // 动态创建新控件
        try
        {
            if (pool.Template?.LoadContent() is Control control)
            {
                canvas.Children.Add(control);
                return control;
            }
        }
        catch (Exception ex)
        {
            // 模板加载失败
            System.Diagnostics.Debug.WriteLine($"动态创建控件失败: {ex.Message}");
            return null;
        }

        return null;
    }

    private static void AddItemToCanvas(Canvas canvas, object item, int? index = null)
    {
        if (!CanvasPools.TryGetValue(canvas, out var pool) || item is null) return;

        // 从可用池获取或创建控件
        var control = GetOrCreateControl(canvas, pool);
        if (control is null) return;

        // 配置控件
        control.DataContext = item;
        control.Visibility = Visibility.Visible;

        // 设置ZIndex确保正确的显示顺序
        if (index.HasValue)
        {
            Canvas.SetZIndex(control, index.Value);
        }

        // 添加到活动列表
        pool.ActiveControls.Add(control);

        // 设置位置
        UpdateControlPosition(control, item);
    }

    private static void RemoveItemFromCanvas(Canvas canvas, object item)
    {
        if (!CanvasPools.TryGetValue(canvas, out var pool) || item is null) return;

        // 找到对应的控件
        var control = pool.ActiveControls.Find(c => ReferenceEquals(c.DataContext, item));
        if (control is null) return;

        // 回收控件
        RecycleControl(control, pool);
    }

    private static void UpdateControlPosition(Control control, object item)
    {
        if (control is null || item is null) return;

        // 根据您的业务逻辑计算位置
        if (item is ICanvasPositionable positionable)
        {
            Canvas.SetLeft(control, positionable.X);
            Canvas.SetTop(control, positionable.Y);
        }
    }

    private static void RecycleControl(Control control, PoolInfo pool)
    {
        if (control is null || pool is null) return;

        // 从活动列表移除
        pool.ActiveControls.Remove(control);

        // 重置状态
        control.DataContext = null;
        control.Visibility = Visibility.Collapsed;
        Canvas.SetZIndex(control, 0);

        // 放回可用池
        pool.AvailableControls.Enqueue(control);
    }

    private static void ClearCanvas(PoolInfo pool)
    {
        if (pool is null) return;

        // 回收所有活动控件
        foreach (var control in pool.ActiveControls.ToList())
        {
            RecycleControl(control, pool);
        }
    }

    private static void CleanupCanvas(Canvas canvas)
    {
        if (canvas is null) return;

        if (!CanvasPools.TryGetValue(canvas, out var pool)) return;

        // 从附加属性获取集合
        var collection = GetDataContext(canvas);
        if (collection != null)
        {
            collection.CollectionChanged -= OnCollectionChanged;
            DataContextToCanvas.Remove(collection);
        }

        // 清理所有控件
        foreach (var control in pool.ActiveControls.Concat(pool.AvailableControls))
        {
            if (control != null && canvas.Children.Contains(control))
            {
                canvas.Children.Remove(control);
            }
        }

        CanvasPools.Remove(canvas);
    }

    /// <summary>
    /// 用于标识可在Canvas上定位的对象
    /// </summary>
    public interface ICanvasPositionable
    {
        /// <summary>
        /// X坐标
        /// </summary>
        double X { get; }

        /// <summary>
        /// Y坐标
        /// </summary>
        double Y { get; }
    }

    /// <summary>
    /// 预热视图池，预先创建指定数量的控件
    /// </summary>
    /// <param name="canvas">目标Canvas</param>
    /// <param name="count">要预热的控件数量</param>
    public static void PreWarm(Canvas canvas, int count)
    {
        if (canvas is null || count <= 0) return;

        if (!CanvasPools.TryGetValue(canvas, out var pool) || pool.Template is null) return;

        // 确保池中有足够控件
        int needed = count - (pool.AvailableControls.Count + pool.ActiveControls.Count);
        if (needed <= 0) return;

        for (int i = 0; i < needed; i++)
        {
            try
            {
                if (pool.Template.LoadContent() is Control control)
                {
                    control.Visibility = Visibility.Collapsed;
                    canvas.Children.Add(control);
                    pool.AvailableControls.Enqueue(control);
                }
            }
            catch (Exception ex)
            {
                // 记录日志
                System.Diagnostics.Debug.WriteLine($"预热控件失败: {ex.Message}");
                // 继续尝试创建其他控件
            }
        }
    }

    /// <summary>
    /// 获取指定Canvas上活动的控件数量
    /// </summary>
    public static int GetActiveCount(Canvas canvas)
    {
        return canvas is null ? 0 : CanvasPools.TryGetValue(canvas, out var pool) ? pool.ActiveControls.Count : 0;
    }

    /// <summary>
    /// 获取指定Canvas上可用的控件数量
    /// </summary>
    public static int GetAvailableCount(Canvas canvas)
    {
        return canvas is null ? 0 : CanvasPools.TryGetValue(canvas, out var pool) ? pool.AvailableControls.Count : 0;
    }

    /// <summary>
    /// 清理所有视图池
    /// </summary>
    public static void CleanupAll()
    {
        // 获取所有Canvas的副本，避免在枚举时修改集合
        var canvases = new List<Canvas>();
        foreach (var entry in CanvasPools)
        {
            canvases.Add(entry.Key);
        }

        foreach (var canvas in canvases)
        {
            CleanupCanvas(canvas);
        }
    }
}