using Microsoft.AspNetCore.Builder;
using Xunit;

namespace Teamboard.Shared.ServiceDefaults.Tests;

public sealed class ServiceDefaultsGuardTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddServiceDefaults_BlankServiceName_Throws(string serviceName)
    {
        var builder = WebApplication.CreateBuilder();

        Assert.Throws<ArgumentException>(() => builder.AddServiceDefaults(serviceName));
    }

    [Fact]
    public void AddServiceDefaults_NullServiceName_Throws()
    {
        var builder = WebApplication.CreateBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.AddServiceDefaults(null!));
    }
}
