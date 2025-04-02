using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gest_abs.Models;
using gest_abs.DTO;

namespace gest_abs.Controllers
{
    [ApiController]
    [Route("api/rooms")]
    [Authorize(Roles = "admin")]
    public class RoomController : ControllerBase
    {
        private readonly GestionAbsencesContext _context;

        public RoomController(GestionAbsencesContext context)
        {
            _context = context;
        }

        // GET: api/rooms
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetRooms()
        {
            var rooms = await _context.Rooms
                .Select(r => new RoomDTO
                {
                    Id = r.Id,
                    Name = r.Name,
                    Capacity = r.Capacity,
                    Location = r.Location
                })
                .ToListAsync();

            return Ok(rooms);
        }

        // GET: api/rooms/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRoomById(int id)
        {
            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
                return NotFound(new ErrorResponseDTO { Message = "Salle non trouvée." });

            var roomDTO = new RoomDTO
            {
                Id = room.Id,
                Name = room.Name,
                Capacity = room.Capacity,
                Location = room.Location
            };

            return Ok(roomDTO);
        }

        // POST: api/rooms
        [HttpPost]
        public async Task<IActionResult> CreateRoom([FromBody] RoomCreateDTO createDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Vérifier si une salle avec le même nom existe déjà
            var existingRoom = await _context.Rooms
                .FirstOrDefaultAsync(r => r.Name == createDTO.Name);

            if (existingRoom != null)
                return Conflict(new ErrorResponseDTO { Message = "Une salle avec ce nom existe déjà." });

            var room = new Room
            {
                Name = createDTO.Name,
                Capacity = createDTO.Capacity,
                Location = createDTO.Location
            };

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRoomById), new { id = room.Id }, room);
        }

        // PUT: api/rooms/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRoom(int id, [FromBody] RoomUpdateDTO updateDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
                return NotFound(new ErrorResponseDTO { Message = "Salle non trouvée." });

            // Vérifier si une autre salle avec le même nom existe déjà
            var existingRoom = await _context.Rooms
                .FirstOrDefaultAsync(r => r.Name == updateDTO.Name && r.Id != id);

            if (existingRoom != null)
                return Conflict(new ErrorResponseDTO { Message = "Une autre salle avec ce nom existe déjà." });

            room.Name = updateDTO.Name;
            room.Capacity = updateDTO.Capacity;
            room.Location = updateDTO.Location;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Salle mise à jour avec succès." });
        }

        // DELETE: api/rooms/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
                return NotFound(new ErrorResponseDTO { Message = "Salle non trouvée." });

            // Vérifier si la salle a des réservations
            var hasReservations = await _context.Reservations
                .AnyAsync(r => r.RoomId == id);

            if (hasReservations)
                return BadRequest(new ErrorResponseDTO { Message = "Impossible de supprimer cette salle car elle a des réservations." });

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Salle supprimée avec succès." });
        }

        // GET: api/rooms/{id}/reservations
        [HttpGet("{id}/reservations")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRoomReservations(int id, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
                return NotFound(new ErrorResponseDTO { Message = "Salle non trouvée." });

            var query = _context.Reservations
                .Include(r => r.User)
                .Where(r => r.RoomId == id);

            if (startDate.HasValue)
                query = query.Where(r => r.ReservationDate >= DateOnly.FromDateTime(startDate.Value));

            if (endDate.HasValue)
                query = query.Where(r => r.ReservationDate <= DateOnly.FromDateTime(endDate.Value));

            var reservations = await query
                .OrderBy(r => r.ReservationDate)
                .ThenBy(r => r.StartTime)
                .Select(r => new
                {
                    Id = r.Id,
                    UserEmail = r.User.Email,
                    ReservationDate = r.ReservationDate,
                    StartTime = r.StartTime,
                    EndTime = r.EndTime
                })
                .ToListAsync();

            return Ok(reservations);
        }
    }
}

