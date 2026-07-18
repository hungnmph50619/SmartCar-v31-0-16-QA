using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCar.WebUI.Models;

namespace SmartCar.WebUI.Controllers;

public sealed class CommunityController : Controller
{
    private static readonly string[] Categories =
    [
        "Chia sẻ hành trình", "Kinh nghiệm thuê xe", "Hỏi đáp",
        "Kinh nghiệm lái xe", "Chăm sóc xe", "Kinh nghiệm chủ xe"
    ];
    private readonly IHttpClientFactory _clients;
    private readonly IWebHostEnvironment _environment;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    public CommunityController(IHttpClientFactory clients, IWebHostEnvironment environment)
    {
        _clients = clients;
        _environment = environment;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index(string? query, string? category, string sort = "newest", int page = 1)
    {
        var url = $"api/community/posts?query={Uri.EscapeDataString(query ?? "")}&category={Uri.EscapeDataString(category ?? "")}&sort={Uri.EscapeDataString(sort)}&page={page}";
        try
        {
            var response = await _clients.CreateClient().GetAsync(url);
            var model = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<CommunityFeedViewModel>(JsonOptions)
                : null;
            ViewBag.Query = query; ViewBag.Category = category; ViewBag.Sort = sort;
            return View(model ?? new CommunityFeedViewModel { Categories = Categories.ToList() });
        }
        catch (HttpRequestException)
        {
            ViewBag.Error = "Cộng đồng đang tạm thời gián đoạn. Vui lòng thử lại sau.";
            return View(new CommunityFeedViewModel { Categories = Categories.ToList() });
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var response = await _clients.CreateClient().GetAsync($"api/community/posts/{id}");
        if (!response.IsSuccessStatusCode) return RedirectToAction(nameof(Index));
        var model = await response.Content.ReadFromJsonAsync<CommunityPostDetailViewModel>(JsonOptions);
        return model is null ? RedirectToAction(nameof(Index)) : View(model);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CommunityPostFormViewModel();
        await PopulateFormOptions(model);
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Create(CommunityPostFormViewModel model)
    {
        await PopulateFormOptions(model);
        if (!ModelState.IsValid) return View(model);
        string? imageUrl = null;
        if (model.CoverImage is { Length: > 0 })
        {
            var upload = await SaveCommunityImage(model.CoverImage);
            if (!upload.Success)
            {
                ModelState.AddModelError(nameof(model.CoverImage), upload.Error!);
                return View(model);
            }
            imageUrl = upload.Url;
        }
        var response = await _clients.CreateClient().PostAsJsonAsync("api/community/posts", new
        {
            model.Title, model.Content, model.Category, CoverImageUrl = imageUrl,
            model.LocationName, model.ReservationID, model.Submit, model.IsOfficial
        });
        if (!response.IsSuccessStatusCode)
        {
            var message = await ReadMessage(response);
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }
        var result = await response.Content.ReadFromJsonAsync<CreatePostResponse>(JsonOptions);
        TempData["CommunitySuccess"] = result?.Message ?? (model.Submit ? "Đã gửi bài viết." : "Đã lưu bản nháp.");
        return result?.CommunityPostID > 0 && result.Status == "Đã xuất bản"
            ? RedirectToAction(nameof(Details), new { id = result.CommunityPostID })
            : RedirectToAction(nameof(Mine));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var response = await _clients.CreateClient().GetAsync($"api/community/posts/{id}/mine");
        if (!response.IsSuccessStatusCode) return RedirectToAction(nameof(Mine));
        var source = await response.Content.ReadFromJsonAsync<CommunityPostDetailViewModel>(JsonOptions);
        if (source is null) return RedirectToAction(nameof(Mine));
        var model = new CommunityPostEditViewModel
        {
            CommunityPostID = source.CommunityPostID,
            Title = source.Title, Content = source.Content, Category = source.Category,
            LocationName = source.LocationName, ReservationID = source.ReservationID,
            IsOfficial = source.IsOfficial, ExistingCoverImageUrl = source.CoverImageUrl,
            Status = source.Status, ModerationReason = source.ModerationReason
        };
        await PopulateFormOptions(model);
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Edit(int id, CommunityPostEditViewModel model)
    {
        if (id != model.CommunityPostID) return BadRequest();
        await PopulateFormOptions(model);
        if (!ModelState.IsValid) return View(model);
        var imageUrl = model.ExistingCoverImageUrl;
        if (model.CoverImage is { Length: > 0 })
        {
            var upload = await SaveCommunityImage(model.CoverImage);
            if (!upload.Success)
            {
                ModelState.AddModelError(nameof(model.CoverImage), upload.Error!);
                return View(model);
            }
            imageUrl = upload.Url;
        }
        var response = await _clients.CreateClient().PutAsJsonAsync($"api/community/posts/{id}", new
        {
            model.Title, model.Content, model.Category, CoverImageUrl = imageUrl,
            model.LocationName, model.ReservationID, model.Submit, model.IsOfficial
        });
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, await ReadMessage(response));
            return View(model);
        }
        TempData["CommunitySuccess"] = model.Submit ? "Đã cập nhật và gửi bài viết." : "Đã cập nhật bản nháp.";
        return RedirectToAction(nameof(Mine));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await _clients.CreateClient().DeleteAsync($"api/community/posts/{id}");
        TempData[response.IsSuccessStatusCode ? "CommunitySuccess" : "CommunityError"] =
            response.IsSuccessStatusCode ? "Bài viết đã được ẩn khỏi cộng đồng." : await ReadMessage(response);
        return RedirectToAction(nameof(Mine));
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet]
    public async Task<IActionResult> Review(int id)
    {
        var response = await _clients.CreateClient().GetAsync($"api/community/posts/{id}/review");
        if (!response.IsSuccessStatusCode) return RedirectToAction(nameof(Moderation));
        var model = await response.Content.ReadFromJsonAsync<CommunityPostDetailViewModel>(JsonOptions);
        return model is null ? RedirectToAction(nameof(Moderation)) : View(model);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Mine()
    {
        var response = await _clients.CreateClient().GetAsync("api/community/mine");
        var model = response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<CommunityMineViewModel>(JsonOptions)
            : new CommunityMineViewModel();
        return View(model ?? new CommunityMineViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Comment(int id, string content, int? parentCommentID)
    {
        var response = await _clients.CreateClient().PostAsJsonAsync($"api/community/posts/{id}/comments", new { content, parentCommentID });
        TempData[response.IsSuccessStatusCode ? "CommunitySuccess" : "CommunityError"] =
            response.IsSuccessStatusCode ? "Đã gửi bình luận." : await ReadMessage(response);
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Useful(int id)
    {
        await _clients.CreateClient().PostAsJsonAsync($"api/community/posts/{id}/useful", new { });
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Bookmark(int id)
    {
        await _clients.CreateClient().PostAsJsonAsync($"api/community/posts/{id}/bookmark", new { });
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Report(int id, int? commentID, string reason, string? detail)
    {
        var response = await _clients.CreateClient().PostAsJsonAsync("api/community/reports", new
        {
            CommunityPostID = commentID.HasValue ? (int?)null : id,
            CommunityCommentID = commentID, reason, detail
        });
        TempData[response.IsSuccessStatusCode ? "CommunitySuccess" : "CommunityError"] =
            response.IsSuccessStatusCode ? "Báo cáo đã được tiếp nhận và danh tính của bạn được bảo mật." : await ReadMessage(response);
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet]
    public async Task<IActionResult> Moderation()
    {
        var response = await _clients.CreateClient().GetAsync("api/community/moderation");
        var model = response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<CommunityModerationViewModel>>(JsonOptions) : [];
        return View(model ?? []);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Moderate(int id, string action, string? reason)
    {
        var response = await _clients.CreateClient().PostAsJsonAsync($"api/community/moderation/{id}", new { action, reason });
        TempData[response.IsSuccessStatusCode ? "CommunitySuccess" : "CommunityError"] =
            response.IsSuccessStatusCode ? $"Đã thực hiện: {action}." : await ReadMessage(response);
        return RedirectToAction(nameof(Moderation));
    }

    private async Task PopulateFormOptions(CommunityPostFormViewModel model)
    {
        model.Categories = Categories.ToList();
        try
        {
            var response = await _clients.CreateClient().GetAsync("api/community/completed-trips");
            if (response.IsSuccessStatusCode)
                model.Trips = await response.Content.ReadFromJsonAsync<List<CommunityTripViewModel>>(JsonOptions) ?? [];
        }
        catch (HttpRequestException) { }
    }

    private async Task<(bool Success, string? Url, string? Error)> SaveCommunityImage(IFormFile file)
    {
        if (file.Length > 5 * 1024 * 1024) return (false, null, "Ảnh không được vượt quá 5 MB.");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp")) return (false, null, "Chỉ chấp nhận ảnh JPG, PNG hoặc WEBP.");
        await using var source = file.OpenReadStream();
        var header = new byte[12];
        var read = await source.ReadAsync(header);
        source.Position = 0;
        var valid = read >= 4 && (
            header[0] == 0xFF && header[1] == 0xD8 ||
            header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 ||
            read >= 12 && header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F' && header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P');
        if (!valid) return (false, null, "Nội dung tệp không phải định dạng ảnh hợp lệ.");
        var folder = Path.Combine("uploads", "community", DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"));
        var physicalFolder = Path.Combine(_environment.WebRootPath, folder);
        Directory.CreateDirectory(physicalFolder);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        await using var target = System.IO.File.Create(Path.Combine(physicalFolder, fileName));
        await source.CopyToAsync(target);
        return (true, "/" + folder.Replace('\\', '/') + "/" + fileName, null);
    }

    private static async Task<string> ReadMessage(HttpResponseMessage response)
    {
        try
        {
            var payload = await response.Content.ReadFromJsonAsync<MessageResponse>(JsonOptions);
            return payload?.Message ?? "Không thể xử lý yêu cầu.";
        }
        catch { return "Không thể xử lý yêu cầu."; }
    }
    private sealed class CreatePostResponse { public int CommunityPostID { get; set; } public string Status { get; set; } = ""; public string Message { get; set; } = ""; }
    private sealed class MessageResponse { public string Message { get; set; } = ""; }
}
