using System.Net.Http.Headers;

namespace SmartCar.WebUI.Infrastructure
{
    /// <summary>
    /// Tự động chuyển JWT đang lưu trong cookie đăng nhập của WebUI sang Web API.
    /// Nhờ đó các controller quản trị dùng IHttpClientFactory không bị lỗi 401 do quên gắn Bearer token.
    /// </summary>
    public sealed class UserAccessTokenHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserAccessTokenHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Headers.Authorization is null)
            {
                var token = _httpContextAccessor.HttpContext?.User.FindFirst("carbooktoken")?.Value;
                if (!string.IsNullOrWhiteSpace(token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
