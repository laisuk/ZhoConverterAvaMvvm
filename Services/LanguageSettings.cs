using System;
using System.Collections.Generic;

namespace ZhoConverterAvaMvvm.Services;

[Serializable]
public class LanguageSettings
{
    public List<Language>? Languages { get; set; }
    public int CharCheck { get; set; }
    public Dictionary<string, string>? Punctuations { get; set; }
    public List<string>? TextFileTypes { get; set; }
    public List<string>? OfficeFileTypes { get; set; }
    public string? SegDelimiter { get; set; }
    public string? TagWordCount { get; set; }
    public int Locale {get; set;}
}

[Serializable]
public class Language
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public List<string>? Name { get; set; }
    public string? T2SContent { get; set; }
    public string? S2TContent { get; set; }
    public string? SegmentContent { get; set; }
    public string? TagContent { get; set; }
    public string? StdContent { get; set; }
    public string? ZhtwContent { get; set; }
    public string? HkContent { get; set; }
    public string? CbZhtwContent { get; set; }
    public string? CbPunctuationContent { get; set; }
    public string? CbJiebaContent { get; set; }
    public List<string>? CustomOptions { get; set; }
}