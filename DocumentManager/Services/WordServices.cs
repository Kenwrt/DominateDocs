using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.CustomProperties;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.VariantTypes;
using DocumentFormat.OpenXml.Wordprocessing;
using GemBox.Document;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenXmlPowerTools;
using System.IO.Compression;
using System.Xml.Linq;

//using Xceed.Words.NET;

namespace DocumentManager.Services;

public class WordServices : IWordServices
{
    private ILogger<WordServices> logger;
    private IWebHostEnvironment webEnv;
    private readonly IOptions<DocumentManagerConfigOptions> options;

    public WordServices(ILogger<WordServices> logger, IOptions<DocumentManagerConfigOptions> options, IWebHostEnvironment webEnv)
    {
        this.logger = logger;
        this.options = options;
        this.webEnv = webEnv;
    }

    //public async System.Threading.Tasks.Task GenerateBaselineDocumentAsync(string firmName, string docName)
    //{
    //    var fileName = Path.Combine(webEnv.WebRootPath, $"{docName}-{Guid.NewGuid()}.docx");
    //    Directory.CreateDirectory(Path.Combine(webEnv.WebRootPath, $"{firmName}/DocBaseLineTemplates"));
    //    var fullPath = Path.Combine(webEnv.WebRootPath, $"{firmName}/DocBaseLineTemplates", fileName);

    //    //using var mem = new MemoryStream();
    //    // using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(mem, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))

    //    using (var wordDoc = WordprocessingDocument.Create(fullPath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
    //    {
    //        MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
    //        mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new Body());

    //        // Create header/footer parts
    //        var headerPart = mainPart.AddNewPart<HeaderPart>();
    //        string headerPartId = mainPart.GetIdOfPart(headerPart);
    //        GenerateHeaderPartContent(headerPart);

    //        var footerPart = mainPart.AddNewPart<FooterPart>();
    //        string footerPartId = mainPart.GetIdOfPart(footerPart);
    //        GenerateFooterPartContent(footerPart);

    //        var sectionProps = new SectionProperties(
    //            new HeaderReference() { Type = HeaderFooterValues.Default, Id = headerPartId },
    //            new FooterReference() { Type = HeaderFooterValues.Default, Id = footerPartId },
    //            new PageSize(), new PageMargin());

    //        var body = mainPart.Document.Body;

    //        body.Append(CreateContentControl("RECITALS", $"Recitals Section!"));
    //        body.Append(CreateContentControl("AGREEMENT", $"Agreement Section"));

    //        mainPart.Document.Save();
    //    }

    //    await System.Threading.Tasks.Task.CompletedTask; // Simulate async operation
    //}

    //private void GenerateHeaderPartContent(HeaderPart headerPart)
    //{
    //    var header = new Header();

    //    var table = new DocumentFormat.OpenXml.Wordprocessing.Table();

    //    // Table properties with black borders
    //    DocumentFormat.OpenXml.Wordprocessing.TableProperties tableProperties = new DocumentFormat.OpenXml.Wordprocessing.TableProperties(
    //        new TableBorders(
    //            new DocumentFormat.OpenXml.Wordprocessing.TopBorder() { Val = BorderValues.Single, Color = "000000", Size = 4 },
    //            new DocumentFormat.OpenXml.Wordprocessing.BottomBorder() { Val = BorderValues.Single, Color = "000000", Size = 4 },
    //            new DocumentFormat.OpenXml.Wordprocessing.LeftBorder() { Val = BorderValues.Single, Color = "000000", Size = 4 },
    //            new DocumentFormat.OpenXml.Wordprocessing.RightBorder() { Val = BorderValues.Single, Color = "000000", Size = 4 },
    //            new DocumentFormat.OpenXml.Wordprocessing.InsideHorizontalBorder() { Val = BorderValues.Single, Color = "000000", Size = 4 },
    //            new DocumentFormat.OpenXml.Wordprocessing.InsideVerticalBorder() { Val = BorderValues.Single, Color = "000000", Size = 4 }
    //        ),
    //        new TableWidth() { Width = "5000", Type = TableWidthUnitValues.Pct } // 100% width
    //    );

    //    table.AppendChild(tableProperties);

    //    var tableRow = new DocumentFormat.OpenXml.Wordprocessing.TableRow();

