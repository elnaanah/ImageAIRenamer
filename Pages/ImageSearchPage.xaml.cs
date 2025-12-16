using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ImageAIRenamer.Models;
using ImageAIRenamer.Services;
using Microsoft.Win32;

namespace ImageAIRenamer.Pages
{
    public partial class ImageSearchPage : Page
    {
        public ObservableCollection<ImageItem> MatchedImages { get; set; } = new();
        private string[] SupportedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private GeminiService? _geminiService;

        public ImageSearchPage()
        {
            InitializeComponent();
            MatchedImagesGrid.ItemsSource = MatchedImages;
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
            MatchedImages.Clear();
            if (!Directory.Exists(folder)) return;

            var files = Directory.GetFiles(folder);
            foreach (var file in files)
            {
                var ext = Path.GetExtension(file).ToLower();
                if (SupportedExtensions.Contains(ext))
                {
                    MatchedImages.Add(new ImageItem
                    {
                        FilePath = file,
                        OriginalName = Path.GetFileNameWithoutExtension(file),
                        Status = "في الانتظار",
                        IsSelected = true
                    });
                }
            }
            StatusText.Text = $"تم تحميل {MatchedImages.Count} صورة.";
            CheckReady();
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            MatchedImages.Clear();
            SourceFolderBox.Text = string.Empty;
            OutputFolderBox.Text = string.Empty;
            SearchDescriptionBox.Clear();
            StatusText.Text = string.Empty;
            ProgressText.Text = string.Empty;
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
            SearchButton.IsEnabled = MatchedImages.Count > 0 
                && !string.IsNullOrWhiteSpace(OutputFolderBox.Text)
                && !string.IsNullOrWhiteSpace(SearchDescriptionBox.Text);
        }

        private void SearchDescriptionBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckReady();
        }

        private void ApiKeysBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Optional: Auto-save on change
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            var apiKeys = ApiKeysBox.Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (apiKeys.Length == 0)
            {
                MessageBox.Show("الرجاء إدخال مفتاح Gemini API واحد على الأقل.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(SearchDescriptionBox.Text))
            {
                MessageBox.Show("الرجاء إدخال وصف البحث.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SaveApiKeysToFile();

            var outputFolder = OutputFolderBox.Text;
            if (!Directory.Exists(outputFolder))
            {
                try { Directory.CreateDirectory(outputFolder); }
                catch { MessageBox.Show("تعذر إنشاء مجلد الإخراج.", "خطأ"); return; }
            }

            SearchButton.IsEnabled = false;
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.Maximum = MatchedImages.Count;
            ProgressBar.Value = 0;

            _geminiService = new GeminiService(apiKeys);
            var searchDescription = SearchDescriptionBox.Text;
            var usedNames = new Dictionary<string, int>();
            int processedCount = 0;
            int matchedCount = 0;

            foreach (var img in MatchedImages)
            {
                img.Status = "جاري المعالجة...";
                ProgressText.Text = $"جاري المعالجة {processedCount + 1} من {MatchedImages.Count}...";
                
                try
                {
                    var result = await _geminiService.SearchImageAsync(img.FilePath, searchDescription);
                    
                    if (result.IsMatch)
                    {
                        img.Status = "مطابق";
                        matchedCount++;
                        
                        // Generate name if not provided
                        if (string.IsNullOrWhiteSpace(result.SuggestedName))
                        {
                            result.SuggestedName = await _geminiService.GenerateTitleAsync(img.FilePath);
                        }
                        
                        var sanitized = SanitizeFilename(result.SuggestedName ?? "صورة");
                        if (string.IsNullOrWhiteSpace(sanitized)) sanitized = "صورة";
                        
                        var baseName = sanitized;
                        int counter = 0;
                        
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
                        
                        int diskCounter = 1;
                        string uniquePath = destPath;
                        while (File.Exists(uniquePath))
                        {
                            uniquePath = Path.Combine(outputFolder, $"{sanitized}_{diskCounter}{ext}");
                            diskCounter++;
                        }
                        
                        newFileName = Path.GetFileName(uniquePath);
                        img.NewName = newFileName;
                    }
                    else
                    {
                        img.Status = "غير مطابق";
                        img.IsSelected = false;
                    }
                }
                catch (Exception)
                {
                    img.Status = "خطأ";
                    img.IsSelected = false;
                }
                
                processedCount++;
                ProgressBar.Value = processedCount;
            }

            ProgressBar.Visibility = Visibility.Collapsed;
            SearchButton.IsEnabled = true;
            StatusText.Text = $"اكتمل البحث! تم العثور على {matchedCount} صورة مطابقة من أصل {MatchedImages.Count}.";
            ProgressText.Text = $"مطابق: {matchedCount} / {MatchedImages.Count}";
        }

        private void CopySelected_Click(object sender, RoutedEventArgs e)
        {
            var outputFolder = OutputFolderBox.Text;
            if (!Directory.Exists(outputFolder))
            {
                MessageBox.Show("الرجاء اختيار مجلد الإخراج أولاً.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var selectedImages = MatchedImages.Where(i => i.IsSelected && i.Status == "مطابق").ToList();
            
            if (selectedImages.Count == 0)
            {
                MessageBox.Show("الرجاء تحديد صورة واحدة على الأقل للنسخ.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int copiedCount = 0;
            int skippedCount = 0;
            
            foreach (var img in selectedImages)
            {
                try
                {
                    var ext = Path.GetExtension(img.FilePath);
                    
                    // إذا لم يكن هناك اسم جديد، استخدم الاسم الأصلي
                    var fileName = !string.IsNullOrWhiteSpace(img.NewName) 
                        ? img.NewName 
                        : Path.GetFileName(img.FilePath);
                    
                    var destPath = Path.Combine(outputFolder, fileName);
                    
                    // تجنب التعارض في الأسماء
                    int counter = 1;
                    string uniquePath = destPath;
                    while (File.Exists(uniquePath))
                    {
                        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                        uniquePath = Path.Combine(outputFolder, $"{nameWithoutExt}_{counter}{ext}");
                        counter++;
                    }
                    
                    File.Copy(img.FilePath, uniquePath, true);
                    img.NewName = Path.GetFileName(uniquePath);
                    img.Status = "تم النسخ";
                    copiedCount++;
                }
                catch (Exception ex)
                {
                    img.Status = "خطأ في النسخ";
                    skippedCount++;
                }
            }

            string message = copiedCount > 0 
                ? $"تم نسخ {copiedCount} صورة بنجاح إلى مجلد الإخراج."
                : "لم يتم نسخ أي صورة.";
            
            if (skippedCount > 0)
            {
                message += $"\n{skippedCount} صورة فشل نسخها.";
            }

            MessageBox.Show(message, copiedCount > 0 ? "نجح" : "تنبيه", MessageBoxButton.OK, 
                copiedCount > 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
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

        private void BackToHome_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.NavigateToWelcome();
            }
        }
    }
}
