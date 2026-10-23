using System.ComponentModel.DataAnnotations;

namespace Teamboard.Shared.ServiceDefaults;

/// <summary>Cấu hình chung của mọi service (tên service dùng cho telemetry).</summary>
public sealed class ServiceDefaultsOptions
{
    [Required(AllowEmptyStrings = false)]
    public string ServiceName { get; set; } = string.Empty;
}
