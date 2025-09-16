using Microsoft.AspNetCore.Mvc;
using markdown_app_aspnet.Utilities;
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
        private readonly IHttpClientFactory _httpClientFactory; // ✅ class-level field

        public GrammarController(FileRegistry fileRegistry, ILogger<GrammarController> logger, IHttpClientFactory httpClientFactory)
        {
            _fileRegistry = fileRegistry;
            _logger = logger;
            _httpClientFactory = httpClientFactory; // ✅ assign in constructor
        }

        [HttpPost("check/{originalFileName}")]
        public async Task<IActionResult> CheckGrammar(string originalFileName)
        {
            var fileEntry = await _fileRegistry.GetFileByOriginalNameAsync(originalFileName);
            if (fileEntry == null)
                return NotFound("File not found in registry.");

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", fileEntry.StoredFile);
            if (!System.IO.File.Exists(filePath))
                return NotFound("Uploaded file not found on disk.");

            var grammarResults = new List<JsonElement>();
            var annotatedText = new StringBuilder();

            using var reader = new StreamReader(filePath);
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
                    // Send line to grammar API
                    var suggestion = await SendToGrammarApiAsync(line);
                    grammarResults.Add(suggestion);

                    // Keep original line in annotated text
                    annotatedText.AppendLine(line);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to check grammar for line: {Line}", line);
                    annotatedText.AppendLine(line);
                }
            }

            // Save grammar-checked file
            var checkedFileName = $"{Path.GetFileNameWithoutExtension(fileEntry.StoredFile)}_grammar_checked.txt";
            var checkedFilePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", checkedFileName);
            await System.IO.File.WriteAllTextAsync(checkedFilePath, annotatedText.ToString());

            return Ok(new
            {
                originalFile = originalFileName,
                storedFile = fileEntry.StoredFile,
                grammarCheckedFile = checkedFileName,
                grammarSuggestions = grammarResults
            });
        }

        // ✅ Instance method using _httpClientFactory
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

