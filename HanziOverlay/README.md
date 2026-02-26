# HanziOverlay

Windows-only desktop overlay for real-time Chinese subtitle translation (Pinyin + English). Reads pixels from a user-selected screen region; no browser injection.

## Prerequisites

- Windows 10 (19041+) or Windows 11
- .NET 8 Desktop Runtime
- Chinese (Simplified) language pack (for OCR)

## Installation

1. Build or download release.
2. Run `HanziOverlay.App.exe`.

## Usage

1. Press **Ctrl+Alt+S** (or click Select Region) to choose the subtitle area.
2. Overlay shows Pinyin and English; Chinese is toggleable.
3. Click a line to copy; right-click for menu.

## Hotkeys

| Hotkey | Action |
|--------|--------|
| Ctrl+Alt+S | Select subtitle region |
| Ctrl+Alt+P | Pause/Resume capture |
| Ctrl+Alt+Space | Freeze/Unfreeze current line |
| Ctrl+Alt+C | Toggle Chinese line |
| Ctrl+Alt+K | Save current line |
| Ctrl+Alt+E | Export saved lines to CSV |

## Troubleshooting

- **OCR not detecting**: Install Chinese (Simplified) language pack; ensure region has good contrast.
- **Flickering**: Lower FPS in settings.
- **Cloud timeout**: Check network; increase timeout in settings.

## Known limitations

- Windows only. No auto-region detection. Cloud translation requires internet.

## Data storage

Saved lines: `%AppData%\HanziOverlay\saved_lines.json`