    //    // Cell 1 - 25%
    //    DocumentFormat.OpenXml.Wordprocessing.TableCell cell1 = CreateTableCell("Cell 1", 1250); // 25% of 5000
    //                                                                                             // Cell 2 - 50%
    //    DocumentFormat.OpenXml.Wordprocessing.TableCell cell2 = CreateTableCell("Cell 2", 2500); // 50% of 5000
    //                                                                                             // Cell 3 - 25%
    //    DocumentFormat.OpenXml.Wordprocessing.TableCell cell3 = CreateTableCell("Cell 3", 1250); // 25% of 5000

    //    tableRow.Append(cell1, cell2, cell3);
    //    table.Append(tableRow);

    //    header.Append(table);
    //    headerPart.Header = header;
    //    headerPart.Header.Save();
    //}

    //private DocumentFormat.OpenXml.Wordprocessing.TableCell CreateTableCell(string text, int widthPercent)
    //{
    //    return new DocumentFormat.OpenXml.Wordprocessing.TableCell(
    //        new DocumentFormat.OpenXml.Wordprocessing.TableCellProperties(
    //            new TableCellWidth() { Width = widthPercent.ToString(), Type = TableWidthUnitValues.Pct }),
    //        new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
    //            new DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties(new Justification() { Val = JustificationValues.Center }),
    //            new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text(text)))
    //    );
    //}

    //private void AddTableToBody(Body body)
    //{
    //    var table = new DocumentFormat.OpenXml.Wordprocessing.Table();

    //    // Table properties (borders and total width)
    //    var tableProperties = new DocumentFormat.OpenXml.Wordprocessing.TableProperties(
    //        new TableBorders(
    //            new DocumentFormat.OpenXml.Wordprocessing.TopBorder() { Val = BorderValues.Single, Color = "000000", Size = 4 },
    //            new DocumentFormat.OpenXml.Wordprocessing.BottomBorder() { Val = BorderValues.Single, Color = "000000", Size = 4 },
    //            new DocumentFormat.OpenXml.Wordprocessing.LeftBorder() { Val = BorderValues.Single, Color = "000000", Size = 4 },
    //            new DocumentFormat.OpenXml.Wordprocessing.RightBorder() { Val = BorderValues.Single, Color = "000000", Size = 4 },
    //            new DocumentFormat.OpenXml.Wordprocessing.InsideHorizontalBorder() { Val = BorderValues.Single, Color = "000000", Size = 4 },
    //            new DocumentFormat.OpenXml.Wordprocessing.InsideVerticalBorder() { Val = BorderValues.Single, Color = "000000", Size = 4 }
    //        ),
    //        new TableWidth() { Width = "5000", Type = TableWidthUnitValues.Pct } // 100%
    //    );

    //    table.AppendChild(tableProperties);

    //    // Create one row with 3 cells
    //    var row = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
    //    row.Append(
    //        CreateTableCell("Body Cell 1", 1250),
    //        CreateTableCell("Body Cell 2", 2500),
    //        CreateTableCell("Body Cell 3", 1250)
    //    );

    //    table.Append(row);
    //    body.Append(table);
    //}

    //public async Task<MemoryStream> ConvertWordToPdfAsync(MemoryStream wordStream, CancellationToken cancellationToken = default)
    //{
    //    if (wordStream == null)
    //        throw new ArgumentNullException(nameof(wordStream));

    //    if (wordStream.CanSeek)
    //        wordStream.Position = 0;

    //    var pdfStream = new MemoryStream();

    //    // GemBox is synchronous; wrap in Task.Run so you can keep async flows.
    //    await Task.Run(() =>
    //    {
    //        cancellationToken.ThrowIfCancellationRequested();

    //        // Load DOCX from stream
    //        var document = DocumentModel.Load(wordStream, LoadOptions.Docx);

    //        // Save as PDF to output stream
    //        document.Save(pdfStream, SaveOptions.PdfDefault);

    //        if (pdfStream.CanSeek)
    //            pdfStream.Position = 0;

    //    }, cancellationToken);

    //    return pdfStream;
    //}

    //private void GenerateFooterPartContent(FooterPart footerPart)
    //{
    //    var footer = new Footer(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("Page Footer © 2025"))));
    //    footerPart.Footer = footer;
    //    footer.Save();
    //}

    //public void ReplaceContentControlText(MainDocumentPart mainPart, string tag, string newValue)
    //{
    //    var sdt = mainPart.Document.Body
    //        .Descendants<SdtBlock>()
    //        .FirstOrDefault(s => s.SdtProperties.GetFirstChild<Tag>()?.Val == tag);

