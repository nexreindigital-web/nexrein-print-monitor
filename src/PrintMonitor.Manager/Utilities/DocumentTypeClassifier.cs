using System.IO;

namespace PrintMonitor.Manager.Utilities;

public record DocumentTypeInfo(string Icon, string TypeName, string BadgeColor, string Category);

public static class DocumentTypeClassifier
{
    public static DocumentTypeInfo Classify(string? documentName)
    {
        if (string.IsNullOrWhiteSpace(documentName))
        {
            return new DocumentTypeInfo("📋", "Generic Document", "#64748B", "Generic");
        }

        var clean = documentName.Trim();
        var lower = clean.ToLowerInvariant();

        // 1. Check file extensions
        var ext = Path.GetExtension(clean).ToLowerInvariant();

        if (ext == ".pdf" || lower.Contains(".pdf"))
            return new DocumentTypeInfo("📕", "PDF Document", "#EF4444", "PDF");

        if (ext is ".docx" or ".doc" or ".docm" or ".dotx" or ".dot" or ".rtf" || lower.Contains("word") || lower.Contains(".docx"))
            return new DocumentTypeInfo("📝", "Word Document (DOCX)", "#3B82F6", "Word");

        if (ext is ".pub" || lower.Contains("publisher") || lower.Contains(".pub"))
            return new DocumentTypeInfo("📰", "Publisher (PUB)", "#14B8A6", "Publisher");

        if (ext is ".xlsx" or ".xls" or ".xlsm" or ".csv" || lower.Contains("excel") || lower.Contains(".xlsx"))
            return new DocumentTypeInfo("📊", "Excel Spreadsheet", "#10B981", "Excel");

        if (ext is ".pptx" or ".ppt" or ".ppsx" || lower.Contains("powerpoint") || lower.Contains(".pptx"))
            return new DocumentTypeInfo("📽️", "PowerPoint Slides", "#F97316", "PowerPoint");

        if (ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".tiff" or ".webp" or ".svg" || lower.Contains("photos") || lower.Contains("paint"))
            return new DocumentTypeInfo("🖼️", "Image Graphic", "#A855F7", "Image");

        if (ext is ".txt" or ".log" or ".json" or ".xml" or ".md" or ".ini" or ".bat" or ".ps1" || lower.Contains("notepad"))
            return new DocumentTypeInfo("📄", "Plain Text / Code", "#64748B", "Text");

        if (lower.Contains("test page") || lower.Contains("printer test") || lower.Contains("sınama sayfası"))
            return new DocumentTypeInfo("🧪", "Printer Test Page", "#F59E0B", "System");

        if (ext is ".html" or ".htm" || lower.Contains("http://") || lower.Contains("https://") || lower.Contains("chrome") || lower.Contains("edge") || lower.Contains("firefox"))
            return new DocumentTypeInfo("🌐", "Web Page", "#06B6D4", "Web");

        return new DocumentTypeInfo("📋", "Document", "#64748B", "Generic");
    }
}
