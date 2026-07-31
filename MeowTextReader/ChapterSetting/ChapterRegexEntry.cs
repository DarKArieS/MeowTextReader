using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MeowTextReader.ChapterSetting
{
    /// <summary>
    /// 清單上的一列 Regex。用物件而非裸字串，TextBox 才綁得到 TwoWay，
    /// 內容重複時也還認得出是哪一列。
    /// </summary>
    public class ChapterRegexEntry : INotifyPropertyChanged
    {
        private string _pattern;

        public ChapterRegexEntry(string pattern = "")
        {
            _pattern = pattern;
        }

        public string Pattern
        {
            get => _pattern;
            set
            {
                if (_pattern == value) return;
                _pattern = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// ListViewItem 的自動化名稱會回退到 ToString()，這裡回傳 Regex 本身，
        /// 避免朗讀程式讀出型別名稱。
        /// </summary>
        public override string ToString() => _pattern;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
