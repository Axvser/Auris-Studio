using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using VeloxDev.Core.MVVM;

namespace Auris_Studio.ViewModels.ComponentModel
{
    public partial class TimeOrderableCollection<T> : ICollection<T>, INotifyCollectionChanged
        where T : class, ITimeOrderable
    {
        private readonly struct ItemLocation(long startTick, int startIndex, long endTick, int endIndex, bool isVirtualized)
        {
            public readonly long StartTick = startTick;
            public readonly int StartIndex = startIndex;
            public readonly long EndTick = endTick;
            public readonly int EndIndex = endIndex;
            public readonly bool IsVirtualized = isVirtualized;
        }

        public event Action<T>? ItemVirtualized;
        public event Action<T>? ItemRestored;
        public event Action<IEnumerable<T>>? Update;

        [VeloxProperty] private long _maxTime = 0;
        [VeloxProperty] private ObservableCollection<T> _visibleItems = [];

        private readonly SortedDictionary<long, List<T>> _timeBuckets = [];
        private readonly SortedDictionary<long, List<T>> _endTimeBuckets = [];
        private readonly Dictionary<T, ItemLocation> _reverseIndex = [];
        private readonly List<long> _endTimeList = [];

        private long _virtualStart = 0;
        private long _virtualEnd = long.MaxValue;
        private bool _isVirtualized = false;
        private bool _isUpdatingMaxTime = false;

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public TimeOrderableCollection() { }

        public TimeOrderableCollection(IEnumerable<T> items)
        {
            if (items == null) return;
            AddRange(items);
        }

        public int Count => _reverseIndex.Count;
        public bool IsReadOnly => false;
        public bool Contains(T item) => item != null && _reverseIndex.ContainsKey(item);

        public void Add(T item)
        {
            if (item == null || _reverseIndex.ContainsKey(item)) return;

            long startTick = item.AbsoluteTime;
            long endTick = startTick + item.DeltaTime;

            AddEndTime(endTick);
            UpdateMaxTime();

            bool isVirtualized = _isVirtualized && !IsItemVisible(startTick, endTick);
            AddToBucket(item, startTick, endTick, isVirtualized);
            item.PropertyChanged += OnItemPropertyChanged;

            if (!isVirtualized)
            {
                int insertIndex = FindInsertIndex(startTick, 0);
                if (insertIndex >= 0 && insertIndex <= _visibleItems.Count)
                {
                    _visibleItems.Insert(insertIndex, item);
                    OnCollectionChanged(NotifyCollectionChangedAction.Add, item, insertIndex);
                }
                else
                {
                    _visibleItems.Add(item);
                    OnCollectionChanged(NotifyCollectionChangedAction.Add, item, _visibleItems.Count - 1);
                }
            }
        }

        public void AddRange(IEnumerable<T> items)
        {
            if (items == null) return;

            var itemsToAdd = items.Where(item => item != null && !_reverseIndex.ContainsKey(item)).ToList();
            if (itemsToAdd.Count == 0) return;

            var newVisibleItems = new List<T>();
            var newVirtualizedItems = new List<T>();

            foreach (var item in itemsToAdd)
            {
                long startTick = item.AbsoluteTime;
                long endTick = startTick + item.DeltaTime;
                bool isVirtualized = _isVirtualized && !IsItemVisible(startTick, endTick);

                AddEndTime(endTick);
                AddToBucket(item, startTick, endTick, isVirtualized);
                item.PropertyChanged += OnItemPropertyChanged;

                if (!isVirtualized)
                {
                    newVisibleItems.Add(item);
                }
                else
                {
                    newVirtualizedItems.Add(item);
                }
            }

            UpdateMaxTime();

            if (newVisibleItems.Count > 0)
            {
                newVisibleItems.Sort((a, b) => a.AbsoluteTime.CompareTo(b.AbsoluteTime));

                int insertIndex = 0;
                foreach (var item in newVisibleItems)
                {
                    insertIndex = FindInsertIndex(item.AbsoluteTime, insertIndex);
                    if (insertIndex >= 0 && insertIndex <= _visibleItems.Count)
                    {
                        _visibleItems.Insert(insertIndex, item);
                    }
                    else
                    {
                        _visibleItems.Add(item);
                    }
                }

                OnCollectionChanged(NotifyCollectionChangedAction.Reset);
            }
        }

        public bool Remove(T item)
        {
            if (item == null || !_reverseIndex.TryGetValue(item, out var location)) return false;

            RemoveEndTime(location.EndTick);
            UpdateMaxTime();

            item.PropertyChanged -= OnItemPropertyChanged;
            RemoveFromBucket(location.StartTick, location.StartIndex, location.EndTick, location.EndIndex);

            if (!location.IsVirtualized)
            {
                int oldIndex = _visibleItems.IndexOf(item);
                if (oldIndex >= 0)
                {
                    _visibleItems.RemoveAt(oldIndex);
                    OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, oldIndex);
                }
            }

            _reverseIndex.Remove(item);
            return true;
        }

