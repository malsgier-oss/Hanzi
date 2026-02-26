using HanziOverlay.Core.Models;
using HanziOverlay.Core.Services.Stabilization;
using Xunit;

namespace HanziOverlay.Tests;

public class SubtitleStabilizerTests
{
    [Fact]
    public void NormalizeText_TrimsAndCollapsesSpaces()
    {
        Assert.Equal("a b c", SubtitleStabilizer.NormalizeText("  a   b   c  "));
        Assert.Equal("", SubtitleStabilizer.NormalizeText("   "));
        Assert.Equal("你好", SubtitleStabilizer.NormalizeText("  你好  "));
    }

    [Fact]
    public void NormalizeText_HandlesNullOrEmpty()
    {
        Assert.Equal("", SubtitleStabilizer.NormalizeText(null));
        Assert.Equal("", SubtitleStabilizer.NormalizeText(""));
    }

    [Fact]
    public void Stabilizer_EmitsAfterConsecutiveSimilarFrames()
    {
        var stabilizer = new SubtitleStabilizer(requiredConsecutiveFrames: 2, similarityThreshold: 0.8);
        StableSubtitle? received = null;
        stabilizer.StableSubtitleChanged += (_, s) => received = s;

        stabilizer.PushOcrResult(new OcrResult("你好世界", 0.9));
        Assert.Null(received);

        stabilizer.PushOcrResult(new OcrResult("你好世界", 0.9));
        Assert.NotNull(received);
        Assert.Equal("你好世界", received!.CnText);
    }

    [Fact]
    public void Stabilizer_DoesNotEmitForDifferentText()
    {
        var stabilizer = new SubtitleStabilizer(requiredConsecutiveFrames: 2);
        int count = 0;
        stabilizer.StableSubtitleChanged += (_, _) => count++;

        stabilizer.PushOcrResult(new OcrResult("第一行", 0.9));
        stabilizer.PushOcrResult(new OcrResult("第二行", 0.9));
        Assert.Equal(0, count);
    }
}
