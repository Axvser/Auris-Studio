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

        [TestMethod]
        public void FindFirstAtOrAfter_ShouldReturnFirstElementAtOrAfterTargetTick_RegardlessOfVirtualization()
        {
            // Arrange: 创建包含虚拟化和非虚拟化元素的集合
            var collection = new TimeOrderableCollection<NoteEventViewModel>();

            // 虚拟化元素
            var virtualizedItemAt100 = new NoteEventViewModel { AbsoluteTime = 100, DeltaTime = 50 };
            // 可见元素
            var expectedItemAt150 = new NoteEventViewModel { AbsoluteTime = 150, DeltaTime = 50 };
            var itemAt200 = new NoteEventViewModel { AbsoluteTime = 200, DeltaTime = 50 };
            // 虚拟化元素
            var virtualizedItemAt250 = new NoteEventViewModel { AbsoluteTime = 250, DeltaTime = 50 };
            // 可见元素
            var itemAt300 = new NoteEventViewModel { AbsoluteTime = 300, DeltaTime = 50 };

            collection.Add(virtualizedItemAt100);
            collection.Add(expectedItemAt150);
            collection.Add(itemAt200);
            collection.Add(virtualizedItemAt250);
            collection.Add(itemAt300);

            // 设置虚拟化范围：120-280，使100和250的元素虚拟化
            collection.Virtualize(120, 280);

            // 测试场景1: tick在虚拟化元素之后，第一个可见元素之前
            var result1 = collection.FindFirstAtOrAfter(120);
            Assert.IsNotNull(result1);
            Assert.AreEqual(expectedItemAt150, result1, "应返回开始时间>=120的第一个元素，无论是否虚拟化");

            // 测试场景2: tick等于虚拟化元素的开始时间
            var result2 = collection.FindFirstAtOrAfter(100);
            Assert.IsNotNull(result2);
            Assert.AreEqual(virtualizedItemAt100, result2, "应返回虚拟化元素本身，因为方法不考虑虚拟化状态");

            // 测试场景3: tick在两个虚拟化元素之间
            var result3 = collection.FindFirstAtOrAfter(220);
            Assert.IsNotNull(result3);
            Assert.AreEqual(virtualizedItemAt250, result3, "应返回开始时间>=220的第一个元素，即使它是虚拟化的");

            // 测试场景4: tick在虚拟化范围之外
            var result4 = collection.FindFirstAtOrAfter(280);
            Assert.IsNotNull(result4);
            Assert.AreEqual(itemAt300, result4, "应返回开始时间>=280的第一个元素，不考虑虚拟化");

            // 测试场景5: tick在第一个虚拟化元素之前
            var result5 = collection.FindFirstAtOrAfter(50);
            Assert.IsNotNull(result5);
            Assert.AreEqual(virtualizedItemAt100, result5, "应返回开始时间>=50的第一个元素，无论是否虚拟化");
        }

        [TestMethod]
        public void FindFirstAtOrBefore_ShouldReturnFirstElementAtOrBeforeTargetTick_RegardlessOfVirtualization()
        {
            // Arrange
            var collection = new TimeOrderableCollection<NoteEventViewModel>();

            // 虚拟化元素
            var virtualizedItemAt100 = new NoteEventViewModel { AbsoluteTime = 100, DeltaTime = 50 };
            // 可见元素
            var expectedItemAt150 = new NoteEventViewModel { AbsoluteTime = 150, DeltaTime = 50 };
            var itemAt200 = new NoteEventViewModel { AbsoluteTime = 200, DeltaTime = 50 };
            // 虚拟化元素
            var virtualizedItemAt250 = new NoteEventViewModel { AbsoluteTime = 250, DeltaTime = 50 };
            // 可见元素
            var itemAt300 = new NoteEventViewModel { AbsoluteTime = 300, DeltaTime = 50 };

            collection.Add(virtualizedItemAt100);
            collection.Add(expectedItemAt150);
            collection.Add(itemAt200);
            collection.Add(virtualizedItemAt250);
            collection.Add(itemAt300);

            collection.Virtualize(120, 280);

            // 测试场景1: tick在虚拟化元素和可见元素之间
            var result1 = collection.FindFirstAtOrBefore(140);
            Assert.IsNotNull(result1);
            Assert.AreEqual(virtualizedItemAt100, result1, "应返回开始时间<=140的第一个元素，包括虚拟化元素");

            // 测试场景2: tick等于虚拟化元素的开始时间
            var result2 = collection.FindFirstAtOrBefore(100);
            Assert.IsNotNull(result2);
            Assert.AreEqual(virtualizedItemAt100, result2, "应返回虚拟化元素本身，因为方法不考虑虚拟化状态");

            // 测试场景3: tick在两个可见元素之间
            var result3 = collection.FindFirstAtOrBefore(180);
            Assert.IsNotNull(result3);
            Assert.AreEqual(expectedItemAt150, result3, "应返回开始时间<=180的第一个元素，无论是否虚拟化");

            // 测试场景4: tick在虚拟化范围之外
            var result4 = collection.FindFirstAtOrBefore(320);
            Assert.IsNotNull(result4);
            Assert.AreEqual(itemAt300, result4, "应返回开始时间<=320的第一个元素，不考虑虚拟化");

            // 测试场景5: tick小于第一个元素
            var result5 = collection.FindFirstAtOrBefore(50);
            Assert.IsNull(result5, "当tick小于所有元素的开始时间时，应返回null");
        }

        [TestMethod]
        public void FindFirstAtOrAfter_WithAllVirtualizedItems_ShouldReturnVirtualizedElements()
        {
            // Arrange: 所有元素都虚拟化
            var collection = new TimeOrderableCollection<NoteEventViewModel>();

            var virtualizedItemAt100 = new NoteEventViewModel { AbsoluteTime = 100, DeltaTime = 50 };
            var virtualizedItemAt200 = new NoteEventViewModel { AbsoluteTime = 200, DeltaTime = 50 };
            var virtualizedItemAt300 = new NoteEventViewModel { AbsoluteTime = 300, DeltaTime = 50 };

            collection.Add(virtualizedItemAt100);
            collection.Add(virtualizedItemAt200);
            collection.Add(virtualizedItemAt300);

            // 虚拟化范围不包含任何元素
            collection.Virtualize(400, 500);

            // Act & Assert: Find方法应该返回虚拟化元素
            var result1 = collection.FindFirstAtOrAfter(0);
            Assert.IsNotNull(result1);
            Assert.AreEqual(virtualizedItemAt100, result1, "即使所有元素都虚拟化，FindFirstAtOrAfter也应返回元素");

            var result2 = collection.FindFirstAtOrAfter(150);
            Assert.IsNotNull(result2);
            Assert.AreEqual(virtualizedItemAt200, result2, "应返回虚拟化元素，不考虑虚拟化状态");

            var result3 = collection.FindFirstAtOrAfter(400);
            Assert.IsNull(result3, "当tick超过所有元素的开始时间时，应返回null");
        }

        [TestMethod]
        public void FindFirstAtOrBefore_WithAllVirtualizedItems_ShouldReturnVirtualizedElements()
        {
            // Arrange
            var collection = new TimeOrderableCollection<NoteEventViewModel>();

            var virtualizedItemAt100 = new NoteEventViewModel { AbsoluteTime = 100, DeltaTime = 50 };
            var virtualizedItemAt200 = new NoteEventViewModel { AbsoluteTime = 200, DeltaTime = 50 };
            var virtualizedItemAt300 = new NoteEventViewModel { AbsoluteTime = 300, DeltaTime = 50 };

            collection.Add(virtualizedItemAt100);
            collection.Add(virtualizedItemAt200);
            collection.Add(virtualizedItemAt300);

            collection.Virtualize(400, 500);

            // Act & Assert
            var result1 = collection.FindFirstAtOrBefore(1000);
            Assert.IsNotNull(result1);
            Assert.AreEqual(virtualizedItemAt300, result1, "即使所有元素都虚拟化，FindFirstAtOrBefore也应返回元素");

            var result2 = collection.FindFirstAtOrBefore(250);
            Assert.IsNotNull(result2);
            Assert.AreEqual(virtualizedItemAt200, result2, "应返回虚拟化元素，不考虑虚拟化状态");

            var result3 = collection.FindFirstAtOrBefore(50);
            Assert.IsNull(result3, "当tick小于所有元素的开始时间时，应返回null");
        }

        [TestMethod]
        public void FindMethods_ShouldSupportEventsAtTickZero()
        {
            var collection = new TimeOrderableCollection<NoteEventViewModel>();
            var itemAtZero = new NoteEventViewModel { AbsoluteTime = 0, DeltaTime = 120 };
            var itemAtLater = new NoteEventViewModel { AbsoluteTime = 240, DeltaTime = 120 };

            collection.Add(itemAtZero);
            collection.Add(itemAtLater);

            Assert.AreSame(itemAtZero, collection.FindFirstAtOrAfter(0), "FindFirstAtOrAfter 应正确返回 0 tick 的元素");
            Assert.AreSame(itemAtZero, collection.FindFirstAtOrBefore(0), "FindFirstAtOrBefore 应正确返回 0 tick 的元素");
        }
    }
}
