using Microsoft.AspNetCore.Mvc;
using markdown_app_aspnet.Utilities;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace markdown_app_aspnet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GrammarController : ControllerBase
    {
        private readonly FileRegistry _fileRegistry;
        private readonly ILogger<GrammarController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public GrammarController(FileRegistry fileRegistry, ILogger<GrammarController> logger, IHttpClientFactory httpClientFactory)
        {
            _fileRegistry = fileRegistry;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("check/{originalFileName}")]
        public async Task<IActionResult> CheckGrammar(string originalFileName)
        {
            // 1. Lookup cleaned file
            var fileEntry = await _fileRegistry.GetFileByOriginalNameAsync(originalFileName);
            if (fileEntry == null) return NotFound("File not found in registry.");

            var cleanedFilePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", fileEntry.CleanedFile);
            if (!System.IO.File.Exists(cleanedFilePath)) return NotFound("Cleaned file not found on disk.");

            var grammarResults = new List<JsonElement>();
            var annotatedText = new StringBuilder();

            // 2. Read file line by line
            using var reader = new StreamReader(cleanedFilePath);
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    annotatedText.AppendLine();
                    continue;
                }

                try
                {
                    var suggestion = await SendToGrammarApiAsync(line);
                    grammarResults.Add(suggestion);

                    // For simplicity, append the original line. You could apply fixes if API provides them.
                    annotatedText.AppendLine(line);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to check grammar for line: {Line}", line);
                    annotatedText.AppendLine(line); // keep line even if API fails
                }
            }

            // 3. Save a downloadable grammar-checked file
            var checkedFileName = $"{Path.GetFileNameWithoutExtension(fileEntry.CleanedFile)}_grammar_checked.txt";
            var checkedFilePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", checkedFileName);
            await System.IO.File.WriteAllTextAsync(checkedFilePath, annotatedText.ToString());

            // 4. Return JSON + downloadable file name
            return Ok(new
            {
                originalFile = originalFileName,
                cleanedFile = fileEntry.CleanedFile,
                grammarCheckedFile = checkedFileName,
                grammarSuggestions = grammarResults
            });
        }

        private async Task<JsonElement> SendToGrammarApiAsync(string text)
        {
            var client = _httpClientFactory.CreateClient();

            var payload = new
            {
                text = text,
                language = "en-US"
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.languagetoolplus.com/v2/check", content);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonString);
            return doc.RootElement.Clone();
        }
    }
}
