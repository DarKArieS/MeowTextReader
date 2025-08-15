using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.IO;
using System;

namespace MeowTextReader
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string? _folderPath;
        public string? FolderPath
        {
            get => _folderPath;
            set
            {
                if (_folderPath != value)
                {
                    _folderPath = value;
                    OnPropertyChanged();
                    SaveFolderPath();
                }
            }
        }

        private static string GetSaveFilePath()
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(folder, "MeowTextReader");
            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, "folderpath.txt");
        }

        public MainViewModel()
        {
            LoadFolderPath();
        }

        private void SaveFolderPath()
        {
            if (!string.IsNullOrEmpty(_folderPath))
            {
                File.WriteAllText(GetSaveFilePath(), _folderPath, Encoding.UTF8);
            }
        }

        private void LoadFolderPath()
        {
            string path = GetSaveFilePath();
            if (File.Exists(path))
            {
                _folderPath = File.ReadAllText(path, Encoding.UTF8);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
