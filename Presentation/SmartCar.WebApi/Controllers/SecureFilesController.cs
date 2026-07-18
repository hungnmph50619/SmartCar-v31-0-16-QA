using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SmartCar.Dto.ReservationDtos;
using SmartCar.Persistence.Context;
using SmartCar.WebApi.Services;
using System.Security.Claims;

namespace SmartCar.WebApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/secure-files")]
    public class SecureFilesController : ControllerBase
    {
        private readonly CarBookContext _context;
        private readonly IPrivateFileService _files;
        public SecureFilesController(CarBookContext context, IPrivateFileService files) { _context = context; _files = files; }

        [HttpPost("upload")]
        [EnableRateLimiting("upload")]
        [RequestSizeLimit(35 * 1024 * 1024)]
        public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string category, [FromForm] int? reservationId, [FromForm] int? partnerApplicationId, CancellationToken cancellationToken)
        {
            var userId = CurrentUserId();
            try
            {
                var saved = await _files.SaveAsync(file, userId, (category ?? string.Empty).Trim(), reservationId, partnerApplicationId, cancellationToken);
                return Ok(new SecureFileUploadResultDto
                {
                    PrivateFileId = saved.PrivateFileID,
                    ViewUrl = _files.BuildViewUrl(saved.PrivateFileID),
                    Category = saved.Category
                });
            }
            catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        }


        [HttpDelete("{fileId:guid}")]
        public async Task<IActionResult> DeleteUnattached(Guid fileId, CancellationToken cancellationToken)
        {
            try
            {
                var privileged = User.IsInRole("Admin") || User.IsInRole("Staff");
                var deleted = await _files.DeleteUnattachedAsync(fileId, CurrentUserId(), privileged, cancellationToken);
                return deleted ? NoContent() : NotFound();
            }
            catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
            catch (IOException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    message = ex.Message,
                    retryable = true
                });
            }
        }

        [HttpGet("{fileId:guid}")]
        public async Task<IActionResult> Get(Guid fileId, CancellationToken cancellationToken)
        {
            var item = await _context.PrivateFiles.AsNoTracking().FirstOrDefaultAsync(x => x.PrivateFileID == fileId && !x.IsDeleted, cancellationToken);
            if (item is null) return NotFound();
            var privileged = User.IsInRole("Admin") || User.IsInRole("Staff");
            if (!await _files.CanReadAsync(item, CurrentUserId(), privileged, cancellationToken)) return Forbid();
            var path = _files.GetPhysicalPath(item);
            if (!System.IO.File.Exists(path)) return NotFound();
            Response.Headers.CacheControl = "no-store, no-cache";
            Response.Headers.Pragma = "no-cache";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return PhysicalFile(path, item.ContentType, enableRangeProcessing: true);
        }

        private int CurrentUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    }
}
