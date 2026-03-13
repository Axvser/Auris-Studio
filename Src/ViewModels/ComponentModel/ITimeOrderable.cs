using System.ComponentModel;

namespace Auris_Studio.ViewModels.ComponentModel;

/// <summary>
/// 时间有序对象，时间单位采用 Tick
/// </summary>
public interface ITimeOrderable : INotifyPropertyChanged
{
    public long AbsoluteTime { get; set; }
    public int DeltaTime { get; set; }
}
