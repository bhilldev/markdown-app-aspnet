using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.WebUtilities;
using System.Net;
using markdown_app_aspnet.Utilities;


namespace markdown_app_aspnet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly ILogger<UploadController> _logger;
    private readonly string _targetFilePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");

    public UploadController(ILogger<UploadController> logger)
    {
        _logger = logger;
        Directory.CreateDirectory(_targetFilePath);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadPhysical()
    {
        var request = HttpContext.Request;

        if (!MultipartRequestHelper.IsMultipartContentType(request.ContentType))
        {
            return BadRequest("Expected a multipart request");
        }

        var boundary = HeaderUtilities.RemoveQuotes(
            MediaTypeHeaderValue.Parse(request.ContentType!).Boundary).Value;

        if (string.IsNullOrWhiteSpace(boundary))
        {
            return BadRequest("Missing content-type boundary.");
        }

        var reader = new MultipartReader(boundary, request.BodyReader.AsStream());

        MultipartSection? section;
        while ((section = await reader.ReadNextSectionAsync()) != null)
        {
            if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition))
            {
                if (contentDisposition.DispositionType.Equals("form-data") &&
                    !string.IsNullOrEmpty(contentDisposition.FileName.Value))
                {
                    // Generate a safe filename
                    var trustedFileNameForDisplay = WebUtility.HtmlEncode(contentDisposition.FileName.Value);
                    var trustedFileNameForFileStorage = Path.GetRandomFileName();

                    var saveToPath = Path.Combine(_targetFilePath, trustedFileNameForFileStorage);

                    await using var targetStream = System.IO.File.Create(saveToPath);
                    await section.Body.CopyToAsync(targetStream);

                    _logger.LogInformation(
                        "Uploaded file '{TrustedFileNameForDisplay}' saved to '{SaveToPath}'",
                        trustedFileNameForDisplay, saveToPath);
                }
            }
        }

        return Ok(new { status = "uploaded" });
    }
}
