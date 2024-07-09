using ReactiveUI;
using System.Collections.Generic;
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
    private string? _lblSourceCodeContent;

    public MainWindowViewModel()
    {

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

    public string? LblSourceCodeContent
    {
        get => _lblSourceCodeContent;
        set
        {
            this.RaiseAndSetIfChanged(ref _lblSourceCodeContent, value);
        }
    }
}