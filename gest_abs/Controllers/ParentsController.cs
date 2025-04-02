using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using gest_abs.Services;
using gest_abs.DTO;
using gest_abs.Models;
using System.Security.Claims;

namespace gest_abs.Controllers
{
    [Route("api/parents")]
    [ApiController]
    [Authorize(Roles = "parent")] // 🔹 Seuls les parents peuvent accéder à ces endpoints
    public class ParentsController : ControllerBase
    {
        private readonly ParentService _parentService;
        private readonly GestionAbsencesContext _context;

        public ParentsController(ParentService parentService, GestionAbsencesContext context)
        {
            _parentService = parentService;
            _context = context;
        }

        // 🔹 GET /api/parents/profile → Récupérer le profil du parent connecté
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var parentEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(parentEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var parent = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == parentEmail && u.Role == "parent");

                if (parent == null)
                {
                    return NotFound(new ErrorResponseDTO { Message = "Profil parent non trouvé." });
                }

                return Ok(new
                {
                    Id = parent.Id,
                    Email = parent.Email,
                    Role = parent.Role,
                    CreatedAt = parent.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 PUT /api/parents/profile → Mettre à jour le profil du parent
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] ParentProfileUpdateDTO updateDTO)
        {
            try
            {
                var parentEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(parentEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var parent = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == parentEmail && u.Role == "parent");

                if (parent == null)
                {
                    return NotFound(new ErrorResponseDTO { Message = "Profil parent non trouvé." });
                }

                // Mise à jour du mot de passe si fourni
                if (!string.IsNullOrEmpty(updateDTO.CurrentPassword) && !string.IsNullOrEmpty(updateDTO.NewPassword))
                {
                    // Vérifier l'ancien mot de passe
                    var currentPasswordHash = Services.HasherPassword.HashPassword(updateDTO.CurrentPassword);
                    if (currentPasswordHash != parent.Password)
                    {
                        return BadRequest(new ErrorResponseDTO { Message = "Mot de passe actuel incorrect." });
                    }

                    // Mettre à jour le mot de passe
                    parent.Password = Services.HasherPassword.HashPassword(updateDTO.NewPassword);
                }

                // Mettre à jour l'email si fourni
                if (!string.IsNullOrEmpty(updateDTO.Email) && updateDTO.Email != parent.Email)
                {
                    // Vérifier si l'email est déjà utilisé
                    var emailExists = await _context.Users.AnyAsync(u => u.Email == updateDTO.Email && u.Id != parent.Id);
                    if (emailExists)
                    {
                        return BadRequest(new ErrorResponseDTO { Message = "Cet email est déjà utilisé." });
                    }

                    parent.Email = updateDTO.Email;
                }

                await _context.SaveChangesAsync();
                return Ok(new { Message = "Profil mis à jour avec succès." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 GET /api/parents/students → Récupérer les enfants du parent connecté
        [HttpGet("students")]
        public async Task<IActionResult> GetStudents()
        {
            try
            {
                var parentEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(parentEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var parent = await _context.Users
                    .Include(u => u.Students)
                    .ThenInclude(s => s.Class)
                    .FirstOrDefaultAsync(u => u.Email == parentEmail && u.Role == "parent");

                if (parent == null || !parent.Students.Any())
                {
                    return NotFound(new ErrorResponseDTO { Message = "Aucun élève trouvé pour ce parent." });
                }

                var students = parent.Students.Select(s => new
                {
                    Id = s.Id,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    Birthdate = s.Birthdate,
                    ClassId = s.ClassId,
                    ClassName = s.Class.Name
                }).ToList();

                return Ok(students);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 GET /api/parents/absences → Récupérer les absences des enfants du parent connecté
        [HttpGet("absences")]
        public IActionResult GetAbsences()
        {
            try
            {
                var parentEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                var absences = _parentService.GetParentAbsences(parentEmail);

                if (absences == null || absences.Count == 0)
                    return NotFound(new ErrorResponseDTO { Message = "Aucune absence trouvée." });

                return Ok(absences);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = "Erreur interne du serveur." });
            }
        }
        
        // 🔹 POST /api/parents/absences/{id}/justify → Justifier une absence
        [HttpPost("absences/{id}/justify")]
        public IActionResult JustifyAbsence(int id, [FromBody] JustifyAbsenceDTO justification)
        {
            try
            {
                var parentEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                var success = _parentService.JustifyAbsence(parentEmail, id, justification);

                if (!success)
                    return BadRequest(new ErrorResponseDTO { Message = "Justification impossible ou absence introuvable." });

                return Ok(new { Message = "Absence justifiée avec succès." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = "Erreur interne du serveur." });
            }
        }

        // 🔹 GET /api/parents/notifications → Récupérer les notifications du parent
        [HttpGet("notifications")]
        public IActionResult GetNotifications()
        {
            try
            {
                var parentEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                var notifications = _parentService.GetParentNotifications(parentEmail);

                if (notifications == null || notifications.Count == 0)
                    return NotFound(new ErrorResponseDTO { Message = "Aucune notification trouvée." });

                return Ok(notifications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = "Erreur interne du serveur." });
            }
        }

        // 🔹 PUT /api/parents/notifications/{id}/read → Marquer une notification comme lue
        [HttpPut("notifications/{id}/read")]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            try
            {
                var parentEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(parentEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var parent = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == parentEmail && u.Role == "parent");

                if (parent == null)
                {
                    return NotFound(new ErrorResponseDTO { Message = "Parent non trouvé." });
                }

                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == parent.Id);

                if (notification == null)
                {
                    return NotFound(new ErrorResponseDTO { Message = "Notification non trouvée." });
                }

                notification.IsRead = true;
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Notification marquée comme lue." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 GET /api/parents/stats/{studentId} → Récupérer les statistiques d'un élève
        [HttpGet("stats/{studentId}")]
        public IActionResult GetStudentStats(int studentId)
        {
            try
            {
                var parentEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                var stats = _parentService.GetStudentStats(parentEmail, studentId);

                if (stats == null)
                    return NotFound(new ErrorResponseDTO { Message = "Aucune statistique trouvée pour cet élève." });

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = "Erreur interne du serveur." });
            }
        }

        // 🔹 GET /api/parents/dashboard → Récupérer les données du tableau de bord
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var parentEmail = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(parentEmail))
                {
                    return Unauthorized(new ErrorResponseDTO { Message = "Utilisateur non authentifié." });
                }

                var parent = await _context.Users
                    .Include(u => u.Students)
                    .FirstOrDefaultAsync(u => u.Email == parentEmail && u.Role == "parent");

                if (parent == null || !parent.Students.Any())
                {
                    return NotFound(new ErrorResponseDTO { Message = "Aucun élève trouvé pour ce parent." });
                }

                var studentIds = parent.Students.Select(s => s.Id).ToList();

                // Récupérer les absences récentes
                var recentAbsences = await _context.Absences
                    .Include(a => a.Student)
                    .Where(a => studentIds.Contains(a.StudentId))
                    .OrderByDescending(a => a.AbsenceDate)
                    .Take(5)
                    .Select(a => new
                    {
                        Id = a.Id,
                        StudentName = $"{a.Student.FirstName} {a.Student.LastName}",
                        AbsenceDate = a.AbsenceDate,
                        Status = a.Status
                    })
                    .ToListAsync();

                // Récupérer les notifications non lues
                var unreadNotifications = await _context.Notifications
                    .Where(n => n.UserId == parent.Id && (n.IsRead == false || n.IsRead == null))
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
                    .Where(a => studentIds.Contains(a.StudentId))
                    .ToListAsync();

                var dashboardData = new
                {
                    TotalStudents = parent.Students.Count,
                    TotalAbsences = allAbsences.Count,
                    JustifiedAbsences = allAbsences.Count(a => a.Status == "justifiée"),
                    UnjustifiedAbsences = allAbsences.Count(a => a.Status == "non justifiée"),
                    PendingAbsences = allAbsences.Count(a => a.Status == "en attente"),
                    RecentAbsences = recentAbsences,
                    UnreadNotifications = unreadNotifications,
                    UnreadNotificationsCount = unreadNotifications.Count
                };

                return Ok(dashboardData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }
    }
}

