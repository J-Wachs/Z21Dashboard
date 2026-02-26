namespace Z21Dashboard.Application.Models;

internal record ReadLocoSpeedCurveResponse(
    bool IsSuccess,
    string? ErrorMessage,
    CVSpeedCurveParameters? CurveParameters
);
