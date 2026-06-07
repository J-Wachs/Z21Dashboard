using Z21Dashboard.Application.Models;
using Z21Dashboard.Helpers;

namespace Z21Dashboard.Application.Interfaces;

internal interface ILocoDefinitionService
{
    public Task<Result> DeleteLocomotiveFileAsync(string filePath);
    public Task<Result<LocomotiveDefinitionDto?>> GetLocomotiveAsync(string filePath);
    public Task<Result<IReadOnlyList<LocomotiveListItemDto>?>> GetLocomotivesAsync();
    public Task<Result<string>> SaveLocomotiveAsync(LocomotiveDefinitionDto locomotive);
}
