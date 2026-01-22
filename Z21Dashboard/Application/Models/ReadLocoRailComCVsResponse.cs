namespace Z21Dashboard.Application.Models;

internal record ReadLocoRailComCVsResponse(
    bool IsSuccess,
    string? ErrorMessage,
    CVRailComParameters? CVRailComParameters
    );
