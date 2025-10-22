using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using AvaloniaEdit.Document;
using OpenccFmmsegLib;
using OpenccJiebaLib;
using ReactiveUI;
using ZhoConverterAvaMvvm.Services;
using ZhoConverterAvaMvvm.Views;
using System.Diagnostics;
using ZhoConverterAvaMvvm.Models;

namespace ZhoConverterAvaMvvm.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    // private readonly List<Language>? _languagesInfo;
    private readonly Language? _selectedLanguage;
    private readonly List<string>? _textFileTypes;
    private readonly List<string>? _officeFileTypes;
    private readonly ITopLevelService? _topLevelService;
    private bool _isBtnBatchStartVisible;
    private bool _isBtnOpenFileVisible = true;
    private bool _isBtnProcessVisible = true;
    private bool _isBtnSaveFileVisible = true;
    private bool _isCbJieba;
    private bool _isCbPunctuation = true;
    private bool _isCbZhtw;
    private bool _isCbZhtwEnabled;
    private bool _isCbConvertFilename;
    private bool _isLblFileNameVisible = true;
    private bool _isRbHk;
    private bool _isRbSegment;
    private bool _isRbS2T;
    private bool _isRbStd = true;
    private bool _isRbT2S = true;
    private bool _isRbTag;
    private bool _isRbCustom;
    private bool _isRbZhtw;
    private bool _isTabBatch;
    private bool _isTabMain = true;
    private bool _isTabMessage = true;
    private bool _isTabPreview;
    private bool _isTbOutFolderFocus;
    private string? _lblDestinationCodeContent = string.Empty;
    private string? _lblFilenameContent = string.Empty;
    private string? _lblSourceCodeContent = string.Empty;
    private string? _lblStatusBarContent = string.Empty;
    private string? _lblTotalCharsContent = string.Empty;
    private ObservableCollection<string>? _lbxDestinationItems;
    private ObservableCollection<string>? _lbxSourceItems;
    private int _lbxSourceSelectedIndex;
    private string? _lbxSourceSelectedItem = string.Empty;
    private string? _rbT2SContent = "Hant (繁) to Hans (简)";
    private string? _rbS2TContent = "Hans (简) to Hant (繁)";
    private string? _rbSegmentContent = "Segment (分词)";
    private string? _rbTagContent = "Keywords (关键词)";
    private string? _rbStdContent = "General (通用简繁)";
    private string? _rbZhtwContent = "ZH-TW (中台简繁)";
    private string? _rbHkContent = "ZH-HK (中港简繁)";
    private string? _cbZhtwContent = "ZH-TW Idioms (中台惯用语)";
    private string? _cbPunctuationContent = "Punctuation (标点)";
    private string? _cbJiebaContent = "Jieba (结巴分词)";
    private FontWeight _tabBatchFontWeight = FontWeight.Normal;
    private FontWeight _tabMainFontWeight = FontWeight.Bold;
    private TextDocument? _tbDestinationTextDocument;
    private string? _tbOutFolderText = "./output/";
    private string? _tbPreviewText;
    private TextDocument? _tbSourceTextDocument;
    private string? _tbDelimText;
    private string? _tbWordCountText;
    private string? _currentOpenFileName;
    private string? _selectedItem;
    private readonly int _locale;

    private readonly OpenccFmmseg? _openccFmmseg;
    private readonly OpenccJieba? _openccJieba;

    public ObservableCollection<string> CustomOptions { get; } = new();

    public string? SelectedItem
    {
        get => _selectedItem;
        set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
    }

    public MainWindowViewModel()
    {
        TbSourceTextDocument = new TextDocument();
        TbDestinationTextDocument = new TextDocument();
        LbxSourceItems = new ObservableCollection<string>();
        LbxDestinationItems = new ObservableCollection<string>();
        BtnPasteCommand = ReactiveCommand.CreateFromTask(Paste);
        BtnCopyCommand = ReactiveCommand.CreateFromTask(Copy);
        BtnOpenFileCommand = ReactiveCommand.CreateFromTask(OpenFile);
        BtnSaveFileCommand = ReactiveCommand.CreateFromTask(SaveFile);
        BtnProcessCommand = ReactiveCommand.Create(Process);
        BtnClearTbSourceCommand = ReactiveCommand.Create(ClearTbSource);
        BtnClearTbDestinationCommand = ReactiveCommand.Create(ClearTbDestination);
        BtnAddCommand = ReactiveCommand.CreateFromTask(Add);
        BtnRemoveCommand = ReactiveCommand.Create(Remove);
        BtnClearLbxSourceCommand = ReactiveCommand.Create(ClearLbxSource);
        BtnSelectOutFolderCommand = ReactiveCommand.CreateFromTask(SelectOutFolder);
        BtnPreviewCommand = ReactiveCommand.CreateFromTask(Preview);
        BtnDetectCommand = ReactiveCommand.CreateFromTask(Detect);
        BtnMessagePreviewClearCommand = ReactiveCommand.Create(MessagePreviewClear);
        BtnBatchStartCommand = ReactiveCommand.CreateFromTask(BatchStart);
        CmbCustomGotFocusCommand = ReactiveCommand.Create(() => { IsRbCustom = true; });
        RbSegmentRbTagGotFocusCommand = ReactiveCommand.Create(() => { IsCbJieba = true; });
    }

    public MainWindowViewModel(ITopLevelService topLevelService, LanguageSettingsService languageSettingsService,
        OpenccFmmseg openccFmmseg, OpenccJieba openccJieba) :
        this()
    {
        _topLevelService = topLevelService;
        var languageSettings = languageSettingsService.LanguageSettings!;
        // _languagesInfo = languageSettings.Languages;
        _locale = languageSettings.Locale == 1 ? languageSettings.Locale : 2;
        _selectedLanguage = languageSettings.Languages![_locale];
        _rbT2SContent = _selectedLanguage.T2SContent ?? "zh-Hant to zh-Hans";
        _rbS2TContent = _selectedLanguage.S2TContent ?? "zh-Hans to zh-Hant ";
        _rbSegmentContent = _selectedLanguage.SegmentContent ?? "Segment";
        _rbTagContent = _selectedLanguage.TagContent ?? "Keywords";
        _rbStdContent = _selectedLanguage.StdContent ?? "General";
        _rbZhtwContent = _selectedLanguage.ZhtwContent ?? "ZH-TW";
        _rbHkContent = _selectedLanguage.HkContent ?? "ZH-HK";
        _cbZhtwContent = _selectedLanguage.CbZhtwContent ?? "ZH-TW Idioms";
        _cbPunctuationContent = _selectedLanguage.CbPunctuationContent ?? "Punctuation";
        _cbJiebaContent = _selectedLanguage.CbJiebaContent ?? "Jieba";
        CustomOptions.Clear();
        if (_selectedLanguage.CustomOptions != null)
        {
            foreach (var opt in _selectedLanguage.CustomOptions!)
                CustomOptions.Add(opt);
        }

        SelectedItem = CustomOptions[0]; // Set "Option 1" as default
        _textFileTypes = languageSettings.TextFileTypes;
        _officeFileTypes = languageSettings.OfficeFileTypes;
        _tbDelimText = languageSettings.SegDelimiter;
        _tbWordCountText = languageSettings.TagWordCount;
        _openccFmmseg = openccFmmseg;
        _openccJieba = openccJieba;
    }

    public ReactiveCommand<Unit, Unit> BtnPasteCommand { get; }
    public ReactiveCommand<Unit, Unit> BtnCopyCommand { get; }
    public ReactiveCommand<Unit, Unit> BtnOpenFileCommand { get; }
    public ReactiveCommand<Unit, Unit> BtnSaveFileCommand { get; }
    public ReactiveCommand<Unit, Unit> BtnProcessCommand { get; }
    public ReactiveCommand<Unit, Unit> BtnClearTbSourceCommand { get; }
    public ReactiveCommand<Unit, Unit> BtnClearTbDestinationCommand { get; }
    public ReactiveCommand<Unit, Unit> BtnAddCommand { get; }
    public ReactiveCommand<Unit, Unit> BtnRemoveCommand { get; }
    public ReactiveCommand<Unit, Unit> BtnClearLbxSourceCommand { get; }
    public ReactiveCommand<Unit, Unit> BtnSelectOutFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> BtnPreviewCommand { get; }
    public ReactiveCommand<Unit, Unit> BtnDetectCommand { get; }
    public ReactiveCommand<Unit, Unit> BtnMessagePreviewClearCommand { get; }
    public ReactiveCommand<Unit, Unit> BtnBatchStartCommand { get; }
    public ReactiveCommand<Unit, Unit> CmbCustomGotFocusCommand { get; }
    public ReactiveCommand<Unit, Unit> RbSegmentRbTagGotFocusCommand { get; }

    private async Task Paste()
    {
        var inputText = await _topLevelService!.GetClipboardTextAsync();

        if (string.IsNullOrEmpty(inputText))
        {
            LblStatusBarContent = "Clipboard is empty.";
            return;
        }

        TbSourceTextDocument!.Text = inputText;
        LblStatusBarContent = "Clipboard content pasted";
        var codeText = _openccFmmseg!.ZhoCheck(inputText);
        UpdateEncodeInfo(codeText);
        LblFileNameContent = string.Empty;
        _currentOpenFileName = string.Empty;
    }

    private async Task Copy()
    {
        if (string.IsNullOrEmpty(TbDestinationTextDocument!.Text))
        {
            LblStatusBarContent = "Not copied: Destination content is empty.";
            return;
        }

        try
        {
            await _topLevelService!.SetClipboardTextAsync(TbDestinationTextDocument!.Text);
            LblStatusBarContent = "Text copied to clipboard";
        }
        catch (Exception ex)
        {
            LblStatusBarContent = $"Clipboard error: {ex.Message}";
        }
    }

    private async Task OpenFile()
    {
        var mainWindow = _topLevelService!.GetMainWindow();

        var storageProvider = mainWindow.StorageProvider;
        var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Text File",
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("Text Files") { Patterns = new[] { "*.txt" } },
                new("All Files") { Patterns = new[] { "*.*" } }
            },
            AllowMultiple = false
        });

        if (result.Count <= 0) return;
        var file = result[0];
        {
            var path = file.Path.LocalPath;
            var fileExt = Path.GetExtension(path);
            if (_textFileTypes == null || !_textFileTypes!.Contains(fileExt))
            {
                LblStatusBarContent = $"Error: File type ({fileExt}) not support";
                return;
            }

            try
            {
                await UpdateTbSourceFileContentsAsync(path);
            }
            catch (Exception ex)
            {
                // Handle unexpected exceptions here
                // Console.WriteLine($"Unhandled exception: {ex}");
                LblStatusBarContent = $"Error open file: {ex.Message}";
            }
        }
    }

    private async Task SaveFile()
    {
        var mainWindow = _topLevelService!.GetMainWindow();

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
            await File.WriteAllTextAsync(path, TbDestinationTextDocument!.Text);
            LblStatusBarContent = $"Destination contents saved to file: {path}";
        }
    }

    private void Process()
    {
        if (string.IsNullOrEmpty(TbSourceTextDocument!.Text))
        {
            LblStatusBarContent = "Source content is empty.";
            return;
        }

        if (string.IsNullOrEmpty(LblSourceCodeContent))
        {
            UpdateEncodeInfo(_openccFmmseg!.ZhoCheck(TbSourceTextDocument!.Text));
        }

        var config = GetCurrentConfig();
        string convertedText;
        LblDestinationCodeContent = string.Empty;
        long elapsedMs = 0;

        if (IsRbS2T || IsRbT2S || IsRbCustom)
        {
            var stopwatch = Stopwatch.StartNew();

            convertedText = IsCbJieba
                ? _openccJieba!.Convert(TbSourceTextDocument!.Text, config, IsCbPunctuation)
                : _openccFmmseg!.Convert(TbSourceTextDocument.Text, config, IsCbPunctuation);

            elapsedMs = stopwatch.ElapsedMilliseconds;

            if (IsRbT2S)
            {
                LblDestinationCodeContent = LblSourceCodeContent!.Contains("Non")
                    ? LblSourceCodeContent
                    : _selectedLanguage!.Name![2];
            }
            else if (IsRbS2T)
            {
                LblDestinationCodeContent = LblSourceCodeContent!.Contains("Non")
                    ? LblSourceCodeContent
                    : _selectedLanguage!.Name![1];
            }
            else // Custom
            {
                LblDestinationCodeContent = LblSourceCodeContent!.Contains("Non")
                    ? LblSourceCodeContent
                    : $"Custom: {config}";
            }
        }
        else if (IsRbSegment)
        {
            var stopwatch = Stopwatch.StartNew();

            convertedText = _openccJieba!.JiebaCutAndJoin(TbSourceTextDocument.Text, true, TbDelimText);

            elapsedMs = stopwatch.ElapsedMilliseconds;
            LblDestinationCodeContent = LblSourceCodeContent;
        }
        else if (IsRbTag)
        {
            var wordCount = int.TryParse(TbWordCountText, out var parsed) && parsed > 0 ? parsed : 1;
            TbWordCountText = wordCount.ToString();

            convertedText =
                "===== TextRank Method =====\n" + string.Join("/ ",
                    _openccJieba!.JiebaKeywordExtractTextRank(TbSourceTextDocument.Text, wordCount)) +
                "\n\n====== TF-IDF Method ======\n" +
                string.Join("/ ",
                    _openccJieba.JiebaKeywordExtractTfidf(TbSourceTextDocument.Text, wordCount));

            if (convertedText.Length == 0)
            {
                LblDestinationCodeContent = string.Empty;
                return;
            }

            LblDestinationCodeContent = LblSourceCodeContent;
        }
        else
        {
            return;
        }

        TbDestinationTextDocument!.Text = convertedText;
        LblStatusBarContent = $"Process completed: {config} {(IsCbJieba ? "(Jieba)" : "")}" +
                              (elapsedMs > 0 ? $" — Time used: {elapsedMs} ms" : "");
        TbDestinationTextDocument.UndoStack.ClearAll();
    }

    private async Task BatchStart()
    {
        if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output")))
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output"));

        if (LbxSourceItems!.Count == 0)
        {
            LblStatusBarContent = "Nothing to convert.";
            return;
        }

        if (!Directory.Exists(TbOutFolderText))
        {
            await MessageBox.Show("Invalid output folder:\n " + TbOutFolderText, "Error",
                _topLevelService!.GetMainWindow());
            IsTbOutFolderFocus = true;
            return;
        }

        if (!IsRbS2T && !IsRbT2S && !IsRbCustom)
        {
            await MessageBox.Show("Please select conversion type:\n zh-Hans / zh-Hant", "Error",
                _topLevelService!.GetMainWindow());
            return;
        }

        var config = GetCurrentConfig();
        var conversion = IsRbCustom
            ? config
            : IsRbS2T
                ? RbS2TContent
                : RbT2SContent;
        var region = IsRbStd
            ? RbStdContent
            : IsRbHk
                ? RbHkContent
                : RbZhtwContent;
        var iSZhTwIdioms = IsCbZhtw ? "Yes" : "No";
        var isPunctuations = IsCbPunctuation ? "Yes" : "No";
        var isConvertFilename = IsCbConvertFilename ? "Yes" : "No";

        IsTabMessage = true;
        LbxDestinationItems!.Clear();
        LbxDestinationItems.Add(_locale == 1
            ? $"Conversion Type (轉換方式) => {conversion}"
            : $"Conversion Type (转换方式) => {conversion}");
        if (!IsRbCustom)
        {
            LbxDestinationItems.Add(_locale == 1
                ? $"Region (區域) => {region}"
                : $"Region (区域) => {region}");
            LbxDestinationItems.Add(_locale == 1
                ? $"ZH/TW Idioms (中臺慣用語) => {iSZhTwIdioms}"
                : $"ZH/TW Idioms (中台惯用语) => {iSZhTwIdioms}");
        }

        LbxDestinationItems.Add(_locale == 1
            ? $"Punctuations (標點) => {isPunctuations}"
            : $"Punctuations (标点) => {isPunctuations}");
        LbxDestinationItems.Add(_locale == 1
            ? $"Convert filename (轉換文件名) => {isConvertFilename}"
            : $"Convert filename (转换文件名) => {isConvertFilename}");
        LbxDestinationItems.Add(_locale == 1
            ? $"Output folder: (輸出文件夾) => {TbOutFolderText}"
            : $"Output folder: (输出文件夹) => {TbOutFolderText}");

        var count = 0;

        foreach (var sourceFilePath in LbxSourceItems)
        {
            count++;
            var fileExt = Path.GetExtension(sourceFilePath);
            var filenameWithoutExt = Path.GetFileNameWithoutExtension(sourceFilePath);

            if (!File.Exists(sourceFilePath))
            {
                LbxDestinationItems.Add($"({count}) {sourceFilePath} -> File not found.");
                continue;
            }

            if (!_textFileTypes!.Contains(fileExt) && !_officeFileTypes!.Contains(fileExt))
            {
                LbxDestinationItems.Add($"({count}) [File skipped ({fileExt})] {sourceFilePath}");
                continue;
            }

            var suffix =
                // Set suffix based on the radio button state
                IsRbT2S
                    ? "_Hans"
                    : IsRbS2T
                        ? "_Hant"
                        : IsRbCustom
                            ? $"_{config}"
                            : "_Other";

            if (IsCbConvertFilename)
            {
                filenameWithoutExt = IsCbJieba
                    ? _openccJieba!.Convert(filenameWithoutExt, config, IsCbPunctuation)
                    : _openccFmmseg!.Convert(filenameWithoutExt, config, IsCbPunctuation);
            }

            var outputFilename = Path.Combine(Path.GetFullPath(TbOutFolderText),
                filenameWithoutExt + suffix + fileExt);
            var fileExtNoDot = fileExt.Length > 1 ? fileExt[1..] : "";

            if (OfficeDocModel.OfficeFormats.Contains(fileExtNoDot))
            {
                var converterType = IsCbJieba ? ConverterType.Jieba : ConverterType.Fmmseg;
                var converterHelper = new ConverterHelper(converterType, config);

                var (success, message) = await OfficeDocModel.ConvertOfficeDocAsync(
                    sourceFilePath,
                    outputFilename,
                    fileExtNoDot, // remove "."
                    converterHelper,
                    _openccFmmseg!,
                    _openccJieba!,
                    IsCbPunctuation,
                    true);

                LbxDestinationItems.Add(
                    success
                        ? $"({count}) {outputFilename} -> {message}"
                        : $"({count}) [File skipped] {sourceFilePath} -> {message}"
                );
            }
            else
            {
                string inputText;
                try
                {
                    inputText = await File.ReadAllTextAsync(sourceFilePath);
                }
                catch (Exception)
                {
                    LbxDestinationItems.Add($"({count}) {sourceFilePath} -> Conversion error.");
                    continue;
                }

                // Choose conversion method based on IsCbJieba
                Func<string, string, bool, string> conversionMethod = IsCbJieba
                    ? _openccJieba!.Convert
                    : _openccFmmseg!.Convert;

                // If the suffix isn't "(Other)", perform the conversion
                var convertedText =
                    suffix != "_Other" ? conversionMethod(inputText, config, IsCbPunctuation) : inputText;

                await File.WriteAllTextAsync(outputFilename, convertedText);

                LbxDestinationItems.Add($"({count}) {outputFilename} -> ✅ Done");
            }
        }

        LblStatusBarContent = $"Batch conversion done. ( {config} {(IsCbJieba ? "Jieba " : "")})";
    }

    private void ClearTbSource()
    {
        TbSourceTextDocument!.Text = string.Empty;
        TbSourceTextDocument.UndoStack.ClearAll();
        _currentOpenFileName = string.Empty;
        LblSourceCodeContent = string.Empty;
        LblFileNameContent = string.Empty;
        LblStatusBarContent = "Source text box cleared";
    }

    private void ClearTbDestination()
    {
        TbDestinationTextDocument!.Text = string.Empty;
        TbDestinationTextDocument.UndoStack.ClearAll();
        LblDestinationCodeContent = string.Empty;
        LblStatusBarContent = "Destination contents cleared";
    }

    private async Task Add()
    {
        var mainWindow = _topLevelService!.GetMainWindow();

        var storageProvider = mainWindow.StorageProvider;
        var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Text File",
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("Text Files") { Patterns = new[] { "*.txt" } },
                new("Office Files")
                    { Patterns = new[] { "*.docx", "*.xlsx", "*.pptx", "*.odt", "*.ods", "*.odp", "*.epub" } },
                new("ALL Files") { Patterns = new[] { "*.*" } }
            },
            AllowMultiple = true
        });

        if (result.Count <= 0) return;
        var listBoxItems = LbxSourceItems!.ToList();
        foreach (var file in result)
        {
            var path = file.Path.LocalPath;
            if (!listBoxItems.Contains(path))
                listBoxItems.Add(path);
        }

        var sortedList = listBoxItems.OrderBy(x => x);
        LbxSourceItems!.Clear();
        foreach (var item in sortedList) LbxSourceItems.Add(item);
    }

    private void Remove()
    {
        var index = LbxSourceSelectedIndex;
        var name = LbxSourceSelectedItem;
        if (LbxSourceSelectedIndex == -1 || LbxSourceItems!.Count == 0)
        {
            LblStatusBarContent = "Nothing to remove.";
            return;
        }

        LbxSourceItems!.Remove(LbxSourceSelectedItem!);
        //lbxSource.Items.RemoveAt(lbxSource.SelectedIndex);
        LblStatusBarContent = $"Item ({index}) {name} removed";
    }

    private async Task Preview()
    {
        if (LbxSourceSelectedIndex == -1 || string.IsNullOrWhiteSpace(LbxSourceSelectedItem))
        {
            LblStatusBarContent = "Nothing to preview.";
            return;
        }

        var filename = LbxSourceSelectedItem;
        var fileExtension = Path.GetExtension(filename);

        if (fileExtension.Length > 1 && !_textFileTypes!.Contains(fileExtension))
        {
            IsTabMessage = true;
            LbxDestinationItems!.Add("File type [" + Path.GetExtension(filename) + "] Preview not supported");
            return;
        }

        try
        {
            var displayText = await File.ReadAllTextAsync(filename!);
            IsTabPreview = true;
            TbPreviewText = displayText;
            LblStatusBarContent = $"File {filename} previewed";
        }
        catch (Exception)
        {
            IsTabMessage = true;
            LbxDestinationItems!.Add($"File read error: {filename}");
            LblStatusBarContent = "File read error.";
        }
    }

    private async Task Detect()
    {
        if (LbxSourceItems!.Count == 0)
        {
            LblStatusBarContent = "Nothing to detect.";
            return;
        }

        IsTabMessage = true;
        LbxDestinationItems!.Clear();

        foreach (var item in LbxSourceItems)
        {
            var fileExt = Path.GetExtension(item);

            if (_textFileTypes!.Contains(fileExt))
            {
                string inputText;
                try
                {
                    inputText = await File.ReadAllTextAsync(item);
                }
                catch (Exception)
                {
                    LbxDestinationItems.Add(item + " -> File read error.");
                    continue;
                }

                var textCode = _selectedLanguage!.Name![_openccFmmseg!.ZhoCheck(inputText)];
                LbxDestinationItems.Add($"[{textCode}] {item}");
            }
            else
            {
                LbxDestinationItems.Add($"[File skipped ({fileExt})] {item}");
            }
        }

        LblStatusBarContent = "Batch zho code detection done.";
    }

    private void ClearLbxSource()
    {
        LbxSourceItems!.Clear();
        LblStatusBarContent = "All source entries cleared.";
    }

    private void MessagePreviewClear()
    {
        if (IsTabMessage)
            LbxDestinationItems!.Clear();
        else if (IsTabPreview) TbPreviewText = string.Empty;
    }

    private async Task SelectOutFolder()
    {
        var mainWindow = _topLevelService!.GetMainWindow();

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
            TbOutFolderText = folderPath;
            IsTbOutFolderFocus = false;
            IsTbOutFolderFocus = true;
            LblStatusBarContent = $"Output folder set: {folderPath}";
        }
    }

    internal void UpdateEncodeInfo(int codeText)
    {
        switch (codeText)
        {
            case 1:
                LblSourceCodeContent = _selectedLanguage!.Name![codeText];
                if (!IsRbT2S) IsRbT2S = true;

                break;

            case 2:
                LblSourceCodeContent = _selectedLanguage!.Name![codeText];
                if (!IsRbS2T) IsRbS2T = true;

                break;

            default:
                LblSourceCodeContent = _selectedLanguage!.Name![0];
                break;
        }
    }

    private async Task UpdateTbSourceFileContentsAsync(string filename)
    {
        var fileInfo = new FileInfo(filename);
        if (fileInfo.Length > int.MaxValue)
        {
            LblStatusBarContent = "Error: File too large";
            return;
        }

        _currentOpenFileName = filename;

        // Read file contents
        try
        {
            using var reader = new StreamReader(_currentOpenFileName, Encoding.UTF8, true);
            var contents = await reader.ReadToEndAsync();

            // Display file contents to text box field
            TbSourceTextDocument!.Text = contents;
            LblStatusBarContent = $"File: {_currentOpenFileName}";

            var displayName = fileInfo.Name;
            LblFileNameContent = displayName.Length > 50
                ? $"{displayName[..25]}...{displayName[^15..]}"
                : displayName;

            var codeText = _openccFmmseg!.ZhoCheck(contents);
            UpdateEncodeInfo(codeText);
        }
        catch (Exception ex)
        {
            TbSourceTextDocument!.Text = string.Empty;
            LblSourceCodeContent = string.Empty;
            LblStatusBarContent = "Error: Invalid file";

            // Optionally log the exception
            Console.WriteLine($"Exception in UpdateTbSourceFileContentsAsync: {ex}");
        }
    }

    private string GetCurrentConfig()
    {
        var config = IsRbS2T
            ? IsRbStd
                ? "s2t"
                : IsRbHk
                    ? "s2hk"
                    : IsCbZhtw
                        ? "s2twp"
                        : "s2tw"
            : IsRbStd
                ? "t2s"
                : IsRbHk
                    ? "hk2s"
                    : IsCbZhtw
                        ? "tw2sp"
                        : "tw2s";

        return IsRbSegment
            ? "Segmentation"
            : IsRbTag
                ? "Keywords"
                : IsRbCustom
                    ? SelectedItem!.Split(" ").First()
                    : config;
    }

    public void TbSourceTextChanged()
    {
        LblTotalCharsContent = $"[ Chars: {TbSourceTextDocument!.Text!.Length:N0} ]";
    }

    #region Control Binding fields

    public string? LblSourceCodeContent
    {
        get => _lblSourceCodeContent;
        set => this.RaiseAndSetIfChanged(ref _lblSourceCodeContent, value);
    }

    public string? LblDestinationCodeContent
    {
        get => _lblDestinationCodeContent;
        set => this.RaiseAndSetIfChanged(ref _lblDestinationCodeContent, value);
    }

    public string? LblStatusBarContent
    {
        get => _lblStatusBarContent;
        set => this.RaiseAndSetIfChanged(ref _lblStatusBarContent, value);
    }

    public string? LblFileNameContent
    {
        get => _lblFilenameContent;
        set => this.RaiseAndSetIfChanged(ref _lblFilenameContent, value);
    }

    public TextDocument? TbSourceTextDocument
    {
        get => _tbSourceTextDocument;
        set => this.RaiseAndSetIfChanged(ref _tbSourceTextDocument, value);
    }

    public TextDocument? TbDestinationTextDocument
    {
        get => _tbDestinationTextDocument;
        set => this.RaiseAndSetIfChanged(ref _tbDestinationTextDocument, value);
    }

    public string? LblTotalCharsContent
    {
        get => _lblTotalCharsContent;
        set => this.RaiseAndSetIfChanged(ref _lblTotalCharsContent, value);
    }

    public string? TbDelimText
    {
        get => _tbDelimText;
        set => this.RaiseAndSetIfChanged(ref _tbDelimText, value);
    }

    public string? TbWordCountText
    {
        get => _tbWordCountText;
        set => this.RaiseAndSetIfChanged(ref _tbWordCountText, value);
    }

    public ObservableCollection<string>? LbxSourceItems
    {
        get => _lbxSourceItems;
        set => this.RaiseAndSetIfChanged(ref _lbxSourceItems, value);
    }

    public ObservableCollection<string>? LbxDestinationItems
    {
        get => _lbxDestinationItems;
        set => this.RaiseAndSetIfChanged(ref _lbxDestinationItems, value);
    }

    public int LbxSourceSelectedIndex
    {
        get => _lbxSourceSelectedIndex;
        set => this.RaiseAndSetIfChanged(ref _lbxSourceSelectedIndex, value);
    }

    public string? LbxSourceSelectedItem
    {
        get => _lbxSourceSelectedItem;
        set => this.RaiseAndSetIfChanged(ref _lbxSourceSelectedItem, value);
    }

    public string? TbOutFolderText
    {
        get => _tbOutFolderText;
        set => this.RaiseAndSetIfChanged(ref _tbOutFolderText, value);
    }

    public string? TbPreviewText
    {
        get => _tbPreviewText;
        set => this.RaiseAndSetIfChanged(ref _tbPreviewText, value);
    }

    public string? RbS2TContent
    {
        get => _rbS2TContent;
        set => this.RaiseAndSetIfChanged(ref _rbS2TContent, value);
    }

    public string? RbT2SContent
    {
        get => _rbT2SContent;
        set => this.RaiseAndSetIfChanged(ref _rbT2SContent, value);
    }

    public string? RbSegmentContent
    {
        get => _rbSegmentContent;
        set => this.RaiseAndSetIfChanged(ref _rbSegmentContent, value);
    }

    public string? RbTagContent
    {
        get => _rbTagContent;
        set => this.RaiseAndSetIfChanged(ref _rbTagContent, value);
    }

    public string? RbStdContent
    {
        get => _rbStdContent;
        set => this.RaiseAndSetIfChanged(ref _rbStdContent, value);
    }

    public string? RbZhtwContent
    {
        get => _rbZhtwContent;
        set => this.RaiseAndSetIfChanged(ref _rbZhtwContent, value);
    }

    public string? RbHkContent
    {
        get => _rbHkContent;
        set => this.RaiseAndSetIfChanged(ref _rbHkContent, value);
    }

    public string? CbZhtwContent
    {
        get => _cbZhtwContent;
        set => this.RaiseAndSetIfChanged(ref _cbZhtwContent, value);
    }

    public string? CbPunctuationContent
    {
        get => _cbPunctuationContent;
        set => this.RaiseAndSetIfChanged(ref _cbPunctuationContent, value);
    }

    public string? CbJiebaContent
    {
        get => _cbJiebaContent;
        set => this.RaiseAndSetIfChanged(ref _cbJiebaContent, value);
    }

    public FontWeight TabMainFontWeight
    {
        get => _tabMainFontWeight;
        set => this.RaiseAndSetIfChanged(ref _tabMainFontWeight, value);
    }

    public FontWeight TabBatchFontWeight
    {
        get => _tabBatchFontWeight;
        set => this.RaiseAndSetIfChanged(ref _tabBatchFontWeight, value);
    }

    #endregion

    #region RbCb Boolean Binding Region

    public bool IsRbS2T
    {
        get => _isRbS2T;
        set
        {
            this.RaiseAndSetIfChanged(ref _isRbS2T, value);
            if (!value) return;
            IsRbT2S = false;
            IsRbSegment = false;
            IsRbTag = false;
            IsRbCustom = false;
            LblSourceCodeContent = _selectedLanguage!.Name![2];
        }
    }

    public bool IsRbT2S
    {
        get => _isRbT2S;
        set
        {
            this.RaiseAndSetIfChanged(ref _isRbT2S, value);
            if (!value) return;
            IsRbS2T = false;
            IsRbSegment = false;
            IsRbTag = false;
            IsRbCustom = false;
            LblSourceCodeContent = _selectedLanguage!.Name![1];
        }
    }

    public bool IsRbSegment
    {
        get => _isRbSegment;
        set
        {
            this.RaiseAndSetIfChanged(ref _isRbSegment, value);
            if (!value) return;
            IsRbS2T = false;
            IsRbT2S = false;
            IsRbTag = false;
            IsRbCustom = false;
        }
    }

    public bool IsRbTag
    {
        get => _isRbTag;
        set
        {
            this.RaiseAndSetIfChanged(ref _isRbTag, value);
            if (!value) return;
            IsRbS2T = false;
            IsRbT2S = false;
            IsRbSegment = false;
            IsRbCustom = false;
        }
    }

    public bool IsRbCustom
    {
        get => _isRbCustom;
        set
        {
            this.RaiseAndSetIfChanged(ref _isRbCustom, value);
            if (!value) return;
            IsRbS2T = false;
            IsRbT2S = false;
            IsRbSegment = false;
            IsRbTag = false;
        }
    }

    public bool IsRbStd
    {
        get => _isRbStd;
        set
        {
            this.RaiseAndSetIfChanged(ref _isRbStd, value);
            if (!value) return;
            IsRbZhtw = false;
            IsRbHk = false;
            IsCbZhtw = false;
            IsCbZhtwEnabled = false;
        }
    }

    public bool IsRbZhtw
    {
        get => _isRbZhtw;
        set
        {
            this.RaiseAndSetIfChanged(ref _isRbZhtw, value);
            if (!value) return;
            IsRbStd = false;
            IsRbHk = false;
            IsCbZhtw = true;
            IsCbZhtwEnabled = true;
        }
    }

    public bool IsRbHk
    {
        get => _isRbHk;
        set
        {
            this.RaiseAndSetIfChanged(ref _isRbHk, value);
            if (!value) return;
            IsRbStd = false;
            IsRbZhtw = false;
            IsCbZhtw = false;
            IsCbZhtwEnabled = false;
        }
    }

    public bool IsTabMain
    {
        get => _isTabMain;
        set
        {
            this.RaiseAndSetIfChanged(ref _isTabMain, value);
            if (!value) return;
            IsTabBatch = false;
            IsBtnOpenFileVisible = true;
            IsLblFileNameVisible = true;
            IsBtnSaveFileVisible = true;
            IsBtnProcessVisible = true;
            IsBtnBatchStartVisible = false;
            TabMainFontWeight = FontWeight.Bold;
            TabBatchFontWeight = FontWeight.Normal;
        }
    }

    public bool IsTabBatch
    {
        get => _isTabBatch;
        set
        {
            this.RaiseAndSetIfChanged(ref _isTabBatch, value);
            if (!value) return;
            IsTabMain = false;
            IsBtnOpenFileVisible = false;
            IsLblFileNameVisible = false;
            IsBtnSaveFileVisible = false;
            IsBtnProcessVisible = false;
            IsBtnBatchStartVisible = true;
            TabMainFontWeight = FontWeight.Normal;
            TabBatchFontWeight = FontWeight.Bold;
        }
    }

    public bool IsTabMessage
    {
        get => _isTabMessage;
        set
        {
            this.RaiseAndSetIfChanged(ref _isTabMessage, value);
            if (!value) return;
            IsTabPreview = false;
        }
    }

    public bool IsTabPreview
    {
        get => _isTabPreview;
        set
        {
            this.RaiseAndSetIfChanged(ref _isTabPreview, value);
            if (!value) return;
            IsTabMessage = false;
        }
    }

    public bool IsCbZhtw
    {
        get => _isCbZhtw;
        set
        {
            this.RaiseAndSetIfChanged(ref _isCbZhtw, value);
            if (!value) return;
            IsRbHk = false;
            IsRbStd = false;
        }
    }

    public bool IsCbZhtwEnabled
    {
        get => _isCbZhtwEnabled;
        set => this.RaiseAndSetIfChanged(ref _isCbZhtwEnabled, value);
    }

    public bool IsCbPunctuation
    {
        get => _isCbPunctuation;
        set => this.RaiseAndSetIfChanged(ref _isCbPunctuation, value);
    }

    public bool IsCbJieba
    {
        get => _isCbJieba;
        set => this.RaiseAndSetIfChanged(ref _isCbJieba, value);
    }

    public bool IsCbConvertFilename
    {
        get => _isCbConvertFilename;
        set => this.RaiseAndSetIfChanged(ref _isCbConvertFilename, value);
    }

    public bool IsTbOutFolderFocus
    {
        get => _isTbOutFolderFocus;
        set => this.RaiseAndSetIfChanged(ref _isTbOutFolderFocus, value);
    }

    public bool IsBtnOpenFileVisible
    {
        get => _isBtnOpenFileVisible;
        set => this.RaiseAndSetIfChanged(ref _isBtnOpenFileVisible, value);
    }

    public bool IsBtnSaveFileVisible
    {
        get => _isBtnSaveFileVisible;
        set => this.RaiseAndSetIfChanged(ref _isBtnSaveFileVisible, value);
    }

    public bool IsBtnProcessVisible
    {
        get => _isBtnProcessVisible;
        set => this.RaiseAndSetIfChanged(ref _isBtnProcessVisible, value);
    }

    public bool IsLblFileNameVisible
    {
        get => _isLblFileNameVisible;
        set => this.RaiseAndSetIfChanged(ref _isLblFileNameVisible, value);
    }

    public bool IsBtnBatchStartVisible
    {
        get => _isBtnBatchStartVisible;
        set => this.RaiseAndSetIfChanged(ref _isBtnBatchStartVisible, value);
    }

    #endregion
}