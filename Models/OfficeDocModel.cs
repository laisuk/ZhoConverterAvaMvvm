using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OpenccFmmsegLib;
using OpenccJiebaLib;

namespace ZhoConverterAvaMvvm.Models;

public record ConverterHelper(string Opencc, string Config);

/// <summary>
/// Provides functionality to convert Office document formats (.docx, .xlsx, .pptx, .odt)
/// using the Opencc converter with optional font name preservation.
/// </summary>
public static class OfficeDocModel
{
    // Supported Office file formats for Office documents conversion.
    public static readonly HashSet<string> OfficeFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "docx", "xlsx", "pptx", "odt", "ods", "odp", "epub"
    };

    /// <summary>
    /// Converts an Office document by applying OpenCC conversion on specific XML parts.
    /// Optionally preserves original font names to prevent them from being altered.
    /// </summary>
    /// <param name="inputPath">The full path to the input Office document (e.g., .docx).</param>
    /// <param name="outputPath">The desired full path to the converted output file.</param>
    /// <param name="format">The document format ("docx", "xlsx", "pptx", "odt", "ods", "odp", or "epub").</param>
    /// <param name="converterHelper">The OpenCC converter instance used for conversion.</param>
    /// <param name="converterFmmseg">The OpenccFmmseg converter</param>
    /// <param name="converterJieba">The OpenccJieba converter</param>
    /// <param name="punctuation">Whether to convert punctuation during OpenCC transformation.</param>
    /// <param name="keepFont">If true, font names are preserved using placeholder markers during conversion.</param>
    /// <returns>A tuple indicating whether the conversion succeeded and a status message.</returns>
    public static async Task<(bool Success, string Message)> ConvertOfficeDocAsync(
        string inputPath,
        string outputPath,
        string format,
        ConverterHelper converterHelper,
        OpenccFmmseg converterFmmseg,
        OpenccJieba converterJieba,
        bool punctuation,
        bool keepFont = false)
    {
        if (string.IsNullOrWhiteSpace(inputPath) || !File.Exists(inputPath))
            return (false, "❌ Input file not found.");
        if (string.IsNullOrWhiteSpace(format) || !OfficeFormats.Contains(format))
            return (false, $"❌ Unsupported or invalid format: {format}");

        var tempDir = Path.Combine(Path.GetTempPath(), $"{format}_temp_" + Guid.NewGuid());

        // Choose converter once (case-insensitive)
        var useFmmseg = converterHelper.Opencc.Equals("fmmseg", StringComparison.OrdinalIgnoreCase);
        var convert = useFmmseg
            ? new Func<string, string, bool, string>(converterFmmseg.Convert)
            : converterJieba.Convert;

        try
        {
            ZipFile.ExtractToDirectory(inputPath, tempDir);

            // Collect target XML parts by format
            var targetXmlPaths = format switch
            {
                "docx" => [Path.Combine("word", "document.xml")],
                "xlsx" => [Path.Combine("xl", "sharedStrings.xml")],
                "pptx" => Directory.Exists(Path.Combine(tempDir, "ppt"))
                    ? Directory.GetFiles(Path.Combine(tempDir, "ppt"), "*.xml", SearchOption.AllDirectories)
                        .Where(p => Path.GetFileName(p).StartsWith("slide", StringComparison.OrdinalIgnoreCase)
                                    || p.Contains("notesSlide", StringComparison.OrdinalIgnoreCase)
                                    || p.Contains("slideMaster", StringComparison.OrdinalIgnoreCase)
                                    || p.Contains("slideLayout", StringComparison.OrdinalIgnoreCase)
                                    || p.Contains("comment", StringComparison.OrdinalIgnoreCase))
                        .Select(p => Path.GetRelativePath(tempDir, p))
                        .ToList()
                    : [],
                "odt" or "ods" or "odp" => ["content.xml"],
                "epub" => Directory.Exists(tempDir)
                    ? Directory.GetFiles(tempDir, "*.*", SearchOption.AllDirectories)
                        .Where(f => f.EndsWith(".xhtml", StringComparison.OrdinalIgnoreCase)
                                    || f.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
                                    || f.EndsWith(".opf", StringComparison.OrdinalIgnoreCase)
                                    || f.EndsWith(".ncx", StringComparison.OrdinalIgnoreCase))
                        .Select(f => Path.GetRelativePath(tempDir, f))
                        .ToList()
                    : [],
                _ => null
            };

            if (targetXmlPaths is null || targetXmlPaths.Count == 0)
                return (false, $"⚠️ No valid XML fragments were found for '{format}'.");

            var convertedCount = 0;

            foreach (var relativePath in targetXmlPaths)
            {
                var fullPath = Path.Combine(tempDir, relativePath);
                if (!File.Exists(fullPath)) continue;

                var xmlContent = await File.ReadAllTextAsync(fullPath, Encoding.UTF8);

                Dictionary<string, string>? fontMap = null;
                if (keepFont)
                {
                    var pattern = format switch
                    {
                        "docx" => """(w:eastAsia="|w:ascii="|w:hAnsi="|w:cs=")(.*?)(")""",
                        "xlsx" => """(val=")(.*?)(")""",
                        "pptx" => """(typeface=")(.*?)(")""",
                        "odt" or "ods" or "odp" =>
                            """((?:style:font-name(?:-asian|-complex)?|svg:font-family|style:name)=["'])([^"']+)(["'])""",
                        "epub" => """(font-family\s*:\s*)([^;"']+)([;"'])?""",
                        _ => null
                    };

                    if (pattern is not null)
                    {
                        fontMap = new Dictionary<string, string>();
                        var fontCounter = 0;
                        xmlContent = Regex.Replace(xmlContent, pattern, m =>
                        {
                            var original = m.Groups[2].Value;
                            var marker = $"__F_O_N_T_{fontCounter++}__";
                            fontMap![marker] = original;

                            // group 3 may be optional in epub
                            var tail = m.Groups.Count >= 4 ? m.Groups[3].Value : string.Empty;
                            return m.Groups[1].Value + marker + tail;
                        });
                    }
                }

                // Convert using selected engine
                string convertedXml = convert(xmlContent, converterHelper.Config, punctuation);

                if (fontMap is not null)
                {
                    foreach (var kv in fontMap)
                        convertedXml = convertedXml.Replace(kv.Key, kv.Value);
                }

                await File.WriteAllTextAsync(fullPath, convertedXml, Encoding.UTF8);
                convertedCount++;
            }

            if (convertedCount == 0)
                return (false, $"⚠️ No convertible XML fragments were found in '{format}'.");

            if (File.Exists(outputPath)) File.Delete(outputPath);

            if (format.Equals("epub", StringComparison.OrdinalIgnoreCase))
            {
                var (zipOk, zipMsg) = CreateEpubZipWithSpec(tempDir, outputPath);
                if (!zipOk) return (false, zipMsg);
            }
            else
            {
                ZipFile.CreateFromDirectory(tempDir, outputPath, CompressionLevel.Optimal, includeBaseDirectory: false);
            }

            return (true, $"✅ Successfully converted {convertedCount} fragment(s) in {format} document.");
        }
        catch (Exception ex)
        {
            return (false, $"❌ Conversion failed: {ex.Message}");
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
            catch
            {
                /* best-effort cleanup */
            }
        }
    }


    /// <summary>
    /// Creates a valid EPUB-compliant ZIP archive from the specified source directory.
    /// Ensures the <c>mimetype</c> file is the first entry and uncompressed,
    /// as required by the EPUB specification.
    /// </summary>
    /// <param name="sourceDir">The temporary directory containing EPUB unpacked contents.</param>
    /// <param name="outputPath">The full path of the output EPUB file to be created.</param>
    /// <returns>Tuple indicating success and an informative message.</returns>
    private static (bool Success, string Message) CreateEpubZipWithSpec(string sourceDir, string outputPath)
    {
        var mimePath = Path.Combine(sourceDir, "mimetype");

        try
        {
            using var fs = new FileStream(outputPath, FileMode.Create);
            using var archive = new ZipArchive(fs, ZipArchiveMode.Create);

            // 1. Add mimetype first, uncompressed
            if (File.Exists(mimePath))
            {
                var mimeEntry = archive.CreateEntry("mimetype", CompressionLevel.NoCompression);
                using var entryStream = mimeEntry.Open();
                using var fileStream = File.OpenRead(mimePath);
                fileStream.CopyTo(entryStream);
            }
            else
            {
                return (false, "❌ 'mimetype' file is missing. EPUB requires this as the first entry.");
            }

            // 2. Add the rest (recursively)
            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                if (Path.GetFullPath(file) == Path.GetFullPath(mimePath))
                    continue;

                var entryPath = Path.GetRelativePath(sourceDir, file).Replace('\\', '/');
                var entry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                using var fileStream = File.OpenRead(file);
                fileStream.CopyTo(entryStream);
            }

            return (true, "✅ EPUB archive created successfully.");
        }
        catch (Exception ex)
        {
            return (false, $"❌ Failed to create EPUB: {ex.Message}");
        }
    }
}