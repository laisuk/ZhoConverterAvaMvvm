using System.Collections.Generic;
using ZhoConverterAvaMvvm.Services;

namespace ZhoConverterAvaMvvm.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly ITopLevelService? _topLevelService;
    private readonly List<Language>? _languagesInfo;
    private readonly List<string>? _textFileTypes;

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
}