        public void Clear()
        {
            foreach (var item in _reverseIndex.Keys)
            {
                item.PropertyChanged -= OnItemPropertyChanged;
            }

            _timeBuckets.Clear();
            _endTimeBuckets.Clear();
            _reverseIndex.Clear();
            _endTimeList.Clear();
            _maxTime = 0;
            _isVirtualized = false;
            _virtualStart = 0;
            _virtualEnd = long.MaxValue;

            _visibleItems.Clear();

            OnPropertyChanged(nameof(MaxTime));
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }

        public void Virtualize(long startTime, long endTime)
        {
            if (_virtualStart == startTime && _virtualEnd == endTime && _isVirtualized)
                return;

            _isVirtualized = true;
            _virtualStart = startTime;
            _virtualEnd = endTime;

            var itemsToVirtualize = new List<T>();
            var itemsToRestore = new List<T>();

            foreach (var item in _reverseIndex.Keys)
            {
                var loc = _reverseIndex[item];
                bool wasVirtualized = loc.IsVirtualized;
                bool isNowVirtualized = _isVirtualized && !IsItemVisible(loc.StartTick, loc.EndTick);

                if (wasVirtualized != isNowVirtualized)
                {
                    _reverseIndex[item] = new ItemLocation(
                        loc.StartTick, loc.StartIndex,
                        loc.EndTick, loc.EndIndex,
                        isNowVirtualized
                    );

                    if (wasVirtualized)
                    {
                        itemsToRestore.Add(item);
                    }
                    else
                    {
                        itemsToVirtualize.Add(item);
                    }
                }
            }

            ProcessVisibilityChanges(itemsToVirtualize, itemsToRestore);

            if (itemsToVirtualize.Count > 0 || itemsToRestore.Count > 0)
            {
                Update?.Invoke(_visibleItems);
                OnCollectionChanged(NotifyCollectionChangedAction.Reset);
            }
        }

        public void Restore(Action<T>? added = null)
        {
            if (!_isVirtualized) return;

            _isVirtualized = false;
            _virtualStart = 0;
            _virtualEnd = long.MaxValue;

            var itemsToRestore = _reverseIndex
                .Where(kvp => kvp.Value.IsVirtualized)
                .Select(kvp => kvp.Key)
                .OrderBy(item => item.AbsoluteTime)
                .ToList();

            if (itemsToRestore.Count == 0) return;

            foreach (var item in itemsToRestore)
            {
                var loc = _reverseIndex[item];
                _reverseIndex[item] = new ItemLocation(
                    loc.StartTick, loc.StartIndex,
                    loc.EndTick, loc.EndIndex,
                    false
                );
            }

            int insertIndex = 0;
            foreach (var item in itemsToRestore)
            {
                insertIndex = FindInsertIndex(item.AbsoluteTime, insertIndex);
                if (insertIndex >= 0 && insertIndex <= _visibleItems.Count)
                {
                    _visibleItems.Insert(insertIndex, item);
                }
                else
                {
                    _visibleItems.Add(item);
                }

                added?.Invoke(item);
                ItemRestored?.Invoke(item);
            }

            Update?.Invoke(_visibleItems);
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }

        public IEnumerable<T> QueryAtStart(long tick)
        {
            if (_timeBuckets.TryGetValue(tick, out var bucket))
            {
                foreach (var item in bucket)
                {
                    if (item != null && _reverseIndex.TryGetValue(item, out var loc) && !loc.IsVirtualized)
                        yield return item;
                }
            }
        }

        public IEnumerable<T> QueryAtEnd(long tick)
        {
            if (_endTimeBuckets.TryGetValue(tick, out var bucket))
            {
                foreach (var item in bucket)
                {
                    if (item != null && _reverseIndex.TryGetValue(item, out var loc) && !loc.IsVirtualized)
                        yield return item;
                }
            }
        }

