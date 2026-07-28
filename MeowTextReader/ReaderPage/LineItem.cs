namespace MeowTextReader.ReaderPage
{
    /// <summary>
    /// 檔案中的一行。用獨立物件而非裸字串，才能在內容重複（例如空行）時
    /// 仍正確辨識行號，並讓 ListView.ScrollIntoView 精準定位到指定的那一行。
    /// </summary>
    public class LineItem
    {
        public LineItem(int index, string text)
        {
            Index = index;
            Text = text;
        }

        /// <summary>0-based 行索引。</summary>
        public int Index { get; }

        /// <summary>顯示用的 1-based 行號。</summary>
        public int LineNumber => Index + 1;

        public string Text { get; }

        /// <summary>
        /// ListViewItem 的自動化名稱會回退到 ToString()，這裡回傳純文字，
        /// 避免朗讀程式讀出型別名稱。
        /// </summary>
        public override string ToString() => Text;
    }
}
