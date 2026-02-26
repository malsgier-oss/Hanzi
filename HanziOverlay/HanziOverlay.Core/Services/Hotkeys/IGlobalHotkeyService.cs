namespace HanziOverlay.Core.Services.Hotkeys;

public enum HotkeyAction
{
    SelectRegion,
    PauseResume,
    FreezeUnfreeze,
    ToggleChinese,
    SaveLine,
    ExportSaved
}

public interface IGlobalHotkeyService
{
    event EventHandler<HotkeyAction>? HotkeyPressed;
    void Register(IntPtr windowHandle);
    void Unregister();
}
