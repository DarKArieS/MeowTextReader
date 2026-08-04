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
using System.Threading.Tasks;
using MeowTextReader.Repo.Chapter;
using MeowTextReader.Repo.Model;

namespace MeowTextReader.Repo
{
    public class MainRepo
    {
        private static readonly Lazy<MainRepo> _instance = new(() => new MainRepo());
        public static MainRepo Instance => _instance.Value;

        // 使用者可以在「Config Setting」裡改設定檔位置，所以不是 readonly；
        // 變更只會發生在 MoveSaveLocationTo 裡，且全程持有 _fileLock。
        private string _saveFilePath;
        private AppConfig _config = new();

        // _lock 保護 _config 本身以及底下兩個旗標；_fileLock 只保護「實際落地」這個動作，
        // 讓背景 writer 與 Flush() 不會同時寫同一個檔，同時避免持有 _lock 做慢速 I/O。
        private readonly object _lock = new();
        private readonly object _fileLock = new();
        private bool _pendingSave;
        private bool _writerRunning;

        // 合併視窗。UI 層（捲動、視窗位置）已各有 1 秒 debounce，這裡再壓一層，
        // 把「開檔時連寫兩次」「顏色輸入框每個按鍵一次」這類連發合併成單次寫入。
        private const int CoalesceDelayMs = 500;

        /// <summary>設定檔寫入失敗時觸發。目前只用於診斷輸出，之後要在 UI 上提示可掛這個事件。</summary>
        public static event Action<Exception>? SaveFailed; 

        private MainRepo()
        {
            _saveFilePath = ConfigLocation.Resolve();
            LoadConfig();
        }

        /// <summary>設定 json 檔案的完整路徑，供外部（例如以編輯器打開）使用。</summary>
        public string SaveFilePath
        {
            get { lock (_fileLock) { return _saveFilePath; } }
        }

        /// <summary>設定 json 檔案所在的資料夾。</summary>
        public string SaveFolderPath => Path.GetDirectoryName(SaveFilePath) ?? ConfigLocation.DefaultFolder;

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

        public AppTheme Theme
        {
            get => _config.Theme ?? AppTheme.Default;
            set
            {
                if (_config.Theme != value)
                {
                    _config.Theme = value;
                    SaveConfig();
                    ThemeChanged?.Invoke();
                }
            }
        }

        public static event Action? ThemeChanged;

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

        /// <summary>
        /// 標記設定有變更，實際的序列化與寫檔交給背景 writer。呼叫端（通常是 UI 執行緒）
        /// 立即返回，不會被磁碟 I/O 卡住；<see cref="CoalesceDelayMs"/> 內的多次呼叫會合併成一次寫入。
        /// </summary>
        private void SaveConfig()
        {
            lock (_lock)
            {
                _pendingSave = true;
                if (_writerRunning) return; // 已有 writer 在跑，它會撿走這次變更
                _writerRunning = true;
            }

            _ = Task.Run(WriterLoopAsync);
        }

        private async Task WriterLoopAsync()
        {
            while (true)
            {
                await Task.Delay(CoalesceDelayMs);

                // _fileLock 要涵蓋「序列化 + 落地」整段，不能只包落地：否則 Flush() 可能在
                // _pendingSave 已被清掉、但這份 json 還沒寫出去的空檔看到「沒有待寫變更」而直接返回，
                // 關閉程式時就會漏掉最後一次變更。
                lock (_fileLock)
                {
                    string json;
                    lock (_lock)
                    {
                        if (!_pendingSave)
                        {
                            // 結束條件與 SaveConfig 的 _writerRunning 檢查在同一把鎖內，
                            // 不會出現「writer 剛收工、同時有新變更進來卻沒人處理」的漏窗。
                            _writerRunning = false;
                            return;
                        }

                        _pendingSave = false;
                        json = JsonSerializer.Serialize(_config, SaveJsonOptions);
                    }

                    // 寫檔期間進來的 SaveConfig 只會設 _pendingSave，由下一輪迴圈撿走。
                    WriteAtomic(json);
                }
            }
        }

