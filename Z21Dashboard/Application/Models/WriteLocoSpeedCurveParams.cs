namespace Z21Dashboard.Application.Models;

internal record WriteLocoSpeedCurveParams(
    ProgrammingTarget ProgTarget,
    ushort? LocoAddress,
    CVSpeedCurveParameters CurveParameters
    );
