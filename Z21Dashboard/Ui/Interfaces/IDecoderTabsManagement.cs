using Z21Dashboard.LocomotiveDecoders.Models;
using Z21Dashboard.Ui.Components;

namespace Z21Dashboard.Ui.Interfaces;

public interface IDecoderTabsManagement
{
    /// <summary>
    /// Removes all tabs associated with the given decoder settings from the parent TabControl.
    /// </summary>
    /// <param name="parent">The parent TabControl from which to remove the tabs.</param>
    /// <param name="decoderSettings">The decoder settings whose associated tabs should be removed.</param>
    void RemoveDecoderTabs(TabControl parent, DecoderSettings decoderSettings);
}
