using Microsoft.AspNetCore.Mvc;
using markdown_app_aspnet.Utilities;

namespace markdown_app_aspnet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly ILogger<UploadController> _logger;
        private readonly string _uploadFolder;
        private readonly FileRegistry _fileRegistry;

        public UploadController(ILogger<UploadController> logger)
        {
            _logger = logger;
            _uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            Directory.CreateDirectory(_uploadFolder);

            // Initialize registry file inside uploads folder
            var registryPath = Path.Combine(_uploadFolder, "file_registry.json");
            _fileRegistry = new FileRegistry(registryPath);
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            // Client’s original filename (sanitized)
            var originalFileName = Path.GetFileName(file.FileName);

            // Generate safe, unique filename for storage
            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}";
            var savePath = Path.Combine(_uploadFolder, uniqueFileName);

            // Save uploaded file
            await using (var stream = System.IO.File.Create(savePath))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("Uploaded file '{OriginalName}' saved as '{UniqueName}' at '{SavePath}'",
                originalFileName, uniqueFileName, savePath);

            // Clean Markdown -> plain text
            string cleanedText;
            try
            {
                cleanedText = MarkdownCleaner.CleanMarkdownFile(savePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean Markdown file.");
                return StatusCode(500, "Error processing Markdown file.");
            }

            // Save cleaned version
            var cleanedFileName = $"{Path.GetFileNameWithoutExtension(uniqueFileName)}_cleaned.txt";
            var cleanedFilePath = Path.Combine(_uploadFolder, cleanedFileName);
            await System.IO.File.WriteAllTextAsync(cleanedFilePath, cleanedText);

            // Register in file_registry.json
            await _fileRegistry.RegisterFileAsync(originalFileName, uniqueFileName, cleanedFileName);

            // Response
            return Ok(new
            {
                originalFile = originalFileName,
                storedFile = uniqueFileName,
                cleanedFile = cleanedFileName,
                cleanedContentPreview = cleanedText.Length > 200 ? cleanedText.Substring(0, 200) + "..." : cleanedText
            });
        }
    }
}

