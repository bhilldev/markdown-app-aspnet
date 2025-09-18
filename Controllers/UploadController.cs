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

            var registryPath = Path.Combine(_uploadFolder, "file_registry.json");
            _fileRegistry = new FileRegistry(registryPath);
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var originalFileName = Path.GetFileName(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}";
            var savePath = Path.Combine(_uploadFolder, uniqueFileName);

            await using (var stream = System.IO.File.Create(savePath))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("Uploaded file '{OriginalName}' saved as '{UniqueName}' at '{SavePath}'",
                originalFileName, uniqueFileName, savePath);

            string? htmlFileName = null;
            string? htmlFilePath = null;

            if (Path.GetExtension(originalFileName).Equals(".md", StringComparison.OrdinalIgnoreCase))
            {
                htmlFileName = $"{Path.GetFileNameWithoutExtension(uniqueFileName)}.html";
                htmlFilePath = Path.Combine(_uploadFolder, htmlFileName);

                try
                {
                    MarkdownToHtmlConverter.ConvertFileToHtml(savePath, htmlFilePath, originalFileName);
                    _logger.LogInformation("Converted Markdown file '{OriginalName}' to HTML '{HtmlFileName}'",
                        originalFileName, htmlFileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to convert Markdown to HTML.");
                }
            }

            await _fileRegistry.RegisterFileAsync(originalFileName, uniqueFileName, htmlFileName);

            return Ok(new
            {
                originalFile = originalFileName,
                storedFile = uniqueFileName,
                htmlFile = htmlFileName,
                htmlUrl = htmlFileName != null ? Url.Action(nameof(GetHtmlFile), new { fileName = htmlFileName }) : null
            });
        }

        // ✅ New endpoint: serve generated HTML file
        [HttpGet("html/{fileName}")]
        public IActionResult GetHtmlFile(string fileName)
        {
            var filePath = Path.Combine(_uploadFolder, fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound("HTML file not found.");

            var htmlContent = System.IO.File.ReadAllText(filePath);
            return Content(htmlContent, "text/html");
        }
    }
}


