using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Windows.Storage;

namespace MeowTextReader.Repo
{
    /// <summary>
    /// 決定 appConfig.json 放在哪裡。設定檔本身沒辦法記錄自己的位置（先有雞還是先有蛋），
    /// 所以自訂路徑另外存在預設資料夾底下的純文字指標檔 configPath.txt；
    /// 指標檔不存在（或內容壞掉）就退回預設位置。
    /// </summary>
    public static class ConfigLocation
    {
        public const string ConfigFileName = "appConfig.json";
        private const string PointerFileName = "configPath.txt";

        private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        /// <summary>預設資料夾，同時也是指標檔的所在位置。</summary>
        public static string DefaultFolder { get; } = ResolveDefaultFolder();

        public static string DefaultFilePath => Path.Combine(DefaultFolder, ConfigFileName);

        private static string PointerFilePath => Path.Combine(DefaultFolder, PointerFileName);

        private static string ResolveDefaultFolder()
        {
            // 打包(MSIX)應用程式呼叫 Environment.GetFolderPath(LocalApplicationData) 時，
            // 系統會將路徑重新導向到套件的虛擬容器內，這個路徑只有本行程看得到；
            // 外部程式（例如記事本）用同一個字串路徑打開時會找不到檔案。
            // Windows.Storage.ApplicationData.Current.LocalFolder.Path 回傳的才是實體、
            // 外部程式也能存取到的路徑（%LOCALAPPDATA%\Packages\<PackageFamilyName>\LocalState），
            // 所以優先使用它；未打包執行（例如單元測試、非 MSIX 部署）時再退回原本的方式。
            string folder;
            try
            {
                folder = ApplicationData.Current.LocalFolder.Path;
            }
            catch
            {
                folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }

            string appFolder = Path.Combine(folder, "MeowTextReader");
            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);
            return appFolder;
        }

        /// <summary>目前生效的設定檔完整路徑。</summary>
        public static string Resolve()
        {
            try
            {
                if (File.Exists(PointerFilePath))
                {
                    var custom = File.ReadAllText(PointerFilePath, Utf8NoBom).Trim();
                    if (!string.IsNullOrEmpty(custom) && Path.IsPathFullyQualified(custom))
                    {
                        // 使用者可能把資料夾刪掉或搬到還沒掛載的磁碟，補建一次；
                        // 補不起來就當作指標檔無效，退回預設位置，不要讓 App 開不起來。
                        var dir = Path.GetDirectoryName(custom);
                        if (!string.IsNullOrEmpty(dir))
                        {
                            Directory.CreateDirectory(dir);
                            return custom;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ConfigLocation] 指標檔無效，改用預設位置: {ex}");
            }

            return DefaultFilePath;
        }

        public static bool IsDefault(string path)
        {
            return string.Equals(
                Path.GetFullPath(path), Path.GetFullPath(DefaultFilePath), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 記下自訂路徑。傳入預設路徑等同於清掉自訂設定（直接刪掉指標檔）。
        /// 寫入失敗會往外拋，讓呼叫端能告訴使用者位置沒有改成功。
        /// </summary>
        public static void Save(string path)
        {
            if (IsDefault(path))
            {
                if (File.Exists(PointerFilePath))
                    File.Delete(PointerFilePath);
                return;
            }

            File.WriteAllText(PointerFilePath, path, Utf8NoBom);
        }
    }
}
