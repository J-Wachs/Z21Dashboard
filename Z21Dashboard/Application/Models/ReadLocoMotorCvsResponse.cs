namespace Z21Dashboard.Application.Models;

internal record ReadLocoMotorCvsResponse(
    bool IsSuccess,
    string? ErrorMessage,
    CVMotorParameters? CVMotorParameters
    );
