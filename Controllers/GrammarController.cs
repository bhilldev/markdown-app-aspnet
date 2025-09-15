using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace markdown_app_aspnet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GrammarController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public GrammarController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpPost("check")]
        public async Task<IActionResult> CheckGrammar([FromBody] string text)
        {
            var requestBody = new StringContent(
                $"text={System.Net.WebUtility.UrlEncode(text)}&language=en-US",
                Encoding.UTF8,
                "application/x-www-form-urlencoded"
            );

            var response = await _httpClient.PostAsync("https://api.languagetool.org/v2/check", requestBody);

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "Error calling grammar API");

            var result = await response.Content.ReadAsStringAsync();
            return Ok(result);
        }
    }
}
