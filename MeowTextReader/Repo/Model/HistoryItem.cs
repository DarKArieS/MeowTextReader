using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using MeowTextReader.Repo.Chapter;

namespace MeowTextReader.Repo.Model
{
    public class HistoryItem
    {
        public string? FileName { get; set; }

        /// <summary>
        /// 舊版欄位：像素捲動偏移量。僅供一次性資料遷移，新程式不再寫入。
        /// </summary>
        public int ScrollOffset { get; set; }

        /// <summary>
        /// 最上方可見行的索引（0-based）。這是位置的絕對錨點，不受字體大小、
        /// 視窗尺寸或虛擬化估算誤差影響。
        /// </summary>
        public int? LineIndex { get; set; }

        /// <summary>
        /// 在 LineIndex 該行內的細部偏移比例，範圍 [0, 1)。
        /// </summary>
        public double LineFraction { get; set; }

        /// <summary>
        /// 已讀到的行數（最下方可見行 +1），與 ReaderPage 標題上的百分比同義。
        /// 0 表示舊紀錄還沒有這項資訊。
        /// </summary>
        public int ReadLines { get; set; }

        /// <summary>
        /// 檔案總行數。0 表示舊紀錄還沒有這項資訊。
        /// </summary>
        public int TotalLines { get; set; }

        /// <summary>
        /// 上次掃描出來的章節清單。掃描結果存在這裡，下次開同一個檔案就不必重掃。
        /// </summary>
        public List<ChapterItem>? Chapters { get; set; }

        /// <summary>
        /// <see cref="Chapters"/> 是用什麼條件掃出來的（見 <see cref="MainRepo.BuildChapterCacheKey"/>）。
        /// 條件對不上就代表快取過期，必須重掃。
        /// </summary>
        public string? ChapterCacheKey { get; set; }

        /// <summary>
        /// 這個檔案專屬的章節抓取設定。是否生效由 <see cref="UseDefaultChapterSetting"/> 決定，
        /// 關閉「使用預設值」但還沒填過的話會是 null（見 <see cref="MainRepo.GetChapterSetting"/>）。
        /// </summary>
        public ChapterRegexSetting? ChapterSetting { get; set; }

        /// <summary>
        /// 是否使用全域預設的章節抓取設定。true（含未設定過）時即使 <see cref="ChapterSetting"/>
        /// 已經填過資料也不採用，只是暫時關閉不用；關掉這個開關就能拿回原本填的設定，
        /// 不必重新輸入。
        /// </summary>
        public bool UseDefaultChapterSetting { get; set; } = true;

        /// <summary>
        /// 閱讀進度百分比（0~100）。資訊不足時回傳 null。
        /// </summary>
        [JsonIgnore]
        public double? ProgressPercent
        {
            get
            {
                if (TotalLines <= 0) return null;
                // 舊紀錄只有 LineIndex，退而用「最上方可見行」當作已讀行數。
                int readLines = ReadLines > 0 ? ReadLines : (LineIndex + 1 ?? 0);
                if (readLines <= 0) return null;
                return Math.Clamp((double)readLines / TotalLines * 100.0, 0, 100);
            }
        }
    }
}
