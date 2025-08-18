using System.ComponentModel;
using System.Runtime.CompilerServices;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace MeowTextReader.MainPage
{
    public class FileItem
    {
        public string Name { get; set; } = string.Empty;
        public bool IsFolder { get; set; }
        public string FullPath { get; set; } = string.Empty; // 新增完整路徑屬性
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
        }

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
