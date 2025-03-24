using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using gest_abs.Services;
using gest_abs.DTO;
using System.Security.Claims;

namespace gest_abs.Controllers
{
    [Route("api/parents")]
    [ApiController]
    [Authorize(Roles = "parent")] // 🔹 Seuls les parents peuvent accéder à ces endpoints
    public class ParentsController : ControllerBase
    {
        private readonly ParentService _parentService;

        public ParentsController(ParentService parentService)
        {
            _parentService = parentService;
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

    }
}