using Z21Client.Models;

namespace Z21Dashboard.Application.Interfaces
{
    public interface INmraCvService
    {
        Task<string> GetCVBitName(ushort cvNumber, Bits bit);
        Task<string> GetCVBitDescription(ushort cvNumber, Bits bit);
        Task<(string ZeroDescription, string OneDescription)> GetCVBitValuesDescription(ushort cvNumber, Bits bit);
        Task<string> GetCVDescription(ushort cvNumber);
        Task<(string Name, string Description)> GetCVNameAndDescription(ushort cvNumber);
    }
}