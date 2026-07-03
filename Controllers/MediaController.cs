using ADS2026.Data;
using ADS2026.Hubs;
using ADS2026.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MediaServer.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MediaController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<MediaHub> _hub;

    private static readonly string[] AllowedExtensions =
        [".jpg", ".jpeg", ".png", ".gif", ".webp", ".mp4", ".webm", ".mov", ".ogg"];

    public MediaController(AppDbContext context, IHubContext<MediaHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    //Upload
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] string? title, [FromForm] string? description)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!AllowedExtensions.Contains(ext))
            return BadRequest(new { error = $"Unsupported file type: {ext}. Allowed: jpg, png, gif, webp, mp4, webm, mov." });

        // Save to wwwroot/media/
        var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "media");
        Directory.CreateDirectory(folder);

        var savedName = Guid.NewGuid() + ext;           // unique file on disk
        var filePath = Path.Combine(folder, savedName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var fileType = file.ContentType.StartsWith("video") ? "video" : "image";

        var media = new MediaFile
        {
            FileName = file.FileName,           // original name for display
            Title = title?.Trim(),
            Description = description?.Trim(),
            FileUrl = "/media/" + savedName,   // URL served via static files
            FileType = fileType,
            CreatedAt = DateTime.UtcNow
        };

        _context.MediaFiles.Add(media);
        await _context.SaveChangesAsync();

        await _hub.Clients.All.SendAsync("ReceiveMedia", media);

        return Ok(media);
    }

    //Get All
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _context.MediaFiles
            .OrderByDescending(x => x.Id)
            .ToListAsync();

        return Ok(list);
    }

    //Get By Id + broadcast 
    //[HttpGet("{id}")]
    //public async Task<IActionResult> GetById(int id)
    //{
    //    var media = await _context.MediaFiles.FindAsync(id);
    //    if (media == null)
    //        return NotFound(new { error = "Media not found." });

    //    // Broadcast to all connected clients (manually push to TV)
    //    await _hub.Clients.All.SendAsync("ReceiveMedia", media);

    //    return Ok(media);
    //}
    [HttpPost("{id}/push")]
    public async Task<IActionResult> Push(int id, [FromBody] PushRequest req)
    {
        var media = await _context.MediaFiles.FindAsync(id);
        if (media == null)
            return NotFound(new { error = "Media not found." });

        if (req.All || req.Screens == null || req.Screens.Count == 0)
        {
            await _hub.Clients.All.SendAsync("ReceiveMedia", media);
            return Ok(new { success = true, target = "all" });
        }

        foreach (var screenName in req.Screens.Distinct())
            await _hub.Clients.Group(MediaHub.GroupName(screenName)).SendAsync("ReceiveMedia", media);

        return Ok(new { success = true, target = req.Screens });
    }

    public class PushRequest
    {
        public List<string>? Screens { get; set; }
        public bool All { get; set; } = true;
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMediaRequest req)
    {
        var media = await _context.MediaFiles.FindAsync(id);
        if (media == null)
            return NotFound(new { error = "Media not found." });

        if (req.Title != null) media.Title = req.Title.Trim();
        if (req.Description != null) media.Description = req.Description.Trim();

        await _context.SaveChangesAsync();
        return Ok(media);
    }

    public class UpdateMediaRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
    }

    // Delete
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var media = await _context.MediaFiles.FindAsync(id);
        if (media == null)
            return NotFound(new { error = "Media not found." });

        // Delete physical file from wwwroot/media/
        var filePath = Path.Combine(
            Directory.GetCurrentDirectory(), "wwwroot",
            media.FileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);

        _context.MediaFiles.Remove(media);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"'{media.FileName}' deleted successfully." });
    }
  
}