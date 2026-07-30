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
