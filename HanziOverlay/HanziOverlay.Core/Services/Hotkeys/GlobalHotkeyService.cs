using System.Runtime.InteropServices;

namespace HanziOverlay.Core.Services.Hotkeys;

public class GlobalHotkeyService : IGlobalHotkeyService
{
    private const int WM_HOTKEY = 0x0312;

    private static class Modifiers
    {
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly Dictionary<int, HotkeyAction> _idToAction = new();
    private IntPtr _hwnd;
    private bool _registered;

    public event EventHandler<HotkeyAction>? HotkeyPressed;

    public void Register(IntPtr windowHandle)
    {
        if (_registered)
            Unregister();

        _hwnd = windowHandle;

        RegisterOne(1, Modifiers.MOD_CONTROL | Modifiers.MOD_ALT, 0x53, HotkeyAction.SelectRegion);      // S
        RegisterOne(2, Modifiers.MOD_CONTROL | Modifiers.MOD_ALT, 0x50, HotkeyAction.PauseResume);       // P
        RegisterOne(3, Modifiers.MOD_CONTROL | Modifiers.MOD_ALT, 0x20, HotkeyAction.FreezeUnfreeze);   // Space
        RegisterOne(4, Modifiers.MOD_CONTROL | Modifiers.MOD_ALT, 0x43, HotkeyAction.ToggleChinese);     // C
        RegisterOne(5, Modifiers.MOD_CONTROL | Modifiers.MOD_ALT, 0x4B, HotkeyAction.SaveLine);          // K
        RegisterOne(6, Modifiers.MOD_CONTROL | Modifiers.MOD_ALT, 0x45, HotkeyAction.ExportSaved);        // E

        _registered = true;
    }

    private void RegisterOne(int id, uint mods, uint vk, HotkeyAction action)
    {
        if (RegisterHotKey(_hwnd, id, mods, vk))
            _idToAction[id] = action;
    }

    public void Unregister()
    {
        if (_hwnd == IntPtr.Zero) return;
        foreach (int id in _idToAction.Keys)
            UnregisterHotKey(_hwnd, id);
        _idToAction.Clear();
        _registered = false;
    }

    /// <summary>
    /// Call from WPF HwndSource.AddHook when processing messages. Returns true if the message was WM_HOTKEY and handled.
    /// </summary>
    public bool ProcessMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, out bool handled)
    {
        handled = false;
        if (msg != WM_HOTKEY) return false;
        int id = wParam.ToInt32();
        if (!_idToAction.TryGetValue(id, out HotkeyAction action)) return false;
        handled = true;
        HotkeyPressed?.Invoke(this, action);
        return true;
    }
}
