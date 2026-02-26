namespace Z21Dashboard.Application.Models;

internal record WriteLocoAddressResponse(
    bool IsSuccess,
    string? ErrorMessage
    );
