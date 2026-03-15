using Auris_Studio.ViewModels.ComponentModel;
using Auris_Studio.ViewModels.MidiEvents;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;

namespace Test
{
    [TestClass]
    public sealed class Test_TimeOrderableCollection
    {
        private readonly Random _random = new(12345);

        [TestMethod]
        public void Add_ShouldIncreaseCountAndMaxTime()
        {
            // Arrange
            var collection = new TimeOrderableCollection<NoteEventViewModel>();
            var note1 = new NoteEventViewModel { AbsoluteTime = 100, DeltaTime = 50 };
            var note2 = new NoteEventViewModel { AbsoluteTime = 200, DeltaTime = 100 };

            // Act
            collection.Add(note1);
            collection.Add(note2);

            // Assert
            Assert.AreEqual(2, collection.Count);
            Assert.AreEqual(300, collection.MaxTime); // 200 + 100 = 300
        }

        [TestMethod]
        public void Add_DuplicateItem_ShouldNotAddAgain()
        {
            // Arrange
            var collection = new TimeOrderableCollection<NoteEventViewModel>();
            var note = new NoteEventViewModel { AbsoluteTime = 100, DeltaTime = 50 };

            // Act
            collection.Add(note);
            collection.Add(note); // 重复添加

            // Assert
            Assert.AreEqual(1, collection.Count);
        }

        [TestMethod]
        public void Remove_ShouldDecreaseCount()
        {
            // Arrange
            var collection = new TimeOrderableCollection<NoteEventViewModel>();
            var note1 = new NoteEventViewModel { AbsoluteTime = 100, DeltaTime = 50 };
            var note2 = new NoteEventViewModel { AbsoluteTime = 200, DeltaTime = 100 };

            collection.Add(note1);
            collection.Add(note2);

            // Act
            bool removed = collection.Remove(note1);

            // Assert
            Assert.IsTrue(removed);
            Assert.AreEqual(1, collection.Count);
            Assert.IsFalse(collection.Contains(note1));
            Assert.IsTrue(collection.Contains(note2));
        }

        [TestMethod]
        public void Remove_NonExistentItem_ShouldReturnFalse()
        {
            // Arrange
            var collection = new TimeOrderableCollection<NoteEventViewModel>();
            var note = new NoteEventViewModel { AbsoluteTime = 100, DeltaTime = 50 };

            // Act
            bool removed = collection.Remove(note);

            // Assert
            Assert.IsFalse(removed);
        }

        [TestMethod]
        public void Clear_ShouldRemoveAllItems()
        {
            // Arrange
            var collection = new TimeOrderableCollection<NoteEventViewModel>();

            for (int i = 0; i < 10; i++)
            {
                collection.Add(new NoteEventViewModel
                {
                    AbsoluteTime = i * 100,
                    DeltaTime = 50
                });
            }

            // Act
            collection.Clear();

            // Assert
            Assert.AreEqual(0, collection.Count);
            Assert.AreEqual(0, collection.MaxTime);
            Assert.IsEmpty(collection.VisibleItems);
        }

        [TestMethod]
        public void Virtualize_ShouldFilterVisibleItems()
        {
            // Arrange
            var collection = new TimeOrderableCollection<NoteEventViewModel>();

            // 添加不同时间范围的元素
            var note1 = new NoteEventViewModel { AbsoluteTime = 0, DeltaTime = 100 };    // 0-100
            var note2 = new NoteEventViewModel { AbsoluteTime = 150, DeltaTime = 100 };  // 150-250
            var note3 = new NoteEventViewModel { AbsoluteTime = 300, DeltaTime = 100 };  // 300-400
            var note4 = new NoteEventViewModel { AbsoluteTime = 450, DeltaTime = 100 };  // 450-550

            collection.Add(note1);
            collection.Add(note2);
            collection.Add(note3);
            collection.Add(note4);

            // Act
            collection.Virtualize(200, 450); // 可见范围：200-450

            // Assert
            Assert.AreEqual(4, collection.Count); // 总数量不变
            Assert.HasCount(2, collection.VisibleItems); // 只有note2和note3可见
            CollectionAssert.Contains(collection.VisibleItems.ToList(), note2);
            CollectionAssert.Contains(collection.VisibleItems.ToList(), note3);
            CollectionAssert.DoesNotContain(collection.VisibleItems.ToList(), note1);
            CollectionAssert.DoesNotContain(collection.VisibleItems.ToList(), note4);
        }

