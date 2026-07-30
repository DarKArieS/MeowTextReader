namespace MeowTextReader.Repo.Model
{
    public class ReaderSetting // 移出 MainRepo class，作為獨立 public class
    {
        public double FontSize { get; set; } = 20.0;

        /// <summary>
        /// 行距倍率，實際行高為 FontSize * LineSpacing。
        /// </summary>
        public double LineSpacing { get; set; } = 1.5;

        public string? CustomBackgroundColor { get; set; } = null; // 改名
        public bool UseCustomBackgroundColor { get; set; } = false; // 新增
        public string? CustomForegroundColor { get; set; } = null; // 新增
        public bool UseCustomForegroundColor { get; set; } = false; // 新增
    }
}
