using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CadenceAccounting.Pages
{
    [IgnoreAntiforgeryToken]
    public class TestPostModel : PageModel
    {
        public string? Result { get; set; }

        public void OnGet()
        {
        }

        public void OnPost(string testInput)
        {
            Result = $"POST received successfully! Input: {testInput}";
        }
    }
}
