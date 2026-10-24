using System.Text.RegularExpressions;
using Xunit;

namespace Teamboard.Shared.Http.Tests;

/// <summary>Test chính sách cho bảng mã lỗi dùng chung (`backend-engineering.md` mục 1).</summary>
public sealed partial class SharedErrorsTests
{
    [Fact]
    public void All_ContainsInternalError_AsServerError()
    {
        var error = Assert.Single(SharedErrors.All, e => e.Code == "INTERNAL_ERROR");

        Assert.Equal(500, error.Status);
    }

    [Fact]
    public void All_Codes_AreUniqueAndUpperSnakeCase()
    {
        Assert.NotEmpty(SharedErrors.All);
        Assert.Equal(SharedErrors.All.Count, SharedErrors.All.Select(e => e.Code).Distinct().Count());
        Assert.All(SharedErrors.All, e => Assert.Matches(UpperSnake(), e.Code));
    }

    [Fact]
    public void All_Errors_HaveErrorStatusAndNonEmptyMessage()
    {
        Assert.NotEmpty(SharedErrors.All);
        Assert.All(SharedErrors.All, e =>
        {
            Assert.InRange(e.Status, 400, 599);
            Assert.False(string.IsNullOrWhiteSpace(e.Message));
        });
    }

    [GeneratedRegex("^[A-Z][A-Z0-9]*(_[A-Z0-9]+)*$")]
    private static partial Regex UpperSnake();
}
