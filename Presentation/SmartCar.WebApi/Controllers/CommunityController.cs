using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.Entities;
using SmartCar.Persistence.Context;

namespace SmartCar.WebApi.Controllers;

[Route("api/community")]
[ApiController]
public sealed class CommunityController : ControllerBase
{
    private static readonly string[] Categories =
    [
        "Chia sẻ hành trình", "Kinh nghiệm thuê xe", "Hỏi đáp",
        "Kinh nghiệm lái xe", "Chăm sóc xe", "Kinh nghiệm chủ xe"
    ];
    private readonly CarBookContext _db;
    public CommunityController(CarBookContext db) => _db = db;

    [AllowAnonymous]
    [HttpGet("posts")]
    public async Task<IActionResult> Posts(string? query, string? category, string sort = "newest", int page = 1)
    {
        page = Math.Clamp(page, 1, 1000);
        var posts = _db.CommunityPosts.AsNoTracking()
            .Where(x => x.Status == CommunityPostStatuses.Published);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            posts = posts.Where(x => x.Title.Contains(q) || x.Content.Contains(q) || (x.LocationName != null && x.LocationName.Contains(q)));
        }
        if (!string.IsNullOrWhiteSpace(category)) posts = posts.Where(x => x.Category == category);
        posts = sort == "useful"
            ? posts.OrderByDescending(x => x.Reactions.Count).ThenByDescending(x => x.PublishedAt)
            : sort == "discussed"
                ? posts.OrderByDescending(x => x.Comments.Count(c => c.Status == CommunityPostStatuses.Published)).ThenByDescending(x => x.PublishedAt)
                : posts.OrderByDescending(x => x.PublishedAt ?? x.CreatedAt);

