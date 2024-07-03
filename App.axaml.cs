using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Newtonsoft.Json;
using ZhoConverterAvaMvvm.ViewModels;
using ZhoConverterAvaMvvm.Views;

namespace ZhoConverterAvaMvvm;

public class App : Application
{
    public App()
    {
        var settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LanguageSettings.json");
        LanguageSettings = ReadLanguageSettingsFromJson(settingsFilePath);
    }

    public static LanguageSettings? LanguageSettings { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private static LanguageSettings ReadLanguageSettingsFromJson(string filePath)
    {
        return JsonConvert.DeserializeObject<LanguageSettings>(File.ReadAllText(filePath))!;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };

        base.OnFrameworkInitializationCompleted();
    }
}