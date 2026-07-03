# 虛旅語境 (Virtual Journey Context)

## 📖 專案簡介

**虛旅語境** 是一款以 Unity 為核心開發的互動式 3D 體驗/遊戲，結合了多種角色模型、服裝、髮型與情緒動畫資源，讓使用者可以在虛擬環境中探索、互動與自訂角色外觀。專案同時提供了 Python 相關腳本與素材，用於前處理、動畫匯出或工具輔助。

## 📂 目錄結構說明

```
.
├─ 報告相關/                # 專題報告與簡報
│   ├─ 專題海報.pdf
│   ├─ 專題簡報_初賽完整版.pptx
│   ├─ 專題簡報_完整版PDF.pdf
│   ├─ 專題簡報_決賽刪減版.pptx
│   ├─ 書面報告.pdf
│   └─ 預覽版/            # 可能的報告預覽檔案
├─ 遊戲_相關資源/           # 非 Unity 部分的資源與腳本
│   ├─ animation/            # 動畫檔案（Blender等）
│   ├─ app.py/               # Python 應用程式（可能的工具腳本）
│   ├─ background/           # 背景圖、素材
│   ├─ clothes/              # 服裝模型與貼圖
│   ├─ emotion.blend、emotion.blend1  # Blender 情緒模型檔案
│   ├─ female/               # 女性角色模型與資源
│   ├─ hair/                 # 髮型模型與貼圖
│   ├─ male/                 # 男性角色模型與資源
│   └─ word/                 # 文字相關素材
├─ 遊戲_虛旅語境/           # Unity 專案根目錄
│   ├─ .gitignore
│   ├─ .plastic/              # Plastic SCM 相關檔案
│   ├─ .vs/                  # VS 設定目錄
│   ├─ .vsconfig
│   ├─ Assembly-CSharp-Editor.csproj
│   ├─ Assembly-CSharp.csproj
│   ├─ Assets/               # Unity 資產（模型、音效、腳本等）
│   ├─ Builds/               # 已編譯的執行檔與套件
│   ├─ Library/              # Unity 自動產生的暫存庫
│   ├─ Logs/                 # 執行日誌
│   ├─ Packages/             # Unity 套件管理目錄
│   ├─ ProjectSettings/      # Unity 專案設定檔
│   ├─ Temp/                 # 暫存檔案
│   ├─ UserSettings/         # 使用者自訂設定
│   ├─ ignore.conf
│   ├─ obj/                  # 編譯產出的目錄
│   └─ 遊戲_虛旅語境.sln     # Visual Studio 解決方案檔
└─ README.md                # 本說明文件
```

## 🛠️ 使用技術棧

- **Unity (2021+ 版本) –** 主要遊戲引擎與編輯環境。
- **C# –** Unity 內的腳本語言。
- **Blender –** 用於製作角色動畫與情緒模型（`emotion.blend`）。
- **Python –** 提供輔助腳本（`app.py`）可用於資源前處理或自動化工具。
- **Git –** 版本控制。
- **Visual Studio / Rider –** C# 開發 IDE（可選）。

## 🚀 安裝與執行步驟

1. **克隆倉庫**
   ```bash
   git clone https://github.com/liouyutong/CS_Project.git
   cd CS_Project
   ```
2. **開啟 Unity 專案**
   - 使用 Unity Hub，點選 `Add` → 選擇 `遊戲_虛旅語境` 資料夾（即 `CS_Project/遊戲_虛旅語境`）。
   - 等待 Unity 產生 `Library` 等資料夾（首次開啟會較久）。
3. **執行遊戲**
   - 在 Unity 編輯器中點選 `Play` 按鈕即可開始體驗。

## 📄 報告與簡報

`報告相關` 資料夾內包含完整的專題海報、PowerPoint 簡報（初賽、決賽版本）以及 PDF 書面報告，供評審與展示使用。