namespace IdentityService.Security.Signing;

internal static class SigningKeyRotationPolicy
{
    internal static readonly TimeSpan JwksPublishWindow = TimeSpan.FromMinutes(10);

    internal static readonly TimeSpan MinRetiringDuration =
        TimeSpan.FromHours(24) + TimeSpan.FromSeconds(60);

    internal const string PendingKid = "test-rotate-pending-1";

    internal const string NextActiveKid = "test-rotate-active-2";
}