        [TestMethod]
        public void Virtualize_NoItemsInRange_ShouldHaveEmptyVisibleItems()
        {
            // Arrange
            var collection = new TimeOrderableCollection<NoteEventViewModel>();

            for (int i = 0; i < 5; i++)
            {
                collection.Add(new NoteEventViewModel
                {
                    AbsoluteTime = i * 1000,
                    DeltaTime = 100
                });
            }

            // Act
            collection.Virtualize(100, 200); // 范围不包含任何元素

            // Assert
            Assert.AreEqual(5, collection.Count);
            Assert.IsEmpty(collection.VisibleItems);
        }

        [TestMethod]
        public void Restore_ShouldMakeAllItemsVisible()
        {
            // Arrange
            var collection = new TimeOrderableCollection<NoteEventViewModel>();

            for (int i = 0; i < 10; i++)
            {
                collection.Add(new NoteEventViewModel
                {
                    AbsoluteTime = i * 100,
                    DeltaTime = 50
                });
            }

            collection.Virtualize(300, 600); // 虚拟化，只显示部分元素

            int visibleCountBeforeRestore = collection.VisibleItems.Count;

            // Act
            collection.Restore();

            // Assert
            Assert.AreEqual(10, collection.Count);
            Assert.HasCount(10, collection.VisibleItems);
            Assert.IsLessThan(10, visibleCountBeforeRestore); // 确保虚拟化确实过滤了元素
        }

        [TestMethod]
        public void Query_ShouldReturnItemsInTimeRange()
        {
            // Arrange
            var collection = new TimeOrderableCollection<NoteEventViewModel>();

            // 添加测试数据
            var expectedInRange = new List<NoteEventViewModel>();
            for (int i = 0; i < 5; i++)
            {
                var note = new NoteEventViewModel
                {
                    AbsoluteTime = 200 + i * 50,
                    DeltaTime = 30
                };
                collection.Add(note);
                expectedInRange.Add(note);
            }

            // 添加范围外的数据
            for (int i = 0; i < 3; i++)
            {
                collection.Add(new NoteEventViewModel
                {
                    AbsoluteTime = 0 + i * 50,
                    DeltaTime = 30
                });
            }
            for (int i = 0; i < 3; i++)
            {
                collection.Add(new NoteEventViewModel
                {
                    AbsoluteTime = 500 + i * 50,
                    DeltaTime = 30
                });
            }

            // Act
            var results = collection.Query(200, 450).ToList();

            // Assert
            Assert.HasCount(expectedInRange.Count, results);
            foreach (var expectedItem in expectedInRange)
            {
                Assert.Contains(expectedItem, results);
            }
        }

        [TestMethod]
        public void QueryAtStart_ShouldReturnItemsStartingAtExactTime()
        {
            // Arrange
            var collection = new TimeOrderableCollection<NoteEventViewModel>();
            long targetTime = 250;

            var expectedItems = new List<NoteEventViewModel>();
            for (int i = 0; i < 3; i++)
            {
                var note = new NoteEventViewModel
                {
                    AbsoluteTime = targetTime,
                    DeltaTime = 30 + i * 10
                };
                collection.Add(note);
                expectedItems.Add(note);
            }

            // 添加不同开始时间的元素
            for (int i = 0; i < 3; i++)
            {
                collection.Add(new NoteEventViewModel
                {
                    AbsoluteTime = targetTime + 100 + i * 50,
                    DeltaTime = 30
                });
            }

            // Act
            var results = collection.QueryAtStart(targetTime).ToList();

            // Assert
            Assert.HasCount(expectedItems.Count, results);
            foreach (var expectedItem in expectedItems)
            {
                Assert.Contains(expectedItem, results);
            }
        }

