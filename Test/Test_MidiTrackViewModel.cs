using Auris_Studio.Midi;
using Auris_Studio.ViewModels;
using Auris_Studio.ViewModels.MidiEvents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NAudio.Midi;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Test;

[TestClass]
[DoNotParallelize]
public sealed class Test_MidiTrackViewModel
{
    [TestMethod]
    public void Volume_Set_WhenMissing_ShouldCreateControlEventInCtrls()
    {
        var track = new MidiTrackViewModel
        {
            Volume = 96
        };

        Assert.AreEqual(96, track.Volume, "音量属性应返回新设置的值");
        Assert.AreEqual(1, track.Ctrls.Count, "缺失音量事件时应通过Ctrls创建新的控制器事件");
        Assert.AreEqual(1, track.Volumes.Count, "新增控制器事件后应同步进入Volumes集合");
        var controlEvent = track.Ctrls.Single();
        Assert.AreEqual(MidiController.MainVolume, controlEvent.MidiController, "新建事件应为主音量控制器");
        Assert.AreEqual(0L, controlEvent.AbsoluteTime, "新建的音量事件应落在起始tick");
    }

    [TestMethod]
    public void Pan_Set_WhenExisting_ShouldUpdateFirstPanEventInsteadOfAddingAnother()
    {
        var track = new MidiTrackViewModel();
        var panEvent = new ControlChangeEventViewModel
        {
            AbsoluteTime = 0,
            MidiController = MidiController.Pan,
            Value = 64,
        };
        track.Ctrls.Add(panEvent);

        track.Pan = 32;

        Assert.AreEqual(32, track.Pan, "声相属性应返回更新后的值");
        Assert.AreEqual(1, track.Ctrls.Count, "已有首个声相事件时不应新增额外控制器事件");
        Assert.AreSame(panEvent, track.Pans.Single(), "应直接复用并修改已有的首个声相事件");
        Assert.AreEqual(32, panEvent.Value, "已有的首个声相事件值应被更新");
    }

    [TestMethod]
    public void DuplicateControlEvents_WithSameControllerTimeAndValue_ShouldCollapseToSingleEvent()
    {
        var track = new MidiTrackViewModel();
        var first = new ControlChangeEventViewModel
        {
            AbsoluteTime = 0,
            MidiController = MidiController.MainVolume,
            Value = 100,
        };
        var duplicate = new ControlChangeEventViewModel
        {
            AbsoluteTime = 0,
            MidiController = MidiController.MainVolume,
            Value = 100,
        };

        track.Ctrls.Add(first);
        track.Ctrls.Add(duplicate);

        Assert.AreEqual(1, track.Ctrls.Count, "相同控制器、相同时间、相同值的控制器事件应自动去重");
        Assert.AreEqual(1, track.Volumes.Count, "去重后分类集合中也应只保留一个事件");
        Assert.AreEqual(100, track.Volume, "去重后主音量值应保持不变");
    }

    [TestMethod]
    public void Channel10_ShouldUsePercussionPatchRangeAndDisplayName()
    {
        var track = new MidiTrackViewModel
        {
            Channel = 10
        };

        Assert.IsTrue(track.IsPercussionChannel, "第10通道应被识别为打击乐通道");
        Assert.IsTrue(track.Patch.IsDrum(out _), "第10通道的Patch应切换到打击乐范围");
        Assert.AreEqual(Patch.DrumAcousticBassDrum, track.Patch, "从普通音色切到第10通道时应落到默认打击乐Patch");
        Assert.AreEqual("Acoustic Bass Drum", track.PatchDisplayName, "打击乐Patch显示名应去掉Drum前缀并按单词分隔");
        Assert.IsTrue(track.AvailablePatches.All(patch => patch.IsDrum(out _)), "第10通道的可选Patch列表应全部为打击乐范围");
    }

    [TestMethod]
    public void InitializePlayback_ShouldUseUpdatedPrimaryControlValues()
    {
        var track = new MidiTrackViewModel { Channel = 3 };
        track.Ctrls.Add(new ControlChangeEventViewModel
        {
            AbsoluteTime = 0,
            MidiController = MidiController.MainVolume,
            Value = 64,
        });
        track.Ctrls.Add(new ControlChangeEventViewModel
        {
            AbsoluteTime = 0,
            MidiController = MidiController.Pan,
            Value = 64,
        });

        track.Volume = 96;
        track.Pan = 20;

        List<int> sentMessages = [];
        var initializePlayback = typeof(MidiTrackViewModel).GetMethod("InitializePlayback",
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: [typeof(long), typeof(Action<int>)],
            modifiers: null);
        Assert.IsNotNull(initializePlayback, "测试需要访问内部播放初始化逻辑");
        initializePlayback.Invoke(track, [240L, (Action<int>)sentMessages.Add]);

        Assert.IsTrue(sentMessages.Contains(MidiMessage.ChangeControl((int)MidiController.MainVolume, 96, 3).RawData), "播放初始化时应读取并发送更新后的音量值");
        Assert.IsTrue(sentMessages.Contains(MidiMessage.ChangeControl((int)MidiController.Pan, 20, 3).RawData), "播放初始化时应读取并发送更新后的声相值");
    }

    [TestMethod]
    public void VolumeAndPan_Set_DuringPlayback_ShouldSendImmediateControlChanges()
    {
        var editor = new MidiEditorViewModel();
        var track = new MidiTrackViewModel { Channel = 4 };
        editor.Tracks.Add(track);

        ReflectionHelper.SetProperty(editor, "IsPlaying", true);

        List<int> sentMessages = [];
        var sinkField = typeof(MidiEditorViewModel).GetField("_activePlaybackMessageSink", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(sinkField, "测试需要访问当前播放消息发送委托");
        sinkField.SetValue(editor, (Action<int>)sentMessages.Add);

        track.Volume = 88;
        track.Pan = 12;

        Assert.IsTrue(sentMessages.Contains(MidiMessage.ChangeControl((int)MidiController.MainVolume, 88, 4).RawData), "播放过程中拖动音量托条后，应立即向当前轨道发送新的音量控制消息");
        Assert.IsTrue(sentMessages.Contains(MidiMessage.ChangeControl((int)MidiController.Pan, 12, 4).RawData), "播放过程中拖动声相托条后，应立即向当前轨道发送新的声相控制消息");
    }
}
