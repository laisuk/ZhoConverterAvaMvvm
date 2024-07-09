using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using JiebaNet.Analyser;
using JiebaNet.Segmenter;
using OpenccFmmsegNetLib;
using ZhoConverterAvaMvvm.Services;

namespace ZhoConverterAvaMvvm.Views;

public partial class MainWindow : Window
{
    private readonly List<Language>? _languagesInfo;
    private readonly List<string>? _textFileTypes;
    private string? _currentOpenFileName;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(LanguageSettingsService languageSettingsService) : this()
    {
        var languageSettings = languageSettingsService.LanguageSettings;
        _languagesInfo = languageSettings!.Languages;
        _textFileTypes = languageSettings.TextFileTypes;
        _currentOpenFileName = string.Empty;
    }

    private void RbT2s_Click(object? sender, RoutedEventArgs e)
    {
        LblSourceCode.Content = _languagesInfo![1].Name;
    }

    private void RbS2t_Click(object? sender, RoutedEventArgs e)
    {
        LblSourceCode.Content = _languagesInfo![2].Name;
    }

    private void RbStd_Click(object? sender, RoutedEventArgs e)
    {
        CbCnTw.IsEnabled = false;
        CbCnTw.IsChecked = false;
    }

    private void RbZHTW_Click(object? sender, RoutedEventArgs e)
    {
        CbCnTw.IsEnabled = true;
        CbCnTw.IsChecked = true;
    }

    private void RbHK_Click(object? sender, RoutedEventArgs e)
    {
        CbCnTw.IsEnabled = false;
        CbCnTw.IsChecked = false;
    }

    private void TabMain_GotFocus(object? sender, GotFocusEventArgs e)
    {
        BtnOpenFile.IsEnabled = true;
        BtnOpenFile.IsVisible = true;
        BtnSaveFile.IsEnabled = true;
        BtnSaveFile.IsVisible = true;
        BtnProcess.IsEnabled = true;
        BtnProcess.IsVisible = true;
        LblFileName.IsVisible = true;
        BtnBatchStart.IsEnabled = false;
        BtnBatchStart.IsVisible = false;
        TabMain.FontWeight = FontWeight.Black;
        TabBatch.FontWeight = FontWeight.Normal;
    }

    private void TbSource_TextChanged(object? sender, EventArgs eventArgs)
    {
        LblTotalChars.Content = $"[ Chars: {TbSource.Text!.Length:N0} ]";
    }

    private void BtnClearSource_Click(object? sender, RoutedEventArgs e)
    {
        TbSource.Clear();
        _currentOpenFileName = string.Empty;
        LblSourceCode.Content = string.Empty;
        LblFileName.Content = string.Empty;
        LblStatusBar.Content = "Source text box cleared";
    }

    private async void BtnPaste_Click(object? sender, RoutedEventArgs e)
    {
        var inputText = await Clipboard!.GetTextAsync();

        if (string.IsNullOrEmpty(inputText))
        {
            LblStatusBar.Content = "Clipboard is empty.";
            return;
        }

        TbSource.Text = inputText;
        LblStatusBar.Content = "Clipboard content pasted";
        var codeText = OpenccFmmsegNet.ZhoCheck(inputText);
        UpdateEncodeInfo(codeText);
        LblFileName.Content = string.Empty;
        _currentOpenFileName = string.Empty;
    }

    private void UpdateEncodeInfo(int codeText)
    {
        switch (codeText)
        {
            case 1:
                LblSourceCode.Content = _languagesInfo![codeText].Name;
                if (RbT2S.IsChecked == false) RbT2S.IsChecked = true;

                break;

            case 2:
                LblSourceCode.Content = _languagesInfo![codeText].Name;
                if (RbS2T.IsChecked == false) RbS2T.IsChecked = true;

                break;

            default:
                LblSourceCode.Content = _languagesInfo![0].Name;
                break;
        }
    }

    private void BtnClearDestination_Click(object? sender, RoutedEventArgs e)
    {
        TbDestination.Clear();
        LblDestinationCode.Content = string.Empty;
        LblStatusBar.Content = "Destination contents cleared";
    }

