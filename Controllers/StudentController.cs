using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using gest_abs.Models;
using gest_abs.DTO;
using System.Linq;

namespace gest_abs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]  // Protège l'accès aux routes du contrôleur avec l'authentification JWT
    public class StudentController : ControllerBase
    {
        private readonly GestionAbsencesContext _context;

        public StudentController(GestionAbsencesContext context)
        {
            _context = context;
        }

        // Créer un étudiant
        [HttpPost]
        public IActionResult CreateStudent([FromBody] StudentCreateDTO studentCreateDTO)
        {
            if (studentCreateDTO == null)
            {
                return BadRequest(new ErrorResponseDTO { Message = "Données invalides." });
            }

            // Vérifier si l'utilisateur existe déjà
            var existingUser = _context.Users.FirstOrDefault(u => u.Id == studentCreateDTO.UserId);
            if (existingUser == null)
            {
                return NotFound(new ErrorResponseDTO { Message = "L'utilisateur associé n'existe pas." });
            }

            // Vérifier si la classe existe
            var existingClass = _context.Classes.FirstOrDefault(c => c.Id == studentCreateDTO.ClassId);
            if (existingClass == null)
            {
                return NotFound(new ErrorResponseDTO { Message = "La classe associée n'existe pas." });
            }

            // Créer un nouvel étudiant à partir du DTO
            var student = new Student
            {
                UserId = studentCreateDTO.UserId,
                ClassId = studentCreateDTO.ClassId,
                FirstName = studentCreateDTO.FirstName,
                LastName = studentCreateDTO.LastName,
                Birthdate = studentCreateDTO.Birthdate,  // Directement utilisé comme DateTime?
                Parents = new List<User>() // Assure-toi que la liste des parents est bien initialisée
            };

            // Ajouter un parent si nécessaire
            if (studentCreateDTO.ParentId.HasValue)
            {
                var parent = _context.Users.FirstOrDefault(u => u.Id == studentCreateDTO.ParentId.Value);
                if (parent != null)
                {
                    student.Parents.Add(parent); // Ajouter un parent si l'ID est valide
                }
            }

            _context.Students.Add(student);
            _context.SaveChanges();

            // Retourner l'étudiant créé sous forme de DTO
            var studentDTO = new StudentDTO
            {
                Id = student.Id,
                UserId = student.UserId,
                ClassId = student.ClassId,
                FirstName = student.FirstName,
                LastName = student.LastName,
                Birthdate = student.Birthdate,  // Utiliser directement DateTime?
                ParentId = student.Parents.FirstOrDefault()?.Id // Si un parent est associé, renvoyer son ID
            };

            return CreatedAtAction(nameof(GetStudent), new { id = student.Id }, studentDTO);
        }

        // Récupérer un étudiant par son ID
        [HttpGet("{id}")]
        public IActionResult GetStudent(int id)
        {
            var student = _context.Students.FirstOrDefault(s => s.Id == id);

            if (student == null)
            {
                return NotFound(new ErrorResponseDTO { Message = "Étudiant non trouvé." });
            }

            return Ok(student); // Retourne l'étudiant trouvé
        }

        // Récupérer toutes les absences d'un étudiant
        [HttpGet("{id}/absences")]
        public IActionResult GetStudentAbsences(int id)
        {
            var absences = _context.Absences.Where(a => a.StudentId == id).ToList();
            if (absences == null || !absences.Any())
            {
                return NotFound(new ErrorResponseDTO { Message = "Aucune absence trouvée pour cet étudiant." });
            }

            return Ok(absences);
        }

        // Justifier une absence spécifique
        [HttpPost("{id}/absences/justify")]
        public IActionResult JustifyAbsence(int id, [FromBody] AbsenceJustificationDTO justificationDTO)
        {
            // Recherche de l'absence
            var absence = _context.Absences.FirstOrDefault(a => a.StudentId == id && a.AbsenceDate == justificationDTO.AbsenceDate);

            if (absence == null)
            {
                return NotFound(new ErrorResponseDTO { Message = "Absence non trouvée pour cet étudiant à cette date." });
            }

            // Mise à jour de l'absence avec la justification
            absence.Status = "Justifiée";
            absence.Reason = justificationDTO.Reason;
            absence.Document = justificationDTO.Document; // Si un justificatif est fourni
            absence.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();

            return Ok(new { Message = "Absence justifiée avec succès." });
        }

        // Afficher un classement des étudiants les plus présents (en fonction du nombre d'absences)
        [HttpGet("ranking")]
        public IActionResult GetAbsencesRanking()
        {
            var ranking = _context.Students
                .Select(s => new
                {
                    Student = s,
                    TotalAbsences = _context.Absences.Count(a => a.StudentId == s.Id && a.Status != "Justifiée") // Comptabilise seulement les absences non justifiées
                })
                .OrderBy(r => r.TotalAbsences)  // Classement par nombre d'absences
                .ToList();

            return Ok(ranking);
        }
    }
}
