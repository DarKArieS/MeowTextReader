using System.ComponentModel;
using System.Runtime.CompilerServices;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace MeowTextReader
{
    public class FileItem
    {
        public string Name { get; set; } = string.Empty;
        public bool IsFolder { get; set; }
    }

    public class MainViewModel : INotifyPropertyChanged
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

        public MainViewModel()
        {
            OnPropertyChanged(nameof(FolderPath));
            LoadFolderItems();
        }

        private void LoadFolderItems()
        {
            FolderItems.Clear();
            if (!string.IsNullOrEmpty(FolderPath) && Directory.Exists(FolderPath))
            {
                var dirs = Directory.GetDirectories(FolderPath)
                    .Select(d => new FileItem { Name = Path.GetFileName(d), IsFolder = true });
                var txts = Directory.GetFiles(FolderPath, "*.txt")
                    .Select(f => new FileItem { Name = Path.GetFileName(f), IsFolder = false });
                foreach (var item in dirs.Concat(txts))
                    FolderItems.Add(item);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