    //    if (sdt != null)
    //    {
    //        var run = sdt.Descendants<Run>().FirstOrDefault();
    //        if (run != null)
    //        {
    //            run.RemoveAllChildren<Text>();
    //            run.AppendChild(new Text(newValue));
    //        }
    //    }
    //}

    //private SdtBlock CreateContentControl(string tag, string content)
    //{
    //    return new SdtBlock(
    //        new SdtProperties(
    //            new Tag { Val = tag },
    //            new SdtAlias { Val = tag }
    //        ),
    //        new SdtContentBlock(
    //            new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
    //                new Run(new Text(content))
    //            )
    //        )
    //    );
    //}

    public async Task<MemoryStream> InsertHiddenTagAsync(Stream originalDocStream, Dictionary<String, String> tagList)
    {
        // Copy input stream into memory
        var output = new MemoryStream();
        originalDocStream.CopyTo(output);
        output.Position = 0;

        using var doc = WordprocessingDocument.Open(output, true);
        {
            // Add or get CustomFilePropertiesPart
            var customProps = doc.CustomFilePropertiesPart;
            if (customProps == null)
            {
                customProps = doc.AddCustomFilePropertiesPart();
                customProps.Properties = new Properties();
            }

            var props = customProps.Properties!;

            foreach (var tag in tagList)
            {
                // Remove if already exists
                var existing = props.Elements<CustomDocumentProperty>()
                                .FirstOrDefault(p => p.Name == tag.Key);
                existing?.Remove();

                // Create a new property
                var pid = Int32Value.FromInt32((int)(props.Elements<CustomDocumentProperty>().Count() + 2));

                var newProp = new CustomDocumentProperty
                {
                    Name = tag.Key,
                    FormatId = "{D5CDD505-2E9C-101B-9397-08002B2CF9AE}", // string type
                    PropertyId = pid,
                    VTLPWSTR = new VTLPWSTR(tag.Value)
                };
                props.AppendChild(newProp);
            }
            props.Save();
        }
        ;

        output.Position = 0;

        return output;
    }

    public MemoryStream InsertHiddenTag(Stream originalDocStream, String tagName, String tagValue)
    {
        // Copy input stream into memory
        var output = new MemoryStream();
        originalDocStream.CopyTo(output);
        output.Position = 0;

        using var doc = WordprocessingDocument.Open(output, true);
        {
            // Add or get CustomFilePropertiesPart
            var customProps = doc.CustomFilePropertiesPart;
            if (customProps == null)
            {
                customProps = doc.AddCustomFilePropertiesPart();
                customProps.Properties = new Properties();
            }

            var props = customProps.Properties!;

            // Remove if already exists
            var existing = props.Elements<CustomDocumentProperty>()
                            .FirstOrDefault(p => p.Name == tagName);
            existing?.Remove();

            // Create a new property
            var pid = Int32Value.FromInt32((int)(props.Elements<CustomDocumentProperty>().Count() + 2));

            var newProp = new CustomDocumentProperty
            {
                Name = tagName,
                FormatId = "{D5CDD505-2E9C-101B-9397-08002B2CF9AE}", // string type
                PropertyId = pid,
                VTLPWSTR = new VTLPWSTR(tagValue)
            };
            props.AppendChild(newProp);

            props.Save();

            //var newProp = new CustomDocumentProperty
            //{
            //    Name = tagName,
            //    FormatId = "{D5CDD505-2E9C-101B-9397-08002B2CF9AE}", // string type
            //    PropertyId = pid,
            //    VTLPWSTR = new VTLPWSTR(tagValue)
            //};
        }
        ;

        output.Position = 0;

        return output;
    }

