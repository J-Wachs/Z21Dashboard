using Microsoft.AspNetCore.Components;
using Z21Dashboard.LocomotiveDecoders.Models;
using Z21Dashboard.Ui.Components;

namespace Z21Dashboard.Ui.Models;

/// <summary>
/// Parameters for the <see cref="UIPagesAddPages"/> method, which adds pages
/// to the UI based on the provided decoder settings and programming target.
/// </summary>
/// <param name="Parent"></param>
/// <param name="DecoderSettings"></param>
/// <param name="ProgTarget"></param>
/// <param name="LocoAddress"></param>
/// <param name="ErrorMessages"></param>
/// <param name="ErrorMessagesChanged"></param>
/// <param name="WorkingMessage"></param>
/// <param name="WorkingMessageChanged"></param>
/// <param name="SuccessMessage"></param>
/// <param name="SuccessMessageChanged"></param>
public record UIPagesAddPagesParams(
    TabControl Parent,
    DecoderSettings DecoderSettings,
    ProgrammingTarget ProgTarget,
    ushort? LocoAddress,
    List<string> ErrorMessages,
    EventCallback<List<string>> ErrorMessagesChanged,
    string? WorkingMessage,
    EventCallback<string?> WorkingMessageChanged,
    string? SuccessMessage,
    EventCallback<string?> SuccessMessageChanged
    );
