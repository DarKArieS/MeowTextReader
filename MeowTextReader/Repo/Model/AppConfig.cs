using System.Collections.Generic;
using System.Text.Json.Serialization;
using MeowTextReader.Repo.Chapter;

namespace MeowTextReader.Repo.Model
{
    class AppConfig
    {
        [JsonPropertyName("folderPath")] public string? FolderPath { get; set; }
        public string? OpenFilePath { get; set; }
        public AppPage? LastPage { get; set; }  
        public ReaderSetting ReaderSetting { get; set; } = new ReaderSetting();
        /// <summary>全域章節抓取設定，單一檔案沒有專屬設定時的預設值。</summary>
        public ChapterRegexSetting? ChapterSetting { get; set; }
        [JsonPropertyName("history")] public List<HistoryItem> History { get; set; } = new();
        public List<FolderScrollHistoryItem> FolderScrollPositions { get; set; } = new();
        public WindowPlacement? WindowPlacement { get; set; }
    }
}