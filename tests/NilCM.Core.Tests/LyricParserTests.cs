using NilCM.Core.Lyrics;

namespace NilCM.Core.Tests;

public class LyricParserTests
{
    [Fact]
    public void Parse_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Empty(LyricParser.Parse(null));
        Assert.Empty(LyricParser.Parse(string.Empty));
        Assert.Empty(LyricParser.Parse("   \r\n "));
    }

    [Fact]
    public void Parse_SimpleLine_ParsesTimeAndText()
    {
        var lines = LyricParser.Parse("[00:12.345]hello world");

        var line = Assert.Single(lines);
        Assert.Equal(TimeSpan.FromMilliseconds(12345), line.Time);
        Assert.Equal("hello world", line.Text);
    }

    [Fact]
    public void Parse_MultipleTimeTagsOnOneLine_Expands()
    {
        var lines = LyricParser.Parse("[00:01.000][00:05.500]chorus");

        Assert.Equal(2, lines.Count);
        Assert.Equal(TimeSpan.FromSeconds(1), lines[0].Time);
        Assert.Equal(TimeSpan.FromMilliseconds(5500), lines[1].Time);
        Assert.All(lines, l => Assert.Equal("chorus", l.Text));
    }

    [Fact]
    public void Parse_MetadataAndTextOnlyLines_AreDropped()
    {
        var lines = LyricParser.Parse("[ti:晴天]\n[ar:周杰伦]\njust text\n[00:01.000]real");

        var line = Assert.Single(lines);
        Assert.Equal("real", line.Text);
    }

    [Theory]
    [InlineData(".5", 500)]
    [InlineData(".05", 50)]
    [InlineData(".123", 123)]
    [InlineData("", 0)]
    public void Parse_FractionDigits_AreMilliseconds(string fraction, int expectedMs)
    {
        var lines = LyricParser.Parse($"[00:10{fraction}]x");

        var expected = TimeSpan.FromSeconds(10).Add(TimeSpan.FromMilliseconds(expectedMs));
        Assert.Equal(expected, lines[0].Time);
    }

    [Fact]
    public void Parse_TimeWithoutFraction_Parses()
    {
        var lines = LyricParser.Parse("[01:02]x");

        Assert.Equal(TimeSpan.FromMinutes(1).Add(TimeSpan.FromSeconds(2)), lines[0].Time);
    }

    [Fact]
    public void Parse_PositiveOffset_ShiftsLyricsEarlier()
    {
        var lines = LyricParser.Parse("[offset:+500]\n[00:10.000]x");

        Assert.Equal(TimeSpan.FromMilliseconds(9500), lines[0].Time);
    }

    [Fact]
    public void Parse_NegativeOffset_ShiftsLyricsLater()
    {
        var lines = LyricParser.Parse("[offset:-1000]\n[00:10.000]x");

        Assert.Equal(TimeSpan.FromSeconds(11), lines[0].Time);
    }

    [Fact]
    public void Parse_OffsetClampsNegativeTimesToZero()
    {
        var lines = LyricParser.Parse("[offset:20000]\n[00:01.000]x");

        Assert.Equal(TimeSpan.Zero, lines[0].Time);
    }

    [Fact]
    public void Parse_UnsortedInput_ReturnsSortedByTime()
    {
        var lines = LyricParser.Parse("[00:20.000]b\n[00:03.000]a");

        Assert.Equal(TimeSpan.FromSeconds(3), lines[0].Time);
        Assert.Equal("a", lines[0].Text);
    }
}
