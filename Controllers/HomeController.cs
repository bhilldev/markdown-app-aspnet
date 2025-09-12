using Microsoft.AspNetCore.Mvc;
using markdown_app_aspnet.Utilities;

namespace markdown_app_aspnet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly ILogger<UploadController> _logger;
        private readonly string _uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        private readonly FileRegistry _registry;

        public UploadController(ILogger<UploadController> logger, FileRegistry registry)
        {
            _logger = logger;
            _registry = registry;
            Directory.CreateDirectory(_uploadFolder);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            // Sanitize filename
            var trustedFileName = Path.GetFileName(file.FileName);
            var savePath = Path.Combine(_uploadFolder, trustedFileName);

            // Save original uploaded file
            await using (var stream = System.IO.File.Create(savePath))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("Uploaded file saved to '{SavePath}'", savePath);

            // Clean Markdown
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
            var cleanedFileName = Path.GetFileNameWithoutExtension(trustedFileName) + "_cleaned.txt";
            var cleanedFilePath = Path.Combine(_uploadFolder, cleanedFileName);
            await System.IO.File.WriteAllTextAsync(cleanedFilePath, cleanedText);

            // Register both files in the registry
            var fileId = Guid.NewGuid().ToString("N");
            _registry.AddFile(fileId, savePath);
            _registry.AddFile(fileId + "_cleaned", cleanedFilePath);

            return Ok(new
            {
                fileId,
                originalFile = trustedFileName,
                cleanedFile = cleanedFileName,
                cleanedContentPreview = cleanedText.Length > 200 ? cleanedText.Substring(0, 200) + "..." : cleanedText
            });
        }
    }
}

