using Microsoft.AspNetCore.Mvc;
using markdown_app_aspnet.Utilities;
using Markdig;

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

            // Register in file_registry.json
            await _fileRegistry.RegisterFileAsync(originalFileName, uniqueFileName, null);

            // Response
            return Ok(new
            {
                originalFile = originalFileName,
                storedFile = uniqueFileName
            });
        }


        public async Task<IActionResult> ConvertToHtml(string originalFileName)
        {
            var fileEntry = await _fileRegistry.GetFileByOriginalNameAsync(originalFileName);
            if (fileEntry == null)
                return NotFound("File not found in registry.");

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", fileEntry.StoredFile);
            if (!System.IO.File.Exists(filePath))
                return NotFound("Uploaded file not found on disk.");

            using var reader = new StreamReader(filePath);
            // Configure the pipeline with all advanced extensions active
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var convertedToHtmlFile = Markdown.ToHtml(line, pipeline);

            }

            return Ok(new
            {
                originalFile = originalFileName,
                storedFile = fileEntry.StoredFile,
                htmlFile = convertedToHtmlFile
            });



        }
    }
}

