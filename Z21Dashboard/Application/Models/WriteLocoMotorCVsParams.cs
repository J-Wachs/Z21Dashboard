namespace Z21Dashboard.Application.Models;

internal record WriteLocoMotorCVsParams(
    ProgrammingTarget ProgTarget,
    ushort? LocoAddress,
    CVMotorParameters CVMotorParameters
    );
