using Z21Dashboard.Helpers;
using Z21Dashboard.LocomotiveDecoders.Models;
using Z21Dashboard.NmraDccStandard;
using Z21Dashboard.Resources.Localization;
using Z21Dashboard.Ui.Dashboard.Widgets.ProgrammingDecoderTabs.AdvTabs;
using Z21Dashboard.Ui.Interfaces;
using Z21Dashboard.Ui.Models;
using GlobalConstants = Z21Dashboard.Constants;

namespace Z21Dashboard.Ui.Services;

/// <summary>
/// Add pages to the UI based on the page definitions in the decoder JSON
/// file. Each page type corresponds to a specific Blazor component that
/// is added as the content of the page.
/// </summary>
public class DecoderUIPageBuilder : IDecoderUIPageBuilder
{
    public async Task<Result> AddPagesAsync(UIPagesAddPagesParams parms)
    {
        // Are we to add the Address pages for non-exclusive Märklin Motorola decoders?
        if (parms.DecoderSettings.DecoderProtocols?.ToUpper() is not Constants.MärklinMotorola and
            not Constants.MärklinMotorolaVersion1 and
            not Constants.MärklinMotorolaVersion2)
        {
            await AddPageAddress(parms);
        }

        // Add Meta edit page:
        await AddPageDecoderEditBase(parms);

        if (parms.DecoderSettings.CvMetadata.Where(x => x.Key != Cvs.ShortAddress).Any())
        {
            await AddPageAllCvsReadWrite(parms);
        }

        foreach (var uiPage in parms.DecoderSettings.Pages)
        {
            switch (uiPage.Type)
            {
                case DecoderUIPageTypes.DynamicList:
                    await AddPageDynamicCVList(parms, uiPage);
                    break;
                case DecoderUIPageTypes.FunctionMappingStandard:
                    await AddPageFunctionMappingStandard(parms, uiPage);
                    break;
                case DecoderUIPageTypes.SpeedCurve28:
                    await AddPageSpeedCurve28(parms, uiPage);
                    break;
                case DecoderUIPageTypes.SpeedCurve3And28:
                    await AddPageSpeedCurve3and28(parms, uiPage);
                    break;
                case DecoderUIPageTypes.Esu50708FKeysMapping:
                    await AddPageEsu50708FKeyMapping(parms, uiPage);
                    break;
                case DecoderUIPageTypes.Esu50708Outputs:
                    await AddPageEsu50708Outputs(parms, uiPage);
                    break;
                case DecoderUIPageTypes.MMProperties:
                    await AddPageMMProperties(parms, uiPage);
                    break;
                case DecoderUIPageTypes.ZIMOFKeysMapping:
                    await AddPageZIMO8FKeyMapping(parms, uiPage);
                    break;
                default:
                    // Ups - a page type is unknown !!!
                    return Result.Failure(string.Format(
                        SharedResources.PageTypeInDecoderFileUnknown,
                        parms.DecoderSettings.DecoderFile,
                        uiPage.Title,
                        uiPage.Type
                        ));
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// Adds a new tab page containing a Märklin Motorola properties component to the parent
    /// container specified in the parameters.
    /// </summary>
    /// <param name="parms">The parameters used to configure the new page, including parent container, error messages, working messages, and
    /// success messages.</param>
    /// <param name="uiPage">The UI page definition that provides the identifier and title for the new tab page.</param>
    /// <returns>A task that represents the asynchronous operation of adding the page.</returns>
    private async Task AddPageAddress(UIPagesAddPagesParams parms)
    {
        void fragment(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent(0, typeof(Address));
            builder.AddAttribute(2, "ErrorMessages", parms.ErrorMessages);
            builder.AddAttribute(3, "ErrorMessagesChanged", parms.ErrorMessagesChanged);
            builder.AddAttribute(4, "WorkingMessage", parms.WorkingMessage);
            builder.AddAttribute(5, "WorkingMessageChanged", parms.WorkingMessageChanged);
            builder.AddAttribute(6, "SuccessMessage", parms.SuccessMessage);
            builder.AddAttribute(7, "SuccessMessageChanged", parms.SuccessMessageChanged);
            builder.CloseComponent();
        }

        // Add the tab
        await parms.Parent.AddPage(new TabPageDefinition
        {
            Id = GlobalConstants.DecoderAddressPageId,
            Title = SharedResources.AddressTitle,
            Content = fragment
        });
    }


    /// <summary>
    /// Add a page of the type DecoderEditBase, which allows the user
    /// to edit meta-data about the decoder.
    /// </summary>
    /// <param name="parms"></param>
    /// <returns></returns>
    private async Task AddPageDecoderEditBase(UIPagesAddPagesParams parms)
    {
        void fragment(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent(0, typeof(DecoderEditMeta));
            builder.AddAttribute(2, "ErrorMessages", parms.ErrorMessages);
            builder.AddAttribute(3, "ErrorMessagesChanged", parms.ErrorMessagesChanged);
            builder.AddAttribute(4, "WorkingMessage", parms.WorkingMessage);
            builder.AddAttribute(5, "WorkingMessageChanged", parms.WorkingMessageChanged);
            builder.AddAttribute(6, "SuccessMessage", parms.SuccessMessage);
            builder.AddAttribute(7, "SuccessMessageChanged", parms.SuccessMessageChanged);
            builder.CloseComponent();
        }

        await parms.Parent.AddPage(new TabPageDefinition
        {
            Id = GlobalConstants.DecoderEditBasePageId,
            Title = SharedResources.DecoderEditBaseTitle,
            Content = fragment
        });
    }

    private async Task AddPageAllCvsReadWrite(UIPagesAddPagesParams parms)
    {
        void fragment(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent(0, typeof(AllCvsReadWrite));
            builder.AddAttribute(2, "ErrorMessages", parms.ErrorMessages);
            builder.AddAttribute(3, "ErrorMessagesChanged", parms.ErrorMessagesChanged);
            builder.AddAttribute(4, "WorkingMessage", parms.WorkingMessage);
            builder.AddAttribute(5, "WorkingMessageChanged", parms.WorkingMessageChanged);
            builder.AddAttribute(6, "SuccessMessage", parms.SuccessMessage);
            builder.AddAttribute(7, "SuccessMessageChanged", parms.SuccessMessageChanged);
            builder.CloseComponent();
        }
        // Add the tab
        await parms.Parent.AddPage(new TabPageDefinition
        {
            Id = GlobalConstants.AllCvsReadWritePageId,
            Title = SharedResources.PageAllCvsReadWrite,
            Content = fragment
        });
    }

    private async Task AddPageEsu50708FKeyMapping(
        UIPagesAddPagesParams parms,
        DecoderPage uiPage
    )
    {
        void fragment(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent(0, typeof(Esu50708_09_FKeyMapping));
            builder.AddAttribute(1, "TabId", uiPage.Id);
            builder.AddAttribute(2, "ErrorMessages", parms.ErrorMessages);
            builder.AddAttribute(3, "ErrorMessagesChanged", parms.ErrorMessagesChanged);
            builder.AddAttribute(4, "WorkingMessage", parms.WorkingMessage);
            builder.AddAttribute(5, "WorkingMessageChanged", parms.WorkingMessageChanged);
            builder.AddAttribute(6, "SuccessMessage", parms.SuccessMessage);
            builder.AddAttribute(7, "SuccessMessageChanged", parms.SuccessMessageChanged);
            builder.CloseComponent();
        }
        // Add the tab
        await parms.Parent.AddPage(new TabPageDefinition
        {
            Id = uiPage.Id,
            Title = uiPage.Title,
            Content = fragment
        });
    }


    private async Task AddPageEsu50708Outputs(
        UIPagesAddPagesParams parms,
        DecoderPage uiPage
        )
    {
        void fragment(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent(0, typeof(Esu50708_09Outputs));
            builder.AddAttribute(1, "TabId", uiPage.Id);
            builder.AddAttribute(2, "ErrorMessages", parms.ErrorMessages);
            builder.AddAttribute(3, "ErrorMessagesChanged", parms.ErrorMessagesChanged);
            builder.AddAttribute(4, "WorkingMessage", parms.WorkingMessage);
            builder.AddAttribute(5, "WorkingMessageChanged", parms.WorkingMessageChanged);
            builder.AddAttribute(6, "SuccessMessage", parms.SuccessMessage);
            builder.AddAttribute(7, "SuccessMessageChanged", parms.SuccessMessageChanged);
            builder.CloseComponent();
        }
        // Add the tab
        await parms.Parent.AddPage(new TabPageDefinition
        {
            Id = uiPage.Id,
            Title = uiPage.Title,
            Content = fragment
        });
    }

    /// <summary>
    /// Add a page of type DynamicList to the UI. This page type is used to display a list of CVs that is
    /// dynamically defined in the deocder JSON file. The list can contain both byte and bit CVs, and the
    /// component will handle the display and editing of these CVs accordingly.
    /// </summary>
    /// <param name="parms"></param>
    /// <param name="uiPage"></param>
    /// <returns></returns>
    private async Task AddPageDynamicCVList(
        UIPagesAddPagesParams parms,
        DecoderPage uiPage
        )
    {
        void fragment(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent(0, typeof(DynamicList));
            builder.AddAttribute(1, "TabId", uiPage.Id);
            builder.AddAttribute(2, "ErrorMessages", parms.ErrorMessages);
            builder.AddAttribute(3, "ErrorMessagesChanged", parms.ErrorMessagesChanged);
            builder.AddAttribute(4, "WorkingMessage", parms.WorkingMessage);
            builder.AddAttribute(5, "WorkingMessageChanged", parms.WorkingMessageChanged);
            builder.AddAttribute(6, "SuccessMessage", parms.SuccessMessage);
            builder.AddAttribute(7, "SuccessMessageChanged", parms.SuccessMessageChanged);
            builder.CloseComponent();
        }

        // Add the tab
        await parms.Parent.AddPage(new TabPageDefinition
        {
            Id = uiPage.Id,
            Title = uiPage.Title,
            Content = fragment
        });
    }

    /// <summary>
    /// Adds a new tab page to the parent container using the standard function key mapping component as its content.
    /// </summary>
    /// <param name="parms">The parameters used to configure the new page, including error, working, and success messages, as well as the
    /// parent container to which the page will be added. Cannot be null.</param>
    /// <param name="uiPage">The UI page definition containing the identifier and title for the new tab. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation of adding the page.</returns>
    private async Task AddPageFunctionMappingStandard(
        UIPagesAddPagesParams parms,
        DecoderPage uiPage
        )
    {
        void fragment(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent(0, typeof(FKeyMappingStandard));
            builder.AddAttribute(1, "TabId", uiPage.Id);
            builder.AddAttribute(2, "ErrorMessages", parms.ErrorMessages);
            builder.AddAttribute(3, "ErrorMessagesChanged", parms.ErrorMessagesChanged);
            builder.AddAttribute(4, "WorkingMessage", parms.WorkingMessage);
            builder.AddAttribute(5, "WorkingMessageChanged", parms.WorkingMessageChanged);
            builder.AddAttribute(6, "SuccessMessage", parms.SuccessMessage);
            builder.AddAttribute(7, "SuccessMessageChanged", parms.SuccessMessageChanged);
            builder.CloseComponent();
        }

        // Add the tab
        await parms.Parent.AddPage(new TabPageDefinition
        {
            Id = uiPage.Id,
            Title = uiPage.Title,
            Content = fragment
        });

    }

    /// <summary>
    /// Adds a new tab page containing a SpeedCurve28 component to the parent
    /// container specified in the parameters.
    /// </summary>
    /// <param name="parms">The parameters used to configure the new page, including parent container, error messages, working messages, and
    /// success messages.</param>
    /// <param name="uiPage">The UI page definition that provides the identifier and title for the new tab page.</param>
    /// <returns>A task that represents the asynchronous operation of adding the page.</returns>
    private async Task AddPageSpeedCurve28(
        UIPagesAddPagesParams parms,
        DecoderPage uiPage
        )
    {
        void fragment(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent(0, typeof(SpeedCurve28));
            builder.AddAttribute(1, "TabId", uiPage.Id);
            builder.AddAttribute(2, "ErrorMessages", parms.ErrorMessages);
            builder.AddAttribute(3, "ErrorMessagesChanged", parms.ErrorMessagesChanged);
            builder.AddAttribute(4, "WorkingMessage", parms.WorkingMessage);
            builder.AddAttribute(5, "WorkingMessageChanged", parms.WorkingMessageChanged);
            builder.AddAttribute(6, "SuccessMessage", parms.SuccessMessage);
            builder.AddAttribute(7, "SuccessMessageChanged", parms.SuccessMessageChanged);
            builder.CloseComponent();
        }

        // Add the tab
        await parms.Parent.AddPage(new TabPageDefinition
        {
            Id = uiPage.Id,
            Title = uiPage.Title,
            Content = fragment
        });
    }

    /// <summary>
    /// Adds a new tab page containing a SpeedCurve3And28 component to the parent
    /// container specified in the parameters.
    /// </summary>
    /// <param name="parms">The parameters used to configure the new page, including parent container, error messages, working messages, and
    /// success messages.</param>
    /// <param name="uiPage">The UI page definition that provides the identifier and title for the new tab page.</param>
    /// <returns>A task that represents the asynchronous operation of adding the page.</returns>
    private async Task AddPageSpeedCurve3and28(
        UIPagesAddPagesParams parms,
        DecoderPage uiPage
        )
    {
        void fragment(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent(0, typeof(SpeedCurve3And28));
            builder.AddAttribute(1, "TabId", uiPage.Id);
            builder.AddAttribute(2, "ErrorMessages", parms.ErrorMessages);
            builder.AddAttribute(3, "ErrorMessagesChanged", parms.ErrorMessagesChanged);
            builder.AddAttribute(4, "WorkingMessage", parms.WorkingMessage);
            builder.AddAttribute(5, "WorkingMessageChanged", parms.WorkingMessageChanged);
            builder.AddAttribute(6, "SuccessMessage", parms.SuccessMessage);
            builder.AddAttribute(7, "SuccessMessageChanged", parms.SuccessMessageChanged);
            builder.CloseComponent();
        }

        // Add the tab
        await parms.Parent.AddPage(new TabPageDefinition
        {
            Id = uiPage.Id,
            Title = uiPage.Title,
            Content = fragment
        });
    }

    /// <summary>
    /// Adds a new tab page containing a Märklin Motorola properties component to the parent
    /// container specified in the parameters.
    /// </summary>
    /// <param name="parms">The parameters used to configure the new page, including parent container, error messages, working messages, and
    /// success messages.</param>
    /// <param name="uiPage">The UI page definition that provides the identifier and title for the new tab page.</param>
    /// <returns>A task that represents the asynchronous operation of adding the page.</returns>
    private async Task AddPageMMProperties(
        UIPagesAddPagesParams parms,
        DecoderPage uiPage
        )
    {
        void fragment(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent(0, typeof(MMProperties));
            builder.AddAttribute(1, "TabId", uiPage.Id);
            builder.AddAttribute(2, "ErrorMessages", parms.ErrorMessages);
            builder.AddAttribute(3, "ErrorMessagesChanged", parms.ErrorMessagesChanged);
            builder.AddAttribute(4, "WorkingMessage", parms.WorkingMessage);
            builder.AddAttribute(5, "WorkingMessageChanged", parms.WorkingMessageChanged);
            builder.AddAttribute(6, "SuccessMessage", parms.SuccessMessage);
            builder.AddAttribute(7, "SuccessMessageChanged", parms.SuccessMessageChanged);
            builder.CloseComponent();
        }

        // Add the tab
        await parms.Parent.AddPage(new TabPageDefinition
        {
            Id = uiPage.Id,
            Title = uiPage.Title,
            Content = fragment
        });
    }

    /// <summary>
    /// Adds a new tab page containing ZIMO function keys mapping component to the parent.
    /// </summary>
    /// <param name="parms"></param>
    /// <param name="uiPage"></param>
    /// <returns></returns>
    private async Task AddPageZIMO8FKeyMapping(
        UIPagesAddPagesParams parms,
        DecoderPage uiPage
        )
    {
        void fragment(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent(0, typeof(ZIMO_FKeyMapping));
            builder.AddAttribute(1, "TabId", uiPage.Id);
            builder.AddAttribute(2, "ErrorMessages", parms.ErrorMessages);
            builder.AddAttribute(3, "ErrorMessagesChanged", parms.ErrorMessagesChanged);
            builder.AddAttribute(4, "WorkingMessage", parms.WorkingMessage);
            builder.AddAttribute(5, "WorkingMessageChanged", parms.WorkingMessageChanged);
            builder.AddAttribute(6, "SuccessMessage", parms.SuccessMessage);
            builder.AddAttribute(7, "SuccessMessageChanged", parms.SuccessMessageChanged);
            builder.CloseComponent();
        }
        // Add the tab
        await parms.Parent.AddPage(new TabPageDefinition
        {
            Id = uiPage.Id,
            Title = uiPage.Title,
            Content = fragment
        });
    }
}
