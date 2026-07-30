namespace MeowTextReader.Repo.Model
{
    /// <summary>
    /// 視窗上次關閉時的位置與大小（實體像素）。最大化時記錄的是還原後的大小，
    /// 這樣下次開啟先最大化、使用者按還原時才會回到合理尺寸。
    /// </summary>
    public class WindowPlacement
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsMaximized { get; set; }
    }
}
