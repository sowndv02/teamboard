namespace IdentityService.Tests.Support;

internal sealed class FakeRotationTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public FakeRotationTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;

    public void Advance(TimeSpan span) => _utcNow += span;

    public override DateTimeOffset GetUtcNow() => _utcNow;
}