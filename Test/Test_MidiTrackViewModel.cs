using Auris_Studio.Midi;
using Auris_Studio.ViewModels;
using Auris_Studio.ViewModels.MidiEvents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NAudio.Midi;
using System.Linq;

namespace Test;

[TestClass]
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

    [TestMethod]
    public void Solo_WhenEnabled_ShouldOnlyKeepSoloTrackAudible()
    {
        var editor = new MidiEditorViewModel();
        var soloTrack = new MidiTrackViewModel();
        var otherTrack = new MidiTrackViewModel();
        var thirdTrack = new MidiTrackViewModel();

        editor.Tracks.Add(soloTrack);
        editor.Tracks.Add(otherTrack);
        editor.Tracks.Add(thirdTrack);

        soloTrack.TrackSoloCommand.Execute(null);

        Assert.IsTrue(editor.HasSoloTracks, "存在独奏音轨时编辑器应反映独奏状态");
        Assert.IsTrue(soloTrack.Solo, "执行独奏命令后音轨应处于独奏状态");
        Assert.IsFalse(soloTrack.Muted, "独奏音轨应自动取消静音");
        Assert.IsTrue(soloTrack.IsAudible, "独奏音轨应保持可发声");
        Assert.IsTrue(otherTrack.Muted, "启用独奏后其他音轨应同步进入静音状态");
        Assert.IsFalse(otherTrack.IsAudible, "存在独奏音轨时其他未独奏音轨应被抑制");
        Assert.IsTrue(thirdTrack.Muted, "启用独奏后其余所有音轨都应同步静音");

        otherTrack.TrackSoloCommand.Execute(null);

        Assert.IsFalse(soloTrack.Solo, "切换到另一条独奏时原独奏音轨应被取消独奏");
        Assert.IsTrue(soloTrack.Muted, "切换独奏目标后原独奏音轨应同步静音");
        Assert.IsTrue(otherTrack.Solo, "新的独奏目标应成为唯一独奏音轨");
        Assert.IsFalse(otherTrack.Muted, "新的独奏目标应保持未静音");
        Assert.IsFalse(thirdTrack.Solo, "未选中的其他音轨不应同时保持独奏");

        otherTrack.TrackSoloCommand.Execute(null);

        string trackStatesAfterSoloOff = string.Join(", ", editor.Tracks.Select(track => $"S={track.Solo}/M={track.Muted}"));
        Assert.AreEqual(false, editor.HasSoloTracks, $"取消独奏后编辑器不应继续保留独奏状态。Count={editor.Tracks.Count}; Tracks={trackStatesAfterSoloOff}");
        Assert.IsFalse(soloTrack.Muted, "取消独奏后原音轨应恢复未静音");
        Assert.IsTrue(otherTrack.IsAudible, "取消独奏后其他音轨应恢复可发声");
        Assert.IsFalse(otherTrack.Muted, "取消独奏后当前音轨也应恢复未静音");
        Assert.IsFalse(thirdTrack.Muted, "取消独奏后所有音轨都应恢复未静音");

        soloTrack.TrackSoloCommand.Execute(null);

        Assert.IsTrue(soloTrack.Solo, "重新启用独奏后应再次进入独奏状态");
        Assert.IsTrue(otherTrack.Muted, "独奏期间其他音轨应再次被静音");

        otherTrack.TrackMutedCommand.Execute(null);

        Assert.AreEqual(false, editor.HasSoloTracks, "手动取消其他音轨静音时应自动取消独奏");
        Assert.IsFalse(soloTrack.Solo, "手动取消其他音轨静音时原独奏轨应退出独奏");
        Assert.IsFalse(soloTrack.Muted, "取消独奏后原独奏轨应恢复未静音");
        Assert.IsFalse(otherTrack.Muted, "手动取消静音的音轨应保持未静音");
        Assert.IsTrue(thirdTrack.Muted, "因手动取消单条非独奏轨静音而取消独奏时，不应把其他轨道全部恢复为未静音");
    }
}
