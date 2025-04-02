using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using gest_abs.Services;
using gest_abs.DTO;
using gest_abs.Models;
using System.Security.Claims;

namespace gest_abs.Controllers
{
  [Route("api/points")]
  [ApiController]
  [Authorize] // Tous les utilisateurs authentifiés peuvent accéder aux points
  public class PointsController : ControllerBase
  {
      private readonly GestionAbsencesContext _context;
      private readonly PointsService _pointsService;

      public PointsController(GestionAbsencesContext context, PointsService pointsService)
      {
          _context = context;
          _pointsService = pointsService;
      }

      // 🔹 GET /api/points/student/{id} → Récupérer les points d'un étudiant
      [HttpGet("student/{id}")]
      public async Task<IActionResult> GetStudentPoints(int id)
      {
          try
          {
              var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
              var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;

              // Vérifier les autorisations
              if (userRole == "eleve")
              {
                  // Un élève ne peut voir que ses propres points
                  var user = await _context.Users
                      .FirstOrDefaultAsync(u => u.Email == userEmail && u.Role == "eleve");

                  if (user == null)
                      return Unauthorized(new { Message = "Utilisateur non authentifié." });

                  var student = await _context.Students
                      .FirstOrDefaultAsync(s => s.UserId == user.Id);

                  if (student == null || student.Id != id)
                      return Forbid();
              }
              else if (userRole == "parent")
              {
                  // Un parent ne peut voir que les points de ses enfants
                  var user = await _context.Users
                      .Include(u => u.Students)
                      .FirstOrDefaultAsync(u => u.Email == userEmail && u.Role == "parent");

                  if (user == null)
                      return Unauthorized(new { Message = "Utilisateur non authentifié." });

                  var isParentOfStudent = user.Students.Any(s => s.Id == id);
                  if (!isParentOfStudent)
                      return Forbid();
              }
              else if (userRole == "professeur")
              {
                  // Un professeur ne peut voir que les points des élèves de ses classes
                  var teacher = await _context.Teachers
                      .Include(t => t.User)
                      .Include(t => t.Classes)
                      .ThenInclude(c => c.Students)
                      .FirstOrDefaultAsync(t => t.User.Email == userEmail);

                  if (teacher == null)
                      return Unauthorized(new { Message = "Utilisateur non authentifié." });

                  var isTeacherOfStudent = teacher.Classes.Any(c => c.Students.Any(s => s.Id == id));
                  if (!isTeacherOfStudent)
                      return Forbid();
              }

              var points = await _pointsService.GetStudentPoints(id);
              if (points == null)
              {
                  return NotFound(new { Message = "Points non trouvés pour cet élève." });
              }

              return Ok(points);
          }
          catch (Exception ex)
          {
              return StatusCode(500, new { Message = $"Erreur interne du serveur: {ex.Message}" });
          }
      }

      // 🔹 GET /api/points/class/{id} → Récupérer les points d'une classe
      [HttpGet("class/{id}")]
      [Authorize(Roles = "admin,professeur")] // Seuls les administrateurs et les professeurs peuvent voir les points d'une classe
      public async Task<IActionResult> GetClassPoints(int id)
      {
          try
          {
              var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
              var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;

              // Vérifier les autorisations pour les professeurs
              if (userRole == "professeur")
              {
                  var teacher = await _context.Teachers
                      .Include(t => t.User)
                      .Include(t => t.Classes)
                      .FirstOrDefaultAsync(t => t.User.Email == userEmail);

                  if (teacher == null)
                      return Unauthorized(new { Message = "Utilisateur non authentifié." });

                  var isTeacherOfClass = teacher.Classes.Any(c => c.Id == id);
                  if (!isTeacherOfClass)
                      return Forbid();
              }

              var points = await _pointsService.GetClassPoints(id);
              if (points == null)
              {
                  return NotFound(new { Message = "Points non trouvés pour cette classe." });
              }

              return Ok(points);
          }
          catch (Exception ex)
          {
              return StatusCode(500, new { Message = $"Erreur interne du serveur: {ex.Message}" });
          }
      }

