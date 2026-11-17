using NilCM.Core.Lyrics;
using NilCM.Core.Models;

namespace NilCM.Core.Tests;

public class LyricLocatorTests
{
    private static readonly IReadOnlyList<LyricLine> Lines = LyricParser.Parse(
        "[00:10.000]第一行\n[00:20.000]第二行\n[00:30.000]第三行");

    [Fact]
    public void FindCurrentLine_EmptyLyrics_ReturnsNull()
    {
        Assert.Null(LyricLocator.FindCurrentLineIndex(LyricParser.Parse(null), TimeSpan.FromSeconds(30)));
    }

    [Fact]
    public void FindCurrentLine_BeforeFirstLine_ReturnsNull()
    {
        Assert.Null(LyricLocator.FindCurrentLineIndex(Lines, TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void FindCurrentLine_ExactLineTime_ReturnsThatLine()
    {
        Assert.Equal(1, LyricLocator.FindCurrentLineIndex(Lines, TimeSpan.FromSeconds(20)));
    }

    [Fact]
    public void FindCurrentLine_BetweenLines_ReturnsPreviousLine()
    {
        Assert.Equal(1, LyricLocator.FindCurrentLineIndex(Lines, TimeSpan.FromSeconds(25)));
    }

    [Fact]
    public void FindCurrentLine_AfterLastLine_ReturnsLastLine()
    {
        Assert.Equal(2, LyricLocator.FindCurrentLineIndex(Lines, TimeSpan.FromSeconds(120)));
    }
}
