using Auris_Studio.Midi;
using Auris_Studio.ViewModels;
using Auris_Studio.ViewModels.MidiEvents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NAudio.Midi;
using System.Linq;

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
}