        public IEnumerable<T> Query(long startTime, long endTime)
        {
            var relevantKeys = _timeBuckets.Keys.Where(k => k < endTime).ToList();

            foreach (var key in relevantKeys)
            {
                if (_timeBuckets.TryGetValue(key, out var bucket))
                {
                    foreach (var item in bucket)
                    {
                        if (item != null && _reverseIndex.TryGetValue(item, out var loc) &&
                            !loc.IsVirtualized && loc.EndTick > startTime)
                        {
                            yield return item;
                        }
                    }
                }
            }
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _visibleItems.Count)
                    throw new IndexOutOfRangeException();
                return _visibleItems[index];
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            if (arrayIndex < 0 || arrayIndex >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            int currentIndex = arrayIndex;
            foreach (var item in _visibleItems)
            {
                if (currentIndex >= array.Length) break;
                array[currentIndex++] = item;
            }
        }

        public IEnumerator<T> GetEnumerator() => _visibleItems.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private bool IsItemVisible(long startTick, long endTick)
        {
            return endTick > _virtualStart && startTick < _virtualEnd;
        }

        private void AddToBucket(T item, long startTick, long endTick, bool isVirtualized)
        {
            if (!_timeBuckets.TryGetValue(startTick, out var startList))
            {
                startList = [];
                _timeBuckets[startTick] = startList;
            }
            int startIndex = startList.Count;
            startList.Add(item);

            if (!_endTimeBuckets.TryGetValue(endTick, out var endList))
            {
                endList = [];
                _endTimeBuckets[endTick] = endList;
            }
            int endIndex = endList.Count;
            endList.Add(item);

            _reverseIndex[item] = new ItemLocation(startTick, startIndex, endTick, endIndex, isVirtualized);
        }

        private void RemoveFromBucket(long startTick, int startIndex, long endTick, int endIndex)
        {
            if (_timeBuckets.TryGetValue(startTick, out var startList))
            {
                RemoveFromBucketList(startList, startIndex, startTick, isStartBucket: true);
                if (startList.Count == 0)
                    _timeBuckets.Remove(startTick);
            }

            if (_endTimeBuckets.TryGetValue(endTick, out var endList))
            {
                RemoveFromBucketList(endList, endIndex, endTick, isStartBucket: false);
                if (endList.Count == 0)
                    _endTimeBuckets.Remove(endTick);
            }
        }

        private void RemoveFromBucketList(List<T> bucket, int index, long tick, bool isStartBucket)
        {
            int lastIdx = bucket.Count - 1;
            if (index < 0 || index > lastIdx) return;

            if (index != lastIdx)
            {
                var lastItem = bucket[lastIdx];
                bucket[index] = lastItem;

                if (_reverseIndex.TryGetValue(lastItem, out var loc))
                {
                    bool isNowVirtualized = _isVirtualized && !IsItemVisible(loc.StartTick, loc.EndTick);
                    _reverseIndex[lastItem] = isStartBucket
                        ? new ItemLocation(tick, index, loc.EndTick, loc.EndIndex, isNowVirtualized)
                        : new ItemLocation(loc.StartTick, loc.StartIndex, tick, index, isNowVirtualized);
                }
            }

            bucket.RemoveAt(lastIdx);
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not T item) return;

            if (e.PropertyName != nameof(ITimeOrderable.AbsoluteTime) &&
                e.PropertyName != nameof(ITimeOrderable.DeltaTime))
                return;

            if (!_reverseIndex.TryGetValue(item, out var oldLoc)) return;

            long newStart = item.AbsoluteTime;
            long newEnd = newStart + item.DeltaTime;
            bool wasVirtualized = oldLoc.IsVirtualized;
            bool isNowVirtualized = _isVirtualized && !IsItemVisible(newStart, newEnd);
            bool timeChanged = (oldLoc.StartTick != newStart) || (oldLoc.EndTick != newEnd);

            int oldVisibleIndex = -1;
            if (!wasVirtualized)
            {
                oldVisibleIndex = _visibleItems.IndexOf(item);
            }

            if (oldLoc.EndTick != newEnd)
            {
                RemoveEndTime(oldLoc.EndTick);
                AddEndTime(newEnd);
                UpdateMaxTime();
            }

            if (timeChanged)
            {
                RemoveFromBucket(oldLoc.StartTick, oldLoc.StartIndex, oldLoc.EndTick, oldLoc.EndIndex);

                if (!wasVirtualized && oldVisibleIndex >= 0)
                {
                    _visibleItems.RemoveAt(oldVisibleIndex);
                    OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, oldVisibleIndex);
                }

                AddToBucket(item, newStart, newEnd, isNowVirtualized);

