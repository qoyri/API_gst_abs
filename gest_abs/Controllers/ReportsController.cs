using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using gest_abs.Services;
using gest_abs.DTO;
using System.Security.Claims;

namespace gest_abs.Controllers
{
    [Route("api/reports")]
    [ApiController]
    [Authorize] // Tous les utilisateurs authentifiés peuvent accéder aux rapports
    public class ReportsController : ControllerBase
    {
        private readonly AdminConfigService _configService;

        public ReportsController(AdminConfigService configService)
        {
            _configService = configService;
        }

        // 🔹 POST /api/reports/export → Générer et exporter un rapport
        [HttpPost("export")]
        public async Task<IActionResult> ExportReport([FromBody] ReportExportDTO reportDto)
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                
                // Vérifier les autorisations selon le type de rapport
                if (reportDto.ReportType == "Global" && userRole != "admin")
                {
                    return Forbid();
                }

                if (reportDto.ReportType == "Teacher" && userRole != "admin" && userRole != "professeur")
                {
                    return Forbid();
                }

                var reportData = await _configService.GenerateReport(reportDto);
                if (reportData == null)
                {
                    return NotFound(new ErrorResponseDTO { Message = "Impossible de générer le rapport demandé." });
                }

                // Déterminer le type de contenu et le nom du fichier
                string contentType;
                string fileName;

                switch (reportDto.Format.ToLower())
                {
                    case "pdf":
                        contentType = "application/pdf";
                        fileName = $"rapport_{reportDto.ReportType.ToLower()}_{DateTime.Now:yyyyMMdd}.pdf";
                        break;
                    case "excel":
                        contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        fileName = $"rapport_{reportDto.ReportType.ToLower()}_{DateTime.Now:yyyyMMdd}.xlsx";
                        break;
                    case "csv":
                        contentType = "text/csv";
                        fileName = $"rapport_{reportDto.ReportType.ToLower()}_{DateTime.Now:yyyyMMdd}.csv";
                        break;
                    default:
                        contentType = "text/plain";
                        fileName = $"rapport_{reportDto.ReportType.ToLower()}_{DateTime.Now:yyyyMMdd}.txt";
                        break;
                }

                return File(reportData, contentType, fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 GET /api/reports/templates → Récupérer les modèles de rapports disponibles
        [HttpGet("templates")]
        public IActionResult GetReportTemplates()
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var templates = new List<object>();

                // Ajouter les modèles selon le rôle de l'utilisateur
                if (userRole == "admin")
                {
                    templates.Add(new { Id = "global_monthly", Name = "Rapport mensuel global", Type = "Global" });
                    templates.Add(new { Id = "global_yearly", Name = "Rapport annuel global", Type = "Global" });
                    templates.Add(new { Id = "teacher_performance", Name = "Performance des enseignants", Type = "Teacher" });
                }

                if (userRole == "admin" || userRole == "professeur")
                {
                    templates.Add(new { Id = "class_attendance", Name = "Présence par classe", Type = "Class" });
                    templates.Add(new { Id = "class_ranking", Name = "Classement des élèves", Type = "Class" });
                }

                if (userRole == "admin" || userRole == "professeur" || userRole == "parent" || userRole == "eleve")
                {
                    templates.Add(new { Id = "student_attendance", Name = "Présence d'un élève", Type = "Student" });
                    templates.Add(new { Id = "student_performance", Name = "Performance d'un élève", Type = "Student" });
                }

                return Ok(templates);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }
    }
}

