using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.Storage;
using MeowTextReader.Repo.Chapter;
using MeowTextReader.Repo.Model;

namespace MeowTextReader.Repo
{
    public class MainRepo
    {
        private static readonly Lazy<MainRepo> _instance = new(() => new MainRepo());
        public static MainRepo Instance => _instance.Value;

        private readonly string _saveFilePath;
        private AppConfig _config = new();

        private MainRepo()
        {
            _saveFilePath = GetSaveFilePath();
            LoadConfig();
        }

        private static string GetSaveFilePath()
        {
            // 打包(MSIX)應用程式呼叫 Environment.GetFolderPath(LocalApplicationData) 時，
            // 系統會將路徑重新導向到套件的虛擬容器內，這個路徑只有本行程看得到；
            // 外部程式（例如記事本）用同一個字串路徑打開時會找不到檔案。
            // Windows.Storage.ApplicationData.Current.LocalFolder.Path 回傳的才是實體、
            // 外部程式也能存取到的路徑（%LOCALAPPDATA%\Packages\<PackageFamilyName>\LocalState），
            // 所以優先使用它；未打包執行（例如單元測試、非 MSIX 部署）時再退回原本的方式。
            string folder;
            try
            {
                folder = ApplicationData.Current.LocalFolder.Path;
            }
            catch
            {
                folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }
            string appFolder = Path.Combine(folder, "MeowTextReader");
            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, "appConfig.json");
        }

        /// <summary>設定 json 檔案的完整路徑，供外部（例如以編輯器打開）使用。</summary>
        public string SaveFilePath => _saveFilePath;

        public string? FolderPath
        {
            get => _config.FolderPath;
            set
            {
                if (_config.FolderPath != value)
                {
                    _config.FolderPath = value;
                    SaveConfig();
                }
            }
        }

        public string? OpenFilePath
        {
            get => _config.OpenFilePath;
            set
            {
                if (_config.OpenFilePath != value)
                {
                    _config.OpenFilePath = value;
                    SaveConfig();
                }
            }
        }

        public AppPage LastPage
        {
            get => _config.LastPage ?? AppPage.MainPage;
            set
            {
                if (LastPage != value)
                {
                    _config.LastPage = value;
                    SaveConfig();
                }
            }
        }

        private ReaderSetting _readerSettingCache => _config.ReaderSetting ??= new ReaderSetting();
        public ReaderSetting ReaderSettingObj
        {
            get => _readerSettingCache;
            set
            {
                _config.ReaderSetting = value;
                SaveConfig();
            }
        }
        public double FontSize
        {
            get => _readerSettingCache.FontSize;
            set
            {
                if (_readerSettingCache.FontSize != value)
                {
                    _readerSettingCache.FontSize = value;
                    SaveConfig();
                    ReaderSettingChanged?.Invoke();
                }
            }
        }

        // 下限不設 1.0：行高等於字級時中文字的上下會被裁掉。
        public const double MinLineSpacing = 1.2;
        public const double MaxLineSpacing = 3.0;
        public const double LineSpacingStep = 0.1;
        public const double DefaultLineSpacing = 1.5;

        public double LineSpacing
        {
            get
            {
                // 舊設定檔沒有這個欄位時可能讀到 0，視為未設定。
                var value = _readerSettingCache.LineSpacing;
                return value > 0 ? value : DefaultLineSpacing;
            }
            set
            {
                var clamped = Math.Round(Math.Clamp(value, MinLineSpacing, MaxLineSpacing), 1);
                if (_readerSettingCache.LineSpacing != clamped)
                {
                    _readerSettingCache.LineSpacing = clamped;
                    SaveConfig();
                    ReaderSettingChanged?.Invoke();
                }
            }
        }

        public static event Action? ReaderSettingChanged;

        /// <summary>
        /// 全域章節抓取設定。單一檔案（HistoryItem）沒有專屬設定時，以此為預設值。
        /// </summary>
        public ChapterRegexSetting GlobalChapterSetting
        {
            get => _config.ChapterSetting ??= new ChapterRegexSetting();
            set
            {
                _config.ChapterSetting = value ?? new ChapterRegexSetting();
                SaveConfig();
            }
        }

