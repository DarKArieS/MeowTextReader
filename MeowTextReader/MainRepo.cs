using System;
using System.IO;
using System.Text.Json;

namespace MeowTextReader
{
    public class MainRepo
    {
        private static readonly Lazy<MainRepo> _instance = new(() => new MainRepo());
        public static MainRepo Instance => _instance.Value;

        private string? _folderPath;
        private readonly string _saveFilePath;
        private AppConfig _config = new();

        private class AppConfig
        {
            public string? folderPath { get; set; }
        }

        private MainRepo()
        {
            _saveFilePath = GetSaveFilePath();
            LoadConfig();
        }

        private static string GetSaveFilePath()
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(folder, "MeowTextReader");
            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, "appConfig.json");
        }

        public string? FolderPath
        {
            get => _config.folderPath;
            set
            {
                if (_config.folderPath != value)
                {
                    _config.folderPath = value;
                    SaveConfig();
                }
            }
        }

        private void SaveConfig()
        {
            var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_saveFilePath, json);
        }

        private void LoadConfig()
        {
            if (File.Exists(_saveFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_saveFilePath);
                    _config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
                catch
                {
                    _config = new AppConfig();
                }
            }
        }
    }
}
