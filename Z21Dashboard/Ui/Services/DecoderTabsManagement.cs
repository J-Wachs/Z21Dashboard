using GlobalConstants = Z21Dashboard.Constants;
using Z21Dashboard.LocomotiveDecoders.Models;
using Z21Dashboard.Ui.Components;
using Z21Dashboard.Ui.Interfaces;

namespace Z21Dashboard.Ui.Services;

public class DecoderTabsManagement : IDecoderTabsManagement
{
    /// <inheritdoc/>
    public void RemoveDecoderTabs(
        TabControl parent,
        DecoderSettings decoderSettings
        )
    {
        // Remove tabs associated with the previous selected decoder:
        _ = parent.RemoveTabById(GlobalConstants.DecoderEditBasePageId);
        _ = parent.RemoveTabById(GlobalConstants.DecoderAddressPageId);
        _ = parent.RemoveTabById(GlobalConstants.AllCvsReadWritePageId);
        foreach (var onePage in decoderSettings.Pages)
        {
            _ = parent.RemoveTabById(onePage.Id);
        }
    }
}
