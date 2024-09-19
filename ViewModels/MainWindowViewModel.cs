using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using AvaloniaEdit.Document;
using JiebaNet.Analyser;
using JiebaNet.Segmenter;
using OpenccFmmsegNetLib;
using ReactiveUI;
using ZhoConverterAvaMvvm.Services;
using ZhoConverterAvaMvvm.Views;

namespace ZhoConverterAvaMvvm.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly List<Language>? _languagesInfo;
    private readonly List<string>? _textFileTypes;
    private readonly ITopLevelService? _topLevelService;
    private string? _currentOpenFileName;
    private bool _isBtnBatchStartVisible;
    private bool _isBtnOpenFileVisible = true;
    private bool _isBtnProcessVisible = true;
    private bool _isBtnSaveFileVisible = true;
    private bool _isCbPunctuation = true;
    private bool _isCbZhtw;
    private bool _isCbZhtwEnabled;
    private bool _isLblFileNameVisible = true;
    private bool _isRbHk;
    private bool _isRbJieba;
    private bool _isRbS2T;
    private bool _isRbStd = true;
    private bool _isRbT2S = true;
    private bool _isRbTag;
    private bool _isRbZhtw;
    private bool _isTabBatch;
    private bool _isTabMain = true;
    private bool _isTabMessage = true;
    private bool _isTabPreview;
    private bool _isTbOutFolderFocus;
    private string? _lblDestinationCodeContent;
    private string? _lblFilenameContent;
    private string? _lblSourceCodeContent;
    private string? _lblStatusBarContent;
    private string? _lblTotalCharsContent;
    private ObservableCollection<string>? _lbxDestinationItems;
    private ObservableCollection<string>? _lbxSourceItems;
    private int _lbxSourceSelectedIndex;
    private string? _lbxSourceSelectedItem;
    private string? _rbHkContent = "ZH-HK (中港简繁)";
    private string? _rbS2TContent = "Hans (简体) to Hant (繁体)";
    private string? _rbStdContent = "Standard (标准简繁)";
    private string? _rbT2SContent = "Hant (繁体) to Hans (简体)";
    private string? _rbZhtwContent = "ZH-TW (中台简繁)";
    private FontWeight _tabBatchFontWeight = FontWeight.Normal;
    private FontWeight _tabMainFontWeight = FontWeight.Bold;
    private TextDocument? _tbDestinationTextDocument;
    private string? _tbOutFolderText = "./output/";
    private string? _tbPreviewText;
    private TextDocument? _tbSourceTextDocument;
    private string? _tbWordCountText = "30";

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
    }

    public MainWindowViewModel(ITopLevelService topLevelService, LanguageSettingsService languageSettingsService) :
        this()
    {
        _topLevelService = topLevelService;
        var languageSettings = languageSettingsService.LanguageSettings;
        _languagesInfo = languageSettings?.Languages;
        _textFileTypes = languageSettings?.TextFileTypes;
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
        var codeText = OpenccFmmsegNet.ZhoCheck(inputText);
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
            if (!_textFileTypes!.Contains(fileExt)) 
                {
                    LblStatusBarContent = $"Error: File type ({fileExt}) not support";
                    return;
                }
            UpdateTbSourceFileContents(path);
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

        var config = GetCurrentConfig();
        var convertedText = OpenccFmmsegNet.Convert(TbSourceTextDocument.Text, config, IsCbPunctuation);

        TbDestinationTextDocument!.Text = convertedText;

        if (IsRbT2S)
        {
            LblDestinationCodeContent = LblSourceCodeContent!.Contains("Non")
                ? LblSourceCodeContent
                : _languagesInfo![2].Name;
        }
        else if (IsRbS2T)
        {
            LblDestinationCodeContent = LblSourceCodeContent!.Contains("Non")
                ? LblSourceCodeContent
                : _languagesInfo![1].Name;
        }
        else if (IsRbJieba)
        {
            TbDestinationTextDocument.Text = string.Join("/", new JiebaSegmenter().Cut(TbSourceTextDocument.Text));
            LblDestinationCodeContent = LblSourceCodeContent;
        }
        else if (IsRbTag)
        {
            var wordCount = int.Parse(TbWordCountText!) < 10 ? 10 : int.Parse(TbWordCountText!);
            TbDestinationTextDocument.Text = "===== TextRank Method =====\n" + string.Join("/ ",
                new TextRankExtractor().ExtractTags(TbSourceTextDocument.Text, wordCount));
            TbDestinationTextDocument.Text = TbDestinationTextDocument.Text + "\n\n====== TF-IDF Method ======\n" +
                                             string.Join("/ ",
                                                 new TfidfExtractor().ExtractTags(TbSourceTextDocument.Text,
                                                     wordCount));
            if (TbDestinationTextDocument.Text.Length == 0)
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

        LblStatusBarContent = "Process completed";
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

        if (IsRbS2T == false && IsRbT2S == false)
        {
            await MessageBox.Show("Please select conversion type:\n zh-Hans / zh-Hant", "Error",
                _topLevelService!.GetMainWindow());
            return;
        }

        var conversion = IsRbS2T ? RbS2TContent : RbT2SContent;
        var region = IsRbStd
            ? RbStdContent
            : IsRbHk
                ? RbHkContent
                : RbZhtwContent;
        var iSZhTwIdioms = IsCbZhtw ? "Yes" : "No";
        var isPunctuations = IsCbPunctuation ? "Yes" : "No";

        IsTabMessage = true;
        LbxDestinationItems!.Clear();
        LbxDestinationItems.Add($"Conversion Type (转换方式) => {conversion}");
        LbxDestinationItems.Add($"Region (区域) => {region}");
        LbxDestinationItems.Add($"ZH/TW Idioms (中台惯用语) => {iSZhTwIdioms}");
        LbxDestinationItems.Add($"Punctuations (标点) => {isPunctuations}");
        LbxDestinationItems.Add($"Output folder: (输出文件夹) => {TbOutFolderText}");

        var count = 0;

        foreach (var item in LbxSourceItems)
        {
            count++;
            var sourceFilePath = item;
            var fileExt = Path.GetExtension(sourceFilePath);
            var filenameWithoutExt = Path.GetFileNameWithoutExtension(sourceFilePath);

            if (!File.Exists(sourceFilePath))
            {
                LbxDestinationItems.Add($"({count}) {sourceFilePath} -> File not found.");
                continue;
            }

            if (!_textFileTypes!.Contains(fileExt))
            {
                LbxDestinationItems.Add($"({count}) [File skipped ({fileExt})] {sourceFilePath}");
                continue;
            }

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

            string convertedText;
            string suffix;
            var config = GetCurrentConfig();
            if (IsRbT2S)
            {
                suffix = "(Hans)";
                convertedText = OpenccFmmsegNet.Convert(inputText, config, IsCbPunctuation);
            }
            else if (IsRbS2T)
            {
                suffix = "(Hant)";
                convertedText = OpenccFmmsegNet.Convert(inputText, config, IsCbPunctuation);
            }
            else
            {
                suffix = "(Other)";
                convertedText = inputText;
            }

            var outputFilename = Path.Combine(Path.GetFullPath(TbOutFolderText),
                filenameWithoutExt + suffix + fileExt);
            await File.WriteAllTextAsync(outputFilename, convertedText);

            LbxDestinationItems.Add($"({count}) {outputFilename} -> [Done ✓]");
        }

        LblStatusBarContent = "Batch conversion done.";
    }

    private void ClearTbSource()
    {
        TbSourceTextDocument!.Text = string.Empty;
        _currentOpenFileName = string.Empty;
        LblSourceCodeContent = string.Empty;
        LblFileNameContent = string.Empty;
        LblStatusBarContent = "Source text box cleared";
    }

    private void ClearTbDestination()
    {
        TbDestinationTextDocument!.Text = string.Empty;
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
        if (LbxSourceSelectedIndex == -1)
        {
            LblStatusBarContent = "Nothing to preview.";
            return;
        }

        var filename = LbxSourceSelectedItem;

        if (!_textFileTypes!.Contains(Path.GetExtension(filename)!))
        {
            IsTabMessage = true;
            LbxDestinationItems!.Add("File type [" + Path.GetExtension(filename)! + "] Preview not supported");
            return;
        }

        try
        {
            var displayText = await File.ReadAllTextAsync(filename!);
            IsTabPreview = true;
            TbPreviewText = displayText;
        }
        catch (Exception)
        {
            IsTabPreview = true;
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

                var textCode = _languagesInfo![OpenccFmmsegNet.ZhoCheck(inputText)].Name!;
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
            IsTbOutFolderFocus = true;
            LblStatusBarContent = $"Output folder set: {folderPath}";
        }
    }

    private void UpdateEncodeInfo(int codeText)
    {
        switch (codeText)
        {
            case 1:
                LblSourceCodeContent = _languagesInfo![codeText].Name;
                if (!IsRbT2S) IsRbT2S = true;

                break;

            case 2:
                LblSourceCodeContent = _languagesInfo![codeText].Name;
                if (!IsRbS2T) IsRbS2T = true;

                break;

            default:
                LblSourceCodeContent = _languagesInfo![0].Name;
                break;
        }
    }

    private async void UpdateTbSourceFileContents(string filename)
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
            using var reader = new StreamReader(_currentOpenFileName, System.Text.Encoding.UTF8, true);
            var contents = await reader.ReadToEndAsync();
            // Display file contents to text box field
            TbSourceTextDocument!.Text = contents;
            LblStatusBarContent = $"File: {_currentOpenFileName}";
            var displayName = fileInfo.Name;
            LblFileNameContent =
                displayName.Length > 50 ? $"{displayName[..25]}...{displayName[^15..]}" : displayName;
            var codeText = OpenccFmmsegNet.ZhoCheck(contents);
            UpdateEncodeInfo(codeText);
        }
        catch (Exception)
        {
            TbSourceTextDocument!.Text = string.Empty;
            LblSourceCodeContent = string.Empty;
            LblStatusBarContent = "Error: Invalid file";
            //throw;
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
                    ? "t2hk"
                    : IsCbZhtw
                        ? "tw2sp"
                        : "tw2s";
        return config;
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
            IsRbJieba = false;
            IsRbTag = false;
            LblSourceCodeContent = _languagesInfo![2].Name;
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
            IsRbJieba = false;
            IsRbTag = false;
            LblSourceCodeContent = _languagesInfo![1].Name;
        }
    }

    public bool IsRbJieba
    {
        get => _isRbJieba;
        set
        {
            this.RaiseAndSetIfChanged(ref _isRbJieba, value);
            if (!value) return;
            IsRbS2T = false;
            IsRbT2S = false;
            IsRbTag = false;
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
            IsRbJieba = false;
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