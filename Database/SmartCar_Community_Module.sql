/*
 SmartCar Community module - idempotent SQL Server patch
 Run after SmartCar_FULL_ONE_CLICK_RESET_INSTALL_v31_0_16.sql.
*/
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.CommunityPosts', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.CommunityPosts(
  CommunityPostID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CommunityPosts PRIMARY KEY,
  AuthorAppUserID int NOT NULL,
  Title nvarchar(150) NOT NULL,
  Category nvarchar(50) NOT NULL CONSTRAINT DF_CommunityPosts_Category DEFAULT N'Chia sẻ hành trình',
  Content nvarchar(max) NOT NULL,
  CoverImageUrl nvarchar(500) NULL,
  LocationName nvarchar(200) NULL,
  ReservationID int NULL,
  Status nvarchar(30) NOT NULL CONSTRAINT DF_CommunityPosts_Status DEFAULT N'Bản nháp',
  IsOfficial bit NOT NULL CONSTRAINT DF_CommunityPosts_Official DEFAULT 0,
  IsCommentsLocked bit NOT NULL CONSTRAINT DF_CommunityPosts_Locked DEFAULT 0,
  CreatedAt datetime2 NOT NULL CONSTRAINT DF_CommunityPosts_Created DEFAULT SYSUTCDATETIME(),
  UpdatedAt datetime2 NULL,
  PublishedAt datetime2 NULL,
  ModeratedByAppUserID int NULL,
  ModerationReason nvarchar(1000) NULL,
  RowVersion rowversion NOT NULL,
  CONSTRAINT FK_CommunityPosts_Author FOREIGN KEY(AuthorAppUserID) REFERENCES dbo.AppUsers(AppUserId),
  CONSTRAINT FK_CommunityPosts_Reservation FOREIGN KEY(ReservationID) REFERENCES dbo.Reservations(ReservationID),
  CONSTRAINT FK_CommunityPosts_Moderator FOREIGN KEY(ModeratedByAppUserID) REFERENCES dbo.AppUsers(AppUserId),
  CONSTRAINT CK_CommunityPosts_Status CHECK(Status IN (N'Bản nháp',N'Chờ duyệt',N'Đã xuất bản',N'Đã ẩn',N'Bị từ chối'))
 );
 CREATE INDEX IX_CommunityPosts_Status_PublishedAt ON dbo.CommunityPosts(Status,PublishedAt DESC);
 CREATE INDEX IX_CommunityPosts_Author_CreatedAt ON dbo.CommunityPosts(AuthorAppUserID,CreatedAt DESC);
 CREATE INDEX IX_CommunityPosts_Category ON dbo.CommunityPosts(Category);
END;

