using Microsoft.AspNetCore.Mvc;
using gest_abs.Models;
using gest_abs.DTO;
using Microsoft.EntityFrameworkCore;

namespace gest_abs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AbsenceController : ControllerBase
    {
        private readonly GestionAbsencesContext _context;

        public AbsenceController(GestionAbsencesContext context)
        {
            _context = context;
        }

        // POST: api/Absence
        [HttpPost]
        public async Task<IActionResult> AddAbsence([FromBody] AbsenceDTO absenceDTO)
        {
            if (absenceDTO == null)
            {
                return BadRequest("Absence data is required.");
            }

            // Vérifier si l'étudiant existe
            var student = await _context.Students.FindAsync(absenceDTO.StudentId);
            if (student == null)
            {
                return NotFound("Student not found.");
            }

            // Convertir AbsenceDTO en Absence (pour la base de données)
            var absence = new Absence
            {
                StudentId = absenceDTO.StudentId,
                AbsenceDate = absenceDTO.AbsenceDate,
                Reason = absenceDTO.Reason,
                Status = absenceDTO.Status,
                Document = absenceDTO.Document,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            // Ajouter l'absence
            _context.Absences.Add(absence);
            await _context.SaveChangesAsync();

            // Retourner la réponse avec le DTO
            var responseDTO = new AbsenceDTO
            {
                Id = absence.Id,
                StudentId = absence.StudentId,
                AbsenceDate = absence.AbsenceDate,
                Reason = absence.Reason,
                Status = absence.Status,
                Document = absence.Document,
                CreatedAt = absence.CreatedAt,
                UpdatedAt = absence.UpdatedAt
            };

            return CreatedAtAction(nameof(GetAbsenceById), new { id = absence.Id }, responseDTO);
        }

        // GET: api/Absence/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AbsenceDTO>> GetAbsenceById(int id)
        {
            var absence = await _context.Absences.FindAsync(id);

            if (absence == null)
            {
                return NotFound();
            }

            // Retourner uniquement les informations d'absence en DTO
            var absenceDTO = new AbsenceDTO
            {
                Id = absence.Id,
                StudentId = absence.StudentId,
                AbsenceDate = absence.AbsenceDate,
                Reason = absence.Reason,
                Status = absence.Status,
                Document = absence.Document,
                CreatedAt = absence.CreatedAt,
                UpdatedAt = absence.UpdatedAt
            };

            return Ok(absenceDTO);
        }
    }
}