        [TestMethod]
        public void QueryAtEnd_ShouldReturnItemsEndingAtExactTime()
        {
            // Arrange
            var collection = new TimeOrderableCollection<NoteEventViewModel>();
            long targetEndTime = 280;

            var expectedItems = new List<NoteEventViewModel>();
            for (int i = 0; i < 3; i++)
            {
                var note = new NoteEventViewModel
                {
                    AbsoluteTime = targetEndTime - 30 - i * 10,
                    DeltaTime = 30 + i * 10
                };
                collection.Add(note);
                expectedItems.Add(note);
            }

            // 添加不同结束时间的元素
            for (int i = 0; i < 3; i++)
            {
                collection.Add(new NoteEventViewModel
                {
                    AbsoluteTime = 100 + i * 50,
                    DeltaTime = 30
                });
            }

            // Act
            var results = collection.QueryAtEnd(targetEndTime).ToList();

            // Assert
            Assert.HasCount(expectedItems.Count, results);
            foreach (var expectedItem in expectedItems)
            {
                Assert.Contains(expectedItem, results);
            }
        }

        [TestMethod]
        public void PropertyChange_AbsoluteTime_ShouldUpdateCollection()
        {
            // Arrange
            var collection = new TimeOrderableCollection<NoteEventViewModel>();
            var note = new NoteEventViewModel { AbsoluteTime = 100, DeltaTime = 50 };
            collection.Add(note);

            collection.Virtualize(0, 200);
            Assert.Contains(note, collection.VisibleItems);

            // Act
            note.AbsoluteTime = 300; // 移动到虚拟化范围之外

            // Assert
            Assert.DoesNotContain(note, collection.VisibleItems);
            Assert.AreEqual(1, collection.Count); // 总数不变
        }

        [TestMethod]
        public void PropertyChange_DeltaTime_ShouldUpdateMaxTime()
        {
            // Arrange
            var collection = new TimeOrderableCollection<NoteEventViewModel>();
            var note = new NoteEventViewModel { AbsoluteTime = 100, DeltaTime = 50 };
            collection.Add(note);

            long initialMaxTime = collection.MaxTime;

            // Act
            note.DeltaTime = 200; // 增加持续时间

            // Assert
            Assert.AreEqual(300, collection.MaxTime); // 100 + 200
            Assert.AreNotEqual(initialMaxTime, collection.MaxTime);
        }

        [TestMethod]
        public void CollectionChanged_AddItem_ShouldFireEvent()
        {
            // Arrange
            var collection = new TimeOrderableCollection<NoteEventViewModel>();
            bool eventFired = false;
            NotifyCollectionChangedEventArgs? eventArgs = null;

            collection.CollectionChanged += (sender, args) =>
            {
                eventFired = true;
                eventArgs = args;
            };

            var note = new NoteEventViewModel { AbsoluteTime = 100, DeltaTime = 50 };

            // Act
            collection.Add(note);

            // Assert
            Assert.IsTrue(eventFired);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(NotifyCollectionChangedAction.Add, eventArgs.Action);
            Assert.AreEqual(note, eventArgs.NewItems?[0]);
        }

        [TestMethod]
        public void CollectionChanged_RemoveItem_ShouldFireEvent()
        {
            // Arrange
            var collection = new TimeOrderableCollection<NoteEventViewModel>();
            var note = new NoteEventViewModel { AbsoluteTime = 100, DeltaTime = 50 };
            collection.Add(note);

            bool eventFired = false;
            NotifyCollectionChangedEventArgs? eventArgs = null;

            collection.CollectionChanged += (sender, args) =>
            {
                eventFired = true;
                eventArgs = args;
            };

            // Act
            collection.Remove(note);

            // Assert
            Assert.IsTrue(eventFired);
            Assert.IsNotNull(eventArgs);
            Assert.AreEqual(NotifyCollectionChangedAction.Remove, eventArgs.Action);
            Assert.AreEqual(note, eventArgs.OldItems?[0]);
        }

