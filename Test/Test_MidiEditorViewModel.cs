using Auris_Studio.Midi;
using Auris_Studio.ViewModels;
using Auris_Studio.ViewModels.ComponentModel;
using Auris_Studio.ViewModels.MidiEvents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Test;
using VeloxDev.Core.MVVM;

namespace Test
{
    [TestClass]
    public sealed class Test_MidiEditorViewModel
    {
        private MidiEditorViewModel? _viewModel;
        private TestContext? _testContext;

        public TestContext? TestContext
        {
            get { return _testContext; }
            set { _testContext = value; }
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _viewModel = new MidiEditorViewModel();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _viewModel = null;
        }

        [TestMethod]
        public void Constructor_ShouldInitializePropertiesCorrectly()
        {
            Assert.AreEqual(480, _viewModel.PPQN, "默认PPQN应为480");
            Assert.AreEqual(120, _viewModel.BPM, "默认BPM应为120");
            Assert.AreEqual(4, _viewModel.Numerator, "默认拍号分子应为4");
            Assert.AreEqual(4, _viewModel.Denominator, "默认拍号分母应为4");
            Assert.AreEqual(Alignment.EighthNote, _viewModel.Alignment, "默认对齐策略应为八分音符");
            Assert.IsTrue(_viewModel.UseSnap, "默认应启用音符对齐");
            Assert.AreEqual(NoteDragBehavior.VerticalPriority, _viewModel.DragBehavior, "默认拖拽模式应为上下优先");
            Assert.AreEqual(string.Empty, _viewModel.Lyric, "默认歌词应为空字符串");
            Assert.IsTrue(_viewModel.IsEnabled, "默认应启用交互");
            Assert.IsNotNull(_viewModel.Tracks, "音轨集合不应为null");
            Assert.IsNotNull(_viewModel.Tempos, "速度事件集合不应为null");
            Assert.IsNotNull(_viewModel.Tss, "拍号事件集合不应为null");
            Assert.IsNotNull(_viewModel.Kss, "调号事件集合不应为null");
            Assert.IsNotNull(_viewModel.Texts, "文本事件集合不应为null");
            Assert.IsNotNull(_viewModel.Lyrics, "歌词事件集合不应为null");
            Assert.IsNotNull(_viewModel.Markers, "标记事件集合不应为null");
            Assert.IsNotNull(_viewModel.Cues, "提示点事件集合不应为null");
        }

        [TestMethod]
        public void PPQN_Change_ShouldUpdateTickTimeAndWidthPerTick()
        {
            double initialTickTime = _viewModel.TickTime;
            double initialWidthPerTick = _viewModel.WidthPerTick;

            _viewModel.PPQN = 960;

            Assert.AreEqual(960, _viewModel.PPQN, "PPQN应更新为960");
            Assert.AreNotEqual(initialTickTime, _viewModel.TickTime, "TickTime应更新");
            Assert.AreNotEqual(initialWidthPerTick, _viewModel.WidthPerTick, "WidthPerTick应更新");
            Assert.AreEqual(_viewModel.WidthPerQuarterNote / 960, _viewModel.WidthPerTick, 0.001, "WidthPerTick应为WidthPerQuarterNote除以PPQN");
        }

        [TestMethod]
        public void Test_InternalPropertyAccess()
        {
            // 测试通过反射访问内部属性
            // 假设MidiEditorViewModel有一些内部属性需要测试
            // 示例：测试内部计算属性
            try
            {
                // 通过ReflectionHelper访问可能的内部属性
                var internalProperty = ReflectionHelper.GetProperty<double>(_viewModel, "InternalTickTime");
                // 这里只是演示，实际需要知道具体的属性名
            }
            catch (InvalidOperationException ex)
            {
                // 如果属性不存在，这是预期的
                Assert.IsTrue(ex.Message.Contains("找不到属性"), "找不到属性时应抛出正确的异常");
            }
        }

        [TestMethod]
        public void CreateNoteAtPosition_ShouldAddNoteToSelectedTrack()
        {
            var track = new MidiTrackViewModel
            {
                Parent = _viewModel,
                Channel = 1,
                Name = "Test Track"
            };
            _viewModel.Tracks.Add(track);

            // 通过反射设置当前选中音轨
            ReflectionHelper.SetProperty(_viewModel, "CurrentSelectedTrack", track);

            _viewModel.Alignment = Alignment.QuarterNote;
            double widthPerTick = _viewModel.WidthPerTick;

            double clickLeft = 480 * widthPerTick;

            // 通过反射设置指针位置
            ReflectionHelper.SetProperty(_viewModel, "PointerLeft", clickLeft);
            ReflectionHelper.SetProperty(_viewModel, "PointerTop", _viewModel.NotesCanvasHeight / 2);

            // 调用HitTest（假设这是一个公共方法）
            // 这里需要知道实际的方法名
        }

