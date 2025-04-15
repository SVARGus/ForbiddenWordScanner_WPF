using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForbiddenWordScanner_WPF
{
    public static class RaportGenerator
    {
        private static List<string> _logs = new List<string>();
        private static Dictionary<string, int> _wordCounts = new Dictionary<string, int>();

        public static void Log(string filePath, int size)
        {
            lock (_logs)
            {
                _logs.Add($"{filePath} | Size: {size} bytes");
            }
        }

        public static void SaveTopWords(List<string> words)
        {
            var report = new StringBuilder();
            report.AppendLine("=== REPORT ===");
            foreach (var log in _logs)
                report.AppendLine(log);

            var top10 = _wordCounts.OrderByDescending(kv => kv.Value).Take(10);
            report.AppendLine("\nTop 10 forbidden words:");
            foreach (var word in top10)
                report.AppendLine($"{word.Key} - {word.Value}");

            File.WriteAllText("report.txt", report.ToString());
        }

        public static void AddWordCount(string word, int count)
        {
            lock (_wordCounts)
            {
                if (_wordCounts.ContainsKey(word))
                    _wordCounts[word] += count;
                else
                    _wordCounts[word] = count;
            }
        }
    }
}