        var result = await posts.Skip((page - 1) * 20).Take(20).Select(x => new
        {
            x.CommunityPostID,
            x.Title,
            excerpt = x.Content.Length > 240 ? x.Content.Substring(0, 240) + "…" : x.Content,
            x.Category,
            x.CoverImageUrl,
            x.LocationName,
            x.IsOfficial,
            x.CreatedAt,
            x.PublishedAt,
            authorName = (x.AuthorAppUser.Surname + " " + x.AuthorAppUser.Name).Trim(),
            x.AuthorAppUser.IsVehiclePartner,
            verifiedTrip = x.ReservationID.HasValue,
            usefulCount = x.Reactions.Count,
            commentCount = x.Comments.Count(c => c.Status == CommunityPostStatuses.Published)
        }).ToListAsync();
        return Ok(new { categories = Categories, items = result, page });
    }

    [AllowAnonymous]
    [HttpGet("posts/{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var post = await _db.CommunityPosts.AsNoTracking()
            .Where(x => x.CommunityPostID == id && x.Status == CommunityPostStatuses.Published)
            .Select(x => new
            {
                x.CommunityPostID, x.Title, x.Content, x.Category, x.CoverImageUrl, x.LocationName,
                x.IsOfficial, x.IsCommentsLocked, x.CreatedAt, x.UpdatedAt, x.PublishedAt, x.ReservationID,
                authorID = x.AuthorAppUserID,
                authorName = (x.AuthorAppUser.Surname + " " + x.AuthorAppUser.Name).Trim(),
                x.AuthorAppUser.IsVehiclePartner,
                usefulCount = x.Reactions.Count,
                bookmarkCount = x.Bookmarks.Count,
                comments = x.Comments.Where(c => c.Status == CommunityPostStatuses.Published)
                    .OrderBy(c => c.CreatedAt).Select(c => new
                    {
                        c.CommunityCommentID, c.ParentCommentID, c.Content, c.CreatedAt, c.UpdatedAt,
                        authorID = c.AuthorAppUserID,
                        authorName = (c.AuthorAppUser.Surname + " " + c.AuthorAppUser.Name).Trim(),
                        c.AuthorAppUser.IsVehiclePartner
                    }).ToList()
            }).FirstOrDefaultAsync();
        return post is null ? NotFound(new { message = "Bài viết không tồn tại hoặc chưa được xuất bản." }) : Ok(post);
    }

    [Authorize]
    [HttpGet("completed-trips")]
    public async Task<IActionResult> CompletedTrips()
    {
        var userId = UserId();
        var trips = await _db.Reservations.AsNoTracking()
            .Where(x => x.CustomerAppUserID == userId && x.Status == "Hoàn thành")
            .OrderByDescending(x => x.DropOffDate)
            .Take(30)
            .Select(x => new { x.ReservationID, x.PickUpDate, x.DropOffDate, carName = x.Car.Model })
            .ToListAsync();
        return Ok(trips);
    }

    [Authorize]
    [HttpGet("mine")]
    public async Task<IActionResult> Mine()
    {
        var userId = UserId();
        var items = await _db.CommunityPosts.AsNoTracking().Where(x => x.AuthorAppUserID == userId)
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Select(x => new { x.CommunityPostID, x.Title, x.Category, x.Status, x.CreatedAt, x.UpdatedAt, x.PublishedAt, x.ModerationReason, usefulCount = x.Reactions.Count, commentCount = x.Comments.Count })
            .ToListAsync();
        var bookmarks = await _db.CommunityBookmarks.AsNoTracking().Where(x => x.AppUserID == userId)
            .OrderByDescending(x => x.CreatedAt).Select(x => new { x.CommunityPostID, x.CommunityPost.Title, x.CommunityPost.Category, x.CreatedAt }).ToListAsync();
        return Ok(new { items, bookmarks });
    }

    [Authorize]
    [HttpPost("posts")]
    public async Task<IActionResult> Create(SavePostRequest request)
    {
        var userId = UserId();
        var error = ValidatePost(request);
        if (error != null) return BadRequest(new { message = error });
        if (!await CanPost(userId)) return Forbid();
        var reservationId = await ValidReservationLink(userId, request.ReservationID);
        var isAdmin = User.IsInRole("Admin");
        var trusted = isAdmin || await IsTrustedAuthor(userId);
        var status = request.Submit ? (trusted ? CommunityPostStatuses.Published : CommunityPostStatuses.Pending) : CommunityPostStatuses.Draft;
        var post = new CommunityPost
        {
            AuthorAppUserID = userId, Title = request.Title.Trim(), Content = request.Content.Trim(),
            Category = request.Category.Trim(), CoverImageUrl = CleanImageUrl(request.CoverImageUrl),
            LocationName = string.IsNullOrWhiteSpace(request.LocationName) ? null : request.LocationName.Trim(),
            ReservationID = reservationId, Status = status, IsOfficial = isAdmin && request.IsOfficial,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            PublishedAt = status == CommunityPostStatuses.Published ? DateTime.UtcNow : null
        };
        _db.CommunityPosts.Add(post);
        await _db.SaveChangesAsync();
        return Ok(new { post.CommunityPostID, post.Status, message = status == CommunityPostStatuses.Published ? "Bài viết đã được đăng." : status == CommunityPostStatuses.Pending ? "Bài viết đã được gửi kiểm duyệt." : "Đã lưu bản nháp." });
    }

    [Authorize]
    [HttpPut("posts/{id:int}")]
    public async Task<IActionResult> Update(int id, SavePostRequest request)
    {
        var userId = UserId();
        var post = await _db.CommunityPosts.FirstOrDefaultAsync(x => x.CommunityPostID == id);
        if (post is null) return NotFound();
        if (post.AuthorAppUserID != userId && !User.IsInRole("Admin")) return Forbid();
        if (post.Status == CommunityPostStatuses.Hidden && !User.IsInRole("Admin")) return Conflict(new { message = "Bài đang bị ẩn để kiểm duyệt và chưa thể sửa." });
        var error = ValidatePost(request);
        if (error != null) return BadRequest(new { message = error });
        post.Title = request.Title.Trim(); post.Content = request.Content.Trim(); post.Category = request.Category.Trim();
        post.CoverImageUrl = CleanImageUrl(request.CoverImageUrl);
        post.LocationName = string.IsNullOrWhiteSpace(request.LocationName) ? null : request.LocationName.Trim();
        post.ReservationID = await ValidReservationLink(userId, request.ReservationID);
        post.UpdatedAt = DateTime.UtcNow;
        if (request.Submit)
        {
            var trusted = User.IsInRole("Admin") || await IsTrustedAuthor(userId);
            post.Status = trusted ? CommunityPostStatuses.Published : CommunityPostStatuses.Pending;
            post.PublishedAt ??= trusted ? DateTime.UtcNow : null;
            post.ModerationReason = null;
        }
        else post.Status = CommunityPostStatuses.Draft;
        await _db.SaveChangesAsync();
        return Ok(new { post.CommunityPostID, post.Status });
    }

    [Authorize]
    [HttpDelete("posts/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = UserId();
        var post = await _db.CommunityPosts.FirstOrDefaultAsync(x => x.CommunityPostID == id);
        if (post is null) return NotFound();
        if (post.AuthorAppUserID != userId && !User.IsInRole("Admin")) return Forbid();
        post.Status = CommunityPostStatuses.Hidden;
        post.ModerationReason = "Tác giả đã ẩn bài viết.";
        post.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpPost("posts/{id:int}/comments")]
    public async Task<IActionResult> Comment(int id, CommentRequest request)
    {
        var userId = UserId();
        var post = await _db.CommunityPosts.AsNoTracking().FirstOrDefaultAsync(x => x.CommunityPostID == id && x.Status == CommunityPostStatuses.Published);
        if (post is null) return NotFound();
        if (post.IsCommentsLocked) return Conflict(new { message = "Bài viết đã khóa bình luận." });
        var content = (request.Content ?? "").Trim();
        if (content.Length is < 2 or > 2000) return BadRequest(new { message = "Bình luận phải có từ 2 đến 2.000 ký tự." });
        if (ContainsSensitiveData(content)) return BadRequest(new { message = "Bình luận có thể chứa số điện thoại, CCCD hoặc tài khoản ngân hàng. Vui lòng che thông tin trước khi gửi." });
        if (request.ParentCommentID.HasValue && !await _db.CommunityComments.AnyAsync(x => x.CommunityCommentID == request.ParentCommentID && x.CommunityPostID == id && x.ParentCommentID == null))
            return BadRequest(new { message = "Chỉ hỗ trợ trả lời một cấp." });
        var comment = new CommunityComment { CommunityPostID = id, AuthorAppUserID = userId, ParentCommentID = request.ParentCommentID, Content = content };
        _db.CommunityComments.Add(comment);
        await _db.SaveChangesAsync();
        return Ok(new { comment.CommunityCommentID });
    }

    [Authorize]
    [HttpPost("posts/{id:int}/useful")]
    public async Task<IActionResult> ToggleUseful(int id)
    {
        var userId = UserId();
        if (!await _db.CommunityPosts.AnyAsync(x => x.CommunityPostID == id && x.Status == CommunityPostStatuses.Published)) return NotFound();
        var existing = await _db.CommunityReactions.FirstOrDefaultAsync(x => x.CommunityPostID == id && x.AppUserID == userId);
        if (existing is null) _db.CommunityReactions.Add(new CommunityReaction { CommunityPostID = id, AppUserID = userId });
        else _db.CommunityReactions.Remove(existing);
        await _db.SaveChangesAsync();
        return Ok(new { active = existing is null, count = await _db.CommunityReactions.CountAsync(x => x.CommunityPostID == id) });
    }

    [Authorize]
    [HttpPost("posts/{id:int}/bookmark")]
    public async Task<IActionResult> ToggleBookmark(int id)
    {
        var userId = UserId();
        if (!await _db.CommunityPosts.AnyAsync(x => x.CommunityPostID == id && x.Status == CommunityPostStatuses.Published)) return NotFound();
        var existing = await _db.CommunityBookmarks.FirstOrDefaultAsync(x => x.CommunityPostID == id && x.AppUserID == userId);
        if (existing is null) _db.CommunityBookmarks.Add(new CommunityBookmark { CommunityPostID = id, AppUserID = userId });
        else _db.CommunityBookmarks.Remove(existing);
        await _db.SaveChangesAsync();
        return Ok(new { active = existing is null });
    }

    [Authorize]
    [HttpPost("reports")]
    public async Task<IActionResult> Report(ReportRequest request)
    {
        var userId = UserId();
        if (!request.CommunityPostID.HasValue && !request.CommunityCommentID.HasValue) return BadRequest(new { message = "Phải chọn bài viết hoặc bình luận cần báo cáo." });
        var duplicate = await _db.CommunityReports.AnyAsync(x => x.ReporterAppUserID == userId && x.Status == "Mới" && x.CommunityPostID == request.CommunityPostID && x.CommunityCommentID == request.CommunityCommentID);
        if (duplicate) return Conflict(new { message = "Bạn đã báo cáo nội dung này." });
        _db.CommunityReports.Add(new CommunityReport { ReporterAppUserID = userId, CommunityPostID = request.CommunityPostID, CommunityCommentID = request.CommunityCommentID, Reason = (request.Reason ?? "").Trim(), Detail = request.Detail?.Trim() });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Báo cáo đã được tiếp nhận. SmartCar không tiết lộ danh tính người báo cáo." });
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet("moderation")]
    public async Task<IActionResult> Moderation()
    {
        var posts = await _db.CommunityPosts.AsNoTracking()
            .Where(x => x.Status == CommunityPostStatuses.Pending || x.Status == CommunityPostStatuses.Hidden || x.Reports.Any(r => r.Status == "Mới"))
            .OrderByDescending(x => x.Reports.Count(r => r.Status == "Mới")).ThenBy(x => x.CreatedAt)
            .Select(x => new { x.CommunityPostID, x.Title, x.Category, x.Status, x.CreatedAt, authorName = (x.AuthorAppUser.Surname + " " + x.AuthorAppUser.Name).Trim(), reportCount = x.Reports.Count(r => r.Status == "Mới"), x.ModerationReason })
            .ToListAsync();
        return Ok(posts);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost("moderation/{id:int}")]
    public async Task<IActionResult> Moderate(int id, ModerateRequest request)
    {
        var action = (request.Action ?? "").Trim();
        if (action is not ("Xuất bản" or "Ẩn" or "Từ chối" or "Khóa bình luận" or "Mở bình luận")) return BadRequest(new { message = "Thao tác kiểm duyệt không hợp lệ." });
        var post = await _db.CommunityPosts.FirstOrDefaultAsync(x => x.CommunityPostID == id);
        if (post is null) return NotFound();
        if (action == "Xuất bản") { post.Status = CommunityPostStatuses.Published; post.PublishedAt ??= DateTime.UtcNow; }
        else if (action == "Ẩn") post.Status = CommunityPostStatuses.Hidden;
        else if (action == "Từ chối") post.Status = CommunityPostStatuses.Rejected;
        else if (action == "Khóa bình luận") post.IsCommentsLocked = true;
        else if (action == "Mở bình luận") post.IsCommentsLocked = false;
        post.ModeratedByAppUserID = UserId(); post.ModerationReason = request.Reason?.Trim(); post.UpdatedAt = DateTime.UtcNow;
        _db.CommunityModerationLogs.Add(new CommunityModerationLog { CommunityPostID = id, ModeratorAppUserID = UserId(), Action = action, Reason = request.Reason?.Trim() ?? "Không có ghi chú." });
        var reports = await _db.CommunityReports.Where(x => x.CommunityPostID == id && x.Status == "Mới").ToListAsync();
        foreach (var report in reports) { report.Status = "Đã xử lý"; report.ResolvedAt = DateTime.UtcNow; report.ResolvedByAppUserID = UserId(); }
        await _db.SaveChangesAsync();
        return Ok(new { post.Status, post.IsCommentsLocked });
    }

    private int UserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private async Task<bool> CanPost(int userId) => await _db.AppUsers.AnyAsync(x => x.AppUserId == userId && x.IsActive && !x.IsDeleted && x.EmailConfirmed);
    private async Task<bool> IsTrustedAuthor(int userId)
    {
        var published = await _db.CommunityPosts.CountAsync(x => x.AuthorAppUserID == userId && x.Status == CommunityPostStatuses.Published);
        var completedTrip = await _db.Reservations.AnyAsync(x => x.CustomerAppUserID == userId && x.Status == "Hoàn thành");
        var verifiedPartner = await _db.VehiclePartnerProfiles.AnyAsync(x => x.AppUserID == userId && x.Status == "Đã xác minh");
        return published >= 2 || completedTrip || verifiedPartner;
    }
    private async Task<int?> ValidReservationLink(int userId, int? reservationId)
    {
        if (!reservationId.HasValue) return null;
        return await _db.Reservations.AnyAsync(x => x.ReservationID == reservationId && x.CustomerAppUserID == userId && x.Status == "Hoàn thành") ? reservationId : null;
    }
    private static string? ValidatePost(SavePostRequest request)
    {
        if ((request.Title ?? "").Trim().Length is < 10 or > 150) return "Tiêu đề phải có từ 10 đến 150 ký tự.";
        if ((request.Content ?? "").Trim().Length is < 50 or > 12000) return "Nội dung phải có từ 50 đến 12.000 ký tự.";
        if (!Categories.Contains((request.Category ?? "").Trim())) return "Chuyên mục không hợp lệ.";
        if (ContainsSensitiveData(request.Title + " " + request.Content)) return "Bài viết có thể chứa số điện thoại, CCCD hoặc tài khoản ngân hàng. Vui lòng che thông tin trước khi đăng.";
        return null;
    }
    private static bool ContainsSensitiveData(string value) => Regex.IsMatch(value ?? "", @"(?<!\d)(?:0\d{9}|\d{12}|\d{13,19})(?!\d)");
    private static string? CleanImageUrl(string? value)
    {
        var url = value?.Trim();
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (url.StartsWith("/uploads/community/", StringComparison.OrdinalIgnoreCase)) return url;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps ? url : null;
    }

    public sealed record SavePostRequest(string Title, string Content, string Category, string? CoverImageUrl, string? LocationName, int? ReservationID, bool Submit, bool IsOfficial = false);
    public sealed record CommentRequest(string Content, int? ParentCommentID);
    public sealed record ReportRequest(int? CommunityPostID, int? CommunityCommentID, string Reason, string? Detail);
    public sealed record ModerateRequest(string Action, string? Reason);
}
