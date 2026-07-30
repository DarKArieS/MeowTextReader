namespace MeowTextReader.Repo.Model
{
    /// <summary>
    /// MainPage 各資料夾的瀏覽位置。以項目索引為錨點，理由同 <see cref="HistoryItem.LineIndex"/>。
    /// </summary>
    public class FolderScrollHistoryItem
    {
        public string? FolderPath { get; set; }

        /// <summary>最上方可見項目的索引（0-based）。</summary>
        public int ItemIndex { get; set; }

        public double HorizontalOffset { get; set; }
    }
}