using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MeowTextReader.Repo.Model
{
    class AppConfig
    {
        [JsonPropertyName("folderPath")] public string? FolderPath { get; set; }
        public string? OpenFilePath { get; set; }
        public AppPage? LastPage { get; set; }
        public ReaderSetting ReaderSetting { get; set; } = new ReaderSetting();
        public List<string>? ChapterRegexList { get; set; }
        public int? ChapterTitleMaxLength { get; set; }
        public int? ChapterSkipLines { get; set; }
        [JsonPropertyName("history")] public List<HistoryItem> History { get; set; } = new();
        public List<FolderScrollHistoryItem> FolderScrollPositions { get; set; } = new();
        public WindowPlacement? WindowPlacement { get; set; }
    }
}