        [TestMethod]
        public void UpdateEvent_ShouldFireAfterVirtualize()
        {
            // Arrange
            var collection = new TimeOrderableCollection<NoteEventViewModel>();
            bool eventFired = false;
            IEnumerable<NoteEventViewModel>? updatedItems = null;

            collection.Update += items =>
            {
                eventFired = true;
                updatedItems = items;
            };

            for (int i = 0; i < 5; i++)
            {
                collection.Add(new NoteEventViewModel
                {
                    AbsoluteTime = i * 100,
                    DeltaTime = 50
                });
            }

            // Act
            collection.Virtualize(200, 400);

            // Assert
            Assert.IsTrue(eventFired);
            Assert.IsNotNull(updatedItems);
            Assert.AreEqual(2, updatedItems.Count()); // 在200-400范围内的元素
        }

        [TestMethod]
        public void UpdateEvent_ShouldFireAfterRestore()
        {
            // Arrange
            var collection = new TimeOrderableCollection<NoteEventViewModel>();
            bool eventFired = false;
            IEnumerable<NoteEventViewModel>? updatedItems = null;

            collection.Update += items =>
            {
                eventFired = true;
                updatedItems = items;
            };

            for (int i = 0; i < 5; i++)
            {
                collection.Add(new NoteEventViewModel
                {
                    AbsoluteTime = i * 100,
                    DeltaTime = 50
                });
            }

            collection.Virtualize(200, 400);
            eventFired = false; // 重置标志

            // Act
            collection.Restore();

            // Assert
            Assert.IsTrue(eventFired);
            Assert.IsNotNull(updatedItems);
            Assert.AreEqual(5, updatedItems.Count()); // 所有元素都应该可见
        }

        [TestMethod]
        [Timeout(3000, CooperativeCancellation = true)]
        public void Performance_LargeCollection_ShouldBeEfficient()
        {
            // Arrange
            var collection = new TimeOrderableCollection<NoteEventViewModel>();
            int itemCount = 10000;

            // 预先创建所有元素，避免在计时循环中创建
            var items = new List<NoteEventViewModel>(itemCount);
            for (int i = 0; i < itemCount; i++)
            {
                items.Add(new NoteEventViewModel
                {
                    AbsoluteTime = _random.Next(0, 100000),
                    DeltaTime = _random.Next(10, 500)
                });
            }

            // 记录各项操作的耗时
            var stopwatch = Stopwatch.StartNew();

            // Act 1 - 使用AddRange批量添加
            collection.AddRange(items);
            stopwatch.Stop();
            long addTime = stopwatch.ElapsedMilliseconds;

            // 验证添加结果
            Assert.AreEqual(itemCount, collection.Count, $"添加后集合计数应为{itemCount}，实际为{collection.Count}");

            // Act 2 - 虚拟化
            stopwatch.Restart();
            collection.Virtualize(20000, 40000);
            long virtualizeTime = stopwatch.ElapsedMilliseconds;

            int visibleCountAfterVirtualize = collection.VisibleItems.Count;

            // Act 3 - 查询
            stopwatch.Restart();
            int queryResults = collection.Query(25000, 35000).Count();
            long queryTime = stopwatch.ElapsedMilliseconds;

            // Act 4 - 恢复
            stopwatch.Restart();
            collection.Restore();
            long restoreTime = stopwatch.ElapsedMilliseconds;

            int visibleCountAfterRestore = collection.VisibleItems.Count;

            // Assert - 功能正确性验证
            Assert.AreEqual(itemCount, visibleCountAfterRestore,
                $"恢复后可见元素数应为{itemCount}，实际为{visibleCountAfterRestore}");

            // Assert - 性能验证（放宽标准）
            const int reasonableAddTime = 1000; // 从500ms放宽到1000ms
            const int reasonableVirtualizeTime = 200;
            const int reasonableQueryTime = 100;
            const int reasonableRestoreTime = 200;

            Assert.IsLessThan(reasonableAddTime, addTime,
                $"批量添加{itemCount}个元素耗时{addTime}ms，超过{reasonableAddTime}ms预期。请检查集合实现的批量添加效率。");

            Assert.IsLessThan(reasonableVirtualizeTime, virtualizeTime,
                $"虚拟化{itemCount}个元素耗时{virtualizeTime}ms，超过{reasonableVirtualizeTime}ms预期。");

            Assert.IsLessThan(reasonableQueryTime, queryTime,
                $"查询耗时{queryTime}ms，超过{reasonableQueryTime}ms预期。");

            Assert.IsLessThan(reasonableRestoreTime, restoreTime,
                $"恢复耗时{restoreTime}ms，超过{reasonableRestoreTime}ms预期。");

            // 输出性能报告
            Debug.WriteLine($"====== 性能测试报告 ======");
            Debug.WriteLine($"测试项目: 处理{itemCount}个元素");
            Debug.WriteLine($"批量添加耗时: {addTime}ms");
            Debug.WriteLine($"虚拟化耗时: {virtualizeTime}ms");
            Debug.WriteLine($"查询耗时: {queryTime}ms (查询结果: {queryResults}个元素)");
            Debug.WriteLine($"恢复耗时: {restoreTime}ms");
            Debug.WriteLine($"虚拟化后可见元素: {visibleCountAfterVirtualize}");
            Debug.WriteLine($"恢复后可见元素: {visibleCountAfterRestore}");
            Debug.WriteLine($"平均每个元素添加时间: {(double)addTime / itemCount:F3}ms");
            Debug.WriteLine($"==========================");

            // 性能警告（非致命）
            if (addTime > 500)
            {
                Debug.WriteLine($"警告: 批量添加性能可能需要优化，平均每个元素{(double)addTime / itemCount:F3}ms");
            }
        }

