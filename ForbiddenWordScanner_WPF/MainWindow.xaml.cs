using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ForbiddenWordScanner_WPF
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource _cts;
        private ScanService _scanService;
        public MainWindow()
        {
            InitializeComponent();
            if (!SingleInstance.EnsureSingleInstance())
            {
                MessageBox.Show("Приложение уже запущено.");
                Application.Current.Shutdown();
            }

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string resultsDir = System.IO.Path.Combine(baseDir, "Results");

            DirectoryOutPath.Text = resultsDir;
            UpdateWordScannerUI(false, false);
        }

        private void SelectForbiddenWordButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                ForbiddenWordTextBox.Text = File.ReadAllText(dialog.FileName);
            }
        }

        private void SelectDirectoryPathButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Выберите папку для поиска";
                dialog.ShowNewFolderButton = false;

                System.Windows.Forms.DialogResult result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    SearchDerictoryPath.Text = dialog.SelectedPath;
                }
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(DirectoryOutPath.Text.ToString()))
            {
                Directory.CreateDirectory(DirectoryOutPath.Text.ToString());
            }
            var words = ForbiddenWordTextBox.Text
                .Split(new[] { ' ', '\n', '\r', '\t', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.Trim())
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (words == null || words.Count == 0)
            {
                MessageBox.Show("Список запрещенных слов пуст.");
                return;
            }

            UpdateWordScannerUI(true, false);

            _cts = new CancellationTokenSource();
            _scanService = new ScanService(words, DirectoryOutPath.Text, UpdateProgress, _cts.Token, SearchDerictoryPath.Text);
            await Task.Run(() => _scanService.StartScan());
        }

        private void UpdateProgress(int percent)
        {
            Dispatcher.Invoke(() => SearchProgresBar.Value = percent);
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateWordScannerUI(true, true);
            _scanService.PausedScan();
        }

        private void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateWordScannerUI(true, false);
            _scanService.ResumedScan();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateWordScannerUI(false, false);
            _cts?.Cancel();
        }

        private void SelectDirectoryOutPathButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DirectoryOutPath.Text = dialog.SelectedPath;
            }
        }

        private void UpdateWordScannerUI(bool isRunning, bool isPaused)
        {
            StartButton.IsEnabled = !isRunning;
            PauseButton.IsEnabled = !isPaused && isRunning;
            ResumeButton.IsEnabled = isPaused && isRunning;
            StopButton.IsEnabled = isRunning;
        }
    }
}
