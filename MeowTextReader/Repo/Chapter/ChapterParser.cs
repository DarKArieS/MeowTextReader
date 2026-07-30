using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MeowTextReader.Repo.Chapter
{
    /// <summary>
    /// 依使用者設定的 ChapterRegex 掃描文章，找出章節標題所在的行。
    /// </summary>
    public static class ChapterParser
    {
        /// <summary>
        /// 使用者可以填入任意 Regex，惡性回溯的字串不該讓整個 App 卡住。
        /// </summary>
        private static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(200);

        /// <summary>設定畫面用來擋下寫不出來的 Regex。</summary>
        public static bool IsValidPattern(string? pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern)) return false;
            try
            {
                _ = new Regex(pattern);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// 掃描所有行，回傳依行序排列的章節清單。一行只會產生一個章節，
        /// 即使同時符合多個 Regex。
        /// </summary>
        /// <param name="maxTitleLength">
        /// 章節標題的字數上限。超過這個字數的行不視為章節——內文段落裡提到
        /// 「第三話」之類的字樣時，才不會被誤認成章節標題。0 或負數表示不限制。
        /// </param>
        public static List<ChapterItem> Parse(
            IReadOnlyList<string> lines, IEnumerable<string>? patterns, int maxTitleLength)
        {
            var result = new List<ChapterItem>();
            var regexes = Compile(patterns);
            if (regexes.Count == 0) return result;

            for (int i = 0; i < lines.Count; i++)
            {
                var text = lines[i];
                if (string.IsNullOrWhiteSpace(text)) continue;

                // 先擋字數再比對 Regex：長段落本來就不會是標題，也省掉比對成本。
                var title = text.Trim();
                if (maxTitleLength > 0 && title.Length > maxTitleLength) continue;

                foreach (var regex in regexes)
                {
                    bool matched;
                    try
                    {
                        matched = regex.IsMatch(title);
                    }
                    catch (RegexMatchTimeoutException)
                    {
                        matched = false;
                    }

                    if (!matched) continue;

                    result.Add(new ChapterItem { Title = title, LineIndex = i });
                    break;
                }
            }

            return result;
        }

        private static List<Regex> Compile(IEnumerable<string>? patterns)
        {
            var regexes = new List<Regex>();
            if (patterns == null) return regexes;

            foreach (var pattern in patterns)
            {
                if (string.IsNullOrWhiteSpace(pattern)) continue;
                try
                {
                    regexes.Add(new Regex(pattern, RegexOptions.Compiled, MatchTimeout));
                }
                catch (ArgumentException)
                {
                    // 設定檔可能被手動改壞，寫不出來的 Regex 直接略過而不是讓開檔失敗。
                }
            }
            return regexes;
        }

    }
}
