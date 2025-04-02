using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using gest_abs.Services;
using gest_abs.DTO;
using gest_abs.Models;
using System.Security.Claims;

namespace gest_abs.Controllers
{
    [Route("api/admin/config")]
    [ApiController]
    [Authorize(Roles = "admin")] // 🔹 Seuls les administrateurs peuvent accéder à ces endpoints
    public class AdminConfigController : ControllerBase
    {
        private readonly GestionAbsencesContext _context;
        private readonly AdminConfigService _configService;

        public AdminConfigController(GestionAbsencesContext context, AdminConfigService configService)
        {
            _context = context;
            _configService = configService;
        }

        // 🔹 GET /api/admin/config/alerts → Récupérer la configuration des alertes
        [HttpGet("alerts")]
        public async Task<IActionResult> GetAlertConfig()
        {
            try
            {
                var config = await _configService.GetAlertConfig();
                return Ok(config);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 PUT /api/admin/config/alerts → Mettre à jour la configuration des alertes
        [HttpPut("alerts")]
        public async Task<IActionResult> UpdateAlertConfig([FromBody] AlertConfigDTO configDTO)
        {
            try
            {
                var success = await _configService.UpdateAlertConfig(configDTO);
                if (!success)
                {
                    return BadRequest(new ErrorResponseDTO { Message = "Mise à jour de la configuration impossible." });
                }

                return Ok(new { Message = "Configuration des alertes mise à jour avec succès." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 GET /api/admin/config/points → Récupérer la configuration du système de points
        [HttpGet("points")]
        public async Task<IActionResult> GetPointsConfig()
        {
            try
            {
                var config = await _configService.GetPointsConfig();
                return Ok(config);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 PUT /api/admin/config/points → Mettre à jour la configuration du système de points
        [HttpPut("points")]
        public async Task<IActionResult> UpdatePointsConfig([FromBody] PointsSystemDTO configDTO)
        {
            try
            {
                var success = await _configService.UpdatePointsConfig(configDTO);
                if (!success)
                {
                    return BadRequest(new ErrorResponseDTO { Message = "Mise à jour de la configuration impossible." });
                }

                return Ok(new { Message = "Configuration du système de points mise à jour avec succès." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 POST /api/admin/config/database → Configurer la connexion à la base de données
        [HttpPost("database")]
        public async Task<IActionResult> ConfigureDatabase([FromBody] DatabaseConfigDTO configDTO)
        {
            try
            {
                var success = await _configService.UpdateDatabaseConfig(configDTO);
                if (!success)
                {
                    return BadRequest(new ErrorResponseDTO { Message = "Configuration de la base de données impossible." });
                }

                return Ok(new { Message = "Configuration de la base de données mise à jour avec succès." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }

        // 🔹 POST /api/admin/config/check-alerts → Vérifier et générer les alertes pour les absences dépassant le seuil
        [HttpPost("check-alerts")]
        public async Task<IActionResult> CheckAndGenerateAlerts()
        {
            try
            {
                var result = await _configService.CheckAndGenerateAbsenceAlerts();
                return Ok(new { 
                    Message = "Vérification des alertes terminée.", 
                    AlertsGenerated = result.AlertsGenerated,
                    StudentsWithExcessiveAbsences = result.StudentsWithExcessiveAbsences
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponseDTO { Message = $"Erreur interne du serveur: {ex.Message}" });
            }
        }
    }
}

