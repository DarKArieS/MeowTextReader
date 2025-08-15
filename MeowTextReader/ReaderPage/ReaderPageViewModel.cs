using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

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
                LoadFileLines(path);
            }
        }

        private void LoadFileLines(string? path)
        {
            FileLines.Clear();
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                var lines = File.ReadAllLines(path);
                foreach (var line in lines)
                    FileLines.Add(line);
            }
        }

        public void SaveScrollOffset(double offset)
        {
            if (!string.IsNullOrEmpty(FileName))
            {
                MainRepo.Instance.UpdateHistory(FileName, offset);
            }
        }

        public int? GetSavedScrollOffset()
        {
            if (!string.IsNullOrEmpty(FileName))
            {
                return MainRepo.Instance.GetHistoryScrollOffset(FileName);
            }
            return null;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}