IF OBJECT_ID(N'dbo.CommunityComments', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.CommunityComments(
  CommunityCommentID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CommunityComments PRIMARY KEY,
  CommunityPostID int NOT NULL,
  AuthorAppUserID int NOT NULL,
  ParentCommentID int NULL,
  Content nvarchar(2000) NOT NULL,
  Status nvarchar(30) NOT NULL CONSTRAINT DF_CommunityComments_Status DEFAULT N'Đã xuất bản',
  CreatedAt datetime2 NOT NULL CONSTRAINT DF_CommunityComments_Created DEFAULT SYSUTCDATETIME(),
  UpdatedAt datetime2 NULL,
  CONSTRAINT FK_CommunityComments_Post FOREIGN KEY(CommunityPostID) REFERENCES dbo.CommunityPosts(CommunityPostID) ON DELETE CASCADE,
  CONSTRAINT FK_CommunityComments_Author FOREIGN KEY(AuthorAppUserID) REFERENCES dbo.AppUsers(AppUserId),
  CONSTRAINT FK_CommunityComments_Parent FOREIGN KEY(ParentCommentID) REFERENCES dbo.CommunityComments(CommunityCommentID)
 );
 CREATE INDEX IX_CommunityComments_Post_Created ON dbo.CommunityComments(CommunityPostID,CreatedAt);
 CREATE INDEX IX_CommunityComments_Parent ON dbo.CommunityComments(ParentCommentID);
END;

IF OBJECT_ID(N'dbo.CommunityReactions', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.CommunityReactions(
  CommunityReactionID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CommunityReactions PRIMARY KEY,
  CommunityPostID int NOT NULL, AppUserID int NOT NULL,
  CreatedAt datetime2 NOT NULL CONSTRAINT DF_CommunityReactions_Created DEFAULT SYSUTCDATETIME(),
  CONSTRAINT FK_CommunityReactions_Post FOREIGN KEY(CommunityPostID) REFERENCES dbo.CommunityPosts(CommunityPostID) ON DELETE CASCADE,
  CONSTRAINT FK_CommunityReactions_User FOREIGN KEY(AppUserID) REFERENCES dbo.AppUsers(AppUserId),
  CONSTRAINT UQ_CommunityReactions_Post_User UNIQUE(CommunityPostID,AppUserID)
 );
END;

IF OBJECT_ID(N'dbo.CommunityBookmarks', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.CommunityBookmarks(
  CommunityBookmarkID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CommunityBookmarks PRIMARY KEY,
  CommunityPostID int NOT NULL, AppUserID int NOT NULL,
  CreatedAt datetime2 NOT NULL CONSTRAINT DF_CommunityBookmarks_Created DEFAULT SYSUTCDATETIME(),
  CONSTRAINT FK_CommunityBookmarks_Post FOREIGN KEY(CommunityPostID) REFERENCES dbo.CommunityPosts(CommunityPostID) ON DELETE CASCADE,
  CONSTRAINT FK_CommunityBookmarks_User FOREIGN KEY(AppUserID) REFERENCES dbo.AppUsers(AppUserId),
  CONSTRAINT UQ_CommunityBookmarks_Post_User UNIQUE(CommunityPostID,AppUserID)
 );
END;

IF OBJECT_ID(N'dbo.CommunityReports', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.CommunityReports(
  CommunityReportID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CommunityReports PRIMARY KEY,
  CommunityPostID int NULL, CommunityCommentID int NULL, ReporterAppUserID int NOT NULL,
  Reason nvarchar(100) NOT NULL, Detail nvarchar(1000) NULL,
  Status nvarchar(30) NOT NULL CONSTRAINT DF_CommunityReports_Status DEFAULT N'Mới',
  CreatedAt datetime2 NOT NULL CONSTRAINT DF_CommunityReports_Created DEFAULT SYSUTCDATETIME(),
  ResolvedAt datetime2 NULL, ResolvedByAppUserID int NULL,
  CONSTRAINT CK_CommunityReports_Target CHECK((CommunityPostID IS NOT NULL AND CommunityCommentID IS NULL) OR (CommunityPostID IS NULL AND CommunityCommentID IS NOT NULL)),
  CONSTRAINT FK_CommunityReports_Post FOREIGN KEY(CommunityPostID) REFERENCES dbo.CommunityPosts(CommunityPostID),
  CONSTRAINT FK_CommunityReports_Comment FOREIGN KEY(CommunityCommentID) REFERENCES dbo.CommunityComments(CommunityCommentID),
  CONSTRAINT FK_CommunityReports_Reporter FOREIGN KEY(ReporterAppUserID) REFERENCES dbo.AppUsers(AppUserId),
  CONSTRAINT FK_CommunityReports_Resolver FOREIGN KEY(ResolvedByAppUserID) REFERENCES dbo.AppUsers(AppUserId)
 );
 CREATE INDEX IX_CommunityReports_Status_Created ON dbo.CommunityReports(Status,CreatedAt);
END;

IF OBJECT_ID(N'dbo.CommunityModerationLogs', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.CommunityModerationLogs(
  CommunityModerationLogID bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_CommunityModerationLogs PRIMARY KEY,
  CommunityPostID int NOT NULL, ModeratorAppUserID int NOT NULL,
  Action nvarchar(50) NOT NULL, Reason nvarchar(1000) NOT NULL,
  CreatedAt datetime2 NOT NULL CONSTRAINT DF_CommunityModerationLogs_Created DEFAULT SYSUTCDATETIME(),
  CONSTRAINT FK_CommunityModerationLogs_Post FOREIGN KEY(CommunityPostID) REFERENCES dbo.CommunityPosts(CommunityPostID) ON DELETE CASCADE,
  CONSTRAINT FK_CommunityModerationLogs_User FOREIGN KEY(ModeratorAppUserID) REFERENCES dbo.AppUsers(AppUserId)
 );
 CREATE INDEX IX_CommunityModerationLogs_Post_Created ON dbo.CommunityModerationLogs(CommunityPostID,CreatedAt DESC);
END;

COMMIT TRANSACTION;
