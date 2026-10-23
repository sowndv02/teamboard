namespace Teamboard.Shared.Http;

/// <summary>Một mã lỗi API ổn định kèm status HTTP và message mặc định (tiếng Anh, không lộ chi tiết nội bộ).</summary>
public sealed record ErrorDefinition(string Code, int Status, string Message);
