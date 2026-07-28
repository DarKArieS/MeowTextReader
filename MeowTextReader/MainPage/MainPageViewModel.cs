using System.ComponentModel;
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MeowTextReader.MainPage
{
    public class FileItem : INotifyPropertyChanged
    {
        private double _progressPercent;
        private bool _hasProgress;

        public string Name { get; set; } = string.Empty;
        public bool IsFolder { get; set; }
        public string FullPath { get; set; } = string.Empty; // 新增完整路徑屬性

        /// <summary>閱讀進度百分比（0~100）。</summary>
        public double ProgressPercent
        {
            get => _progressPercent;
            private set
            {
                if (_progressPercent.Equals(value)) return;
                _progressPercent = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProgressLabel));
                OnPropertyChanged(nameof(ProgressText));
            }
        }

        /// <summary>有閱讀紀錄才顯示進度環。</summary>
        public bool HasProgress
        {
            get => _hasProgress;
            private set
            {
                if (_hasProgress == value) return;
                _hasProgress = value;
                OnPropertyChanged();
            }
        }

        /// <summary>進度環旁的簡短標示。</summary>
        public string ProgressLabel => $"{ProgressPercent:0}%";

        /// <summary>滑鼠停留時顯示的完整進度。</summary>
        public string ProgressText => $"閱讀進度 {ProgressPercent:0.00}%";

        public void SetProgress(double percent)
        {
            ProgressPercent = Math.Clamp(percent, 0, 100);
            HasProgress = true;
        }

        /// <summary>
        /// ListViewItem 的自動化名稱會回退到 ToString()，這裡回傳檔名，
        /// 避免朗讀程式讀出型別名稱。
        /// </summary>
        public override string ToString() => Name;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class MainPageViewModel : INotifyPropertyChanged
    {
        public string? FolderPath
        {
            get => MainRepo.Instance.FolderPath;
            set
            {
                if (MainRepo.Instance.FolderPath != value)
                {
                    MainRepo.Instance.FolderPath = value;
                    OnPropertyChanged();
                    LoadFolderItems();
                }
            }
        }

        public ObservableCollection<FileItem> FolderItems { get; } = new();

        public ICommand FolderItemClickCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand OpenWithDefaultEditorCommand { get; }
        public ICommand OpenInExplorerCommand { get; }

        public MainPageViewModel()
        {
            FolderItemClickCommand = new RelayCommand<FileItem>(OnFolderItemClick);
            BackCommand = new RelayCommand(BackToParent);
            OpenWithDefaultEditorCommand = new RelayCommand<FileItem>(OpenWithDefaultEditor, CanOpenFile);
            OpenInExplorerCommand = new RelayCommand<FileItem>(OpenInExplorer, (i) => true);
            OnPropertyChanged(nameof(FolderPath));
            LoadFolderItems();
        }

        /// <summary>
        /// 記錄指定資料夾的瀏覽位置。folderPath 由呼叫端傳入而非直接用 <see cref="FolderPath"/>，
        /// 因為切換資料夾時要存的是「切換前」那個資料夾。
        /// </summary>
        public void SaveScrollPosition(string? folderPath, int itemIndex, double horizontalOffset)
        {
            if (string.IsNullOrEmpty(folderPath)) return;
            MainRepo.Instance.UpdateFolderScrollPosition(folderPath, itemIndex, horizontalOffset);
        }

        public MainRepo.FolderScrollHistoryItem? GetSavedScrollPosition(string? folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return null;
            return MainRepo.Instance.GetFolderScrollPosition(folderPath);
        }

        private void OnFolderItemClick(FileItem? item)
        {
            if (item != null && item.IsFolder && !string.IsNullOrEmpty(FolderPath))
            {
                FolderPath = Path.Combine(FolderPath, item.Name);
            }
        }

        private void BackToParent()
        {
            if (!string.IsNullOrEmpty(FolderPath))
            {
                var parent = Directory.GetParent(FolderPath);
                if (parent != null)
                {
                    FolderPath = parent.FullName;
                }
            }
        }

        private bool CanOpenFile(FileItem? item)
        {
            return item != null && !item.IsFolder && !string.IsNullOrEmpty(item.FullPath);
        }

        private void OpenWithDefaultEditor(FileItem? item)
        {
            if (item == null || item.IsFolder || string.IsNullOrEmpty(item.FullPath)) return;
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = item.FullPath,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void OpenInExplorer(FileItem? item)
        {
            if (item == null || string.IsNullOrEmpty(item.FullPath)) return;
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{item.FullPath}\"",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void LoadFolderItems()
        {
            FolderItems.Clear();
            if (!string.IsNullOrEmpty(FolderPath) && Directory.Exists(FolderPath))
            {
                var dirs = Directory.GetDirectories(FolderPath)
                    .Where(d =>
                    {
                        try
                        {
                            var di = new DirectoryInfo(d);
                            return !di.Attributes.HasFlag(FileAttributes.Hidden) &&
                                   (di.Attributes & FileAttributes.Directory) != 0 &&
                                   HasAccess(d);
                        }
                        catch
                        {
                            return false;
                        }
                    })
                    .Select(d => new FileItem { Name = Path.GetFileName(d), IsFolder = true, FullPath = d });
                var txts = Directory.GetFiles(FolderPath, "*.txt")
                    .Where(f =>
                    {
                        try
                        {
                            var fi = new FileInfo(f);
                            return !fi.Attributes.HasFlag(FileAttributes.Hidden);
                        }
                        catch
                        {
                            return false;
                        }
                    })
                    .Select(f => new FileItem { Name = Path.GetFileName(f), IsFolder = false, FullPath = f });
                foreach (var item in dirs.Concat(txts))
                    FolderItems.Add(item);
            }

            ApplyReadingProgress();
        }

        /// <summary>
        /// 依閱讀紀錄標上進度。舊紀錄沒有總行數，只能實際數過檔案才算得出來，
        /// 這部分丟到背景做，之後補寫回設定檔，下次進來就走快路徑。
        /// </summary>
        private void ApplyReadingProgress()
        {
            var pending = new List<FileItem>();

            foreach (var item in FolderItems)
            {
                if (item.IsFolder) continue;

                var history = MainRepo.Instance.GetHistoryItem(HistoryKey(item));
                if (history == null) continue;

                if (history.ProgressPercent is double percent)
                    item.SetProgress(percent);
                else if (history.LineIndex.HasValue)
                    pending.Add(item);
            }

            if (pending.Count > 0)
                _ = CountLinesAndApplyProgressAsync(pending, ++_loadGeneration);
        }

        private int _loadGeneration;

        private async Task CountLinesAndApplyProgressAsync(List<FileItem> pending, int generation)
        {
            foreach (var item in pending)
            {
                string path = item.FullPath;
                int totalLines = await Task.Run(() => CountLines(path));

                // 期間切換過資料夾的話這些項目已經不在畫面上了。
                if (generation != _loadGeneration) return;
                if (totalLines <= 0) continue;

                string key = HistoryKey(item);
                var history = MainRepo.Instance.GetHistoryItem(key);
                if (history?.LineIndex is not int lineIndex) continue;

                int readLines = Math.Clamp(lineIndex + 1, 1, totalLines);
                MainRepo.Instance.BackfillHistoryLineCounts(key, readLines, totalLines);
                item.SetProgress((double)readLines / totalLines * 100.0);
            }
        }

        private static int CountLines(string path)
        {
            try
            {
                int count = 0;
                foreach (var _ in File.ReadLines(path)) count++;
                return count;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>閱讀紀錄是以不含副檔名的檔名為 key（見 ReaderPageViewModel）。</summary>
        private static string HistoryKey(FileItem item) => Path.GetFileNameWithoutExtension(item.FullPath);

        private static bool HasAccess(string path)
        {
            try
            {
                // 嘗試列舉內容以測試權限
                Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }
        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter) => _execute();
        public event EventHandler? CanExecuteChanged;
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;
        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }
        public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
        public void Execute(object? parameter) => _execute((T?)parameter);
        public event EventHandler? CanExecuteChanged;
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
