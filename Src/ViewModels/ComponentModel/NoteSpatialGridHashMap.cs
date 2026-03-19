using Auris_Studio.ViewModels.MidiEvents;
using System.ComponentModel;

namespace Auris_Studio.ViewModels.ComponentModel
{
    public class NoteSpatialGridHashMap(double cellSize = 20.0)
    {
        private readonly Dictionary<CellKey, HashSet<NoteEventViewModel>> _grid = [];
        private readonly Dictionary<NoteEventViewModel, (double left, double bottom, double width, double height, INotifyPropertyChanged notifier)> _trackedNotes = [];
        private readonly double _cellSize = Math.Max(1.0, cellSize);

        /// <summary>
        /// 单元格键，用于网格索引
        /// </summary>
        private readonly struct CellKey(int x, int y) : IEquatable<CellKey>
        {
            public readonly int X = x;
            public readonly int Y = y;

            public bool Equals(CellKey other) => X == other.X && Y == other.Y;
            public override bool Equals(object? obj) => obj is CellKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(X, Y);
        }

        /// <summary>
        /// 插入音符到索引
        /// </summary>
        public void Insert(NoteEventViewModel note)
        {
            if (note == null || _trackedNotes.ContainsKey(note)) return;

            // 获取音符当前位置
            double left = note.Left;
            double bottom = note.Bottom;
            double width = note.Width;
            double height = note.Height;

            // 注册属性变化监听
            note.PropertyChanged += OnNotePropertyChanged;
            _trackedNotes[note] = (left, bottom, width, height, note);

            // 索引音符
            IndexNote(note, left, bottom, width, height);
        }

        /// <summary>
        /// 从索引中移除音符
        /// </summary>
        public void Remove(NoteEventViewModel note)
        {
            if (note == null || !_trackedNotes.TryGetValue(note, out var entry)) return;

            // 移除属性变化监听
            note.PropertyChanged -= OnNotePropertyChanged;

            // 从索引中移除音符
            DeindexNote(note, entry.left, entry.bottom, entry.width, entry.height);

            // 从跟踪字典中移除
            _trackedNotes.Remove(note);
        }