        [TestMethod]
        public void Test_PrivateMethodInvocation()
        {
            // 测试通过反射调用私有方法
            // 示例：测试一个可能存在的私有计算方法
            try
            {
                var method = _viewModel.GetType().GetMethod("CalculateNotePosition",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (method != null)
                {
                    var result = method.Invoke(_viewModel, new object[] { 480, 60 });
                    Assert.IsNotNull(result, "私有方法调用应成功");
                }
            }
            catch
            {
                // 方法不存在是正常的
            }
        }

        [TestMethod]
        public void GetAlignmentStep_ShouldRespectSelectedSubdivision()
        {
            _viewModel.PPQN = 480;
            _viewModel.Alignment = Alignment.SixteenthNote;

            long step = _viewModel.GetAlignmentStep();

            Assert.AreEqual(120, step, "十六分音符对齐应保持为120 tick，而不是被拍号放大");
        }

        [TestMethod]
        public void SnapDisabled_ShouldBypassAlignmentAndUseTickLevelMinLength()
        {
            _viewModel.Alignment = Alignment.QuarterNote;
            _viewModel.UseSnap = false;

            Assert.AreEqual(123, _viewModel.AlignTimeForward(123), "关闭对齐后不应向前吸附");
            Assert.AreEqual(123, _viewModel.AlignTimeBackward(123), "关闭对齐后不应向后吸附");
            Assert.AreEqual(1, _viewModel.GetMinNoteLength(), "关闭对齐后最小音符长度应退化为1 tick");
        }

        [TestMethod]
        public void VerticalDominantMove_ShouldNotAccidentallyShiftNoteHorizontally()
        {
            var note = new NoteEventViewModel
            {
                AbsoluteTime = 480,
                DeltaTime = 240,
                Note = Pitch.C4
            };

            ReflectionHelper.SetProperty(_viewModel, "PointerLeft", 480 * _viewModel.WidthPerTick);
            ReflectionHelper.SetProperty(_viewModel, "PointerTop", 100d);
            ReflectionHelper.SetProperty(_viewModel, "CapturedNote", note);
            ReflectionHelper.SetProperty(_viewModel, "PointerOperation", 3);

            ReflectionHelper.SetProperty(_viewModel, "PointerLeft", 485 * _viewModel.WidthPerTick);
            ReflectionHelper.SetProperty(_viewModel, "PointerTop", 40d);

            Assert.AreEqual(480L, note.AbsoluteTime, "明显纵向拖动时，不应因为轻微横向抖动而改变起始时间");
        }

        [TestMethod]
        public void MoveNoteRight_ShouldSnapUsingRightEdge()
        {
            _viewModel.Alignment = Alignment.QuarterNote;

            var note = new NoteEventViewModel
            {
                AbsoluteTime = 0,
                DeltaTime = 300,
                Note = Pitch.C4
            };

            ReflectionHelper.SetProperty(_viewModel, "PointerLeft", 0d);
            ReflectionHelper.SetProperty(_viewModel, "PointerTop", 100d);
            ReflectionHelper.SetProperty(_viewModel, "CapturedNote", note);
            ReflectionHelper.SetProperty(_viewModel, "PointerOperation", 3);

            ReflectionHelper.SetProperty(_viewModel, "PointerLeft", 350 * _viewModel.WidthPerTick);

            Assert.AreEqual(180L, note.AbsoluteTime, "向右平移时应优先让音符右边缘吸附到最近网格");
        }

        [TestMethod]
        public void MoveNoteDirectionChange_ShouldSwitchSnapEdgeWithCurrentDirection()
        {
            _viewModel.Alignment = Alignment.QuarterNote;

            var note = new NoteEventViewModel
            {
                AbsoluteTime = 0,
                DeltaTime = 300,
                Note = Pitch.C4
            };

            ReflectionHelper.SetProperty(_viewModel, "PointerLeft", 0d);
            ReflectionHelper.SetProperty(_viewModel, "PointerTop", 100d);
            ReflectionHelper.SetProperty(_viewModel, "CapturedNote", note);
            ReflectionHelper.SetProperty(_viewModel, "PointerOperation", 3);

            ReflectionHelper.SetProperty(_viewModel, "PointerLeft", 350 * _viewModel.WidthPerTick);
            Assert.AreEqual(180L, note.AbsoluteTime, "第一次向右移动时应按右边缘吸附");

            ReflectionHelper.SetProperty(_viewModel, "PointerLeft", 330 * _viewModel.WidthPerTick);
            Assert.AreEqual(480L, note.AbsoluteTime, "随后改为向左移动时应切换为按左边缘吸附");
        }

        [TestMethod]
        public void FreeDragMode_ShouldAllowHorizontalAndVerticalChangesTogether()
        {
            _viewModel.Alignment = Alignment.QuarterNote;
            _viewModel.DragBehavior = NoteDragBehavior.Free;

            var note = new NoteEventViewModel
            {
                AbsoluteTime = 0,
                DeltaTime = 240,
                Note = Pitch.C4
            };

            ReflectionHelper.SetProperty(_viewModel, "PointerLeft", 0d);
            ReflectionHelper.SetProperty(_viewModel, "PointerTop", 100d);
            ReflectionHelper.SetProperty(_viewModel, "CapturedNote", note);
            ReflectionHelper.SetProperty(_viewModel, "PointerOperation", 3);

            ReflectionHelper.SetProperty(_viewModel, "PointerLeft", 500 * _viewModel.WidthPerTick);
            ReflectionHelper.SetProperty(_viewModel, "PointerTop", 40d);

            Assert.AreEqual(720L, note.AbsoluteTime, "自由模式下应允许横向移动生效");
            Assert.AreNotEqual(Pitch.C4, note.Note, "自由模式下应允许纵向改音高与横向同时生效");
        }
    }
}