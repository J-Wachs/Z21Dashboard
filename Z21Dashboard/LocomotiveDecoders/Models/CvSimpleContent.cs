using System.Collections.ObjectModel;

namespace Z21Dashboard.LocomotiveDecoders.Models;

public class CvSimpleContent : PageContentBase
{
    public required ObservableCollection<CvItem> Content { get; set; }
}
