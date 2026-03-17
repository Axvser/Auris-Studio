using Auris_Studio.ViewModels.ComponentModel;
using Auris_Studio.ViewModels.MidiEvents;
using System.Diagnostics;

namespace Test
{
    [TestClass]
    public sealed class Test_NoteSpatialGridHashMap
    {
        private readonly Random _random = new(12345);

        // 辅助方法：创建音符并设置属性
        private NoteEventViewModel CreateNote(double left, double bottom, double width, double height)
        {
            var note = new NoteEventViewModel();

            // 使用反射设置属性
            ReflectionHelper.SetProperty(note, "Left", left);
            ReflectionHelper.SetProperty(note, "Bottom", bottom);
            ReflectionHelper.SetProperty(note, "Width", width);
            ReflectionHelper.SetProperty(note, "Height", height);

            return note;
        }

        // 辅助方法：获取音符属性
        private (double left, double bottom, double width, double height) GetNoteProperties(NoteEventViewModel note)
        {
            double left = ReflectionHelper.GetProperty<double>(note, "Left");
            double bottom = ReflectionHelper.GetProperty<double>(note, "Bottom");
            double width = ReflectionHelper.GetProperty<double>(note, "Width");
            double height = ReflectionHelper.GetProperty<double>(note, "Height");

            return (left, bottom, width, height);
        }

        [TestMethod]
        public void Insert_ShouldAddNoteToIndex()
        {
            // Arrange
            var spatialIndex = new NoteSpatialGridHashMap(20.0);
            var note = CreateNote(100, 50, 30, 20);

            // Act
            spatialIndex.Insert(note);

            // Assert
            Assert.AreEqual(1, spatialIndex.Count);

            // 查询应该能找到这个音符
            var result = spatialIndex.PointQuery(115, 60); // 在音符中心附近
            Assert.AreEqual(note, result);
        }

        [TestMethod]
        public void PropertyChange_Position_ShouldUpdateIndex()
        {
            // Arrange
            var spatialIndex = new NoteSpatialGridHashMap(20.0);
            var note = CreateNote(100, 50, 30, 20);

            spatialIndex.Insert(note);

            // 验证初始位置可以查询到
            Assert.AreEqual(note, spatialIndex.PointQuery(115, 60));

            // Act
            ReflectionHelper.SetProperty(note, "Left", 200); // 移动音符位置
            ReflectionHelper.SetProperty(note, "Bottom", 100);

            // Assert
            // 旧位置应该查不到
            Assert.IsNull(spatialIndex.PointQuery(115, 60));
            // 新位置应该能查到
            Assert.AreEqual(note, spatialIndex.PointQuery(215, 110));
        }

        [TestMethod]
        public void PropertyChange_Size_ShouldUpdateIndex()
        {
            // Arrange
            var spatialIndex = new NoteSpatialGridHashMap(20.0);
            var note = CreateNote(100, 50, 30, 20);

            spatialIndex.Insert(note);

            // 获取初始属性验证
            var initialProps = GetNoteProperties(note);
            Assert.AreEqual(100, initialProps.left);
            Assert.AreEqual(50, initialProps.bottom);
            Assert.AreEqual(30, initialProps.width);
            Assert.AreEqual(20, initialProps.height);

            // 验证初始位置在音符内部
            Assert.AreEqual(note, spatialIndex.PointQuery(115, 60));
            // 验证音符外部
            Assert.IsNull(spatialIndex.PointQuery(140, 60));

            // Act
            ReflectionHelper.SetProperty(note, "Width", 50); // 增大宽度
            ReflectionHelper.SetProperty(note, "Height", 30); // 增大高度

            // 验证属性已更新
            var updatedProps = GetNoteProperties(note);
            Assert.AreEqual(50, updatedProps.width);
            Assert.AreEqual(30, updatedProps.height);

            // Assert
            // 之前音符外部的位置现在应该在内部
            Assert.AreEqual(note, spatialIndex.PointQuery(140, 60));
        }

