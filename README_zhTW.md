# SpineViewerWPF

SpineViewerWPF v3 是 Windows/.NET 8 的 Spine 資產檢視器，可讀取多個歷史
Runtime 版本匯出的檔案。

[English](README.md)

## 功能

- 透過隔離的 Runtime Adapter，自動偵測並載入 `2.1.08` 到 `4.3` 的 Spine
  匯出檔。
- 一般 RGBA 播放使用 OpenTK GPU viewport；GPU 初始化或渲染失敗時會保留
  資產並切換為 CPU fallback。
- Screenshot、色彩通道檢視、CLI 輸出與 PNG 序列匯出使用可重現的 CPU
  renderer。
- 可透過檔案對話框或拖放開啟 JSON／binary skeleton，自動尋找 atlas，找不到
  時可手動指定。
- 提供動畫／Skin、播放速度、預覽 FPS、平移／縮放／Fit、多場景圖層、
  Dark／Light theme 與 GPU／CPU 效能狀態。
- 無動畫資產顯示 setup pose；仍可選 Skin、建立圖層、儲存 sidecar、截圖及
  匯出單幀靜態 PNG。
- 同一工作區 session 內，依完整素材路徑記住動畫／Skin；sidecar 明確選擇優先，
  不建立跨啟動的設定快取。
- 圖層、Transform、Slot 顯示／透明度及具名 Attachment 選擇會以非破壞方式
  儲存在版本化的 `*.spineviewer.json` sidecar。
- 桌面版可將目前動畫匯出為 GIF、動態 WebP、APNG 或 MP4（H.264），以使用者
  自行安裝的 [FFmpeg](https://ffmpeg.org/) 編碼同一組可重現的畫格。

支援的 Runtime 選項：

```text
2.1.08  2.1.25  3.1.07  3.2.xx  3.4.02  3.5.51  3.6.32
3.6.39  3.6.53  3.7.94  3.8.95  4.0.31  4.0.64  4.1  4.2  4.3
```

## 下載

Windows x64 免安裝版發布於 [Releases](https://github.com/kiletw/SpineViewerWPF/releases)
頁面。解壓縮後執行 `SpineViewerWPF.exe`，不需要另外安裝 .NET。執行檔尚未
程式碼簽章，第一次執行時 SmartScreen 可能會顯示警告。

SpineViewerWPF 整合了 Spine Runtimes：每位使用者都必須自行取得
[Spine Editor 授權](https://esotericsoftware.com/spine-editor-license)。詳見
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。

## 系統需求

- Windows
- 建置：.NET SDK 9.0.300 或之後的 9.0 feature band（由 `global.json` 固定；
  專案目標為 .NET 8），從原始碼執行需要
  [.NET 8 runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

互動播放建議使用支援 OpenGL 的 GPU。程式會顯示目前使用 GPU 或 CPU
fallback。

## 專案入口

- WPF 程式：`src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj`
- CLI：`src/SpineViewerWPF.Cli/SpineViewerWPF.Cli.csproj`
- Application／Core contract：`src/SpineViewerWPF.Application`、
  `src/SpineViewerWPF.Core`
- 隔離的 Runtime Adapter：`runtimes/SpineRuntime.*`

## 建置與執行

```powershell
dotnet restore SpineViewerWPF.sln
dotnet build SpineViewerWPF.sln -c Release --no-restore
dotnet run --project src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj -c Release --no-restore
```

CLI 範例：

```powershell
dotnet run --project src/SpineViewerWPF.Cli/SpineViewerWPF.Cli.csproj -c Release -- inspect "asset.skel" --atlas "asset.atlas" --format json
dotnet run --project src/SpineViewerWPF.Cli/SpineViewerWPF.Cli.csproj -c Release -- render "asset.skel" --atlas "asset.atlas" --animation "idle" --time 0.5 --output "frame.png" --overwrite
```

需要強制指定 Runtime 時可加入 `--runtime`；省略時會使用匯出檔內的版本資訊
自動選擇。無動畫資產可省略 `render` 的 `--animation` 以輸出 setup pose
（仍需指定 `--time`）；有動畫的資產仍需提供具名動畫。

## 驗證

```powershell
dotnet run --project tests/SpineViewerWPF.Application.Smoke/SpineViewerWPF.Application.Smoke.csproj -c Release --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v3.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v42.ps1 -Offline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-v43.ps1 -Offline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1
```

3.8、4.1、4.2 與 4.3 的離線相容性腳本使用 gitignored 的官方範例 cache。若 cache
尚未建立，先將對應腳本移除 `-Offline` 執行一次。

GitHub Actions 會在每個 pull request 與推送到 `master` 時執行建置、
Application.Smoke 與 `scripts/test-v3.ps1`。

## 發布

在 `master` 的 commit 上推送 SemVer tag 即會自動發布 GitHub Release：

```powershell
git tag v3.0.0-alpha.1
git push origin v3.0.0-alpha.1
```

帶有 `-alpha.1` 等後綴的 tag 會標記為 prerelease。要在本機產生相同的套件
（輸出到 `artifacts/release`）：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/publish-release.ps1 -Version 3.0.0-local.1
```

## 目前限制

- Runtime 4.3 已接入並有固定版本的官方 JSON／binary fixture 驗證，並非全面相容
  宣稱。官方 4.2／4.3 的 PMA、多頁 atlas、個別 blend parity 與長時間 Physics
  仍有限制或未驗證；詳見[相容性矩陣](docs/ai/04-runtime-matrix.md)。
- Spine JSON、binary、atlas 與 texture 原始檔皆為唯讀；Save／Save As 儲存的是
  Viewer sidecar，不會修改 Spine 原始檔。
- 可輸出 Screenshot、可重現的 PNG 序列，以及（僅桌面版）GIF、WebP、APNG 與
  MP4。尚未實作 PSD 匯出，CLI 也還不能輸出這些編碼格式。
- GIF、WebP、APNG 與 MP4 匯出需要 FFmpeg，程式不內附。會優先使用在 Export
  設定中選擇的 FFmpeg（跨啟動記住），否則使用 `PATH` 上的 `ffmpeg`；找不到時
  匯出會提示如何設定。WebP 需要含 `libwebp`、MP4 需要含 `libx264` 的 FFmpeg
  版本。
- Screenshot 與所有序列／動畫匯出都包含依序合成的可見圖層、Transform、透明度
  及 Slot 設定。Viewport 背景不寫入輸出；全部圖層隱藏時輸出透明畫面。GIF、
  WebP 與 APNG 保留透明並無限循環；GIF 每幀時間以 1/100 秒為單位取整。MP4
  不支援透明，會合成在選定的背景色（預設黑色）上，並補齊為偶數寬高。
- 多 Track 動畫混合、Attachment 製作、完整 Adobe 式拖曳 Dock、執行期間語言
  切換及 MCP 工具仍為 deferred。

官方 Spine Runtime 原始碼的授權與 commit metadata 保留在 `runtimes/`。
