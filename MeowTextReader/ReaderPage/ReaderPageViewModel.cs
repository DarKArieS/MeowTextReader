using System.ComponentModel;
using System.Runtime.CompilerServices;
using MeowTextReader;
using System.IO;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace MeowTextReader.ReaderPage
{
    public class ReaderPageViewModel : INotifyPropertyChanged
    {
        private string? _fileName;
        public ObservableCollection<string> FileLines { get; } = new();

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

        public ReaderPageViewModel()
        {
            var path = MainRepo.Instance.OpenFilePath;
            if (!string.IsNullOrEmpty(path))
            {
                FileName = Path.GetFileNameWithoutExtension(path);
                _ = LoadFileLinesAsync(path);
            }
        }

        private async Task LoadFileLinesAsync(string? path)
        {
            FileLines.Clear();
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                var lines = await File.ReadAllLinesAsync(path);
                foreach (var line in lines)
                    FileLines.Add(line);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}