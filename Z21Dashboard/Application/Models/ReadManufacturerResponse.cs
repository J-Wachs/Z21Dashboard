namespace Z21Dashboard.Application.Models;

internal record ReadManufacturerResponse(
    bool IsSuccess,
    string? ErrorMessage,
    byte ManufacturerId,
    string ManufacturerName,
    byte ManufacturerVersionNbr
    );