    private async void BtnCopy_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(TbDestination.Text))
        {
            LblStatusBar.Content = "Not copied: Destination content is empty.";
            return;
        }

        try
        {
            await Clipboard!.SetTextAsync(TbDestination.Text);
            LblStatusBar.Content = "Text copied to clipboard";
        }
        catch (Exception ex)
        {
            LblStatusBar.Content = $"Clipboard error: {ex.Message}";
        }
    }

    private void TabBatch_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (!Directory.Exists(TbOutFolder.Text))
            TbOutFolder.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");

        BtnOpenFile.IsEnabled = false;
        BtnOpenFile.IsVisible = false;
        BtnSaveFile.IsEnabled = false;
        BtnSaveFile.IsVisible = false;
        BtnProcess.IsEnabled = false;
        BtnProcess.IsVisible = false;
        LblFileName.IsVisible = false;
        BtnBatchStart.IsEnabled = true;
        BtnBatchStart.IsVisible = true;
        TabBatch.FontWeight = FontWeight.Black;
        TabMain.FontWeight = FontWeight.Normal;
    }

    private async void BtnAdd_Click(object? sender, RoutedEventArgs e)
    {
        var mainWindow = this;

        var storageProvider = mainWindow.StorageProvider;
        var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Text File",
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("Text Files") { Patterns = new[] { "*.txt" } }
            },
            AllowMultiple = true
        });

        if (result.Count <= 0) return;
        var listBoxItems = LbxSource.Items.ToList();
        foreach (var file in result)
        {
            var path = file.Path.LocalPath;
            if (!listBoxItems.Contains(path))
                listBoxItems.Add(path);
        }
        var sortedList = listBoxItems.OrderBy(x => x);
        LbxSource.Items.Clear();
        foreach (var item in sortedList)
        {
            LbxSource.Items.Add(item);
        }
    }

    private void BtnRemove_Click(object? sender, RoutedEventArgs e)
    {
        var index = LbxSource.SelectedIndex;
        var name = LbxSource.SelectedItem as string;
        if (LbxSource.SelectedIndex == -1)
        {
            LblStatusBar.Content = "Nothing to remove.";
            return;
        }

        LbxSource.Items.Remove(LbxSource.SelectedItem);
        //lbxSource.Items.RemoveAt(lbxSource.SelectedIndex);
        LblStatusBar.Content = $"Item ({index}) {name} removed";
    }

    private void BtnPreview_Click(object? sender, RoutedEventArgs e)
    {
        if (LbxSource.SelectedIndex == -1)
        {
            LblStatusBar.Content = "Nothing to preview.";
            return;
        }

        var filename = LbxSource.SelectedItem as string;

        if (!_textFileTypes!.Contains(Path.GetExtension(filename)!))
        {
            TabMessage.IsSelected = true;
            LbxDestination.Items.Add("File type [" + Path.GetExtension(filename)! + "] Preview not supported");
            return;
        }

        try
        {
            var displayText = File.ReadAllText(filename!);
            TabPreview.IsSelected = true;
            TbPreview.Text = displayText;
        }
        catch (Exception)
        {
            TabPreview.IsSelected = true;
            LbxDestination.Items.Add($"File read error: {filename}");
            LblStatusBar.Content = "File read error.";
        }
    }

    private void BtnDetect_Click(object? sender, RoutedEventArgs e)
    {
        if (LbxSource.Items.Count == 0)
        {
            LblStatusBar.Content = "Nothing to detect.";
            return;
        }

        TabMessage.IsSelected = true;
        LbxDestination.Items.Clear();

        foreach (var item in LbxSource.Items)
        {
            var sourceFilePath = item!.ToString();
            var fileExt = Path.GetExtension(sourceFilePath)!;

            if (_textFileTypes!.Contains(fileExt))
            {
                string inputText;
                try
                {
                    inputText = File.ReadAllText(sourceFilePath!);
                }
                catch (Exception)
                {
                    LbxDestination.Items.Add(sourceFilePath + " -> File read error.");
                    continue;
                }

                var textCode = _languagesInfo![OpenccFmmsegNet.ZhoCheck(inputText)].Name!;
                LbxDestination.Items.Add($"[{textCode}] {sourceFilePath}");
            }
            else
            {
                LbxDestination.Items.Add($"[File skipped ({fileExt})] {sourceFilePath}");
            }
        }

        LblStatusBar.Content = "Batch zho code detection done.";
    }

    private void BtnClearListBox_Click(object? sender, RoutedEventArgs e)
    {
        LbxSource.Items.Clear();
        LblStatusBar.Content = "All source entries cleared.";
    }

    private async void BtnSelectOutFolder_Click(object? sender, RoutedEventArgs e)
    {
        var mainWindow = this;

        // Show folder picker dialog
        var result = await mainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Output Folder"
            // InitialDirectory is not supported in FolderPickerOpenOptions, can be handled differently if needed
        });

        // Process folder picker dialog results
        if (result.Count > 0)
        {
            var folderPath = result[0].Path.LocalPath;
            TbOutFolder.Text = folderPath;
            TbOutFolder.Focus();
        }
    }

    private void BtnMessagePreviewClear_Click(object? sender, RoutedEventArgs e)
    {
        if (TabMessage.IsSelected)
            LbxDestination.Items.Clear();
        else if (TabPreview.IsSelected) TbPreview.Text = string.Empty;
    }

    private void BtnProcess_Click(object? sender, RoutedEventArgs e)
    {
        if (TabBatch.IsSelected) return;

        if (string.IsNullOrEmpty(TbSource.Text))
        {
            LblStatusBar.Content = "Source content is empty.";
            return;
        }

        var config = GetCurrentConfig();
        var convertedText = OpenccFmmsegNet.Convert(TbSource.Text, config, (bool)CbPunctuation.IsChecked!);

        TbDestination.Text = convertedText;

        if (RbT2S.IsChecked == true)
        {
            LblDestinationCode.Content = LblSourceCode.Content!.ToString()!.Contains("Non")
                ? LblSourceCode.Content
                : _languagesInfo![2].Name;
        }
        else if (RbS2T.IsChecked == true)
        {
            LblDestinationCode.Content = LblSourceCode.Content!.ToString()!.Contains("Non")
                ? LblSourceCode.Content
                : _languagesInfo![1].Name;
        }
        else if (RbJieba.IsChecked == true)
        {
            TbDestination.Text = string.Join("/", new JiebaSegmenter().Cut(TbSource.Text));
            LblDestinationCode.Content = LblSourceCode.Content;
        }
        else if (RbTag.IsChecked == true)
        {
            var wordCount = int.Parse(TbWordCount.Text!) < 10 ? 10 : int.Parse(TbWordCount.Text!);
            TbDestination.Text = "===== TextRank Method =====\n" + string.Join("/ ",
                new TextRankExtractor().ExtractTags(TbSource.Text, wordCount));
            TbDestination.Text = TbDestination.Text + "\n\n====== TF-IDF Method ======\n" +
                                 string.Join("/ ",
                                     new TfidfExtractor().ExtractTags(TbSource.Text, wordCount));
            if (TbDestination.Text.Length == 0)
            {
                LblDestinationCode.Content = string.Empty;
                return;
            }

            LblDestinationCode.Content = LblSourceCode.Content;
        }
        else
        {
            return;
        }

        LblStatusBar.Content = "Process completed";
    }

    private async void BtnBatchStart_Click(object? sender, RoutedEventArgs e)
    {
        if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output")))
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output"));

        if (LbxSource.Items.Count == 0)
        {
            LblStatusBar.Content = "Nothing to convert.";
            return;
        }

        if (!Directory.Exists(TbOutFolder.Text))
        {
            await MessageBox.Show("Invalid output folder:\n " + TbOutFolder.Text, "Error", this);
            TbOutFolder.Focus();
            return;
        }

        if (RbS2T.IsChecked == false && RbT2S.IsChecked == false)
        {
            await MessageBox.Show("Please select conversion type:\n zh-Hans / zh-Hant", "Error", this);
            return;
        }

        var conversion = (bool)RbS2T.IsChecked! ? RbS2T.Content!.ToString()! : RbT2S.Content!.ToString()!;
        var region = (bool)RbStd.IsChecked!
            ? RbStd.Content!.ToString()!
            : (bool)RbHk.IsChecked!
                ? RbHk.Content!.ToString()!
                : RbZhtw.Content!.ToString()!;
        var iSZhTwIdioms = (bool)CbCnTw.IsChecked! ? "Yes" : "No";
        var isPunctuations = (bool)CbPunctuation.IsChecked! ? "Yes" : "No";

        TabMessage.IsSelected = true;
        LbxDestination.Items.Clear();
        LbxDestination.Items.Add($"Conversion Type (转换方式) => {conversion}");
        LbxDestination.Items.Add($"Region (区域) => {region}");
        LbxDestination.Items.Add($"ZH/TW Idioms (中台惯用语) => {iSZhTwIdioms}");
        LbxDestination.Items.Add($"Punctuations (标点) => {isPunctuations}");
        LbxDestination.Items.Add($"Output folder: (输出文件夹) => {TbOutFolder.Text}");

        var count = 0;

        foreach (var item in LbxSource.Items)
        {
            count++;
            var sourceFilePath = item!.ToString();
            // var basename = Path.GetFileName(sourceFilePath)!;
            var fileExt = Path.GetExtension(sourceFilePath)!;
            var filenameWithoutExt = Path.GetFileNameWithoutExtension(sourceFilePath);

            if (!File.Exists(sourceFilePath))
            {
                LbxDestination.Items.Add($"({count}) {sourceFilePath} -> File not found.");
                continue;
            }

            if (!_textFileTypes!.Contains(fileExt))
            {
                LbxDestination.Items.Add($"({count}) [File skipped ({fileExt})] {sourceFilePath}");
                continue;
            }

            string inputText;
            try
            {
                inputText = await File.ReadAllTextAsync(sourceFilePath);
            }
            catch (Exception)
            {
                LbxDestination.Items.Add($"({count}) {sourceFilePath} -> Conversion error.");
                continue;
            }

            string convertedText;
            string suffix;
            var config = GetCurrentConfig();
            if (RbT2S.IsChecked == true)
            {
                suffix = "(Hans)";
                convertedText = OpenccFmmsegNet.Convert(inputText, config, (bool)CbPunctuation.IsChecked);
            }
            else if (RbS2T.IsChecked == true)
            {
                suffix = "(Hant)";
                convertedText = OpenccFmmsegNet.Convert(inputText, config, (bool)CbPunctuation.IsChecked);
            }
            else
            {
                suffix = "(Other)";
                convertedText = inputText;
            }

            var outputFilename = Path.Combine(Path.GetFullPath(TbOutFolder.Text),
                filenameWithoutExt + suffix + fileExt);
            await File.WriteAllTextAsync(outputFilename, convertedText);

            LbxDestination.Items.Add($"({count}) {outputFilename} -> [Done ✓]");
        }

        LblStatusBar.Content = "Batch conversion done.";
    }

    private async void BtnOpenFile_Click(object? sender, RoutedEventArgs e)
    {
        var mainWindow = this;

        var storageProvider = mainWindow.StorageProvider;
        var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Text File",
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("Text Files") { Patterns = new[] { "*.txt" } }
            },
            AllowMultiple = false
        });

        if (result.Count <= 0) return;
        var file = result[0];
        {
            var path = file.Path.LocalPath;
            UpdateTbSourceFileContents(path);
        }
    }

    private async void UpdateTbSourceFileContents(string filename)
    {
        var fileInfo = new FileInfo(filename);
        if (fileInfo.Length > int.MaxValue)
        {
            LblStatusBar.Content = "Error: File too large";
            return;
        }

        _currentOpenFileName = filename;

        // Read file contents
        try
        {
            using var reader = new StreamReader(_currentOpenFileName);
            var contents = await reader.ReadToEndAsync();
            // Display file contents to text box field
            TbSource.Clear();
            TbSource.Text = contents;
            LblStatusBar.Content = $"File: {_currentOpenFileName}";
            var displayName = fileInfo.Name;
            LblFileName.Content =
                displayName.Length > 50 ? $"{displayName[..25]}...{displayName[^15..]}" : displayName;
            var codeText = OpenccFmmsegNet.ZhoCheck(contents);
            UpdateEncodeInfo(codeText);
        }
        catch (Exception)
        {
            TbSource.Clear();
            LblSourceCode.Content = string.Empty;
            LblStatusBar.Content = "Error: Invalid file";
            //throw;
        }
    }

    private async void BtnSaveFile_Click(object? sender, RoutedEventArgs e)
    {
        var mainWindow = this;

        var storageProvider = mainWindow.StorageProvider;
        var result = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Text File",
            SuggestedFileName = "document.txt",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new("Text Files") { Patterns = new[] { "*.txt" } }
            }
        });

        if (result != null)
        {
            var path = result.Path.LocalPath;
            await File.WriteAllTextAsync(path, TbDestination.Text);
        }
    }

    private void BtnExit_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private string GetCurrentConfig()
    {
        var config = RbS2T.IsChecked == true
            ? RbStd.IsChecked == true
                ? "s2t"
                : RbHk.IsChecked == true
                    ? "s2hk"
                    : CbCnTw.IsChecked == true
                        ? "s2twp"
                        : "s2tw"
            : RbStd.IsChecked == true
                ? "t2s"
                : RbHk.IsChecked == true
                    ? "t2hk"
                    : CbCnTw.IsChecked == true
                        ? "tw2sp"
                        : "tw2s";
        return config;
    }
}