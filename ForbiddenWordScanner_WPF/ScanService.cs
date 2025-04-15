using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;

namespace ForbiddenWordScanner_WPF
{
    public class ScanService
    {
        private List<string> _forbiddenWords; // Список плохих слов
        private string _outDerictoryPath; // Папка сохранения отчета и измененных документов
        private string _selectSearchDerictoryPath; // Дериктория поиска, по умолчанию все дериктории на компьютере
        private Action<int> _progressBarCallback; // Прогресс бар
        private CancellationToken _token; //
        private ManualResetEvent _pauseEvent = new ManualResetEvent(true); // Сигнал состояния
        private object _lock = new object(); // 

        public ScanService(List<string> forbiddenWords, string outDerictoryPath, Action<int> progressBarCallback, CancellationToken token, string selectSearchDerictoryPath = null)
        {
            _forbiddenWords = forbiddenWords;
            _outDerictoryPath = outDerictoryPath;
            _progressBarCallback = progressBarCallback;
            _token = token;
            _selectSearchDerictoryPath = selectSearchDerictoryPath;
        }

        public void StartScan()
        {
            List<string> searchFilesInDerictory = FindFiles();
            int total  = searchFilesInDerictory.Count;
            int processed = 0;

            Parallel.ForEach(searchFilesInDerictory, new ParallelOptions { CancellationToken = _token }, file =>
            {
                _pauseEvent.WaitOne();

                try
                {
                    string content = File.ReadAllText(file);
                    bool contains = _forbiddenWords.Any(w => content.Contains(w));
                    if (contains)
                    {
                        string dest = Path.Combine(_outDerictoryPath, Path.GetFileName(file));
                        File.Copy(file, dest, true);

                        string replase = ReplaseWords(content);
                        File.WriteAllText(Path.Combine(_outDerictoryPath, "replaced_" + Path.GetFileName(file)), replase);
                        //lock (_lock) { RaportGenerator.Log(file, content.Length); }
                        lock (_lock)
                        {
                            foreach (var word in _forbiddenWords)
                            {
                                int count = Regex.Matches(content, Regex.Escape(word), RegexOptions.IgnoreCase).Count;
                                RaportGenerator.AddWordCount(word, count);
                            }

                            RaportGenerator.Log(file, content.Length);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                Interlocked.Increment(ref processed);
                _progressBarCallback((processed * 100) / total);
            });
            RaportGenerator.SaveTopWords(_forbiddenWords);
        }

        private List<string> FindFiles()
        {
            List<string> newFindFiles = new List<string>();
            if(!string.IsNullOrEmpty(_selectSearchDerictoryPath))
            {
                try
                {
                    if(Directory.Exists(_selectSearchDerictoryPath))
                    {
                        var files = Directory.GetFiles(_selectSearchDerictoryPath, "*.*", SearchOption.AllDirectories);
                        newFindFiles.AddRange(files);
                    }
                    else
                    {
                        MessageBox.Show($"Дериктория {_selectSearchDerictoryPath} не найдена");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка доступа к директории: {ex.Message}");
                }
            }
            else
            {
                DriveInfo[] drives = DriveInfo.GetDrives();
                foreach (DriveInfo drive in drives)
                {
                    if (!drive.IsReady)
                        continue;

                    if (drive.DriveType != DriveType.Fixed && drive.DriveType != DriveType.Removable)
                        continue;

                    try
                    {
                        var files = Directory.GetFiles(drive.RootDirectory.FullName, "*.*", SearchOption.AllDirectories);
                        newFindFiles.AddRange(files);
                    }
                    catch(Exception ex)
                    { 
                        MessageBox.Show($"Ошибка доступа к {drive.Name}: {ex.Message}"); 
                    }
                }
            }

                return newFindFiles;
        }

        private string ReplaseWords(string content)
        {
            foreach(var word in _forbiddenWords)
            {
                content = Regex.Replace(content, Regex.Escape(word), "*******", RegexOptions.IgnoreCase);
            }
            return content;
        }

        public void StopScan()
        {
            _pauseEvent.Reset();
            _pauseEvent.Dispose();
        }

        public void PausedScan ()
        {
            _pauseEvent.Reset();
        }

        public void ResumedScan()
        {
            _pauseEvent.Set();
        }
    }
}
