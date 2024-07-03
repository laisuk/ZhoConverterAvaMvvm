﻿using System.Collections.Generic;

namespace ZhoConverterAvaMvvm;

public class LanguageSettings
{
    public List<Language>? Languages { get; set; }
    public int CharCheck { get; set; }
    public Dictionary<char, char>? Punctuations { get; set; }
    public List<string>? TextFileTypes { get; set; }
}

public class Language
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
}