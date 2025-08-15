using System.ComponentModel;
using System.Runtime.CompilerServices;
using MeowTextReader;
using System.IO;

namespace MeowTextReader.ReaderPage
{
    public class ReaderPageViewModel : INotifyPropertyChanged
    {
        private string? _filePath;
        private string? _fileContent;

        public string? FilePath
        {
            get => _filePath;
            set
            {
                if (_filePath != value)
                {
                    _filePath = value;
                    OnPropertyChanged();
                    LoadFileContent();
                }
            }
        }

        public string? FileContent
        {
            get => _fileContent;
            private set
            {
                if (_fileContent != value)
                {
                    _fileContent = value;
                    OnPropertyChanged();
                }
            }
        }

        public ReaderPageViewModel()
        {
            FilePath = MainRepo.Instance.OpenFilePath;
        }

        private void LoadFileContent()
        {
            if (!string.IsNullOrEmpty(FilePath) && File.Exists(FilePath))
            {
                FileContent = File.ReadAllText(FilePath);
            }
            else
            {
                FileContent = null;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}