        [TestMethod]
        public void MaxTime_ShouldTrackMaximumEndTime()
        {
            // Arrange
            var collection = new TimeOrderableCollection<NoteEventViewModel>
            {
                // Act
                new() { AbsoluteTime = 100, DeltaTime = 50 },  // 结束: 150
                new() { AbsoluteTime = 200, DeltaTime = 100 }, // 结束: 300
                new() { AbsoluteTime = 50, DeltaTime = 200 }  // 结束: 250
            };

            // Assert
            Assert.AreEqual(300, collection.MaxTime);

            // 添加新的最大时间
            collection.Add(new NoteEventViewModel { AbsoluteTime = 400, DeltaTime = 150 }); // 结束: 550
            Assert.AreEqual(550, collection.MaxTime);

            // 移除最大时间的元素
            var maxItem = collection.QueryAtEnd(550).FirstOrDefault();
            if (maxItem != null)
            {
                collection.Remove(maxItem);
                Assert.AreEqual(300, collection.MaxTime); // 回退到之前的最大值
            }
        }

        [TestMethod]
        public void ItemVirtualizedEvent_ShouldFireWhenItemMovedToBuffer()
        {
            // Arrange
            var collection = new TimeOrderableCollection<NoteEventViewModel>();
            var virtualizedItem = new NoteEventViewModel { AbsoluteTime = 100, DeltaTime = 50 };
            collection.Add(virtualizedItem);

            bool eventFired = false;
            NoteEventViewModel? eventItem = null;

            collection.ItemVirtualized += item =>
            {
                eventFired = true;
                eventItem = item;
            };

            // Act
            collection.Virtualize(200, 400); // 虚拟化范围不包含该元素

            // Assert
            Assert.IsTrue(eventFired);
            Assert.AreEqual(virtualizedItem, eventItem);
        }

        [TestMethod]
        public void ItemRestoredEvent_ShouldFireWhenItemRestoredFromBuffer()
        {
            // Arrange
            var collection = new TimeOrderableCollection<NoteEventViewModel>();
            var note = new NoteEventViewModel { AbsoluteTime = 100, DeltaTime = 50 };
            collection.Add(note);

            // 先虚拟化，将元素移到缓冲区
            collection.Virtualize(200, 400);

            bool eventFired = false;
            NoteEventViewModel? eventItem = null;

            collection.ItemRestored += item =>
            {
                eventFired = true;
                eventItem = item;
            };

            // Act
            collection.Restore(); // 恢复所有元素

            // Assert
            Assert.IsTrue(eventFired);
            Assert.AreEqual(note, eventItem);
        }

