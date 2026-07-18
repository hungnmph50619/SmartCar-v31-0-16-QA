using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SmartCar.WebUI.Models;

public sealed class CommunityFeedViewModel
{
    public List<string> Categories { get; set; } = [];
    public List<CommunityPostSummaryViewModel> Items { get; set; } = [];
    public int Page { get; set; }
}
public sealed class CommunityPostSummaryViewModel
{
    public int CommunityPostID { get; set; }
    public string Title { get; set; } = "";
    public string Excerpt { get; set; } = "";
    public string Category { get; set; } = "";
    public string? CoverImageUrl { get; set; }
    public string? LocationName { get; set; }
    public bool IsOfficial { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string AuthorName { get; set; } = "";
    public bool IsVehiclePartner { get; set; }
    public bool VerifiedTrip { get; set; }
    public int UsefulCount { get; set; }
    public int CommentCount { get; set; }
}
public sealed class CommunityPostDetailViewModel
{
    public int CommunityPostID { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string Category { get; set; } = "";
    public string? CoverImageUrl { get; set; }
    public string? LocationName { get; set; }
    public bool IsOfficial { get; set; }
    public bool IsCommentsLocked { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int? ReservationID { get; set; }
    public int AuthorID { get; set; }
    public string AuthorName { get; set; } = "";
    public bool IsVehiclePartner { get; set; }
    public int UsefulCount { get; set; }
    public int BookmarkCount { get; set; }
    public List<CommunityCommentViewModel> Comments { get; set; } = [];
}
public sealed class CommunityCommentViewModel
{
    public int CommunityCommentID { get; set; }
    public int? ParentCommentID { get; set; }
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int AuthorID { get; set; }
    public string AuthorName { get; set; } = "";
    public bool IsVehiclePartner { get; set; }
}
public sealed class CommunityPostFormViewModel
{
    [Required, StringLength(150, MinimumLength = 10)] public string Title { get; set; } = "";
    [Required, StringLength(12000, MinimumLength = 50)] public string Content { get; set; } = "";
    [Required] public string Category { get; set; } = "Chia sẻ hành trình";
    [StringLength(200)] public string? LocationName { get; set; }
    public int? ReservationID { get; set; }
    public bool Submit { get; set; }
    public bool IsOfficial { get; set; }
    public IFormFile? CoverImage { get; set; }
    public List<string> Categories { get; set; } = [];
    public List<CommunityTripViewModel> Trips { get; set; } = [];
}
public sealed class CommunityTripViewModel
{
    public int ReservationID { get; set; }
    public DateTime PickUpDate { get; set; }
    public DateTime DropOffDate { get; set; }
    public string CarName { get; set; } = "";
}
public sealed class CommunityMineViewModel
{
    public List<CommunityMinePostViewModel> Items { get; set; } = [];
    public List<CommunityBookmarkViewModel> Bookmarks { get; set; } = [];
}
public sealed class CommunityMinePostViewModel
{
    public int CommunityPostID { get; set; }
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? ModerationReason { get; set; }
    public int UsefulCount { get; set; }
    public int CommentCount { get; set; }
}
public sealed class CommunityBookmarkViewModel
{
    public int CommunityPostID { get; set; }
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
public sealed class CommunityModerationViewModel
{
    public int CommunityPostID { get; set; }
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string AuthorName { get; set; } = "";
    public int ReportCount { get; set; }
    public string? ModerationReason { get; set; }
}
