using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ImageAIRenamer.Models;
using ImageAIRenamer.Services;
using Microsoft.Win32; 

namespace ImageAIRenamer
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<ImageItem> Images { get; set; } = new();
        private string[] SupportedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        
        public MainWindow()
        {
            InitializeComponent();
            ImagesGrid.ItemsSource = Images;
            LoadApiKeysFromFile();
        }

        private void BrowseSource_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                SourceFolderBox.Text = dialog.FolderName;
                LoadImages(dialog.FolderName);
            }
        }

        private void BrowseOutput_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                OutputFolderBox.Text = dialog.FolderName;
                CheckReady();
            }
        }

        private void LoadImages(string folder)
        {
            Images.Clear();
            if (!Directory.Exists(folder)) return;

            var files = Directory.GetFiles(folder);
            foreach (var file in files)
            {
                var ext = Path.GetExtension(file).ToLower();
                if (SupportedExtensions.Contains(ext))
                {
                    Images.Add(new ImageItem
                    {
                        FilePath = file,
                        OriginalName = Path.GetFileNameWithoutExtension(file),
                        Status = "Pending"
                    });
                }
            }
            StatusText.Text = $"Loaded {Images.Count} images.";
            CheckReady();
        }

        private void ClearInstructions_Click(object sender, RoutedEventArgs e)
        {
            CustomInstructionsBox.Clear();
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            Images.Clear();
            SourceFolderBox.Text = string.Empty;
            OutputFolderBox.Text = string.Empty;
            CustomInstructionsBox.Clear();
            StatusText.Text = string.Empty;
            ProgressBar.Value = 0;
            ProgressBar.Visibility = Visibility.Collapsed;
            CheckReady();
        }

        private string GetConfigFilePath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var folder = Path.Combine(appData, "ImageAIRenamer");
            try
            {
                Directory.CreateDirectory(folder);
            }
            catch
            {
            }
            return Path.Combine(folder, "apikeys.txt");
        }

        private void SaveApiKeysToFile()
        {
            try
            {
                var path = GetConfigFilePath();
                File.WriteAllText(path, ApiKeysBox.Text ?? string.Empty);
            }
            catch
            {
            }
        }

        private void LoadApiKeysFromFile()
        {
            try
            {
                var path = GetConfigFilePath();
                if (File.Exists(path))
                {
                    ApiKeysBox.Text = File.ReadAllText(path);
                }
            }
            catch
            {
            }
        }

        private void CheckReady()
        {
            ProcessButton.IsEnabled = Images.Count > 0 && !string.IsNullOrWhiteSpace(OutputFolderBox.Text);
        }

        private async void Process_Click(object sender, RoutedEventArgs e)
        {
            var apiKeys = ApiKeysBox.Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (apiKeys.Length == 0)
            {
                MessageBox.Show("Please enter at least one Gemini API Key.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SaveApiKeysToFile();

            var outputFolder = OutputFolderBox.Text;
            if (!Directory.Exists(outputFolder))
            {
                try { Directory.CreateDirectory(outputFolder); }
                catch { MessageBox.Show("Could not create output directory.", "Error"); return; }
            }

            ProcessButton.IsEnabled = false;
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.Maximum = Images.Count;
            ProgressBar.Value = 0;

            var service = new GeminiService(apiKeys);
            var usedNames = new Dictionary<string, int>();
            var customInstructions = CustomInstructionsBox.Text;

            foreach (var img in Images)
            {
                img.Status = "Renaming...";
                
                try
                {
                    var title = await service.GenerateTitleAsync(img.FilePath, customInstructions);
                    var sanitized = SanitizeFilename(title);
                    
                    if (string.IsNullOrWhiteSpace(sanitized)) sanitized = "Image";
                    
                    var baseName = sanitized;
                    int counter = 0;
                    
                    // Check if this name was already used in this batch
                    if (usedNames.ContainsKey(baseName))
                    {
                        counter = usedNames[baseName];
                    }
                    usedNames[baseName] = counter + 1;
                    
                    if (counter > 0)
                    {
                        sanitized = $"{baseName}_{counter}";
                    }

                    var ext = Path.GetExtension(img.FilePath);
                    var newFileName = sanitized + ext;
                    var destPath = Path.Combine(outputFolder, newFileName);
                    
                    // Handle existing files in the folder to avoid overwrite if random collision
                    int diskCounter = 1;
                    string uniquePath = destPath;
                    while (File.Exists(uniquePath))
                    {
                         uniquePath = Path.Combine(outputFolder, $"{sanitized}_{diskCounter}{ext}");
                         diskCounter++;
                    }
                    
                    newFileName = Path.GetFileName(uniquePath);

                    File.Copy(img.FilePath, uniquePath, true);
                    
                    img.NewName = newFileName;
                    img.Status = "Done";
                }
                catch (Exception)
                {
                    img.Status = "Error";
                }
                
                ProgressBar.Value++;
            }

            ProgressBar.Visibility = Visibility.Collapsed;
            ProcessButton.IsEnabled = true;
            StatusText.Text = "Processing Complete!";
            MessageBox.Show("Processing Complete!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        protected override void OnClosed(EventArgs e)
        {
            SaveApiKeysToFile();
            base.OnClosed(e);
        }

        private string SanitizeFilename(string name)
        {
             var s = Regex.Replace(name, @"\s+", "_");
             s = Regex.Replace(s, @"[^\p{L}\p{N}_]", "");
             s = Regex.Replace(s, @"_+", "_");
             s = s.Trim('_');
             return s;
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image img && img.DataContext is ImageItem item)
            {
                 try
                 {
                     Process.Start(new ProcessStartInfo(item.FilePath) { UseShellExecute = true });
                 }
                 catch { }
            }
        }
    }
}
