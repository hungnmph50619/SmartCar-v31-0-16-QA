using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace SmartCar.WebUI.Controllers
{
    [Authorize]
    public class SecureFilesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public SecureFilesController(IHttpClientFactory httpClientFactory, IWebHostEnvironment environment)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> View(Guid id, CancellationToken cancellationToken)
        {
            if (id == Guid.Empty) return NotFound();
            var client = _httpClientFactory.CreateClient();
            var token = User.FindFirst("carbooktoken")?.Value;
            if (!string.IsNullOrWhiteSpace(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/secure-files/{id:D}");
            if (Request.Headers.TryGetValue("Range", out var range))
                request.Headers.TryAddWithoutValidation("Range", range.ToString());

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            Response.StatusCode = (int)response.StatusCode;
            Response.Headers.CacheControl = "no-store, no-cache";
            Response.Headers.Pragma = "no-cache";
            Response.Headers["X-Content-Type-Options"] = "nosniff";

            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.PartialContent)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden) return Forbid();
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return NotFound();
                return StatusCode((int)response.StatusCode);
            }

            if (response.Content.Headers.ContentType is not null)
                Response.ContentType = response.Content.Headers.ContentType.ToString();
            if (response.Content.Headers.ContentLength.HasValue)
                Response.ContentLength = response.Content.Headers.ContentLength.Value;
            if (response.Content.Headers.ContentRange is not null)
                Response.Headers["Content-Range"] = response.Content.Headers.ContentRange.ToString();
            if (response.Headers.AcceptRanges.Count > 0)
                Response.Headers["Accept-Ranges"] = string.Join(",", response.Headers.AcceptRanges);

            await response.Content.CopyToAsync(Response.Body, cancellationToken);
            return new EmptyResult();
        }
    }
}
