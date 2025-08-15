using System.ComponentModel;
using System.Runtime.CompilerServices;
using MeowTextReader;
using System.IO;

namespace MeowTextReader.ReaderPage
{
    public class ReaderPageViewModel : INotifyPropertyChanged
    {
        private string? _fileName;
        private string? _fileContent;

        public string? FileName
        {
            get => _fileName;
            set
            {
                if (_fileName != value)
                {
                    _fileName = value;
                    OnPropertyChanged();
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
            var path = MainRepo.Instance.OpenFilePath;
            if (!string.IsNullOrEmpty(path))
            {
                FileName = Path.GetFileNameWithoutExtension(path);
                LoadFileContent(path);
            }
        }

        private void LoadFileContent(string? path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                FileContent = File.ReadAllText(path);
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