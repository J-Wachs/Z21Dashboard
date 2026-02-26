using Z21Client.Models;
using Z21Dashboard.Application.Models;

namespace Z21Dashboard.Application.Interfaces;

internal interface IDecoderProgHelperService
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="progTarget"></param>
    /// <param name="locoAddress"></param>
    /// <param name="cvsToRequest"></param>
    /// <returns></returns>
    Task<(bool IsSuccess, string? ErrorMessage, List<CVValue> CvValues)> ReadCVValuesAsync(ProgrammingTarget progTarget, ushort? locoAddress, Queue<ushort> cvsToRequest);

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Task<ReadLocoAddressResponse> ReadLocoAddressAsync();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="progTarget"></param>
    /// <param name="locoAddress"></param>
    /// <returns></returns>
    Task<ReadManufacturerResponse> ReadLocoDecoderManufacturerAsync(ProgrammingTarget progTarget, ushort? locoAddress);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="progTarget"></param>
    /// <param name="locoAddress"></param>
    /// <returns></returns>
    Task<ReadLocoMotorCvsResponse> ReadLocoMotorCVsAsync(ProgrammingTarget progTarget, ushort? locoAddress);

    /// <summary>
    /// Reads all RailCom-related CVs from the locomotive.
    /// </summary>
    Task<ReadLocoRailComCVsResponse> ReadLocoRailComCVsAsync(ProgrammingTarget progTarget, ushort? locoAddress);

    Task<ReadLocoSpeedCurveResponse> ReadLocoSpeedCurveCVsAsync(ProgrammingTarget progTarget, ushort? locoAddress);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="progTarget"></param>
    /// <param name="locoAddress"></param>
    /// <param name="cvsToWrite"></param>
    /// <returns></returns>
    Task<(bool IsSuccess, string? ErrorMessage)> WriteCVValuesAsync(ProgrammingTarget progTarget, ushort? locoAddress, Queue<CVValue> cvsToWrite);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="writeLocoAddressParams"></param>
    /// <returns></returns>
    Task<WriteLocoAddressResponse> WriteLocoAddressAsync(ushort newLocoAddress);

    Task<WriteLocoMotorCVsResponse> WriteLocoMotorCVsAsync(WriteLocoMotorCVsParams writeLocoMotorCVsParams);

    /// <summary>
    /// Writes all RailCom-related CVs to the locomotive.
    /// </summary>
    Task<WriteLocoRailComCVsResponse> WriteLocoRailComCVsAsync(WriteLocoRailComCVsParams parms);

    Task<WriteLocoSpeedCurveResponse> WriteLocoSpeedCurveCVsAsync(WriteLocoSpeedCurveParams parms);
}
