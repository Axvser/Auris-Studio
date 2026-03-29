using Auris_Studio.Midi;
using Auris_Studio.ViewModels;
using Auris_Studio.ViewModels.MidiEvents;
using Auris_Studio.ViewModels.Workflows;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NAudio.Midi;
using System.Linq;

namespace Test;

[TestClass]
public sealed class Test_UseCaseScenarios
{
    [TestMethod]
    public void ImportMidiRead_ShouldPopulateTracksAndSelectFirstTrack()
    {
        var viewModel = new MidiEditorViewModel();
        var midiResult = new MidiResult
        {
            deltaTicksPerQuarterNote = 960
        };

        midiResult.tempoEvs.Add(new TempoEvent(600000, 0));
        midiResult.tsEvs.Add(new TimeSignatureEvent(0, 3, 2, 24, 8));

        var noteOn = new NoteOnEvent(120, 1, (int)Pitch.C4, 96, 240)
        {
            OffEvent = new NoteEvent(360, 1, MidiCommandCode.NoteOff, (int)Pitch.C4, 0)
        };
        midiResult.noteOnEvs[1][Patch.AcousticGrandPiano] = [noteOn];

        viewModel.Read(midiResult);

        Assert.AreEqual(960, viewModel.PPQN, "导入后应采用文件中的 PPQN");
        Assert.AreEqual(100, viewModel.BPM, "应根据首个速度事件更新 BPM");
        Assert.AreEqual(3, viewModel.Numerator, "应根据首个拍号事件更新拍号分子");
        Assert.AreEqual(4, viewModel.Denominator, "应根据首个拍号事件更新拍号分母");
        Assert.HasCount(1, viewModel.Tracks, "导入单通道 MIDI 时应创建一条音轨");
        Assert.AreSame(viewModel.Tracks[0], viewModel.CurrentSelectedTrack, "导入后应自动选中首个可用音轨");
        Assert.AreEqual(1, viewModel.Tracks[0].Notes.Count, "音轨应包含导入的音符");
        Assert.AreEqual(120L, viewModel.Tracks[0].Notes.Single().AbsoluteTime, "音符起始时间应来自导入结果");
    }

    [TestMethod]
    public void ExportMidiWrite_ShouldGenerateDefaultMetaEventsAndTrackData()
    {
        var viewModel = new MidiEditorViewModel();
        ReflectionHelper.SetProperty(viewModel, "BPM", 90);
        ReflectionHelper.SetProperty(viewModel, "Numerator", 3);
        ReflectionHelper.SetProperty(viewModel, "Denominator", 4);

        var track = new MidiTrackViewModel
        {
            Parent = viewModel,
            Channel = 2,
            Patch = Patch.AcousticGrandPiano,
            Name = "Lead"
        };
        track.Notes.Add(new NoteEventViewModel
        {
            AbsoluteTime = 240,
            DeltaTime = 120,
            Note = Pitch.C4
        });
        track.Ctrls.Add(new ControlChangeEventViewModel
        {
            AbsoluteTime = 0,
            MidiController = MidiController.MainVolume,
            Value = 100
        });
        viewModel.Tracks.Add(track);

        var midiResult = new MidiResult();
        viewModel.Write(midiResult);

        Assert.HasCount(1, midiResult.tempoEvs, "缺失速度事件时应回填默认速度事件");
        Assert.HasCount(1, midiResult.tsEvs, "缺失拍号事件时应回填默认拍号事件");
        Assert.HasCount(1, midiResult.patchEvs[2][Patch.AcousticGrandPiano], "导出时应写出音轨音色事件");
        Assert.HasCount(1, midiResult.noteOnEvs[2][Patch.AcousticGrandPiano], "导出时应写出 NoteOn 事件");
        Assert.HasCount(1, midiResult.noteOffEvs[2][Patch.AcousticGrandPiano], "导出时应写出 NoteOff 事件");
        Assert.HasCount(1, midiResult.cCEvs[2][Patch.AcousticGrandPiano], "导出时应写出控制器事件");
    }

    [TestMethod]
    public void TrackSelection_ShouldToggleEditableNotesBetweenTracks()
    {
        var viewModel = new MidiEditorViewModel();
        var firstTrack = new MidiTrackViewModel();
        var secondTrack = new MidiTrackViewModel();
        var firstNote = new NoteEventViewModel { AbsoluteTime = 0, DeltaTime = 120, Note = Pitch.C4 };
        var secondNote = new NoteEventViewModel { AbsoluteTime = 120, DeltaTime = 120, Note = Pitch.D4 };

        firstTrack.Notes.Add(firstNote);
        secondTrack.Notes.Add(secondNote);
        viewModel.Tracks.Add(firstTrack);
        viewModel.Tracks.Add(secondTrack);

        firstTrack.TrackSelectCommand.Execute(null);

        Assert.AreSame(firstTrack, viewModel.CurrentSelectedTrack, "选择首轨后应更新当前选中音轨");
        Assert.IsTrue(firstNote.IsEnabled, "当前轨道音符应保持可编辑");
        Assert.IsFalse(secondNote.IsEnabled, "非当前轨道音符应被禁用编辑");

        secondTrack.TrackSelectCommand.Execute(null);

        Assert.AreSame(secondTrack, viewModel.CurrentSelectedTrack, "再次选择其他音轨后应切换当前选中音轨");
        Assert.IsFalse(firstNote.IsEnabled, "被切出的音轨音符应被禁用编辑");
        Assert.IsTrue(secondNote.IsEnabled, "新选中音轨音符应恢复可编辑");
    }

    [TestMethod]
    public void BasicPitchConfigDefaults_ShouldExposeUsableConfiguration()
    {
        var config = new BasicPitchConfigViewModel();

        Assert.IsFalse(string.IsNullOrWhiteSpace(config.OutputDirectory), "默认输出目录不应为空");
        Assert.IsFalse(string.IsNullOrWhiteSpace(config.ModelPath), "默认模型路径不应为空");
        Assert.IsFalse(string.IsNullOrWhiteSpace(config.ExePath), "默认可执行文件路径不应为空");
        Assert.AreEqual(120d, config.Tempo, 0.001, "默认节拍应为 120 BPM");
        Assert.AreEqual(1, config.DefChannel, "默认通道应为 1");
    }
}
