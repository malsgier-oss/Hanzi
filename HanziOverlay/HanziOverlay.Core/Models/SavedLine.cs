namespace HanziOverlay.Core.Models;

public record SavedLine(
    DateTime Timestamp,
    string CN,
    string Pinyin,
    string EN,
    double Confidence);
