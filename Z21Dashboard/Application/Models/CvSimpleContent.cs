using System.Collections.ObjectModel;

namespace Z21Dashboard.Application.Models;

public class CvSimpleContent : PageContentBase
{
    public required ObservableCollection<CvItem> Content { get; set; }
}

