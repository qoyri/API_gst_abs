using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gest_abs.Models;
using gest_abs.DTO;
using gest_abs.Services;

namespace gest_abs.Controllers
{
    [ApiController]
    [Route("api/teacher")]
    [Authorize(Roles = "professeur")]
    public class TeacherController : ControllerBase
    {
        private readonly GestionAbsencesContext _context;
        private readonly TeacherService _teacherService;

        public TeacherController(GestionAbsencesContext context, TeacherService teacherService)
        {
            _context = context;
            _teacherService = teacherService;
        }

        #region Profil Enseignant

        // GET: api/teacher/profile
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });

            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.User.Email == email);

            if (teacher == null)
                return NotFound(new ErrorResponseDTO { Message = "Profil enseignant non trouvé." });

            var teacherProfile = new TeacherProfileDTO
            {
                Id = teacher.Id,
                Email = teacher.User.Email,
                Subject = teacher.Subject,
                CreatedAt = teacher.User.CreatedAt
            };

            return Ok(teacherProfile);
        }

        // PUT: api/teacher/profile
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] TeacherUpdateProfileDTO updateDTO)
        {
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });

            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.User.Email == email);

            if (teacher == null)
                return NotFound(new ErrorResponseDTO { Message = "Profil enseignant non trouvé." });

            // Mise à jour des informations du profil
            teacher.Subject = updateDTO.Subject;
            
            // Si changement de mot de passe
            if (!string.IsNullOrEmpty(updateDTO.NewPassword))
            {
                // Vérifier l'ancien mot de passe (à implémenter)
                // teacher.User.Password = HashPassword(updateDTO.NewPassword);
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Profil mis à jour avec succès." });
        }

        #endregion

        #region Gestion des Classes

        // GET: api/teacher/classes
        [HttpGet("classes")]
        public async Task<IActionResult> GetClasses()
        {
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });

            var teacherId = await _context.Teachers
                .Where(t => t.User.Email == email)
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            var classes = await _context.Classes
                .Where(c => c.TeacherId == teacherId)
                .Select(c => new ClassDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    StudentCount = c.Students.Count
                })
                .ToListAsync();

            return Ok(classes);
        }

        // GET: api/teacher/classes/{id}
        [HttpGet("classes/{id}")]
        public async Task<IActionResult> GetClassById(int id)
        {
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });

            var teacherId = await _context.Teachers
                .Where(t => t.User.Email == email)
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            var classEntity = await _context.Classes
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.Id == id && c.TeacherId == teacherId);

            if (classEntity == null)
                return NotFound(new ErrorResponseDTO { Message = "Classe non trouvée ou non autorisée." });

            var classDTO = new ClassDetailDTO
            {
                Id = classEntity.Id,
                Name = classEntity.Name,
                Students = classEntity.Students.Select(s => new StudentDTO
                {
                    Id = s.Id,
                    ClassId = s.ClassId,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    Birthdate = s.Birthdate
                }).ToList()
            };

            return Ok(classDTO);
        }

        #endregion

        #region Gestion des Absences

        // GET: api/teacher/absences
        [HttpGet("absences")]
        public async Task<IActionResult> GetAbsences([FromQuery] AbsenceFilterDTO filter)
        {
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });

            var teacherId = await _context.Teachers
                .Where(t => t.User.Email == email)
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            // Récupérer les classes de l'enseignant
            var classIds = await _context.Classes
                .Where(c => c.TeacherId == teacherId)
                .Select(c => c.Id)
                .ToListAsync();

            // Récupérer les étudiants de ces classes
            var studentIds = await _context.Students
                .Where(s => classIds.Contains(s.ClassId))
                .Select(s => s.Id)
                .ToListAsync();

            var query = _context.Absences
                .Include(a => a.Student)
                .ThenInclude(s => s.Class)
                .Where(a => studentIds.Contains(a.StudentId));

            // Appliquer les filtres
            if (filter.ClassId.HasValue)
            {
                var studentsInClass = await _context.Students
                    .Where(s => s.ClassId == filter.ClassId)
                    .Select(s => s.Id)
                    .ToListAsync();
                
                query = query.Where(a => studentsInClass.Contains(a.StudentId));
            }

            if (filter.StudentId.HasValue)
                query = query.Where(a => a.StudentId == filter.StudentId);

            if (filter.StartDate.HasValue)
                query = query.Where(a => a.AbsenceDate >= DateOnly.FromDateTime(filter.StartDate.Value));

            if (filter.EndDate.HasValue)
                query = query.Where(a => a.AbsenceDate <= DateOnly.FromDateTime(filter.EndDate.Value));

            if (filter.Status != null)
                query = query.Where(a => a.Status == filter.Status);

            var absences = await query
                .Select(a => new AbsenceDTO
                {
                    Id = a.Id,
                    StudentId = a.StudentId,
                    StudentName = $"{a.Student.FirstName} {a.Student.LastName}",
                    ClassId = a.Student.ClassId,
                    ClassName = a.Student.Class.Name,
                    AbsenceDate = a.AbsenceDate,
                    Status = a.Status,
                    Reason = a.Reason,
                    Document = a.Document
                })
                .OrderByDescending(a => a.AbsenceDate)
                .ToListAsync();

            return Ok(absences);
        }

        // POST: api/teacher/absences
        [HttpPost("absences")]
        public async Task<IActionResult> CreateAbsence([FromBody] AbsenceCreateDTO createDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });

            var teacherId = await _context.Teachers
                .Where(t => t.User.Email == email)
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            // Vérifier que l'étudiant existe et appartient à une classe de l'enseignant
            var student = await _context.Students
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.Id == createDTO.StudentId && s.Class.TeacherId == teacherId);

            if (student == null)
                return BadRequest(new ErrorResponseDTO { Message = "Étudiant non trouvé ou n'appartient pas à vos classes." });

            var absence = new Absence
            {
                StudentId = createDTO.StudentId,
                AbsenceDate = DateOnly.FromDateTime(createDTO.AbsenceDate),
                Status = createDTO.Status ?? "en attente",
                Reason = createDTO.Reason,
                Document = createDTO.Document,
                CreatedAt = DateTime.UtcNow
            };

            _context.Absences.Add(absence);
            await _context.SaveChangesAsync();

            // Notifier les parents (à implémenter dans un service)
            await _teacherService.NotifyParentsAboutAbsence(absence.Id);

            return CreatedAtAction(nameof(GetAbsenceById), new { id = absence.Id }, absence);
        }

        // GET: api/teacher/absences/{id}
        [HttpGet("absences/{id}")]
        public async Task<IActionResult> GetAbsenceById(int id)
        {
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });

            var teacherId = await _context.Teachers
                .Where(t => t.User.Email == email)
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            // Récupérer les classes de l'enseignant
            var classIds = await _context.Classes
                .Where(c => c.TeacherId == teacherId)
                .Select(c => c.Id)
                .ToListAsync();

            // Récupérer les étudiants de ces classes
            var studentIds = await _context.Students
                .Where(s => classIds.Contains(s.ClassId))
                .Select(s => s.Id)
                .ToListAsync();

            var absence = await _context.Absences
                .Include(a => a.Student)
                .ThenInclude(s => s.Class)
                .FirstOrDefaultAsync(a => a.Id == id && studentIds.Contains(a.StudentId));

            if (absence == null)
                return NotFound(new ErrorResponseDTO { Message = "Absence non trouvée ou non autorisée." });

            var absenceDTO = new AbsenceDetailDTO
            {
                Id = absence.Id,
                StudentId = absence.StudentId,
                StudentName = $"{absence.Student.FirstName} {absence.Student.LastName}",
                ClassId = absence.Student.ClassId,
                ClassName = absence.Student.Class.Name,
                AbsenceDate = absence.AbsenceDate,
                Status = absence.Status,
                Reason = absence.Reason,
                Document = absence.Document,
                CreatedAt = absence.CreatedAt,
                UpdatedAt = absence.UpdatedAt
            };

            return Ok(absenceDTO);
        }

        // PUT: api/teacher/absences/{id}
        [HttpPut("absences/{id}")]
        public async Task<IActionResult> UpdateAbsence(int id, [FromBody] AbsenceUpdateDTO updateDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });

            var teacherId = await _context.Teachers
                .Where(t => t.User.Email == email)
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            // Récupérer les classes de l'enseignant
            var classIds = await _context.Classes
                .Where(c => c.TeacherId == teacherId)
                .Select(c => c.Id)
                .ToListAsync();

            // Récupérer les étudiants de ces classes
            var studentIds = await _context.Students
                .Where(s => classIds.Contains(s.ClassId))
                .Select(s => s.Id)
                .ToListAsync();

            var absence = await _context.Absences
                .FirstOrDefaultAsync(a => a.Id == id && studentIds.Contains(a.StudentId));

            if (absence == null)
                return NotFound(new ErrorResponseDTO { Message = "Absence non trouvée ou non autorisée." });

            // Mise à jour des informations
            absence.AbsenceDate = DateOnly.FromDateTime(updateDTO.AbsenceDate);
            absence.Status = updateDTO.Status;
            absence.Reason = updateDTO.Reason;
            absence.Document = updateDTO.Document;
            absence.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Absence mise à jour avec succès." });
        }

        // DELETE: api/teacher/absences/{id}
        [HttpDelete("absences/{id}")]
        public async Task<IActionResult> DeleteAbsence(int id)
        {
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });

            var teacherId = await _context.Teachers
                .Where(t => t.User.Email == email)
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            // Récupérer les classes de l'enseignant
            var classIds = await _context.Classes
                .Where(c => c.TeacherId == teacherId)
                .Select(c => c.Id)
                .ToListAsync();

            // Récupérer les étudiants de ces classes
            var studentIds = await _context.Students
                .Where(s => classIds.Contains(s.ClassId))
                .Select(s => s.Id)
                .ToListAsync();

            var absence = await _context.Absences
                .FirstOrDefaultAsync(a => a.Id == id && studentIds.Contains(a.StudentId));

            if (absence == null)
                return NotFound(new ErrorResponseDTO { Message = "Absence non trouvée ou non autorisée." });

            _context.Absences.Remove(absence);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Absence supprimée avec succès." });
        }

        #endregion

        #region Rapports et Statistiques

        // GET: api/teacher/reports/absences
        [HttpGet("reports/absences")]
        public async Task<IActionResult> GetAbsenceReport([FromQuery] ReportFilterDTO filter)
        {
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });

            var teacherId = await _context.Teachers
                .Where(t => t.User.Email == email)
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            var report = await _teacherService.GenerateAbsenceReport(teacherId, filter);
            return Ok(report);
        }

        // GET: api/teacher/stats/class/{classId}
        [HttpGet("stats/class/{classId}")]
        public async Task<IActionResult> GetClassStats(int classId)
        {
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });

            var teacherId = await _context.Teachers
                .Where(t => t.User.Email == email)
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            // Vérifier que l'enseignant a accès à cette classe
            var hasAccess = await _context.Classes
                .AnyAsync(c => c.Id == classId && c.TeacherId == teacherId);

            if (!hasAccess)
                return Forbid();

            var stats = await _teacherService.GetClassStatistics(classId);
            return Ok(stats);
        }

        // GET: api/teacher/stats/student/{studentId}
        [HttpGet("stats/student/{studentId}")]
        public async Task<IActionResult> GetStudentStats(int studentId)
        {
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });

            var teacherId = await _context.Teachers
                .Where(t => t.User.Email == email)
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            // Vérifier que l'étudiant appartient à une classe de l'enseignant
            var student = await _context.Students
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.Id == studentId && s.Class.TeacherId == teacherId);

            if (student == null)
                return NotFound(new ErrorResponseDTO { Message = "Étudiant non trouvé ou non autorisé." });

            var stats = await _teacherService.GetStudentStatistics(studentId);
            return Ok(stats);
        }

        #endregion

        #region Réservation de Salles

        // GET: api/teacher/rooms
        [HttpGet("rooms")]
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

        // GET: api/teacher/rooms/{id}
        [HttpGet("rooms/{id}")]
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

        // GET: api/teacher/reservations
        [HttpGet("reservations")]
        public async Task<IActionResult> GetReservations([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });

            var userId = await _context.Users
                .Where(u => u.Email == email)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            var query = _context.Reservations
                .Include(r => r.Room)
                .Where(r => r.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(r => r.ReservationDate >= DateOnly.FromDateTime(startDate.Value));

            if (endDate.HasValue)
                query = query.Where(r => r.ReservationDate <= DateOnly.FromDateTime(endDate.Value));

            var reservations = await query
                .Select(r => new ReservationDTO
                {
                    Id = r.Id,
                    RoomId = r.RoomId,
                    RoomName = r.Room.Name,
                    ReservationDate = r.ReservationDate,
                    StartTime = r.StartTime,
                    EndTime = r.EndTime
                })
                .OrderBy(r => r.ReservationDate)
                .ThenBy(r => r.StartTime)
                .ToListAsync();

            return Ok(reservations);
        }

        // POST: api/teacher/reservations
        [HttpPost("reservations")]
        public async Task<IActionResult> CreateReservation([FromBody] ReservationCreateDTO createDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });

            var userId = await _context.Users
                .Where(u => u.Email == email)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            // Vérifier que la salle existe
            var room = await _context.Rooms.FindAsync(createDTO.RoomId);
            if (room == null)
                return BadRequest(new ErrorResponseDTO { Message = "Salle non trouvée." });

            // Vérifier que la salle n'est pas déjà réservée
            var isRoomAvailable = await _teacherService.IsRoomAvailable(
                createDTO.RoomId,
                DateOnly.FromDateTime(createDTO.ReservationDate),
                TimeOnly.FromTimeSpan(createDTO.StartTime),
                TimeOnly.FromTimeSpan(createDTO.EndTime));

            if (!isRoomAvailable)
                return BadRequest(new ErrorResponseDTO { Message = "La salle est déjà réservée pour cette période." });

            var reservation = new Reservation
            {
                UserId = userId,
                RoomId = createDTO.RoomId,
                ReservationDate = DateOnly.FromDateTime(createDTO.ReservationDate),
                StartTime = TimeOnly.FromTimeSpan(createDTO.StartTime),
                EndTime = TimeOnly.FromTimeSpan(createDTO.EndTime)
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReservationById), new { id = reservation.Id }, reservation);
        }

        // GET: api/teacher/reservations/{id}
        [HttpGet("reservations/{id}")]
        public async Task<IActionResult> GetReservationById(int id)
        {
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });

            var userId = await _context.Users
                .Where(u => u.Email == email)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            var reservation = await _context.Reservations
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (reservation == null)
                return NotFound(new ErrorResponseDTO { Message = "Réservation non trouvée ou non autorisée." });

            var reservationDTO = new ReservationDTO
            {
                Id = reservation.Id,
                RoomId = reservation.RoomId,
                RoomName = reservation.Room.Name,
                ReservationDate = reservation.ReservationDate,
                StartTime = reservation.StartTime,
                EndTime = reservation.EndTime
            };

            return Ok(reservationDTO);
        }

        // PUT: api/teacher/reservations/{id}
        [HttpPut("reservations/{id}")]
        public async Task<IActionResult> UpdateReservation(int id, [FromBody] ReservationUpdateDTO updateDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });

            var userId = await _context.Users
                .Where(u => u.Email == email)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (reservation == null)
                return NotFound(new ErrorResponseDTO { Message = "Réservation non trouvée ou non autorisée." });

            // Vérifier que la salle n'est pas déjà réservée (en excluant la réservation actuelle)
            var isRoomAvailable = await _teacherService.IsRoomAvailableExcludingReservation(
                reservation.RoomId,
                DateOnly.FromDateTime(updateDTO.ReservationDate),
                TimeOnly.FromTimeSpan(updateDTO.StartTime),
                TimeOnly.FromTimeSpan(updateDTO.EndTime),
                id);

            if (!isRoomAvailable)
                return BadRequest(new ErrorResponseDTO { Message = "La salle est déjà réservée pour cette période." });

            // Mise à jour des informations
            reservation.ReservationDate = DateOnly.FromDateTime(updateDTO.ReservationDate);
            reservation.StartTime = TimeOnly.FromTimeSpan(updateDTO.StartTime);
            reservation.EndTime = TimeOnly.FromTimeSpan(updateDTO.EndTime);

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Réservation mise à jour avec succès." });
        }

        // DELETE: api/teacher/reservations/{id}
        [HttpDelete("reservations/{id}")]
        public async Task<IActionResult> DeleteReservation(int id)
        {
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });

            var userId = await _context.Users
                .Where(u => u.Email == email)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (reservation == null)
                return NotFound(new ErrorResponseDTO { Message = "Réservation non trouvée ou non autorisée." });

            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Réservation supprimée avec succès." });
        }

        #endregion
    }
}

