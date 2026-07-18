using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartCar.Domain.Entities;

public static class CommunityPostStatuses
{
    public const string Draft = "Bản nháp";
    public const string Pending = "Chờ duyệt";
    public const string Published = "Đã xuất bản";
    public const string Hidden = "Đã ẩn";
    public const string Rejected = "Bị từ chối";
}

public sealed class CommunityPost
{
    public int CommunityPostID { get; set; }
    public int AuthorAppUserID { get; set; }
    public AppUser AuthorAppUser { get; set; } = null!;
    [MaxLength(150)] public string Title { get; set; } = string.Empty;
    [MaxLength(50)] public string Category { get; set; } = "Chia sẻ hành trình";
    [MaxLength(12000)] public string Content { get; set; } = string.Empty;
    [MaxLength(500)] public string? CoverImageUrl { get; set; }
    [MaxLength(200)] public string? LocationName { get; set; }
    public int? ReservationID { get; set; }
    public Reservation? Reservation { get; set; }
    [MaxLength(30)] public string Status { get; set; } = CommunityPostStatuses.Draft;
    public bool IsOfficial { get; set; }
    public bool IsCommentsLocked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int? ModeratedByAppUserID { get; set; }
    [MaxLength(1000)] public string? ModerationReason { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public List<CommunityComment> Comments { get; set; } = [];
    public List<CommunityReaction> Reactions { get; set; } = [];
    public List<CommunityBookmark> Bookmarks { get; set; } = [];
    public List<CommunityReport> Reports { get; set; } = [];
}

public sealed class CommunityComment
{
    public int CommunityCommentID { get; set; }
    public int CommunityPostID { get; set; }
    public CommunityPost CommunityPost { get; set; } = null!;
    public int AuthorAppUserID { get; set; }
    public AppUser AuthorAppUser { get; set; } = null!;
    public int? ParentCommentID { get; set; }
    public CommunityComment? ParentComment { get; set; }
    [MaxLength(2000)] public string Content { get; set; } = string.Empty;
    [MaxLength(30)] public string Status { get; set; } = CommunityPostStatuses.Published;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public sealed class CommunityReaction
{
    public int CommunityReactionID { get; set; }
    public int CommunityPostID { get; set; }
    public CommunityPost CommunityPost { get; set; } = null!;
    public int AppUserID { get; set; }
    public AppUser AppUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class CommunityBookmark
{
    public int CommunityBookmarkID { get; set; }
    public int CommunityPostID { get; set; }
    public CommunityPost CommunityPost { get; set; } = null!;
    public int AppUserID { get; set; }
    public AppUser AppUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class CommunityReport
{
    public int CommunityReportID { get; set; }
    public int? CommunityPostID { get; set; }
    public CommunityPost? CommunityPost { get; set; }
    public int? CommunityCommentID { get; set; }
    public CommunityComment? CommunityComment { get; set; }
    public int ReporterAppUserID { get; set; }
    public AppUser ReporterAppUser { get; set; } = null!;
    [MaxLength(100)] public string Reason { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Detail { get; set; }
    [MaxLength(30)] public string Status { get; set; } = "Mới";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public int? ResolvedByAppUserID { get; set; }
}

public sealed class CommunityModerationLog
{
    public long CommunityModerationLogID { get; set; }
    public int CommunityPostID { get; set; }
    public CommunityPost CommunityPost { get; set; } = null!;
    public int ModeratorAppUserID { get; set; }
    public AppUser ModeratorAppUser { get; set; } = null!;
    [MaxLength(50)] public string Action { get; set; } = string.Empty;
    [MaxLength(1000)] public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