        /// <summary>
        /// 查询指定矩形区域内的所有音符
        /// </summary>
        public IEnumerable<NoteEventViewModel> Query(double queryLeft, double queryBottom, double queryWidth, double queryHeight)
        {
            if (queryWidth <= 0 || queryHeight <= 0) yield break;

            double queryRight = queryLeft + queryWidth;
            double queryTop = queryBottom + queryHeight;
            var seen = new HashSet<NoteEventViewModel>();

            // 计算查询区域覆盖的单元格
            int minX = (int)Math.Floor(queryLeft / _cellSize);
            int maxX = (int)Math.Ceiling(queryRight / _cellSize);
            int minY = (int)Math.Floor(queryBottom / _cellSize);
            int maxY = (int)Math.Ceiling(queryTop / _cellSize);

            // 遍历所有覆盖的单元格
            for (int x = minX; x < maxX; x++)
            {
                for (int y = minY; y < maxY; y++)
                {
                    var cellKey = new CellKey(x, y);
                    if (_grid.TryGetValue(cellKey, out var set))
                    {
                        foreach (var note in set)
                        {
                            if (!seen.Add(note)) continue;

                            // 精确检查音符是否在查询区域内
                            if (_trackedNotes.TryGetValue(note, out var noteEntry))
                            {
                                double noteLeft = noteEntry.left;
                                double noteBottom = noteEntry.bottom;
                                double noteWidth = noteEntry.width;
                                double noteHeight = noteEntry.height;
                                double noteRight = noteLeft + noteWidth;
                                double noteTop = noteBottom + noteHeight;

                                // 检查矩形相交
                                if (noteLeft < queryRight && noteRight > queryLeft &&
                                    noteBottom < queryTop && noteTop > queryBottom)
                                {
                                    yield return note;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 点查询：检查指定点是否命中任何音符
        /// </summary>
        public NoteEventViewModel? PointQuery(double x, double y)
        {
            // 计算点所在的单元格
            int cellX = (int)Math.Floor(x / _cellSize);
            int cellY = (int)Math.Floor(y / _cellSize);
            var cellKey = new CellKey(cellX, cellY);

            if (_grid.TryGetValue(cellKey, out var set))
            {
                // 检查该单元格中的所有音符
                foreach (var note in set)
                {
                    if (_trackedNotes.TryGetValue(note, out var noteEntry))
                    {
                        double noteLeft = noteEntry.left;
                        double noteBottom = noteEntry.bottom;
                        double noteWidth = noteEntry.width;
                        double noteHeight = noteEntry.height;
                        double noteRight = noteLeft + noteWidth;
                        double noteTop = noteBottom + noteHeight;

                        // 检查点是否在音符矩形内
                        if (x >= noteLeft && x < noteRight && y >= noteBottom && y < noteTop)
                        {
                            return note;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 清除所有索引
        /// </summary>
        public void Clear()
        {
            // 移除所有属性变化监听
            foreach (var note in _trackedNotes.Keys)
            {
                note.PropertyChanged -= OnNotePropertyChanged;
            }

            _trackedNotes.Clear();
            _grid.Clear();
        }

        /// <summary>
        /// 获取索引中的音符总数
        /// </summary>
        public int Count => _trackedNotes.Count;

        /// <summary>
        /// 处理音符属性变化事件
        /// </summary>
        private void OnNotePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not NoteEventViewModel note) return;

            // 只监听位置和尺寸相关属性
            if (e.PropertyName != nameof(NoteEventViewModel.Left) &&
                e.PropertyName != nameof(NoteEventViewModel.Bottom) &&
                e.PropertyName != nameof(NoteEventViewModel.Width) &&
                e.PropertyName != nameof(NoteEventViewModel.Height))
            {
                return;
            }

            if (!_trackedNotes.TryGetValue(note, out var oldEntry)) return;

            // 获取新位置
            double newLeft = note.Left;
            double newBottom = note.Bottom;
            double newWidth = note.Width;
            double newHeight = note.Height;

            // 检查位置是否发生变化
            if (Math.Abs(oldEntry.left - newLeft) < 0.001 &&
                Math.Abs(oldEntry.bottom - newBottom) < 0.001 &&
                Math.Abs(oldEntry.width - newWidth) < 0.001 &&
                Math.Abs(oldEntry.height - newHeight) < 0.001)
            {
                return; // 位置未变化
            }

            // 从旧位置移除索引
            DeindexNote(note, oldEntry.left, oldEntry.bottom, oldEntry.width, oldEntry.height);

            // 在新位置重新索引
            IndexNote(note, newLeft, newBottom, newWidth, newHeight);

            // 更新跟踪字典
            _trackedNotes[note] = (newLeft, newBottom, newWidth, newHeight, note);
        }

        /// <summary>
        /// 索引音符到网格
        /// </summary>
        private void IndexNote(NoteEventViewModel note, double left, double bottom, double width, double height)
        {
            if (width <= 0 || height <= 0) return;

            double right = left + width;
            double top = bottom + height;

            // 计算音符覆盖的单元格范围
            int minX = (int)Math.Floor(left / _cellSize);
            int maxX = (int)Math.Ceiling(right / _cellSize);
            int minY = (int)Math.Floor(bottom / _cellSize);
            int maxY = (int)Math.Ceiling(top / _cellSize);

            // 将音符添加到所有覆盖的单元格
            for (int x = minX; x < maxX; x++)
            {
                for (int y = minY; y < maxY; y++)
                {
                    var cellKey = new CellKey(x, y);
                    if (!_grid.TryGetValue(cellKey, out var set))
                    {
                        set = [];
                        _grid[cellKey] = set;
                    }
                    set.Add(note);
                }
            }
        }

        /// <summary>
        /// 从网格中移除音符索引
        /// </summary>
        private void DeindexNote(NoteEventViewModel note, double left, double bottom, double width, double height)
        {
            if (width <= 0 || height <= 0) return;

            double right = left + width;
            double top = bottom + height;

            // 计算音符覆盖的单元格范围
            int minX = (int)Math.Floor(left / _cellSize);
            int maxX = (int)Math.Ceiling(right / _cellSize);
            int minY = (int)Math.Floor(bottom / _cellSize);
            int maxY = (int)Math.Ceiling(top / _cellSize);

            // 从所有覆盖的单元格中移除音符
            for (int x = minX; x < maxX; x++)
            {
                for (int y = minY; y < maxY; y++)
                {
                    var cellKey = new CellKey(x, y);
                    if (_grid.TryGetValue(cellKey, out var set))
                    {
                        set.Remove(note);
                        if (set.Count == 0)
                        {
                            _grid.Remove(cellKey);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取所有索引的音符
        /// </summary>
        public IEnumerable<NoteEventViewModel> GetAllNotes()
        {
            return _trackedNotes.Keys;
        }

        /// <summary>
        /// 批量插入音符
        /// </summary>
        public void InsertRange(IEnumerable<NoteEventViewModel> notes)
        {
            foreach (var note in notes)
            {
                Insert(note);
            }
        }

        /// <summary>
        /// 批量移除音符
        /// </summary>
        public void RemoveRange(IEnumerable<NoteEventViewModel> notes)
        {
            foreach (var note in notes)
            {
                Remove(note);
            }
        }

        /// <summary>
        /// 重新构建整个索引（用于音符列表变化时）
        /// </summary>
        public void Rebuild(IEnumerable<NoteEventViewModel> notes)
        {
            Clear();
            InsertRange(notes);
        }
    }
}