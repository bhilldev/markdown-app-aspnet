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

        public UploadController(ILogger<UploadController> logger)
        {
            _logger = logger;
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

            // Save the original uploaded file
            await using (var stream = System.IO.File.Create(savePath))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("Uploaded file saved to '{SavePath}'", savePath);

            // Read the uploaded file and clean Markdown
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

            // Optionally save the cleaned version
            var cleanedFilePath = Path.Combine(_uploadFolder, Path.GetFileNameWithoutExtension(trustedFileName) + "_cleaned.txt");
            await System.IO.File.WriteAllTextAsync(cleanedFilePath, cleanedText);

            return Ok(new
            {
                originalFile = trustedFileName,
                cleanedFile = Path.GetFileName(cleanedFilePath),
                cleanedContentPreview = cleanedText.Length > 200 ? cleanedText.Substring(0, 200) + "..." : cleanedText
            });
        }
    }
}

