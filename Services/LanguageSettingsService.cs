using System.IO;
using Newtonsoft.Json;

namespace ZhoConverterAvaMvvm.Services;

public class LanguageSettingsService
{
    public LanguageSettingsService(string settingsFilePath)
    {
        LanguageSettings = ReadLanguageSettingsFromJson(settingsFilePath);
    }

    public LanguageSettings? LanguageSettings { get; private set; }

    private LanguageSettings ReadLanguageSettingsFromJson(string filePath)
    {
        if (File.Exists(filePath)) return JsonConvert.DeserializeObject<LanguageSettings>(File.ReadAllText(filePath))!;
        const string languageSettingsText = """
                                            {
                                                "Languages": [
                                                    {
                                                        "id": 0,
                                                        "code": "non-zho",
                                                        "name": "Non-zho (其它)"
                                                    },
                                                    {
                                                        "id": 1,
                                                        "code": "zh-Hant",
                                                        "name": "zh-Hant (繁体)"
                                                    },
                                                    {
                                                        "id": 2,
                                                        "code": "zh-Hans",
                                                        "name": "zh-Hans (简体)"
                                                    }
                                                ],
                                                "CharCheck": 50,
                                                "Punctuations": {
                                                    "“": "「",
                                                    "”": "」",
                                                    "‘": "『",
                                                    "’": "』"
                                                },
                                                "TextFileTypes": [
                                                  ".txt",
                                                  ".srt",
                                                  ".vtt",
                                                  ".ass",
                                                  ".xml",
                                                  ".ttml2",
                                                  ".csv",
                                                  ".json",
                                                  ".html",
                                                  ".cs",
                                                  ".py",
                                                  ".java",
                                                  ".md",
                                                  ".js"
                                                ],
                                                "TagWordCount": "30",
                                                "SegDelimiter": "/ "
                                            }
                                            """;

        File.WriteAllText(filePath,
            languageSettingsText);
        return JsonConvert.DeserializeObject<LanguageSettings>(File.ReadAllText(filePath))!;
    }
}