        [TestMethod]
        [Timeout(5000, CooperativeCancellation = true)]
        public void Performance_LargeCollection_ShouldBeEfficient()
        {
            // Arrange
            var spatialIndex = new NoteSpatialGridHashMap(20.0);
            int noteCount = 10000;

            // 预创建所有音符
            var notes = new List<NoteEventViewModel>(noteCount);
            for (int i = 0; i < noteCount; i++)
            {
                var note = new NoteEventViewModel();
                ReflectionHelper.SetProperty(note, "Left", _random.Next(0, 10000));
                ReflectionHelper.SetProperty(note, "Bottom", _random.Next(0, 10000));
                ReflectionHelper.SetProperty(note, "Width", _random.Next(10, 100));
                ReflectionHelper.SetProperty(note, "Height", _random.Next(10, 100));
                notes.Add(note);
            }

            var stopwatch = Stopwatch.StartNew();

            // Act 1 - 批量插入
            spatialIndex.InsertRange(notes);
            stopwatch.Stop();
            long insertTime = stopwatch.ElapsedMilliseconds;

            Assert.AreEqual(noteCount, spatialIndex.Count);

            // Act 2 - 属性变更性能
            int propertyChangeCount = 1000;

            stopwatch.Restart();
            for (int i = 0; i < propertyChangeCount; i++)
            {
                var note = notes[_random.Next(0, noteCount)];
                ReflectionHelper.SetProperty(note, "Left", _random.Next(0, 10000));
                ReflectionHelper.SetProperty(note, "Bottom", _random.Next(0, 10000));
            }
            long propertyChangeTime = stopwatch.ElapsedMilliseconds;

            // 输出性能报告
            Debug.WriteLine($"====== NoteSpatialGridHashMap 性能测试报告 ======");
            Debug.WriteLine($"音符数量: {noteCount}");
            Debug.WriteLine($"批量插入耗时: {insertTime}ms");
            Debug.WriteLine($"属性变更次数: {propertyChangeCount}, 耗时: {propertyChangeTime}ms");
            Debug.WriteLine($"==================================================");

            // 清理
            spatialIndex.Clear();
        }

        [TestMethod]
        public void Query_ShouldReturnAllNotesInRectangle()
        {
            // Arrange
            var spatialIndex = new NoteSpatialGridHashMap(20.0);

            // 创建测试音符
            var notesInRange = new List<NoteEventViewModel>();
            var notesOutOfRange = new List<NoteEventViewModel>();

            // 在查询范围内的音符
            for (int i = 0; i < 5; i++)
            {
                var note = new NoteEventViewModel();
                ReflectionHelper.SetProperty(note, "Left", 200 + i * 20);
                ReflectionHelper.SetProperty(note, "Bottom", 200 + i * 20);
                ReflectionHelper.SetProperty(note, "Width", 25);
                ReflectionHelper.SetProperty(note, "Height", 15);

                spatialIndex.Insert(note);
                notesInRange.Add(note);
            }

            // 在查询范围外的音符
            for (int i = 0; i < 3; i++)
            {
                var note = new NoteEventViewModel();
                ReflectionHelper.SetProperty(note, "Left", 0 + i * 20);
                ReflectionHelper.SetProperty(note, "Bottom", 0 + i * 20);
                ReflectionHelper.SetProperty(note, "Width", 25);
                ReflectionHelper.SetProperty(note, "Height", 15);

                spatialIndex.Insert(note);
                notesOutOfRange.Add(note);
            }

            for (int i = 0; i < 3; i++)
            {
                var note = new NoteEventViewModel();
                ReflectionHelper.SetProperty(note, "Left", 500 + i * 20);
                ReflectionHelper.SetProperty(note, "Bottom", 500 + i * 20);
                ReflectionHelper.SetProperty(note, "Width", 25);
                ReflectionHelper.SetProperty(note, "Height", 15);

                spatialIndex.Insert(note);
                notesOutOfRange.Add(note);
            }

            // 查询矩形: 200-300 x 200-300
            double queryLeft = 200;
            double queryBottom = 200;
            double queryWidth = 100;
            double queryHeight = 100;

            // Act
            var results = spatialIndex.Query(queryLeft, queryBottom, queryWidth, queryHeight).ToList();

            // Assert
            Assert.AreEqual(notesInRange.Count, results.Count);
            foreach (var expectedNote in notesInRange)
            {
                Assert.Contains(expectedNote, results);
            }

            foreach (var outsideNote in notesOutOfRange)
            {
                Assert.DoesNotContain(outsideNote, results);
            }
        }
    }
}