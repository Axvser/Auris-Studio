using Auris_Studio.ViewModels.ComponentModel;
using VeloxDev.Core.MVVM;

namespace Auris_Studio.ViewModels;

public partial class CuttingLineViewModel : ITimeOrderable
{
    [VeloxProperty] public partial long AbsoluteTime { get; set; }
    [VeloxProperty] public partial int DeltaTime { get; set; }

    [VeloxProperty] public partial double Left { get; set; }
    [VeloxProperty] public partial double Width { get; set; }
    [VeloxProperty] public partial string Text { get; set; }
}