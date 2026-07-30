using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace MeowTextReader.Repo.Chapter
{
    /// <summary>
    /// 由 ChapterRegex 掃描出來的一個章節位置。會序列化進 appConfig.json 的閱讀紀錄，
    /// 所以屬性都必須可寫。
    /// </summary>
    public class ChapterItem : INotifyPropertyChanged
    {
        private bool _isCurrent;

        /// <summary>章節標題，取自比對到的整行文字（已去頭尾空白）。</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>章節標題所在的 0-based 行索引。</summary>
        public int LineIndex { get; set; }

        /// <summary>顯示用的 1-based 行號。</summary>
        [JsonIgnore]
        public int LineNumber => LineIndex + 1;

        /// <summary>
        /// 目前閱讀進度所在的章節。純 UI 狀態，不需要也不應該存進設定檔。
        /// </summary>
        [JsonIgnore]
        public bool IsCurrent
        {
            get => _isCurrent;
            set
            {
                if (_isCurrent == value) return;
                _isCurrent = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// ListViewItem 的自動化名稱會回退到 ToString()，這裡回傳標題，
        /// 避免朗讀程式讀出型別名稱。
        /// </summary>
        public override string ToString() => Title;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
