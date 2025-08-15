using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MeowTextReader.ReaderPage
{
    public class ReaderPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Add properties and logic for the ReaderPage here
    }
}