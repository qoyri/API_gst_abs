using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using gest_abs.Services;
using gest_abs.DTO;
using gest_abs.Models;
using System.Security.Claims;

namespace gest_abs.Controllers
{
    [Route("api/teacher-portal")]
    [ApiController]
    [Authorize(Roles = "professeur")] // 🔹 Seuls les professeurs peuvent accéder à ces endpoints
    public class TeacherPortalController : ControllerBase
    {
        private readonly TeacherService _teacherService;
        private readonly GestionAbsencesContext _context;

        public TeacherPortalController(TeacherService teacherService, GestionAbsencesContext context)
        {
            _teacherService = teacherService;
            _context = context;
        }

        // 🔹 GET /api/teacher-portal/dashboard → Récupérer le tableau de bord du professeur
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var teacherEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(teacherEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var dashboard = await _teacherService.GetTeacherDashboard(teacherEmail);
                if (dashboard == null)
                {
                    return NotFound(new ErrorResponseDTO { Message = "Tableau de bord non trouvé." });
                }

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 GET /api/teacher-portal/profile → Récupérer le profil du professeur
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var teacherEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(teacherEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var profile = await _teacherService.GetTeacherProfile(teacherEmail);
                if (profile == null)
                {
                    return NotFound(new ErrorResponseDTO { Message = "Profil non trouvé." });
                }

                return Ok(profile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 PUT /api/teacher-portal/profile → Mettre à jour le profil du professeur
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] TeacherUpdateProfileDTO updateDTO)
        {
            try
            {
                var teacherEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(teacherEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var success = await _teacherService.UpdateTeacherProfile(teacherEmail, updateDTO);
                if (!success)
                {
                    return BadRequest(new ErrorResponseDTO { Message = "Mise à jour du profil impossible." });
                }

                return Ok(new { Message = "Profil mis à jour avec succès." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 GET /api/teacher-portal/classes → Récupérer les classes du professeur
        [HttpGet("classes")]
        public async Task<IActionResult> GetClasses()
        {
            try
            {
                var teacherEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(teacherEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var classes = await _teacherService.GetTeacherClasses(teacherEmail);
                return Ok(classes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 GET /api/teacher-portal/classes/{id} → Récupérer les détails d'une classe
        [HttpGet("classes/{id}")]
        public async Task<IActionResult> GetClassDetails(int id)
        {
            try
            {
                var teacherEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(teacherEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var classDetails = await _teacherService.GetClassDetails(teacherEmail, id);
                if (classDetails == null)
                {
                    return NotFound(new ErrorResponseDTO { Message = "Classe non trouvée ou non autorisée." });
                }

                return Ok(classDetails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 GET /api/teacher-portal/notifications → Récupérer les notifications du professeur
        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            try
            {
                var teacherEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(teacherEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var notifications = await _teacherService.GetTeacherNotifications(teacherEmail);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 PUT /api/teacher-portal/notifications/{id}/read → Marquer une notification comme lue
        [HttpPut("notifications/{id}/read")]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            try
            {
                var teacherEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(teacherEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var success = await _teacherService.MarkNotificationAsRead(teacherEmail, id);
                if (!success)
                {
                    return NotFound(new ErrorResponseDTO { Message = "Notification non trouvée ou non autorisée." });
                }

                return Ok(new { Message = "Notification marquée comme lue." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 GET /api/teacher-portal/reservations → Récupérer les réservations du professeur
        [HttpGet("reservations")]
        public async Task<IActionResult> GetReservations([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var teacherEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(teacherEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var reservations = await _teacherService.GetTeacherReservations(teacherEmail, startDate, endDate);
                return Ok(reservations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 POST /api/teacher-portal/reservations → Créer une réservation
        [HttpPost("reservations")]
        public async Task<IActionResult> CreateReservation([FromBody] ReservationCreateDTO createDTO)
        {
            try
            {
                var teacherEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(teacherEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var reservation = await _teacherService.CreateReservation(teacherEmail, createDTO);
                if (reservation == null)
                {
                    return BadRequest(new ErrorResponseDTO { Message = "Création de réservation impossible." });
                }

                return CreatedAtAction(nameof(GetReservationById), new { id = reservation.Id }, reservation);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 GET /api/teacher-portal/reservations/{id} → Récupérer une réservation
        [HttpGet("reservations/{id}")]
        public async Task<IActionResult> GetReservationById(int id)
        {
            try
            {
                var teacherEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(teacherEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var userId = await _context.Users
                    .Where(u => u.Email == teacherEmail)
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync();

                var reservation = await _context.Reservations
                    .Include(r => r.Room)
                    .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

                if (reservation == null)
                {
                    return NotFound(new ErrorResponseDTO { Message = "Réservation non trouvée ou non autorisée." });
                }

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
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 PUT /api/teacher-portal/reservations/{id} → Mettre à jour une réservation
        [HttpPut("reservations/{id}")]
        public async Task<IActionResult> UpdateReservation(int id, [FromBody] ReservationUpdateDTO updateDTO)
        {
            try
            {
                var teacherEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(teacherEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var success = await _teacherService.UpdateReservation(teacherEmail, id, updateDTO);
                if (!success)
                {
                    return BadRequest(new ErrorResponseDTO { Message = "Mise à jour de réservation impossible." });
                }

                return Ok(new { Message = "Réservation mise à jour avec succès." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 DELETE /api/teacher-portal/reservations/{id} → Supprimer une réservation
        [HttpDelete("reservations/{id}")]
        public async Task<IActionResult> DeleteReservation(int id)
        {
            try
            {
                var teacherEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(teacherEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var success = await _teacherService.DeleteReservation(teacherEmail, id);
                if (!success)
                {
                    return NotFound(new ErrorResponseDTO { Message = "Réservation non trouvée ou non autorisée." });
                }

                return Ok(new { Message = "Réservation supprimée avec succès." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 POST /api/teacher-portal/absences → Créer une absence
        [HttpPost("absences")]
        public async Task<IActionResult> CreateAbsence([FromBody] AbsenceCreateDTO createDTO)
        {
            try
            {
                var teacherEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(teacherEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var absence = await _teacherService.CreateAbsence(teacherEmail, createDTO);
                if (absence == null)
                {
                    return BadRequest(new ErrorResponseDTO { Message = "Création d'absence impossible." });
                }

                return CreatedAtAction("GetAbsenceById", "TeacherController", new { id = absence.Id }, absence);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 GET /api/teacher-portal/stats/class/{classId} → Récupérer les statistiques d'une classe
        [HttpGet("stats/class/{classId}")]
        public async Task<IActionResult> GetClassStats(int classId)
        {
            try
            {
                var teacherEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(teacherEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var teacherId = await _context.Teachers
                    .Where(t => t.User.Email == teacherEmail)
                    .Select(t => t.Id)
                    .FirstOrDefaultAsync();

                // Vérifier que l'enseignant a accès à cette classe
                var hasAccess = await _context.Classes
                    .AnyAsync(c => c.Id == classId && c.TeacherId == teacherId);

                if (!hasAccess)
                    return Forbid();

                var stats = await _teacherService.GetClassStatistics(classId);
                if (stats == null)
                {
                    return NotFound(new ErrorResponseDTO { Message = "Statistiques non trouvées." });
                }

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 GET /api/teacher-portal/stats/student/{studentId} → Récupérer les statistiques d'un étudiant
        [HttpGet("stats/student/{studentId}")]
        public async Task<IActionResult> GetStudentStats(int studentId)
        {
            try
            {
                var teacherEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(teacherEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var teacherId = await _context.Teachers
                    .Where(t => t.User.Email == teacherEmail)
                    .Select(t => t.Id)
                    .FirstOrDefaultAsync();

                // Vérifier que l'étudiant appartient à une classe de l'enseignant
                var student = await _context.Students
                    .Include(s => s.Class)
                    .FirstOrDefaultAsync(s => s.Id == studentId && s.Class.TeacherId == teacherId);

                if (student == null)
                    return NotFound(new ErrorResponseDTO { Message = "Étudiant non trouvé ou non autorisé." });

                var stats = await _teacherService.GetStudentStatistics(studentId);
                if (stats == null)
                {
                    return NotFound(new ErrorResponseDTO { Message = "Statistiques non trouvées." });
                }

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 GET /api/teacher-portal/reports/absences → Générer un rapport d'absences
        [HttpGet("reports/absences")]
        public async Task<IActionResult> GetAbsenceReport([FromQuery] ReportFilterDTO filter)
        {
            try
            {
                var teacherEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(teacherEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var teacherId = await _context.Teachers
                    .Where(t => t.User.Email == teacherEmail)
                    .Select(t => t.Id)
                    .FirstOrDefaultAsync();

                var report = await _teacherService.GenerateAbsenceReport(teacherId, filter);
                if (report == null)
                {
                    return NotFound(new ErrorResponseDTO { Message = "Rapport non généré." });
                }

                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }
    }
}

