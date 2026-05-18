using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;
using RanitaApi.Models;

namespace RanitaApi.Controllers
{
    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public EventsController(AppDbContext db) { _db = db; }

        // GET /api/events — liste tous les événements (admin)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var events = await _db.SiteEvents
                .OrderBy(e => e.StartDate)
                .ToListAsync();
            return Ok(events);
        }

        // GET /api/events/current — événement actif aujourd'hui (home)
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrent()
        {
            var today = DateTime.UtcNow.Date;
            var ev = await _db.SiteEvents
                .Where(e => e.IsActive &&
                    (e.StartDate == null || e.StartDate.Value.Date <= today) &&
                    (e.EndDate == null || e.EndDate.Value.Date >= today))
                .OrderBy(e => e.StartDate)
                .FirstOrDefaultAsync();

            if (ev == null) return Ok(null);
            return Ok(ev);
        }

        // POST /api/events — créer un événement
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SiteEvent ev)
        {
            ev.CreatedAt = DateTime.UtcNow;
            _db.SiteEvents.Add(ev);
            await _db.SaveChangesAsync();
            return Ok(ev);
        }

        // PUT /api/events/{id} — modifier un événement
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SiteEvent updated)
        {
            var ev = await _db.SiteEvents.FindAsync(id);
            if (ev == null) return NotFound();

            ev.Name = updated.Name;
            ev.Color = updated.Color;
            ev.StartDate = updated.StartDate;
            ev.EndDate = updated.EndDate;
            ev.PromoText = updated.PromoText;
            ev.SlideTitle = updated.SlideTitle;
            ev.SlideSub = updated.SlideSub;
            ev.SlideCta = updated.SlideCta;
            ev.SlideLink = updated.SlideLink;
            ev.SlideDisc = updated.SlideDisc;
            ev.SlideImg = updated.SlideImg;
            ev.IsActive = updated.IsActive;

            await _db.SaveChangesAsync();
            return Ok(ev);
        }

        // DELETE /api/events/{id} — supprimer un événement
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ev = await _db.SiteEvents.FindAsync(id);
            if (ev == null) return NotFound();
            _db.SiteEvents.Remove(ev);
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }
    }
}