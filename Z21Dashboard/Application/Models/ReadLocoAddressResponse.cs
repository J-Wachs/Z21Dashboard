namespace Z21Dashboard.Application.Models;

internal record ReadLocoAddressResponse(
    bool IsSuccess,
    string? ErrorMessage,
    ushort? LocoAddress
    );
