# TimeOrderableCollection 时间复杂度分析

> Virtualize方法首次调用后进入虚拟化模式

## 1. 添加单个元素操作 (Add 方法)

### 涉及函数
```csharp
public void Add(T item)
private void AddToBucket(T item, long startTick, long endTick, bool isVirtualized)
private int FindInsertIndex(long targetStartTime, int startSearchIndex = 0)
private void AddEndTime(long endTime)
private void UpdateMaxTime()
private bool IsItemVisible(long startTick, long endTick)
```

### 时间复杂度分析
- **查找插入位置 (FindInsertIndex)**: O(n)，其中n是当前可见元素数量
- **添加到时间桶 (AddToBucket)**:
  - 添加开始时间桶: O(1) 平均，O(log n) 最坏（SortedDictionary操作）
  - 添加结束时间桶: O(1) 平均，O(log n) 最坏
  - 更新反向索引: O(1)
- **更新结束时间列表 (AddEndTime)**: O(log m)，其中m是不同结束时间的数量（二分查找+插入）
- **更新最大时间 (UpdateMaxTime)**: O(1)
- **可见性检查 (IsItemVisible)**: O(1)
- **可见列表插入**: O(n)（最坏情况）

**总体时间复杂度**: O(n + log m)，其中n是可见元素数量，m是结束时间数量

### 集合变更
- 如果元素在当前虚拟化范围内可见: 触发`CollectionChanged(NotifyCollectionChangedAction.Add)`
- 如果元素虚拟化: 触发`ItemVirtualized`事件
- 如果最大时间变化: 触发`PropertyChanged(nameof(MaxTime))`

## 2. 批量添加操作 (AddRange 方法)

### 涉及函数
```csharp
public void AddRange(IEnumerable<T> items)
private void AddToBucket(T item, long startTick, long endTick, bool isVirtualized)
private int FindInsertIndex(long targetStartTime, int startSearchIndex = 0)
private void AddEndTime(long endTime)
private void UpdateMaxTime()
```

### 时间复杂度分析
- **过滤重复项**: O(k)，其中k是输入元素数量
- **批量添加到索引**: O(k × log m)（k个元素，每个元素O(log m)）
- **可见元素排序**: O(v log v)，其中v是需要插入的可见元素数量
- **批量插入到可见列表**: O(n + v)（n是现有可见元素数量）

**总体时间复杂度**: O(k × log m + v log v + n + v)

### 集合变更
- 如果有可见元素添加: 触发一次`CollectionChanged(NotifyCollectionChangedAction.Reset)`
- 触发`Update`事件
- 不触发`ItemVirtualized`事件（批量处理中不触发）

## 3. 删除操作 (Remove 方法)

### 涉及函数
```csharp
public bool Remove(T item)
private void RemoveFromBucket(long startTick, int startIndex, long endTick, int endIndex)
private void RemoveEndTime(long endTime)
private void UpdateMaxTime()
```

### 时间复杂度分析
- **反向索引查找**: O(1) 平均
- **从时间桶移除 (RemoveFromBucket)**:
  - 从开始时间桶移除: O(1) 平均
  - 从结束时间桶移除: O(1) 平均
  - 可能需要更新最后一个元素的索引位置: O(1)
- **从结束时间列表移除 (RemoveEndTime)**: O(log m)（二分查找）
- **从可见列表移除**: O(n) 最坏（需要查找元素位置）

**总体时间复杂度**: O(n + log m)

### 集合变更
- 如果元素可见: 触发`CollectionChanged(NotifyCollectionChangedAction.Remove)`
- 如果最大时间变化: 触发`PropertyChanged(nameof(MaxTime))`

## 4. 虚拟化操作 (Virtualize 方法)

### 涉及函数
```csharp
public void Virtualize(long startTime, long endTime)
private bool IsItemVisible(long startTick, long endTick)
private void ProcessVisibilityChanges(List<T> itemsToVirtualize, List<T> itemsToRestore)
```

### 时间复杂度分析
- **遍历所有元素检查可见性**: O(N)，其中N是集合中总元素数量
- **计算可见性变化**: O(N)
- **处理可见性变化 (ProcessVisibilityChanges)**:
  - 虚拟化元素移除: O(v × n)，其中v是需虚拟化的元素数量
  - 恢复元素插入: O(r × n)，其中r是需恢复的元素数量
  - 排序恢复元素: O(r log r)

