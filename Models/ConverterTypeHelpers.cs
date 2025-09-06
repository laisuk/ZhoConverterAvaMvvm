namespace ZhoConverterAvaMvvm.Models;

/// <summary>
/// Represents the OpenCC converter configuration used during document conversion.
/// </summary>
/// <param name="Opencc">
/// The converter type to use (e.g. <see cref="ConverterType.Fmmseg"/> or <see cref="ConverterType.Jieba"/>).
/// </param>
/// <param name="Config">
/// The OpenCC configuration name (e.g. "s2t", "t2s", "s2tw").
/// — Controls the dictionary and conversion rules applied.
/// </param>
public readonly record struct ConverterHelper(ConverterType Opencc, string Config)
{
    /// <summary>
    /// Initializes a new <see cref="ConverterHelper"/> by parsing a string
    /// into a <see cref="ConverterType"/> and storing the specified config.
    /// </summary>
    /// <param name="opencc">The converter type as a string ("fmmseg" or "jieba").</param>
    /// <param name="config">The OpenCC configuration to apply.</param>
    public ConverterHelper(string opencc, string config)
        : this(ConverterTypeHelpers.Parse(opencc), config)
    {
    }
}

/// <summary>
/// Specifies which OpenCC converter implementation to use.
/// </summary>
public enum ConverterType
{
    /// <summary>
    /// Use the FMMSEG-based converter for segmentation and conversion.
    /// </summary>
    Fmmseg,

    /// <summary>
    /// Use the Jieba-based converter for segmentation and conversion.
    /// </summary>
    Jieba
}

/// <summary>
/// Helper methods for working with <see cref="ConverterType"/> values.
/// </summary>
public static class ConverterTypeHelpers
{
    /// <summary>
    /// Parses a string into a <see cref="ConverterType"/> value.
    /// Falls back to a default value if the input is null, empty, or unrecognized.
    /// </summary>
    /// <param name="value">The string value to parse, e.g. "fmmseg" or "jieba".</param>
    /// <param name="default">The default <see cref="ConverterType"/> to use if parsing fails.</param>
    /// <returns>
    /// The corresponding <see cref="ConverterType"/> if recognized;
    /// otherwise returns the specified default.
    /// </returns>
    public static ConverterType Parse(string value, ConverterType @default = ConverterType.Fmmseg)
    {
        if (string.IsNullOrWhiteSpace(value)) return @default;

        return value.Trim().ToLowerInvariant() switch
        {
            "fmmseg" => ConverterType.Fmmseg,
            "jieba" => ConverterType.Jieba,
            _ => @default
        };
    }
}