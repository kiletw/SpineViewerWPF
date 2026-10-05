# TASK-058 交接文件：Photoshop 風格介面檢視與調整

## 一、背景與設計參考 (Design References)

依據使用者需求，以專業桌面工具為標竿重新調整 `SpineViewerWPF` 介面，以下列設計語言作為意圖參考。TASK-059 校正：`docs/ai/14-ui-product-design.md` 的原工作目錄差異僅有 BOM，未包含原紀錄宣稱新增的參考章節：

1. **Adobe Photoshop (面板組織與圖層底欄)**：
   - 頂部選項列（Options Bar）：命令按鈕具備統一的微框底色與分組分隔線（Separators）。
   - 圖層底欄（Layers Action Footer）：將原先散落在圖層面板底部的 4 行零散按鈕，重組成類似 Photoshop 圖層面板下方的橫向整合工具條。
2. **Adobe After Effects (視口與時間軸優先)**：
   - 視口畫布保持最大且無遮擋。
   - 底部播放列專注於影音傳輸控制，移除混在檢查器面板中的重複設定。
3. **Esoteric Spine 原廠編輯器 (動畫與造型)**：
   - 動畫搜尋框與插槽搜尋框增加清晰直覺的水印提示（Placeholder）與搜尋圖示。
4. **Unity Editor (檢查器分組與卡片分段)**：
   - 右側屬性面板的 3x2 分頁從「易混淆的浮動文字＋位移底線」改造成「清晰的卡片式膠囊分段按鈕（Segmented Tab Cards）」。

---

## 二、介面修改項目 (Visual Changes)

| 面板區塊 | 修改前現況 | 修改後優化 (TASK-058) |
| :--- | :--- | :--- |
| **頂部工具列** | `Save`、`Undo`、`Redo`、`Screenshot`、`Export` 為無框浮動純文字，像網頁超連結。 | `ToolbarButton` 增加微邊框與微暗底色；群組之間增加 1px 垂直分隔線，形成專業桌面軟體的 Options Bar。 |
| **圖層面板底部** | `Add`、`Remove`、`Duplicate`、`↑`、`↓`、`Auto layout` 分成 4 行錯落小方塊，右側大片空白。 | 封裝成圖層清單後的一體化操作卡片（Border + Grid，位於可捲動內容中，並非固定底欄）：<br>• 第 1 行：三等分排列 `[Add layer]` `[Remove]` `[Duplicate]`<br>• 第 2 行：緊湊排列 `[ ↑ ]` `[ ↓ ]` 與填滿寬度的 `[ Auto layout ]`。 |
| **屬性檢查器分頁** | 3x2 標籤頁只有綠色底線，第二排標籤與第一排文字黏在一起，層級視覺混亂。 | 每個 `TabItem` 獨立成微圓角卡片按鈕，選中時亮起 Accent 外框並加粗文字，6 個分頁清爽整齊如控制面板。 |
| **動畫與插槽搜尋框** | 純黑無提示方塊，初次開啟容易困惑。 | 內嵌 `🔍` 搜尋圖示與斜體灰色浮水印提示（`Filter animations`、`Filter slots`），輸入文字時自動隱藏提示。 |
| **時間與播放設定** | `Speed`、`Preview FPS`、`Export FPS` 與重複的 `Loop` 勾選框零散放在動畫列表下方。 | 獨立出 `Timing & Playback` 屬性卡片容器；移除與底部時間軸完全重複的 `[x] Loop` 勾選框。 |
| **底部狀態列** | `Runtime`、`No issues`、`[GPU]`、`Paused` 擠在一起缺少視覺區隔。 | 區塊間加入優雅的暗色垂直分隔線 `|`，標籤與文字垂直居中對齊。 |

---

## 三、保留之行為與相容性 (Preserved Contracts)

- **自動化測試 ID**：全數保留 `scripts/test-ui-shell.ps1` 要求的 85 個 `AutomationProperties.AutomationId`（如 `Main.Asset.AnimationSearch`、`Main.Scene.AddLayer`、`Main.Properties.Tabs` 等）。
- **捷徑與輸入綁定**：Ctrl+O、Ctrl+S、Ctrl+Z、Ctrl+Y、Ctrl+Shift+D、Ctrl+Alt+V、Ctrl+Shift+I 等全數維持原合約。
- **後端與運算邏輯**：GPU/CPU Viewport、專案匯入匯出、多圖層合成、物理演算與 Runtime 相容性完全不變。

---

## 四、原任務記載的驗證結果 (Historical Validation Results)

下列為原交接紀錄，TASK-059 未將其視為本輪驗證證據；本輪結果另見 TASK-059。固定底欄尚未實作，本輪只校正描述，不擴大 UI scope。

1. **編譯**：`dotnet build src/SpineViewerWPF.Wpf/SpineViewerWPF.Wpf.csproj -c Release`（0 警告，0 錯誤）。
2. **應用層 Smoke 測試**：`SpineViewerWPF.Application.Smoke` 執行通過。
3. **UI 自動化測試**：`powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/test-ui-shell.ps1`
   - Build: `passed`
   - AutomationIds: `85`
   - EditorShortcuts: `8`
   - CompactWorkspaceTokens: `24`
   - MenuAndActivityStructure: `passed`
   - DuplicateLayerCommand: `passed`
4. **程式碼檢查**：`git diff --check` 無多餘空格或格式異常。
5. **截圖校驗**：經由視窗繪製（PrintWindow）截圖比對，新版介面層次分明、排版均衡規整。

