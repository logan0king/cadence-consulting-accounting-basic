using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;

namespace CadenceAccounting.Pages.Reports
{
    public class ViewDocumentModel : PageModel
    {
        public string DocumentTitle { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public string MarkdownContent { get; set; } = string.Empty;
        public bool IsMarkdown { get; set; } = false;

        public IActionResult OnGet(string? doc = null)
        {
            if (string.IsNullOrEmpty(doc))
            {
                return RedirectToPage("/Reports/Documentation");
            }

            var documentPath = doc.ToLower() switch
            {
                "manual" => "docs/ReportDesignerUserManual.md",
                "quickref" => "docs/ReportDesignerQuickReference.md",
                "tutorials" => "docs/ReportDesignerVideoTutorialScript.md",
                _ => "docs/ReportDesignerUserManual.md"
            };

            DocumentTitle = doc.ToLower() switch
            {
                "manual" => "Report Designer User Manual",
                "quickref" => "Quick Reference Card",
                "tutorials" => "Video Tutorial Scripts",
                _ => "Report Designer User Manual"
            };

            try
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), documentPath);
                
                if (System.IO.File.Exists(fullPath))
                {
                    var content = System.IO.File.ReadAllText(fullPath);
                    
                    if (documentPath.EndsWith(".md"))
                    {
                        IsMarkdown = true;
                        MarkdownContent = content;
                        // For now, we'll show the raw markdown
                        // In a real implementation, you'd convert it to HTML
                        HtmlContent = $"<pre>{System.Net.WebUtility.HtmlEncode(content)}</pre>";
                    }
                    else
                    {
                        HtmlContent = content;
                    }
                }
                else
                {
                    HtmlContent = "<p>Document not found. Please check the file path.</p>";
                }
            }
            catch (Exception ex)
            {
                HtmlContent = $"<p>Error loading document: {ex.Message}</p>";
            }

            return Page();
        }
    }
}