**总体时间复杂度**: O(N + (v + r) × n + r log r)

### 集合变更
- 触发`Update`事件
- 触发`CollectionChanged(NotifyCollectionChangedAction.Reset)`
- 为每个虚拟化元素触发`ItemVirtualized`事件
- 为每个恢复元素触发`ItemRestored`事件

## 5. 恢复操作 (Restore 方法)

### 涉及函数
```csharp
public void Restore(Action<T>? added = null)
private int FindInsertIndex(long targetStartTime, int startSearchIndex = 0)
```

### 时间复杂度分析
- **筛选虚拟化元素**: O(N)
- **排序恢复元素**: O(r log r)，其中r是虚拟化元素数量
- **插入到可见列表**: O(r × (n + r))，每次插入都可能需要遍历现有列表

**总体时间复杂度**: O(N + r log r + r × (n + r))

### 集合变更
- 为每个恢复元素触发`added`回调（如果提供）
- 为每个恢复元素触发`ItemRestored`事件
- 触发`Update`事件
- 触发`CollectionChanged(NotifyCollectionChangedAction.Reset)`

## 6. 查询操作

### Query 方法
```csharp
public IEnumerable<T> Query(long startTime, long endTime)
```

**时间复杂度**: O(k + v)，其中k是相关时间桶的数量，v是符合条件的可见元素数量

### QueryAtStart 方法
```csharp
public IEnumerable<T> QueryAtStart(long tick)
```

**时间复杂度**: O(1) 获取桶，O(v) 遍历桶中可见元素

### QueryAtEnd 方法
```csharp
public IEnumerable<T> QueryAtEnd(long tick)
```

**时间复杂度**: O(1) 获取桶，O(v) 遍历桶中可见元素

## 7. 属性变更响应 (OnItemPropertyChanged)

### 涉及函数
```csharp
private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
private bool IsItemVisible(long startTick, long endTick)
private int FindInsertIndex(long targetStartTime, int startSearchIndex = 0)
private void AddEndTime(long endTime)
private bool RemoveEndTime(long endTime)
private void UpdateMaxTime()
private void AddToBucket(T item, long startTick, long endTick, bool isVirtualized)
private void RemoveFromBucket(long startTick, int startIndex, long endTick, int endIndex)
```

### 时间复杂度分析
- **时间范围不变，仅可见性变化**: O(n)（查找位置+插入/移除）
- **时间范围变化**:
  - 从旧桶移除: O(1)
  - 添加到新桶: O(1)
  - 从可见列表移除: O(n)
  - 插入到新位置: O(n)
  - 更新结束时间列表: O(log m)

**总体时间复杂度**: 最坏情况 O(n + log m)

### 集合变更
- 如果位置变化: 触发`CollectionChanged`（Add/Remove）
- 如果最大时间变化: 触发`PropertyChanged(nameof(MaxTime))`
- 如果虚拟化状态变化: 触发`ItemVirtualized`或`ItemRestored`事件

## 8. 清除操作 (Clear 方法)

### 时间复杂度
- **清理所有内部数据结构**: O(N)
- **清理事件订阅**: O(N)

**总体时间复杂度**: O(N)

### 集合变更
- 触发`CollectionChanged(NotifyCollectionChangedAction.Reset)`
- 触发`PropertyChanged(nameof(MaxTime))`

## 总结

| 操作 | 时间复杂度 | 主要集合变更事件 |
|------|-----------|-----------------|
| Add | O(n + log m) | CollectionChanged(Add), ItemVirtualized |
| AddRange | O(k log m + v log v + n) | CollectionChanged(Reset), Update |
| Remove | O(n + log m) | CollectionChanged(Remove) |
| Virtualize | O(N + (v + r)n + r log r) | Update, CollectionChanged(Reset), ItemVirtualized, ItemRestored |
| Restore | O(N + r log r + r × (n + r)) | Update, CollectionChanged(Reset), ItemRestored |
| Query | O(k + v) | 无 |
| PropertyChanged | O(n + log m) | 多种（取决于变化类型） |
| Clear | O(N) | CollectionChanged(Reset) |

其中：
- N: 集合中总元素数量
- n: 当前可见元素数量
- m: 不同结束时间的数量
- k: 查询时相关时间桶的数量
- v: 符合条件的可见元素数量
- r: 需要恢复的元素数量