                if (!isNowVirtualized)
                {
                    int newIndex = FindInsertIndex(newStart, 0);
                    if (newIndex >= 0 && newIndex <= _visibleItems.Count)
                    {
                        _visibleItems.Insert(newIndex, item);
                        OnCollectionChanged(NotifyCollectionChangedAction.Add, item, newIndex);
                    }
                    else
                    {
                        _visibleItems.Add(item);
                        OnCollectionChanged(NotifyCollectionChangedAction.Add, item, _visibleItems.Count - 1);
                    }

                    if (wasVirtualized)
                    {
                        ItemRestored?.Invoke(item);
                    }
                }
                else
                {
                    if (!wasVirtualized)
                    {
                        ItemVirtualized?.Invoke(item);
                    }
                }
            }
            else if (wasVirtualized != isNowVirtualized)
            {
                _reverseIndex[item] = new ItemLocation(
                    oldLoc.StartTick, oldLoc.StartIndex,
                    oldLoc.EndTick, oldLoc.EndIndex,
                    isNowVirtualized
                );

                if (wasVirtualized && !isNowVirtualized)
                {
                    int newIndex = FindInsertIndex(newStart, 0);
                    if (newIndex >= 0 && newIndex <= _visibleItems.Count)
                    {
                        _visibleItems.Insert(newIndex, item);
                        OnCollectionChanged(NotifyCollectionChangedAction.Add, item, newIndex);
                    }
                    else
                    {
                        _visibleItems.Add(item);
                        OnCollectionChanged(NotifyCollectionChangedAction.Add, item, _visibleItems.Count - 1);
                    }
                    ItemRestored?.Invoke(item);
                }
                else if (!wasVirtualized && isNowVirtualized)
                {
                    if (oldVisibleIndex >= 0)
                    {
                        _visibleItems.RemoveAt(oldVisibleIndex);
                        OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, oldVisibleIndex);
                        ItemVirtualized?.Invoke(item);
                    }
                }
            }
        }

        private int FindInsertIndex(long targetStartTime, int startSearchIndex = 0)
        {
            for (int i = startSearchIndex; i < _visibleItems.Count; i++)
            {
                if (_visibleItems[i].AbsoluteTime >= targetStartTime)
                    return i;
            }
            return _visibleItems.Count;
        }

        private void AddEndTime(long endTime)
        {
            int index = _endTimeList.BinarySearch(endTime);
            if (index < 0) index = ~index;
            _endTimeList.Insert(index, endTime);
        }

        private bool RemoveEndTime(long endTime)
        {
            int index = _endTimeList.BinarySearch(endTime);
            if (index >= 0)
            {
                _endTimeList.RemoveAt(index);
                return true;
            }
            return false;
        }

        private void UpdateMaxTime()
        {
            if (_isUpdatingMaxTime) return;

            _isUpdatingMaxTime = true;
            try
            {
                long newMax = _endTimeList.Count > 0 ? _endTimeList[^1] : 0;
                if (newMax != _maxTime)
                {
                    _maxTime = newMax;
                    OnPropertyChanged(nameof(MaxTime));
                }
            }
            finally
            {
                _isUpdatingMaxTime = false;
            }
        }

        private void ProcessVisibilityChanges(List<T> itemsToVirtualize, List<T> itemsToRestore)
        {
            if (itemsToVirtualize.Count > 0)
            {
                var itemsWithIndices = itemsToVirtualize
                    .Select(item => (item, index: _visibleItems.IndexOf(item)))
                    .Where(x => x.index >= 0)
                    .OrderByDescending(x => x.index)
                    .ToList();

                foreach (var (item, index) in itemsWithIndices)
                {
                    _visibleItems.RemoveAt(index);
                    ItemVirtualized?.Invoke(item);
                }
            }

            if (itemsToRestore.Count > 0)
            {
                itemsToRestore.Sort((a, b) => a.AbsoluteTime.CompareTo(b.AbsoluteTime));

                int insertIndex = 0;
                foreach (var item in itemsToRestore)
                {
                    insertIndex = FindInsertIndex(item.AbsoluteTime, insertIndex);
                    if (insertIndex >= 0 && insertIndex <= _visibleItems.Count)
                    {
                        _visibleItems.Insert(insertIndex, item);
                    }
                    else
                    {
                        insertIndex = _visibleItems.Count;
                        _visibleItems.Add(item);
                    }
                    ItemRestored?.Invoke(item);
                }
            }
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, T? item = null, int index = -1, int oldIndex = -1)
        {
            CollectionChanged?.Invoke(this, action switch
            {
                NotifyCollectionChangedAction.Add when item != null
                    => new NotifyCollectionChangedEventArgs(action, item, index),
                NotifyCollectionChangedAction.Remove when item != null
                    => new NotifyCollectionChangedEventArgs(action, item, index),
                NotifyCollectionChangedAction.Move when item != null && oldIndex >= 0
                    => new NotifyCollectionChangedEventArgs(action, item, index, oldIndex),
                _ => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)
            });
        }
    }
}