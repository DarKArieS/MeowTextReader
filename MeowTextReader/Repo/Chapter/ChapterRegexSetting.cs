using System;
using System.Collections.Generic;

namespace MeowTextReader.Repo.Chapter
{
    /// <summary>
    /// 一組章節抓取設定：Regex 清單、標題字數上限、開頭跳過行數。
    /// 可能是全域預設值（<see cref="MeowTextReader.Repo.MainRepo.GlobalChapterSetting"/>），
    /// 也可能是單一檔案（<see cref="MeowTextReader.Repo.Model.HistoryItem.ChapterSetting"/>）的專屬設定。
    /// </summary>
    public class ChapterRegexSetting
    {
        /// <summary>使用者沒設定過時採用的章節 Regex。</summary>
        public static readonly IReadOnlyList<string> DefaultRegexList = new[]
        {
            "第([0-9一二三四五六七八九十]+)話",
            "第([0-9一二三四五六七八九十]+)章"
        };

        public const int MinTitleMaxLength = 8;
        public const int MaxTitleMaxLength = 300;
        public const int DefaultTitleMaxLength = 60;

        public const int MinSkipLines = 0;
        public const int MaxSkipLines = 1000;
        public const int DefaultSkipLines = 0;

        public List<string>? ChapterRegexList { get; set; }
        public int? ChapterTitleMaxLength { get; set; }
        public int? ChapterSkipLines { get; set; }

        /// <summary>沒設定過時給預設 Regex；使用者刻意清空則尊重其設定，不再塞回預設值。</summary>
        public List<string> EffectiveRegexList() => ChapterRegexList ?? new List<string>(DefaultRegexList);

        /// <summary>章節標題的字數上限，超過這個字數的行不視為章節。</summary>
        public int EffectiveTitleMaxLength() =>
            Math.Clamp(ChapterTitleMaxLength ?? DefaultTitleMaxLength, MinTitleMaxLength, MaxTitleMaxLength);

        /// <summary>章節掃描要跳過的開頭行數。</summary>
        public int EffectiveSkipLines() =>
            Math.Clamp(ChapterSkipLines ?? DefaultSkipLines, MinSkipLines, MaxSkipLines);
    }
}
