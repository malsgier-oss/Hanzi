using HanziOverlay.Core.Models;

namespace HanziOverlay.Core.Services.Persistence;

public interface ISavedLineStore
{
    void AddLine(SavedLine line);
    IReadOnlyList<SavedLine> GetAll();
    void ExportToCsv(string filePath);
}
