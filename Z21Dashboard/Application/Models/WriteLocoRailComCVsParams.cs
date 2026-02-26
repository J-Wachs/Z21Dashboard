namespace Z21Dashboard.Application.Models;

internal record WriteLocoRailComCVsParams(
    ProgrammingTarget ProgTarget,
    ushort? LocoAddress,
    CVRailComParameters CVRailComParameters
    );