        /// <summary>
        /// 取得指定檔案生效中的章節抓取設定：檔案關掉「使用預設值」且填過自己的設定時用它，
        /// 否則退回全域預設值。
        /// </summary>
        public ChapterRegexSetting GetChapterSetting(string? fileName)
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                var history = GetHistoryItem(fileName);
                if (history != null && !history.UseDefaultChapterSetting && history.ChapterSetting != null)
                    return history.ChapterSetting;
            }
            return GlobalChapterSetting;
        }

        /// <summary>
        /// 取得這個檔案「已經存起來」的章節抓取設定，不管「使用預設值」開關的狀態。
        /// 給 ChapterSettingDialog 初始化編輯畫面用：即使目前開著「使用預設值」，
        /// 使用者之前填過的自訂設定也要顯示出來，不能被開關狀態蓋掉。
        /// </summary>
        public ChapterRegexSetting GetStoredChapterSetting(string? fileName)
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                var history = GetHistoryItem(fileName);
                if (history?.ChapterSetting != null) return history.ChapterSetting;
            }
            return GlobalChapterSetting;
        }

        /// <summary>
        /// 寫入指定檔案專屬的章節抓取設定，並關閉這個檔案的「使用預設值」開關。
        /// </summary>
        public void SetChapterSetting(string fileName, ChapterRegexSetting setting)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            var item = _config.History.FirstOrDefault(h => h.FileName == fileName);
            if (item == null)
            {
                item = new HistoryItem { FileName = fileName };
                _config.History.Add(item);
            }
            item.ChapterSetting = setting;
            item.UseDefaultChapterSetting = false;
            SaveConfig();
        }

        /// <summary>
        /// 切換指定檔案的「使用預設值」開關，不動到底下已經存的自訂設定資料，
        /// 之後關掉開關還能拿回原本填的值。
        /// </summary>
        public void SetChapterSettingUseDefault(string fileName, bool useDefault)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            var item = _config.History.FirstOrDefault(h => h.FileName == fileName);
            if (item == null)
            {
                if (useDefault) return; // 本來就沒紀錄，等於已經在使用預設值，不必新增一筆空的 HistoryItem
                item = new HistoryItem { FileName = fileName };
                _config.History.Add(item);
            }

            if (item.UseDefaultChapterSetting == useDefault) return;
            item.UseDefaultChapterSetting = useDefault;
            SaveConfig();
        }

        private const char CacheKeySeparator = (char)1;

        /// <summary>
        /// 章節快取的有效性條件：Regex 清單 + 標題字數上限 + 起始行數 + 檔案行數。
        /// 任一改變就代表舊的章節資料不能用了。
        /// </summary>
        public static string BuildChapterCacheKey(
            IEnumerable<string> patterns, int titleMaxLength, int skipLines, int lineCount)
        {
            // 分隔符用不會出現在 Regex 設定裡的控制字元，避免不同清單湊出同一把 key。
            return string.Join(CacheKeySeparator, patterns)
                   + CacheKeySeparator + titleMaxLength
                   + CacheKeySeparator + skipLines
                   + CacheKeySeparator + lineCount;
        }

        /// <summary>
        /// 記下掃描出來的章節，下次開同一個檔案直接取用。
        /// </summary>
        public void UpdateChapters(string fileName, List<ChapterItem> chapters, string cacheKey)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            var item = _config.History.FirstOrDefault(h => h.FileName == fileName);
            if (item == null)
            {
                item = new HistoryItem { FileName = fileName };
                _config.History.Add(item);
            }

            item.Chapters = chapters;
            item.ChapterCacheKey = cacheKey;
            SaveConfig();
        }

        public List<HistoryItem> History => _config.History;

        /// <summary>
        /// 記錄閱讀位置。以行索引為錨點，不再儲存像素偏移量。
        /// readLines/totalLines 只用來算閱讀進度，不影響位置還原。
        /// </summary>
        public void UpdateHistory(string fileName, int lineIndex, double lineFraction, int readLines, int totalLines)
        {
            var item = _config.History.FirstOrDefault(h => h.FileName == fileName);
            if (item == null)
            {
                item = new HistoryItem { FileName = fileName };
                _config.History.Add(item);
            }
            else if (item.LineIndex == lineIndex && Math.Abs(item.LineFraction - lineFraction) < 0.01
                     && item.ReadLines == readLines && item.TotalLines == totalLines)
            {
                return; // 位置沒有實質變化，不需要重寫設定檔
            }

            item.LineIndex = lineIndex;
            item.LineFraction = lineFraction;
            item.ReadLines = readLines;
            item.TotalLines = totalLines;
            item.ScrollOffset = 0; // 舊欄位已遷移完成
            SaveConfig();
        }

        /// <summary>
        /// 舊紀錄沒有行數資訊，由 MainPage 實際數過檔案後補寫回來，
        /// 之後就不必每次進資料夾都重數一次。
        /// </summary>
        public void BackfillHistoryLineCounts(string fileName, int readLines, int totalLines)
        {
            if (totalLines <= 0) return;

            var item = _config.History.FirstOrDefault(h => h.FileName == fileName);
            if (item == null) return;
            if (item.ReadLines == readLines && item.TotalLines == totalLines) return;

            item.ReadLines = readLines;
            item.TotalLines = totalLines;
            SaveConfig();
        }

        public HistoryItem? GetHistoryItem(string fileName)
        {
            return _config.History.FirstOrDefault(h => h.FileName == fileName);
        }

        /// <summary>
        /// 記錄某個資料夾在 MainPage 的瀏覽位置。
        /// </summary>
        public void UpdateFolderScrollPosition(string folderPath, int itemIndex, double horizontalOffset)
        {
            if (string.IsNullOrEmpty(folderPath)) return;

            var item = FindFolderScrollItem(folderPath);
            if (item == null)
            {
                item = new FolderScrollHistoryItem { FolderPath = folderPath };
                _config.FolderScrollPositions.Add(item);
            }
            else if (item.ItemIndex == itemIndex && Math.Abs(item.HorizontalOffset - horizontalOffset) < 1.0)
            {
                return; // 位置沒有實質變化，不需要重寫設定檔
            }

            item.ItemIndex = itemIndex;
            item.HorizontalOffset = horizontalOffset;
            SaveConfig();
        }

        public FolderScrollHistoryItem? GetFolderScrollPosition(string folderPath)
        {
            return string.IsNullOrEmpty(folderPath) ? null : FindFolderScrollItem(folderPath);
        }

        private FolderScrollHistoryItem? FindFolderScrollItem(string folderPath)
        {
            return _config.FolderScrollPositions.FirstOrDefault(
                f => string.Equals(f.FolderPath, folderPath, StringComparison.OrdinalIgnoreCase));
        }

        public WindowPlacement? WindowPlacement
        {
            get => _config.WindowPlacement;
            set
            {
                var current = _config.WindowPlacement;
                if (current != null && value != null
                    && current.X == value.X && current.Y == value.Y
                    && current.Width == value.Width && current.Height == value.Height
                    && current.IsMaximized == value.IsMaximized)
                {
                    return; // 沒有實質變化，不需要重寫設定檔
                }

                _config.WindowPlacement = value;
                SaveConfig();
            }
        }

        public void SetOpenFilePath(string path)
        {
            OpenFilePath = path;
        }

        public void SetBackgroundColor(string? color, bool useCustom)
        {
            if (useCustom) {
                _readerSettingCache.CustomBackgroundColor = color;
            }
            _readerSettingCache.UseCustomBackgroundColor = useCustom;
            SaveConfig();
            ReaderSettingChanged?.Invoke();
        }

        public void SetForegroundColor(string? color, bool useCustom)
        {
            if (useCustom) {
                _readerSettingCache.CustomForegroundColor = color;
            }
            _readerSettingCache.UseCustomForegroundColor = useCustom;
            SaveConfig();
            ReaderSettingChanged?.Invoke();
        }

        private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private static readonly JsonSerializerOptions SaveJsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            // 預設編碼器會將非 ASCII 字元（例如中文）跳脫成 \uXXXX，
            // 這裡改用 UnsafeRelaxedJsonEscaping 讓設定檔顯示真實字元，方便使用者用編輯器檢視/編輯。
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            // LastPage 用列舉名稱（例如 "MainPage"）存檔，方便使用者直接看懂/編輯設定檔。
            Converters = { new JsonStringEnumConverter() }
        };

        private void SaveConfig()
        {
            var json = JsonSerializer.Serialize(_config, SaveJsonOptions);
            File.WriteAllText(_saveFilePath, json, Utf8NoBom);
        }

        private void LoadConfig()
        {
            if (File.Exists(_saveFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_saveFilePath, Utf8NoBom);
                    _config = JsonSerializer.Deserialize<AppConfig>(json, SaveJsonOptions) ?? new AppConfig();
                }
                catch
                {
                    HandleCorruptConfig();
                }
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

        private const uint MB_YESNO = 0x00000004;
        private const uint MB_ICONWARNING = 0x00000030;
        private const int IDYES = 6;

        /// <summary>
        /// 設定檔壞掉、解析失敗時的處理。這裡發生在 MainRepo 建構子（App 啟動早期），
        /// 還沒有視窗可以掛 ContentDialog，所以改用原生 MessageBox 同步詢問。
        /// 選「是」就覆蓋成全新預設值；選「否」則打開設定檔讓使用者自行修正，並直接關閉程式，
        /// 避免任何一方的資料被覆蓋掉。
        /// </summary>
        private void HandleCorruptConfig()
        {
            int result = MessageBoxW(
                IntPtr.Zero,
                "設定檔讀取失敗，可能已損毀。\n\n選「是」會覆蓋成全新的預設設定；\n選「否」會用編輯器開啟設定檔，並關閉本程式讓你自行修正。",
                "設定檔錯誤",
                MB_YESNO | MB_ICONWARNING);

            if (result == IDYES)
            {
                _config = new AppConfig();
                SaveConfig();
            }
            else
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _saveFilePath,
                        UseShellExecute = true
                    });
                }
                catch
                {
                    // 開不了編輯器也沒關係，至少不覆蓋使用者的設定檔。
                }
                Environment.Exit(1);
            }
        }
    }
}