    public MemoryStream InsertHiddenTag(Stream originalDocStream, Dictionary<String, String> tagList)
    {
        // Copy input stream into memory
        var output = new MemoryStream();
        originalDocStream.CopyTo(output);
        output.Position = 0;

        using var doc = WordprocessingDocument.Open(output, true);
        {
            // Add or get CustomFilePropertiesPart
            var customProps = doc.CustomFilePropertiesPart;
            if (customProps == null)
            {
                customProps = doc.AddCustomFilePropertiesPart();
                customProps.Properties = new Properties();
            }

            var props = customProps.Properties!;

            foreach (var tag in tagList)
            {
                // Remove if already exists
                var existing = props.Elements<CustomDocumentProperty>()
                                .FirstOrDefault(p => p.Name == tag.Key);
                existing?.Remove();

                // Create a new property
                var pid = Int32Value.FromInt32((int)(props.Elements<CustomDocumentProperty>().Count() + 2));

                var newProp = new CustomDocumentProperty
                {
                    Name = tag.Key,
                    FormatId = "{D5CDD505-2E9C-101B-9397-08002B2CF9AE}", // string type
                    PropertyId = pid,
                    VTLPWSTR = new VTLPWSTR(tag.Value)
                };
                props.AppendChild(newProp);
            }
            props.Save();
        }
       ;

        output.Position = 0;

        return output;
    }

    public async Task<MemoryStream> InsertHiddenTagAsync(Stream originalDocStream, String tagName, String tagValue)
    {
        // Copy input stream into memory
        var output = new MemoryStream();
        originalDocStream.CopyTo(output);
        output.Position = 0;

        using var doc = WordprocessingDocument.Open(output, true);
        {
            // Add or get CustomFilePropertiesPart
            var customProps = doc.CustomFilePropertiesPart;
            if (customProps == null)
            {
                customProps = doc.AddCustomFilePropertiesPart();
                customProps.Properties = new Properties();
            }

            var props = customProps.Properties!;

            // Remove if already exists
            var existing = props.Elements<CustomDocumentProperty>()
                            .FirstOrDefault(p => p.Name == tagName);
            existing?.Remove();

            // Create a new property
            var pid = Int32Value.FromInt32((int)(props.Elements<CustomDocumentProperty>().Count() + 2));

            var newProp = new CustomDocumentProperty
            {
                Name = tagName,
                FormatId = "{D5CDD505-2E9C-101B-9397-08002B2CF9AE}", // string type
                PropertyId = pid,
                VTLPWSTR = new VTLPWSTR(tagValue)
            };
            props.AppendChild(newProp);

            props.Save();

            //var newProp = new CustomDocumentProperty
            //{
            //    Name = tagName,
            //    FormatId = "{D5CDD505-2E9C-101B-9397-08002B2CF9AE}", // string type
            //    PropertyId = pid,
            //    VTLPWSTR = new VTLPWSTR(tagValue)
            //};
        }
        ;

        output.Position = 0;

        return output;
    }

    public async Task<string> RetrieveHiddenTagsAsync(Stream originalDocStream, string tagName)
    {
        string tagValue = string.Empty;

        // Copy input stream into memory
        var output = new MemoryStream();

        originalDocStream.Position = 0; // Ensure we start from the beginning of the stream

        originalDocStream.CopyTo(output);

        output.Position = 0;

        using var doc = WordprocessingDocument.Open(output, false);
        {
            var customProps = doc.CustomFilePropertiesPart?.Properties;

            var prop = customProps?.Elements<CustomDocumentProperty>()
                                  .FirstOrDefault(p => p.Name == tagName);
            tagValue = prop?.InnerText;
        }

        output.Position = 0;

        return tagValue ?? string.Empty;
    }

    private DocumentFormat.OpenXml.Drawing.Paragraph MarkerParagraph(string text, bool hidden)
    {
        var rPr = new RunProperties();
        if (hidden) rPr.Append(new Vanish()); // Hidden font property
        var r = new DocumentFormat.OpenXml.Drawing.Run(rPr, new Text(text));
        var p = new DocumentFormat.OpenXml.Drawing.Paragraph(r);
        return p;
    }

