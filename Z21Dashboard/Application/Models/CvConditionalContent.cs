using System.Collections.ObjectModel;

namespace Z21Dashboard.Application.Models;

/// <summary>
/// Represents page content defined by a set of conditional rules that determine which child content is displayed.
/// </summary>
/// <remarks></remarks>
public class CvConditionalContent : PageContentBase
{
    public required ObservableCollection<CvConditionalRule> Content { get; set; }
}
