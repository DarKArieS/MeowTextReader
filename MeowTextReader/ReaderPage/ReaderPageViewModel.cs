using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace MeowTextReader.ReaderPage
{
    public class ReaderPageViewModel : INotifyPropertyChanged
    {
        private string? _fileName;
        private double _fontSize;
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

        public double FontSize
        {
            get => _fontSize;
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
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
            FontSize = MainRepo.Instance.FontSize;
            MainRepoFontSizeWatcher();
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

        // 監聽 MainRepo.FontSize 變化（簡單 polling 方式）
        private async void MainRepoFontSizeWatcher()
        {
            double last = FontSize;
            while (true)
            {
                await System.Threading.Tasks.Task.Delay(500);
                var current = MainRepo.Instance.FontSize;
                if (current != last)
                {
                    FontSize = current;
                    last = current;
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}