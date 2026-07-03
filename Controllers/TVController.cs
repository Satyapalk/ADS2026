using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ADS2026.Data;
using ADS2026.Models;

namespace ADS2026.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TVController : ControllerBase
    {
        private readonly AppDbContext _db;
        public TVController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _db.TVs.OrderBy(t => t.Floor).ThenBy(t => t.Room).ToListAsync());

        [HttpGet("grouped")]
        public async Task<IActionResult> GetGrouped()
        {
            var tvs = await _db.TVs.Where(t => t.Enabled).ToListAsync();
            var grouped = tvs
                .GroupBy(t => t.Floor)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Floor = g.Key,
                    Rooms = g.GroupBy(t => t.Room)
                             .Select(r => new { Room = r.Key, TVs = r.ToList() })
                });
            return Ok(grouped);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TVRequest req)
        {
            var name = req.Name?.Trim();
            var room = req.Room?.Trim();
            var floor = req.Floor?.Trim();

            if (string.IsNullOrWhiteSpace(name)) return BadRequest(new { message = "TV name is required." });
            if (string.IsNullOrWhiteSpace(room)) return BadRequest(new { message = "Room is required." });
            if (string.IsNullOrWhiteSpace(floor)) return BadRequest(new { message = "Floor is required." });

            bool exists = await _db.TVs.AnyAsync(t =>
                t.Name.ToLower() == name.ToLower() &&
                t.Room.ToLower() == room.ToLower() &&
                t.Floor.ToLower() == floor.ToLower());
            if (exists) return Conflict(new { message = $"\"{name}\" already exists in {room}, Floor {floor}." });

            var tv = new TV { Name = name, Room = room, Floor = floor };
            _db.TVs.Add(tv);
            await _db.SaveChangesAsync();
            return Ok(tv);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] TVRequest req)
        {
            var tv = await _db.TVs.FindAsync(id);
            if (tv == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(req.Name)) tv.Name = req.Name.Trim();
            if (!string.IsNullOrWhiteSpace(req.Room)) tv.Room = req.Room.Trim();
            if (!string.IsNullOrWhiteSpace(req.Floor)) tv.Floor = req.Floor.Trim();

            await _db.SaveChangesAsync();
            return Ok(tv);
        }

        [HttpPatch("{id}/toggle")]
        public async Task<IActionResult> Toggle(int id)
        {
            var tv = await _db.TVs.FindAsync(id);
            if (tv == null) return NotFound();
            tv.Enabled = !tv.Enabled;
            await _db.SaveChangesAsync();
            return Ok(tv);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var tv = await _db.TVs.FindAsync(id);
            if (tv == null) return NotFound();
            _db.TVs.Remove(tv);
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }
    }

    public class TVRequest
    {
        public string? Name { get; set; }
        public string? Room { get; set; }
        public string? Floor { get; set; }
    }
}