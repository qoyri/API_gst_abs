using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using gest_abs.Services;
using gest_abs.DTO;
using gest_abs.Models;
using System.Security.Claims;

namespace gest_abs.Controllers
{
    [Route("api/student-portal")]
    [ApiController]
    [Authorize(Roles = "eleve")] // 🔹 Seuls les élèves peuvent accéder à ces endpoints
    public class StudentPortalController : ControllerBase
    {
        private readonly StudentService _studentService;
        private readonly GestionAbsencesContext _context;

        public StudentPortalController(StudentService studentService, GestionAbsencesContext context)
        {
            _studentService = studentService;
            _context = context;
        }

        // 🔹 GET /api/student-portal/profile → Récupérer le profil de l'élève connecté
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var studentEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(studentEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == studentEmail && u.Role == "eleve");

                if (user == null)
                {
                    return NotFound(new ErrorResponseDTO { Message = "Utilisateur non trouvé." });
                }

                var student = await _context.Students
                    .Include(s => s.Class)
                    .FirstOrDefaultAsync(s => s.UserId == user.Id);

                if (student == null)
                {
                    return NotFound(new ErrorResponseDTO { Message = "Profil étudiant non trouvé." });
                }

                return Ok(new
                {
                    Id = student.Id,
                    UserId = student.UserId,
                    FirstName = student.FirstName,
                    LastName = student.LastName,
                    Birthdate = student.Birthdate,
                    ClassId = student.ClassId,
                    ClassName = student.Class.Name,
                    Email = user.Email
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 PUT /api/student-portal/profile → Mettre à jour le profil de l'élève
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] StudentProfileUpdateDTO updateDTO)
        {
            try
            {
                var studentEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(studentEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == studentEmail && u.Role == "eleve");

                if (user == null)
                {
                    return NotFound(new ErrorResponseDTO { Message = "Utilisateur non trouvé." });
                }

                // Mise à jour du mot de passe si fourni
                if (!string.IsNullOrEmpty(updateDTO.CurrentPassword) && !string.IsNullOrEmpty(updateDTO.NewPassword))
                {
                    // Vérifier l'ancien mot de passe
                    var currentPasswordHash = Services.HasherPassword.HashPassword(updateDTO.CurrentPassword);
                    if (currentPasswordHash != user.Password)
                    {
                        return BadRequest(new ErrorResponseDTO { Message = "Mot de passe actuel incorrect." });
                    }

                    // Mettre à jour le mot de passe
                    user.Password = Services.HasherPassword.HashPassword(updateDTO.NewPassword);
                }

                // Mettre à jour l'email si fourni
                if (!string.IsNullOrEmpty(updateDTO.Email) && updateDTO.Email != user.Email)
                {
                    // Vérifier si l'email est déjà utilisé
                    var emailExists = await _context.Users.AnyAsync(u => u.Email == updateDTO.Email && u.Id != user.Id);
                    if (emailExists)
                    {
                        return BadRequest(new ErrorResponseDTO { Message = "Cet email est déjà utilisé." });
                    }

                    user.Email = updateDTO.Email;
                }

                await _context.SaveChangesAsync();
                return Ok(new { Message = "Profil mis à jour avec succès." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 GET /api/student-portal/absences → Récupérer les absences de l'élève connecté
        [HttpGet("absences")]
        public async Task<IActionResult> GetAbsences()
        {
            try
            {
                var studentEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(studentEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var absences = await _studentService.GetStudentAbsences(studentEmail);
                if (absences == null || !absences.Any())
                {
                    return NotFound(new ErrorResponseDTO { Message = "Aucune absence trouvée." });
                }

                return Ok(absences);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 GET /api/student-portal/absences/{id} → Récupérer les détails d'une absence
        [HttpGet("absences/{id}")]
        public async Task<IActionResult> GetAbsenceDetails(int id)
        {
            try
            {
                var studentEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(studentEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var absenceDetails = await _studentService.GetAbsenceDetails(studentEmail, id);
                if (absenceDetails == null)
                {
                    return NotFound(new ErrorResponseDTO { Message = "Absence non trouvée ou non autorisée." });
                }

                return Ok(absenceDetails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 GET /api/student-portal/notifications → Récupérer les notifications de l'élève
        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            try
            {
                var studentEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(studentEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var notifications = await _studentService.GetStudentNotifications(studentEmail);
                if (notifications == null || !notifications.Any())
                {
                    return NotFound(new ErrorResponseDTO { Message = "Aucune notification trouvée." });
                }

                return Ok(notifications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 PUT /api/student-portal/notifications/{id}/read → Marquer une notification comme lue
        [HttpPut("notifications/{id}/read")]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            try
            {
                var studentEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(studentEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var success = await _studentService.MarkNotificationAsRead(studentEmail, id);
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

        // 🔹 GET /api/student-portal/stats → Récupérer les statistiques de l'élève
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var studentEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(studentEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var stats = await _studentService.GetStudentStats(studentEmail);
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

        // 🔹 GET /api/student-portal/schedule → Récupérer l'emploi du temps de l'élève
        [HttpGet("schedule")]
        public async Task<IActionResult> GetSchedule([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var studentEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(studentEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                // Si aucune date n'est spécifiée, utiliser la semaine en cours
                if (!startDate.HasValue)
                {
                    startDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1); // Lundi de la semaine en cours
                }

                if (!endDate.HasValue)
                {
                    endDate = startDate.Value.AddDays(6); // Dimanche de la semaine en cours
                }

                var schedule = await _studentService.GetStudentSchedule(studentEmail, startDate.Value, endDate.Value);
                if (schedule == null || !schedule.Any())
                {
                    return NotFound(new ErrorResponseDTO { Message = "Aucun emploi du temps trouvé pour cette période." });
                }

                return Ok(schedule);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 GET /api/student-portal/dashboard → Récupérer les données du tableau de bord
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var studentEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(studentEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == studentEmail && u.Role == "eleve");

                if (user == null)
                {
                    return NotFound(new ErrorResponseDTO { Message = "Utilisateur non trouvé." });
                }

                var student = await _context.Students
                    .Include(s => s.Class)
                    .FirstOrDefaultAsync(s => s.UserId == user.Id);

                if (student == null)
                {
                    return NotFound(new ErrorResponseDTO { Message = "Profil étudiant non trouvé." });
                }

                // Récupérer les absences récentes
                var recentAbsences = await _context.Absences
                    .Where(a => a.StudentId == student.Id)
                    .OrderByDescending(a => a.AbsenceDate)
                    .Take(5)
                    .Select(a => new
                    {
                        Id = a.Id,
                        AbsenceDate = a.AbsenceDate,
                        Status = a.Status,
                        Reason = a.Reason
                    })
                    .ToListAsync();

                // Récupérer les notifications non lues
                var unreadNotifications = await _context.Notifications
                    .Where(n => n.UserId == user.Id && (n.IsRead == false || n.IsRead == null))
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(5)
                    .Select(n => new
                    {
                        Id = n.Id,
                        Message = n.Message,
                        CreatedAt = n.CreatedAt
                    })
                    .ToListAsync();

                // Calculer les statistiques globales
                var allAbsences = await _context.Absences
                    .Where(a => a.StudentId == student.Id)
                    .ToListAsync();

                // Récupérer l'emploi du temps du jour
                var today = DateOnly.FromDateTime(DateTime.Today);
                var todaySchedule = await _context.Schedules
                    .Where(s => s.ClassId == student.ClassId && s.Date == today)
                    .OrderBy(s => s.StartTime)
                    .Select(s => new
                    {
                        Id = s.Id,
                        Subject = s.Subject,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        Description = s.Description
                    })
                    .ToListAsync();

                var dashboardData = new
                {
                    StudentName = $"{student.FirstName} {student.LastName}",
                    ClassName = student.Class.Name,
                    TotalAbsences = allAbsences.Count,
                    JustifiedAbsences = allAbsences.Count(a => a.Status == "justifiée"),
                    UnjustifiedAbsences = allAbsences.Count(a => a.Status == "non justifiée"),
                    PendingAbsences = allAbsences.Count(a => a.Status == "en attente"),
                    RecentAbsences = recentAbsences,
                    UnreadNotifications = unreadNotifications,
                    UnreadNotificationsCount = unreadNotifications.Count,
                    TodaySchedule = todaySchedule
                };

                return Ok(dashboardData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 GET /api/student-portal/class-ranking → Récupérer le classement de la classe
        [HttpGet("class-ranking")]
        public async Task<IActionResult> GetClassRanking()
        {
            try
            {
                var studentEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(studentEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var ranking = await _studentService.GetClassRanking(studentEmail);
                if (ranking == null || !ranking.Any())
                {
                    return NotFound(new ErrorResponseDTO { Message = "Aucun classement trouvé." });
                }

                return Ok(ranking);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }
    }
}

