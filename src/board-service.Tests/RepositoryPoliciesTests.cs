using BoardService.Tests.Support;
using Xunit;

namespace BoardService.Tests;

/// <summary>Unit test cho các luật quét của @S14 và @S15, để chứng minh luật bắt được vi phạm (không phải test luôn xanh).</summary>
public sealed class RepositoryPoliciesTests
{
    [Theory]
    [InlineData("10.0.13", true)]
    [InlineData("1.24.2-preview.1", true)]
    [InlineData("10.*", false)]
    [InlineData("*", false)]
    [InlineData("[10.0,11.0)", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsPinnedVersion_Version_ReturnsExpected(string? version, bool expected) =>
        Assert.Equal(expected, RepositoryPolicies.IsPinnedVersion(version));

    [Theory]
    [InlineData("      MSSQL_SA_PASSWORD: \"${SA_PASSWORD}\"", false)]
    [InlineData("      ConnectionStrings__DefaultConnection: \"Server=db;Password=${SA_PASSWORD};\"", false)]
    [InlineData("      MSSQL_SA_PASSWORD: \"Matkhau-that-123\"", true)]
    [InlineData("      API_KEY: abcdef123456", true)]
    [InlineData("      ASPNETCORE_URLS: http://+:7070", false)]
    public void IsHardcodedSecret_Line_ReturnsExpected(string line, bool expected) =>
        Assert.Equal(expected, RepositoryPolicies.IsHardcodedSecret(line));
}