        /// <summary>
        /// 先寫暫存檔再原子替換，避免寫到一半中斷時把使用者的閱讀進度與章節快取一起毀掉。
        /// 寫入失敗不往外拋：呼叫端有可能是 ScrollViewer_ViewChanged 這種 UI 事件，
        /// 讓例外冒出去會變成 UI 執行緒未處理例外。
        /// </summary>
        private void WriteAtomic(string json)
        {
            lock (_fileLock)
            {
                try
                {
                    WriteAtomicCore(_saveFilePath, json);
                }
                catch (Exception ex)
                {
                    // 不重試：檔案可能被使用者用編輯器鎖住，重試迴圈只會空轉。
                    // 下一次真正的設定變更自然會再寫一次。
                    Debug.WriteLine($"[MainRepo] 設定檔寫入失敗: {ex}");
                    SaveFailed?.Invoke(ex);
                }
            }
        }

        private static void WriteAtomicCore(string path, string json)
        {
            var tmp = path + ".tmp";
            try
            {
                File.WriteAllText(tmp, json, Utf8NoBom);
                if (File.Exists(path))
                    File.Replace(tmp, path, null);
                else
                    File.Move(tmp, path);
            }
            catch
            {
                try
                {
                    if (File.Exists(tmp)) File.Delete(tmp);
                }
                catch
                {
                    // 清不掉暫存檔也沒關係，下次寫入會覆蓋它。
                }

                throw;
            }
        }

        /// <summary>
        /// 把還沒寫出去的變更立刻同步寫入。用於程式關閉，或外部（編輯器）要讀這個檔案之前。
        /// </summary>
        public void Flush()
        {
            // 先搶 _fileLock：背景 writer 手上若已有一份還沒落地的 json，這裡會等它寫完，
            // 才不會誤判成「沒有待寫變更」而漏掉最後一次寫入。lock 是可重入的，
            // 底下的 WriteAtomic 再拿一次同一把鎖沒有問題。
            lock (_fileLock)
            {
                string json;
                lock (_lock)
                {
                    // 沒有待寫變更、而且檔案已經存在時才可以省略；否則仍要寫出一份，
                    // 讓「用編輯器開啟設定檔」在從未存檔過的情況下也有東西可開。
                    if (!_pendingSave && File.Exists(_saveFilePath)) return;
                    _pendingSave = false;
                    json = JsonSerializer.Serialize(_config, SaveJsonOptions);
                }

                WriteAtomic(json);
            }
        }

        /// <summary>
        /// 把設定檔搬到新位置：目前記憶體裡的設定寫過去，成功後刪掉舊檔並記住新路徑。
        /// 記憶體狀態完全不動，所以不需要重新啟動。失敗會往外拋，此時舊檔與舊路徑都還在。
        /// </summary>
        public void MoveSaveLocationTo(string newPath)
        {
            lock (_fileLock)
            {
                var oldPath = _saveFilePath;
                if (string.Equals(Path.GetFullPath(oldPath), Path.GetFullPath(newPath),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                string json;
                lock (_lock)
                {
                    _pendingSave = false;
                    json = JsonSerializer.Serialize(_config, SaveJsonOptions);
                }

                var dir = Path.GetDirectoryName(newPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                // 順序很重要：先把內容寫到新位置，再改指標，最後才刪舊檔。
                // 任何一步失敗都還留著一份完整的設定，不會兩頭落空。
                WriteAtomicCore(newPath, json);
                ConfigLocation.Save(newPath);
                _saveFilePath = newPath;

                try
                {
                    if (File.Exists(oldPath)) File.Delete(oldPath);
                }
                catch (Exception ex)
                {
                    // 舊檔刪不掉不影響運作，留著當備份也無妨。
                    Debug.WriteLine($"[MainRepo] 舊設定檔刪除失敗: {ex}");
                }
            }
        }

        /// <summary>
        /// 檢查指定檔案是不是一份解析得動的設定檔。給「變更位置」時判斷目標要載入還是覆蓋用。
        /// </summary>
        public static bool IsValidConfigFile(string path)
        {
            try
            {
                var json = File.ReadAllText(path, Utf8NoBom);
                if (string.IsNullOrWhiteSpace(json)) return false;
                return JsonSerializer.Deserialize<AppConfig>(json, SaveJsonOptions) != null;
            }
            catch
            {
                return false;
            }
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
                // 這裡跑在建構子（App 啟動早期），另一個分支還會 Environment.Exit，
                // 不能仰賴背景 writer，直接同步寫出去。
                WriteAtomic(JsonSerializer.Serialize(_config, SaveJsonOptions));
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
