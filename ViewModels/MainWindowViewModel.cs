using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AvaloniaEdit.Document;
using OpenccFmmsegNetLib;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using ZhoConverterAvaMvvm.Services;

namespace ZhoConverterAvaMvvm.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly ITopLevelService? _topLevelService;
    private readonly List<Language>? _languagesInfo;
    private readonly List<string>? _textFileTypes;
    private bool _isRbS2T;
    private bool _isRbT2S = true;
    private bool _isRbJieba;
    private bool _isRbTag;
    private bool _isRbStd = true;
    private bool _isRbZhtw;
    private bool _isRbHk;
    private bool _isCbZhtw;
    private bool _isCbPunctuation;
    private TextDocument? _tbSourceTextDocument;
    private TextDocument? _tbDestinationTextDocument;
    private string? _lblSourceCodeContent;
    private string? _lblDestinationCodeContent;
    private string? _lblStatusBarContent;
    private string? _lblFilenameContent;
    private string? _currentOpenFileName;
    private string? _lblTotalCharsContent;

    public MainWindowViewModel()
    {
        TbSourceTextDocument = new TextDocument();
        BtnPasteCommand = ReactiveCommand.CreateFromTask(BtnPaste);
        BtnOpenFileCommand = ReactiveCommand.CreateFromTask(BtnOpenFile);
        BtnClearTbSourceCommand = ReactiveCommand.Create(BtnClearTbSource);
        BtnClearTbDestinationCommand = ReactiveCommand.Create(BtnClearTbDestination);

    }



    public MainWindowViewModel(ITopLevelService topLevelService, LanguageSettingsService languageSettingsService) : this()
    {
        _topLevelService = topLevelService;
        var languageSettings = languageSettingsService.LanguageSettings;
        _languagesInfo = languageSettings?.Languages;
        _textFileTypes = languageSettings?.TextFileTypes;
    }

    #region RbCbRegion
    public bool IsRbS2T
    {
        get => _isRbS2T;
        set
        {
            this.RaiseAndSetIfChanged(ref _isRbS2T, value);
            if (value)
            {
                IsRbT2S = false;
                IsRbJieba = false;
                IsRbTag = false;
            }
        }
    }

    public bool IsRbT2S
    {
        get => _isRbT2S;
        set
        {
            this.RaiseAndSetIfChanged(ref _isRbT2S, value);
            if (value)
            {
                IsRbS2T = false;
                IsRbJieba = false;
                IsRbTag = false;
            }
        }
    }

    public bool IsRbJieba
    {
        get => _isRbJieba;
        set
        {
            this.RaiseAndSetIfChanged(ref _isRbJieba, value);
            if (value)
            {
                IsRbS2T = false;
                IsRbT2S = false;
                IsRbTag = false;
            }
        }
    }

    public bool IsRbTag
    {
        get => _isRbTag;
        set
        {
            this.RaiseAndSetIfChanged(ref _isRbTag, value);
            if (value)
            {
                IsRbS2T = false;
                IsRbT2S = false;
                IsRbJieba = false;
            }
        }
    }

    public bool IsRbStd
    {
        get => _isRbStd;
        set
        {
            this.RaiseAndSetIfChanged(ref _isRbStd, value);
            if (value)
            {
                IsRbZhtw = false;
                IsRbHk = false;
            }
        }
    }

    public bool IsRbZhtw
    {
        get => _isRbZhtw;
        set
        {
            this.RaiseAndSetIfChanged(ref _isRbZhtw, value);
            if (value)
            {
                IsRbStd = false;
                IsRbHk = false;
            }
        }
    }

    public bool IsRbHk
    {
        get => _isRbHk;
        set
        {
            this.RaiseAndSetIfChanged(ref _isRbHk, value);
            if (value)
            {
                IsRbStd = false;
                IsRbZhtw = false;
            }
        }
    }

    public bool IsCbZhtw
    {
        get { return _isCbZhtw; }
        set { this.RaiseAndSetIfChanged(ref _isCbZhtw, value); }
    }

    public bool IsCbPunctuation
    {
        get { return _isCbPunctuation; }
        set { this.RaiseAndSetIfChanged(ref _isCbPunctuation, value); }
    }

    #endregion

    public string? LblSourceCodeContent { get => _lblSourceCodeContent; set => this.RaiseAndSetIfChanged(ref _lblSourceCodeContent, value); }
    public string? LblDestinationCodeContent { get => _lblDestinationCodeContent; set => this.RaiseAndSetIfChanged(ref _lblDestinationCodeContent, value); }
    public string? LblStatusBarContent { get => _lblStatusBarContent; set => this.RaiseAndSetIfChanged(ref _lblStatusBarContent, value); }
    public string? LblFileNameContentt { get => _lblFilenameContent; set => this.RaiseAndSetIfChanged(ref _lblFilenameContent, value); }
    public TextDocument? TbSourceTextDocument { get => _tbSourceTextDocument; set => this.RaiseAndSetIfChanged(ref _tbSourceTextDocument, value); }
    public TextDocument? TbDestinationTextDocument { get => _tbDestinationTextDocument; set => this.RaiseAndSetIfChanged(ref _tbDestinationTextDocument, value); }
    public string? LblTotalCharsContent { get => _lblTotalCharsContent; set => this.RaiseAndSetIfChanged(ref _lblTotalCharsContent, value); }

    public ReactiveCommand<Unit, Unit> BtnPasteCommand { get; }
    public ReactiveCommand<Unit, Unit> BtnOpenFileCommand { get; }
    public ReactiveCommand<Unit, Unit> BtnClearTbSourceCommand { get; }
    public ReactiveCommand<Unit, Unit> BtnClearTbDestinationCommand { get; }

    private async Task BtnPaste()
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
        LblFileNameContentt = string.Empty;
        _currentOpenFileName = string.Empty;
    }

    private async Task BtnOpenFile()
    {
        var mainWindow = _topLevelService!.GetMainWindow();

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

    private void BtnClearTbSource()
    {
        TbSourceTextDocument!.Text = string.Empty;
        _currentOpenFileName = string.Empty;
        LblSourceCodeContent = string.Empty;
        LblFileNameContentt = string.Empty;
        LblStatusBarContent = "Source text box cleared";
    }

    private void BtnClearTbDestination()
    {
        TbDestinationTextDocument!.Text = string.Empty;
        LblDestinationCodeContent = string.Empty;
        LblStatusBarContent = "Destination contents cleared";
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
            using var reader = new StreamReader(_currentOpenFileName);
            var contents = await reader.ReadToEndAsync();
            // Display file contents to text box field
            TbSourceTextDocument!.Text = contents;
            LblStatusBarContent = $"File: {_currentOpenFileName}";
            var displayName = fileInfo.Name;
            LblFileNameContentt =
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
        var config = IsRbS2T == true
            ? IsRbStd == true
                ? "s2t"
                : IsRbHk == true
                    ? "s2hk"
                    : IsCbZhtw == true
                        ? "s2twp"
                        : "s2tw"
            : IsRbStd == true
                ? "t2s"
                : IsRbHk == true
                    ? "t2hk"
                    : IsCbZhtw == true
                        ? "tw2sp"
                        : "tw2s";
        return config;
    }

    public void TbSourceTextChanged()
    {
        LblTotalCharsContent = $"[ Chars: {TbSourceTextDocument!.Text!.Length:N0} ]";
    }
}