using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MeowTextReader.Repo
{
    /// <summary>
    /// 用預設編輯器打開檔案，並在可能的情況下跳到指定行。
    /// Process.Start(UseShellExecute = true) 只會把檔名丟給 Shell 關聯的預設程式，
    /// 無法傳遞行號，所以先查出預設程式是誰，若在已知支援跳行參數的編輯器清單中，
    /// 就直接呼叫該執行檔並帶上對應參數；否則退回原本「純打開、不跳行」的行為。
    /// </summary>
    public static class ExternalEditorLauncher
    {
        private const uint ASSOCF_NONE = 0;
        private const uint ASSOCSTR_EXECUTABLE = 2;

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern uint AssocQueryString(
            uint flags, uint str, string pszAssoc, string? pszExtra,
            StringBuilder? pszOut, ref uint pcchOut);

        public static void OpenFileAtLine(string filePath, int lineNumber)
        {
            try
            {
                var editorExePath = GetDefaultEditorExecutable(Path.GetExtension(filePath));
                var editorFileName = editorExePath != null ? Path.GetFileName(editorExePath) : null;

                if (editorExePath != null && editorFileName != null)
                {
                    if (editorFileName.Equals("notepad++.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = editorExePath,
                            Arguments = $"-n{lineNumber} \"{filePath}\"",
                            UseShellExecute = false
                        });
                        return;
                    }
                    if (editorFileName.Equals("Code.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = editorExePath,
                            Arguments = $"-g \"{filePath}:{lineNumber}\"",
                            UseShellExecute = false
                        });
                        return;
                    }
                    if (editorFileName.Equals("sublime_text.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = editorExePath,
                            Arguments = $"\"{filePath}:{lineNumber}\"",
                            UseShellExecute = false
                        });
                        return;
                    }
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private static string? GetDefaultEditorExecutable(string extension)
        {
            try
            {
                uint size = 0;
                AssocQueryString(ASSOCF_NONE, ASSOCSTR_EXECUTABLE, extension, null, null, ref size);
                if (size == 0) return null;

                var sb = new StringBuilder((int)size);
                var result = AssocQueryString(ASSOCF_NONE, ASSOCSTR_EXECUTABLE, extension, null, sb, ref size);
                return result == 0 ? sb.ToString() : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
