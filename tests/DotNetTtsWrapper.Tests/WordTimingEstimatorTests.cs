using DotNetTtsWrapper.Utils;
using Xunit;

namespace DotNetTtsWrapper.Tests;

public class WordTimingEstimatorTests
{
    [Fact]
    public void Estimate_ReturnsEmpty_ForEmptyText()
    {
        var result = WordTimingEstimator.EstimateWordBoundaries("");
        Assert.Empty(result);
    }

    [Fact]
    public void Estimate_ReturnsOnePerWord()
    {
        var result = WordTimingEstimator.EstimateWordBoundaries("one two three");
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Estimate_StartsAtZero()
    {
        var result = WordTimingEstimator.EstimateWordBoundaries("hello world");
        Assert.Equal(0, result[0].StartSeconds, 0.001);
    }

    [Fact]
    public void Estimate_LongerWordsTakeMoreTime()
    {
        var result = WordTimingEstimator.EstimateWordBoundaries("a antidisestablishmentarianism");
        Assert.True(result[1].EndSeconds - result[1].StartSeconds > result[0].EndSeconds - result[0].StartSeconds);
    }

    [Fact]
    public void Estimate_ScalesToActualDuration()
    {
        var result = WordTimingEstimator.EstimateWordBoundaries("one two three four", totalDurationSeconds: 2.0);
        var totalEst = result.Last().EndSeconds;
        Assert.Equal(2.0, totalEst, 0.01);
    }

    [Fact]
    public void Estimate_PreservesCharacterOffsets()
    {
        var text = "hello world";
        var result = WordTimingEstimator.EstimateWordBoundaries(text);
        Assert.Equal(0, result[0].TextOffset);
        Assert.Equal(6, result[1].TextOffset);
        Assert.Equal("hello", text.Substring(result[0].TextOffset, result[0].TextLength));
        Assert.Equal("world", text.Substring(result[1].TextOffset, result[1].TextLength));
    }

    [Fact]
    public void Flat_ReturnsCorrectTiming()
    {
        var result = WordTimingEstimator.EstimateWordBoundariesFlat("one two three");
        Assert.Equal(3, result.Count);
        Assert.Equal(0, result[0].StartSeconds, 0.001);
        Assert.Equal(0.3, result[0].EndSeconds, 0.001);
        Assert.Equal(0.6, result[2].StartSeconds, 0.001);
    }

    [Fact]
    public void Estimate_RespectsWordsPerMinute()
    {
        var slow = WordTimingEstimator.EstimateWordBoundaries("test", wordsPerMinute: 100);
        var fast = WordTimingEstimator.EstimateWordBoundaries("test", wordsPerMinute: 300);
        Assert.True(slow[0].EndSeconds - slow[0].StartSeconds > fast[0].EndSeconds - fast[0].StartSeconds);
    }
}