      // 🔹 POST /api/points/student/{id}/add → Ajouter des points à un étudiant
      [HttpPost("student/{id}/add")]
      [Authorize(Roles = "admin,professeur")] // Seuls les administrateurs et les professeurs peuvent ajouter des points
      public async Task<IActionResult> AddPointsToStudent(int id, [FromBody] PointsAddDTO pointsDTO)
      {
          try
          {
              var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
              var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;

              // Vérifier les autorisations pour les professeurs
              if (userRole == "professeur")
              {
                  var teacher = await _context.Teachers
                      .Include(t => t.User)
                      .Include(t => t.Classes)
                      .ThenInclude(c => c.Students)
                      .FirstOrDefaultAsync(t => t.User.Email == userEmail);

                  if (teacher == null)
                      return Unauthorized(new { Message = "Utilisateur non authentifié." });

                  var isTeacherOfStudent = teacher.Classes.Any(c => c.Students.Any(s => s.Id == id));
                  if (!isTeacherOfStudent)
                      return Forbid();
              }

              var success = await _pointsService.AddPointsToStudent(id, pointsDTO);
              if (!success)
              {
                  return BadRequest(new { Message = "Impossible d'ajouter des points à cet élève." });
              }

              return Ok(new { Message = "Points ajoutés avec succès." });
          }
          catch (Exception ex)
          {
              return StatusCode(500, new { Message = $"Erreur interne du serveur: {ex.Message}" });
          }
      }

      // 🔹 GET /api/points/ranking/class/{id} → Récupérer le classement des élèves d'une classe
      [HttpGet("ranking/class/{id}")]
      public async Task<IActionResult> GetClassRanking(int id)
      {
          try
          {
              var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
              var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;

              // Vérifier les autorisations
              if (userRole == "eleve")
              {
                  // Un élève ne peut voir que le classement de sa propre classe
                  var user = await _context.Users
                      .FirstOrDefaultAsync(u => u.Email == userEmail && u.Role == "eleve");

                  if (user == null)
                      return Unauthorized(new { Message = "Utilisateur non authentifié." });

                  var student = await _context.Students
                      .FirstOrDefaultAsync(s => s.UserId == user.Id);

                  if (student == null || student.ClassId != id)
                      return Forbid();
              }
              else if (userRole == "parent")
              {
                  // Un parent ne peut voir que le classement des classes de ses enfants
                  var user = await _context.Users
                      .Include(u => u.Students)
                      .FirstOrDefaultAsync(u => u.Email == userEmail && u.Role == "parent");

                  if (user == null)
                      return Unauthorized(new { Message = "Utilisateur non authentifié." });

                  var isParentOfStudentInClass = user.Students.Any(s => s.ClassId == id);
                  if (!isParentOfStudentInClass)
                      return Forbid();
              }
              else if (userRole == "professeur")
              {
                  // Un professeur ne peut voir que le classement de ses classes
                  var teacher = await _context.Teachers
                      .Include(t => t.User)
                      .Include(t => t.Classes)
                      .FirstOrDefaultAsync(t => t.User.Email == userEmail);

                  if (teacher == null)
                      return Unauthorized(new { Message = "Utilisateur non authentifié." });

                  var isTeacherOfClass = teacher.Classes.Any(c => c.Id == id);
                  if (!isTeacherOfClass)
                      return Forbid();
              }

              var ranking = await _pointsService.GetClassRanking(id);
              if (ranking == null || !ranking.Any())
              {
                  return NotFound(new { Message = "Classement non trouvé pour cette classe." });
              }

              return Ok(ranking);
          }
          catch (Exception ex)
          {
              return StatusCode(500, new { Message = $"Erreur interne du serveur: {ex.Message}" });
          }
      }

      // 🔹 GET /api/points/ranking/global → Récupérer le classement global des élèves
      [HttpGet("ranking/global")]
      [Authorize(Roles = "admin,professeur")] // Seuls les administrateurs et les professeurs peuvent voir le classement global
      public async Task<IActionResult> GetGlobalRanking()
      {
          try
          {
              var ranking = await _pointsService.GetGlobalRanking();
              if (ranking == null || !ranking.Any())
              {
                  return NotFound(new { Message = "Classement global non trouvé." });
              }

              return Ok(ranking);
          }
          catch (Exception ex)
          {
              return StatusCode(500, new { Message = $"Erreur interne du serveur: {ex.Message}" });
          }
      }
  }
}

