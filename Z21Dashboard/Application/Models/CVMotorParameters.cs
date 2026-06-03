using System.ComponentModel.DataAnnotations;
using Z21Dashboard.Resources.Localization;

namespace Z21Dashboard.Application.Models;

public class CVMotorParameters
{
    public bool? CV29_0_ForwardDirection { get; set; }
    public bool? CV29_1_SpeedSteps { get; set; }
    public bool? CV29_2_DCAnalog { get; set; }
    
    [Range(0, 255, ErrorMessageResourceType = typeof(ValidationErrorMessages), ErrorMessageResourceName = "AccelerationRateInvalid")]
    public byte? CV3_AccRate { get; set; }

    [Range(0, 255, ErrorMessageResourceType = typeof(ValidationErrorMessages), ErrorMessageResourceName = "DeaccelerationRateInvalid")]
    public byte? CV4_DecRate { get; set; }

    [Range(0, 255, ErrorMessageResourceType = typeof(ValidationErrorMessages), ErrorMessageResourceName = "PWMPeriodInvalid")]
    public byte? CV9_PWMPeriod { get; set; }

    [Range(0, 255, ErrorMessageResourceType = typeof(ValidationErrorMessages), ErrorMessageResourceName = "KickStartInvalid")]
    public byte? CV65_KickStart { get; set; }
}
