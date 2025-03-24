using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using gest_abs.Models;
using gest_abs.DTO;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace gest_abs.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "admin")] // 🔹 Seuls les admins peuvent gérer les parents
    public class AdminController : ControllerBase
    {
        private readonly GestionAbsencesContext _context;

        public AdminController(GestionAbsencesContext context)
        {
            _context = context;
        }

        // 🔹 GET /api/admin/parents → Lister tous les parents
        [HttpGet("parents")]
        public IActionResult GetAllParents()
        {
            var parents = _context.Users
                .Where(u => u.Role == "parent")
                .Select(p => new ParentDTO
                {
                    Id = p.Id,
                    Email = p.Email,
                    CreatedAt = p.CreatedAt.HasValue ? p.CreatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "N/A"
                })
                .ToList();

            return Ok(parents);
        }

        // 🔹 GET /api/admin/parents/{id} → Voir les détails d’un parent
        [HttpGet("parents/{id}")]
        public IActionResult GetParentById(int id)
        {
            var parent = _context.Users
                .Where(u => u.Id == id && u.Role == "parent")
                .Select(p => new ParentDTO
                {
                    Id = p.Id,
                    Email = p.Email,
                    CreatedAt = p.CreatedAt.HasValue ? p.CreatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "N/A"
                })
                .FirstOrDefault();

            if (parent == null)
                return NotFound(new ErrorResponseDTO { Message = "Parent non trouvé." });

            return Ok(parent);
        }

        // 🔹 POST /api/admin/parents → Créer un parent
        [HttpPost("parents")]
        public IActionResult CreateParent([FromBody] ParentDTO newParentDTO)
        {
            if (_context.Users.Any(u => u.Email == newParentDTO.Email))
                return Conflict(new ErrorResponseDTO { Message = "Cet email est déjà utilisé." });

            if (!new EmailAddressAttribute().IsValid(newParentDTO.Email))
                return BadRequest(new ErrorResponseDTO { Message = "Email invalide." });

            var newUser = new User
            {
                Email = newParentDTO.Email,
                Role = "parent",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            return CreatedAtAction(nameof(GetParentById), new { id = newUser.Id }, newParentDTO);
        }

        // 🔹 PUT /api/admin/parents/{id} → Modifier un parent
        [HttpPut("parents/{id}")]
        public IActionResult UpdateParent(int id, [FromBody] ParentDTO updatedParentDTO)
        {
            var existingParent = _context.Users.FirstOrDefault(u => u.Id == id && u.Role == "parent");

            if (existingParent == null)
                return NotFound(new ErrorResponseDTO { Message = "Parent non trouvé." });

            if (!new EmailAddressAttribute().IsValid(updatedParentDTO.Email))
                return BadRequest(new ErrorResponseDTO { Message = "Email invalide." });

            existingParent.Email = updatedParentDTO.Email;
            _context.SaveChanges();

            return Ok(new { Message = "Parent mis à jour avec succès." });
        }

        // 🔹 DELETE /api/admin/parents/{id} → Supprimer un parent
        // 🔹 DELETE /api/admin/parents/{id} → Supprimer un parent
        [HttpDelete("parents/{id}")]
        public IActionResult DeleteParent(int id)
        {
            var parent = _context.Users.FirstOrDefault(u => u.Id == id && u.Role == "parent");

            if (parent == null)
                return NotFound(new ErrorResponseDTO { Message = "Parent non trouvé." });

            // 🔹 Vérifier si des élèves sont liés à ce parent
            var students = _context.Students.Where(s => s.Parents.Contains(parent)).ToList();

            if (students.Any())
            {
                // 🔹 Détacher les enfants en mettant parent_id = NULL
                foreach (var student in students)
                {
                    student.Parents.Remove(parent);
                }
            }

            _context.Users.Remove(parent);
            _context.SaveChanges();

            return Ok(new { Message = "Parent supprimé avec succès." });
        }

    }
}
