using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DocumentManager.RazorLite;

public static class DocxUtils
{
    // Creates a minimal docx with styled content and marker lines
    public static void CreateSampleTemplate(string path)
    {
        using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var main = doc.AddMainDocumentPart();
        main.Document = new Document(new Body());
        var body = main.Document.Body!;

        // Title styled paragraph
        body.Append(CreateParagraph("LOAN AGREEMENT", bold: true, fontSize: "32"));

        body.Append(CreateParagraph("This Loan Agreement (\"Agreement\") is entered into on {LoanDate} between:"));
        body.Append(CreateParagraph("Borrower: {Name}"));
        body.Append(CreateParagraph("Loan Amount: {LoanAmount}"));
        body.Append(new Paragraph(new Run(new Break())));

        // IF block
        body.Append(MarkerParagraph("[[IF Ken == \"Good Guy\"]]", hidden: true));
        body.Append(CreateParagraph("Dear {Name},", italic: true));
        body.Append(CreateParagraph("Thank you for not being terrible.", italic: false));
        body.Append(MarkerParagraph("[[END]]", hidden: true));

        body.Append(new Paragraph(new Run(new Break())));

        // FOREACH block
        body.Append(MarkerParagraph("[[FOREACH item in Model.Sections]]", hidden: true));
        // bullet-like paragraph with bold title and body
        var p = new Paragraph();
        var pPr = new ParagraphProperties();
        p.Append(pPr);
        p.Append(new Run(new Text("• ")));
        var runTitle = new Run();
        runTitle.Append(new RunProperties(new Bold()));
        runTitle.Append(new Text("{item.Title}"));
        p.Append(runTitle);
        p.Append(new Run(new Text(": ")));
        p.Append(new Run(new Text("{item.Body}")));
        body.Append(p);
        body.Append(MarkerParagraph("[[ENDFOREACH]]", hidden: true));

        main.Document.Save();
    }

    public static Paragraph MarkerParagraph(string text, bool hidden)
    {
        var rPr = new RunProperties();
        if (hidden) rPr.Append(new Vanish()); // Hidden font property
        var r = new Run(rPr, new Text(text));
        var p = new Paragraph(r);
        return p;
    }

    public static Paragraph CreateParagraph(string text, bool bold = false, bool italic = false, string? fontSize = null)
    {
        var rPr = new RunProperties();
        if (bold) rPr.Append(new Bold());
        if (italic) rPr.Append(new Italic());
        if (!string.IsNullOrEmpty(fontSize)) rPr.Append(new FontSize { Val = fontSize });

        var r = new Run();
        if (rPr.HasChildren) r.Append(rPr);
        r.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });

        return new Paragraph(r);
    }
}