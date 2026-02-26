# HanziOverlay

Windows-only desktop overlay for real-time Chinese subtitle translation. Shows **Pinyin** and **English** (and optional **Chinese**) for text captured from a user-selected screen region. Works over any website or player (e.g. YouTube, VLC) by reading pixels—no browser injection.

## Features

- **Always-on-top overlay**: movable, resizable, adjustable opacity
- **Screen region capture**: select a rectangle (e.g. subtitle area) with Ctrl+Alt+S
- **OCR**: Windows built-in OCR (Chinese Simplified); requires language pack
- **Stabilization**: avoids flicker by committing text only after it is stable across frames
- **Pinyin**: tone marks via NPinyin
- **Translation**: offline placeholder by default; optional cloud (LibreTranslate or OpenAI-compatible) with async replace
- **Copy**: click any line to copy; right-click for Copy All, Save, Toggle Chinese, Opacity
- **Freeze (Study mode)**: freeze current line with Ctrl+Alt+Space; Save and Export from control window

## Prerequisites

- **Windows 10** (build 19041+) or **Windows 11**
- **.NET 8 Desktop Runtime**  
  - [Download](https://dotnet.microsoft.com/download/dotnet/8.0) — install "Desktop Runtime"
- **Chinese (Simplified) OCR** — see [Installing Chinese OCR](#installing-chinese-ocr) below.

## Installing Chinese OCR

HanziOverlay uses Windows’ built-in OCR. You must install **Chinese (Simplified)** so that text recognition works.

### Windows 11

1. Press **Win + I** to open **Settings**.
2. Go to **Time & language** → **Language & region**.
3. Under **Preferred languages**, click **Add a language**.
4. Search for **Chinese** and choose **中文(简体)** (Chinese Simplified). Click **Next**.
5. Check **Install language pack** (and **Text-to-speech** if you want speech). Click **Install**.
6. Wait for the download and install to finish.
7. (Optional) In **Preferred languages**, click the **⋯** next to 中文(简体) → **Language options** → under **Recognize text in images**, ensure it’s available (OCR uses the same language data).
8. **Restart HanziOverlay** (or restart the PC if the app still shows “OCR: Not available”).

### Windows 10

1. Press **Win + I** to open **Settings**.
2. Go to **Time & language** → **Language**.
3. Under **Preferred languages**, click **Add a language**.
4. Choose **中文(简体)** (Chinese Simplified) and click **Next**, then **Install**.
5. Wait for the language pack to install.
6. **Restart HanziOverlay** (or restart the PC if needed).

### Check that it worked

- Open HanziOverlay. In the control window, under the buttons it should say **OCR: Ready (Chinese)**.
- If it still says **OCR: Not available**, restart the app or the PC and try again.

## Installation

1. Clone or download this repo.
2. Build (see below) or use a pre-built release.
3. Run `HanziOverlay.App.exe` (or `dotnet run --project HanziOverlay.App`).

## Build (Visual Studio / .NET CLI)

```powershell
cd HanziOverlay
dotnet restore HanziOverlay.sln
dotnet build HanziOverlay.sln --configuration Release
```

Run (from the `HanziOverlay` folder you must pass `--project` because the folder contains the solution, not a single project):

```powershell
dotnet run --project HanziOverlay.App\HanziOverlay.App.csproj
```

Or open `HanziOverlay.sln` in Visual Studio 2022 and press F5.

## First-time setup

1. Launch the app. You’ll see the **control window** and the **overlay** (semi-transparent).
2. **Select region**: press **Ctrl+Alt+S** (or click "Select Region"). A full-screen dimmed overlay appears; **drag a rectangle** over the area where subtitles appear (e.g. bottom of the video). Release to confirm; press **ESC** to cancel.
3. Capture starts automatically. Point the overlay at the subtitle area; the app will OCR and show Pinyin + English.
4. (Optional) Enable **Cloud translation** in the control window and configure endpoint/API key (see below).

## Usage

- **Overlay**: drag to move; resize from corner; use right-click menu for Copy All, Save, Toggle Chinese, Opacity.
- **Click a line** (Pinyin, English, or Chinese) to copy that line to the clipboard.
- **Freeze**: Ctrl+Alt+Space to freeze the current line (study mode); capture continues but the overlay doesn’t update until you unfreeze.
- **Save**: Ctrl+Alt+K or Save button saves the current line to `%AppData%\HanziOverlay\saved_lines.json`.
- **Export**: Ctrl+Alt+E or Export button exports all saved lines to a CSV file (Timestamp, Chinese, Pinyin, English, Confidence).
- **History**: last 30 stable lines appear in the control window; click one to show it on the overlay again.

## Hotkeys

| Hotkey           | Action                    |
|------------------|---------------------------|
| Ctrl+Alt+S       | Select subtitle region    |
| Ctrl+Alt+P       | Pause / Resume capture    |
| Ctrl+Alt+Space   | Freeze / Unfreeze line    |
| Ctrl+Alt+C       | Toggle Chinese line       |
| Ctrl+Alt+K       | Save current line         |
| Ctrl+Alt+E       | Export saved lines to CSV |

## Cloud translation (optional)

- **Off by default**. When off, the English line shows "(translation offline)".
- **LibreTranslate** (no API key for public endpoint):  
  - Check "Cloud translation" in the control window.  
  - Default endpoint: `https://libretranslate.com/translate`.  
  - Rate limits may apply on the public server.
- **OpenAI-compatible** (API key required):  
  - Use an endpoint that accepts OpenAI-style chat completions (e.g. OpenAI, Azure OpenAI, or compatible APIs).  
  - Configure endpoint and API key in settings (stored in `%AppData%\HanziOverlay\settings.json`).  
  - **Do not hardcode API keys**; the app reads from settings. Keys are stored in plain text—keep the settings file private.

(Endpoint and API key are persisted in settings; UI for editing them can be added in a future version.)

## Troubleshooting

- **OCR not detecting text**  
  - Install **Chinese (Simplified)** language/OCR (see Prerequisites).  
  - Ensure the selected region has clear, high-contrast text.  
  - Try a slightly larger region or different FPS (3 / 6 / 10).

- **Flickering or unstable text**  
  - Lower **FPS** (e.g. 3) in the control window.  
  - The stabilizer requires the same text for 2+ frames or high confidence; noisy OCR can still cause some flicker.

- **Cloud translation fails or times out**  
  - Check internet and firewall.  
  - For LibreTranslate, the public endpoint may be rate-limited.  
  - Increase timeout in settings if needed (default 5 s).

- **Overlay not on top**  
  - The overlay is created as Topmost; if another fullscreen app captures focus, move the overlay or use Alt+Tab and click the overlay once.

## Known limitations

- **Windows only**; .NET 8 WPF.
- **No auto region detection**; you must select the subtitle area once.
- **No accounts or telemetry**; all processing is local except optional cloud translation.
- **Cloud translation** requires internet and (for OpenAI-style) an API key.
- **OCR** depends on Windows language packs; some environments may require MSIX or different setup.

## Data storage

- **Saved lines**: `%AppData%\HanziOverlay\saved_lines.json`
- **Settings**: `%AppData%\HanziOverlay\settings.json`  
  (FPS, opacity, cloud toggle, capture region, etc.)

Export path is chosen by the user when using Export (Ctrl+Alt+E).