    private DocumentFormat.OpenXml.Drawing.Paragraph CreateParagraph(string text, bool bold = false, bool italic = false, string? fontSize = null)
    {
        var rPr = new RunProperties();
        if (bold) rPr.Append(new Bold());
        if (italic) rPr.Append(new Italic());
        if (!string.IsNullOrEmpty(fontSize)) rPr.Append(new FontSize { Val = fontSize });

        var r = new DocumentFormat.OpenXml.Drawing.Run();
        if (rPr.HasChildren) r.Append(rPr);
        r.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });

        return new DocumentFormat.OpenXml.Drawing.Paragraph(r);
    }

    public void CheckForDuplicateZipEntries(byte[] docxBytes)
    {
        using var za = new ZipArchive(new MemoryStream(docxBytes), ZipArchiveMode.Read);
        var dups = za.Entries
            .GroupBy(e => e.FullName, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .ToList();

        if (dups.Count == 0)
            Console.WriteLine("No duplicate ZIP entries.");
        else
            foreach (var d in dups)
                logger.LogDebug($"DUP: {d.Name} x{d.Count}");
    }

    public byte[] RemoveBadCompatSetting(byte[] docxBytes)
    {
        using var ms = new MemoryStream();
        ms.Write(docxBytes, 0, docxBytes.Length);
        ms.Position = 0;

        using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true))
        {
            var entry = zip.GetEntry("word/settings.xml");
            if (entry == null) return docxBytes;

            XDocument xdoc;
            using (var s = entry.Open())
            {
                xdoc = XDocument.Load(s);
            }

            // WordprocessingML namespace
            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

            // Remove: <w:compatSetting w:name="useWord2013TrackBottomHyphenation" ... />
            var badNodes = xdoc
                .Descendants(w + "compatSetting")
                .Where(e => (string?)e.Attribute(w + "name") == "useWord2013TrackBottomHyphenation")
                .ToList();

            if (badNodes.Count == 0)
                return docxBytes;

            foreach (var n in badNodes)
                n.Remove();

            // Replace the entry contents (ZipArchive entry stream is write-only-ish)
            entry.Delete();
            var newEntry = zip.CreateEntry("word/settings.xml", CompressionLevel.Optimal);

            using (var outStream = newEntry.Open())
            {
                xdoc.Save(outStream);
            }
        }

        return ms.ToArray();
    }

    public byte[] ReplaceTokens(byte[] docxBytes, IDictionary<string, string> map)
    {
        if (docxBytes == null || docxBytes.Length == 0)
            throw new ArgumentException("Empty document.");

        // Wrap the entire DOCX package
        var wml = new WmlDocument("template.docx", docxBytes);

        // Replace longer keys first to avoid overlap {Borrower.Name} vs {Borrower.Name(s)}
        foreach (var kv in map.OrderByDescending(k => k.Key.Length))
        {
            var replacement = kv.Value ?? string.Empty;
            // matchCase: true if your placeholders are exact-cased, false if you want case-insensitive
            wml = TextReplacer.SearchAndReplace(wml, kv.Key, replacement, matchCase: true);
        }

        return wml.DocumentByteArray;
    }

    public List<DocError> ValidateDocx(byte[] bytes)
    {
        List<DocError> errorsList = new List<DocError>();

        using var ms = new MemoryStream(bytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var validator = new OpenXmlValidator(FileFormatVersions.Office2013); // or Office2013
        var errors = validator.Validate(doc);

        foreach (var e in errors)
        {
            errorsList.Add(new DocError
            {
                Description = e.Description,
                PartUri = e.Part?.Uri.ToString(),
                XPath = e.Path?.XPath,
                Id = e.Id
            });
        }

        return errorsList;
    }

    public string DetectFormatBySignature(byte[] b)
    {
        try
        {
            if (b.Length >= 2 && b[0] == (byte)'P' && b[1] == (byte)'K') return "ZIP (likely DOCX/XLSX/PPTX)";
            if (b.Length >= 8 && b[0] == 0xD0 && b[1] == 0xCF && b[2] == 0x11 && b[3] == 0xE0) return "OLE (likely legacy .doc/.xls)";
            if (b.Length >= 5 && b[0] == '<') return "Looks like XML/HTML text";
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }

        return "Unknown";
    }

    public sealed class DocError
    {
        public string Description { get; set; }
        public string PartUri { get; set; }
        public string XPath { get; set; }
        public string Id { get; set; }
    }

    public async Task<MemoryStream> ConvertWordToPdfAsync(MemoryStream wordStream, CancellationToken cancellationToken = default)
    {
        if (wordStream == null)
            throw new ArgumentNullException(nameof(wordStream));

        if (wordStream.CanSeek)
            wordStream.Position = 0;

        var pdfStream = new MemoryStream();

        // GemBox is synchronous; wrap in Task.Run so you can keep async flows.

        // Load DOCX from stream
        var document = DocumentModel.Load(wordStream);

        // Save as PDF to output stream
        document.Save(pdfStream, GemBox.Document.SaveOptions.PdfDefault);

        if (pdfStream.CanSeek)
            pdfStream.Position = 0;

        return pdfStream;
    }
}