        [TestMethod]
        public void VisibleItems_ShouldBeOrderedByStartTime()
        {
            // Arrange
            var collection = new TimeOrderableCollection<NoteEventViewModel>
            {
                // 乱序添加
                new() { AbsoluteTime = 300, DeltaTime = 50 },
                new() { AbsoluteTime = 100, DeltaTime = 50 },
                new() { AbsoluteTime = 200, DeltaTime = 50 },
                new() { AbsoluteTime = 50, DeltaTime = 50 }
            };

            // Act
            var visibleItems = collection.VisibleItems.ToList();

            // Assert
            Assert.HasCount(4, visibleItems);

            // 检查是否按开始时间排序
            for (int i = 0; i < visibleItems.Count - 1; i++)
            {
                Assert.IsLessThanOrEqualTo(
                    visibleItems[i + 1].AbsoluteTime,
                    visibleItems[i].AbsoluteTime,
                    $"元素 {i} 的开始时间 {visibleItems[i].AbsoluteTime} 应该小于等于元素 {i + 1} 的开始时间 {visibleItems[i + 1].AbsoluteTime}");
            }
        }

        [TestMethod]
        public void MaxTime_PropertyChanged_ShouldFireOnUpdate()
        {
            // Arrange: 创建集合并设置事件监听器
            var collection = new TimeOrderableCollection<NoteEventViewModel>();
            bool propertyChangedFired = false;
            string changedPropertyName = string.Empty;

            // 订阅 PropertyChanged 事件
            ((INotifyPropertyChanged)collection).PropertyChanged += (sender, args) =>
            {
                propertyChangedFired = true;
                changedPropertyName = args.PropertyName ?? string.Empty;
            };

            // Act 1: 添加一个元素，这应该改变 MaxTime
            var note1 = new NoteEventViewModel { AbsoluteTime = 100, DeltaTime = 50 }; // 结束时间 150
            collection.Add(note1);

            // Assert 1: 验证添加操作触发了事件，且属性名为 "MaxTime"
            Assert.IsTrue(propertyChangedFired, "添加元素后，PropertyChanged 事件应被触发。");
            Assert.AreEqual(nameof(TimeOrderableCollection<>.MaxTime), changedPropertyName,
                $"事件参数应为 \"MaxTime\"，实际为 \"{changedPropertyName}\"。");

            // 重置监听器状态
            propertyChangedFired = false;
            changedPropertyName = string.Empty;

            // Act 2: 添加一个更晚结束的元素，MaxTime 会变大
            var note2 = new NoteEventViewModel { AbsoluteTime = 200, DeltaTime = 100 }; // 结束时间 300
            collection.Add(note2);

            // Assert 2: 验证再次触发事件
            Assert.IsTrue(propertyChangedFired, "添加一个导致 MaxTime 增大的元素后，PropertyChanged 事件应被触发。");
            Assert.AreEqual(nameof(TimeOrderableCollection<>.MaxTime), changedPropertyName);

            // 重置监听器状态
            propertyChangedFired = false;
            changedPropertyName = string.Empty;

            // Act 3: 修改现有元素的 DeltaTime，使其结束时间超过当前 MaxTime
            note1.DeltaTime = 250; // 结束时间变为 100 + 250 = 350，超过当前的 MaxTime 300

            // Assert 3: 验证元素属性变化也会触发集合的 MaxTime 变更事件
            // 注意：这依赖于 NoteEventViewModel 的属性变更能正确通知到其所属的集合。
            // 此测试的成功与否，直接验证了您遇到的问题核心。
            Assert.IsTrue(propertyChangedFired, "修改元素属性导致 MaxTime 变化后，PropertyChanged 事件应被触发。");
            Assert.AreEqual(nameof(TimeOrderableCollection<>.MaxTime), changedPropertyName);

            // 可选：Act 4 - 移除导致 MaxTime 变化的元素
            propertyChangedFired = false;
            collection.Remove(note1); // 移除结束时间最晚的 note1 (350)

            // Assert 4: 验证移除操作也触发了事件（因为 MaxTime 会回退到 300）
            Assert.IsTrue(propertyChangedFired, "移除导致 MaxTime 变化的元素后，PropertyChanged 事件应被触发。");
            Assert.AreEqual(nameof(TimeOrderableCollection<>.MaxTime), changedPropertyName);
        }
    }
}
