using gest_abs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using gest_abs.DTO;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;

namespace gest_abs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly GestionAbsencesContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IConfiguration configuration, GestionAbsencesContext context, ILogger<AuthController> logger)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] UserLoginDTO userLoginDTO)
        {
            _logger.LogInformation($"Tentative de connexion pour l'utilisateur: {userLoginDTO.Username}");

            // Rechercher l'utilisateur dans la base de données par Email/Username
            var user = _context.Users.SingleOrDefault(u => u.Email == userLoginDTO.Username);
            
            if (user == null)
            {
                _logger.LogWarning($"Utilisateur non trouvé: {userLoginDTO.Username}");
                return Unauthorized(new ErrorResponseDTO
                {
                    Message = "Nom d'utilisateur ou mot de passe incorrect."
                });
            }

            _logger.LogInformation($"Utilisateur trouvé: {user.Email}, Rôle: {user.Role}");

            // Vérification du mot de passe haché
            bool passwordValid = VerifyPassword(userLoginDTO.Password, user.Password);
            _logger.LogInformation($"Vérification du mot de passe: {(passwordValid ? "Réussie" : "Échouée")}");

            if (!passwordValid)
                return Unauthorized(new ErrorResponseDTO
                {
                    Message = "Nom d'utilisateur ou mot de passe incorrect."
                });

            // Générer un token JWT pour l'utilisateur
            var token = GenerateJwtToken(user.Email, user.Role);
            _logger.LogInformation($"Token généré pour l'utilisateur: {user.Email}");

            // Décoder le token pour vérifier les claims
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            
            foreach (var claim in jwtToken.Claims)
            {
                _logger.LogInformation($"Claim dans le token: {claim.Type} = {claim.Value}");
            }

            // Retourner l'utilisateur et le token via un DTO
            var response = new AuthResponseDTO
            {
                Token = token,
                Username = user.Email,
                Role = user.Role
            };

            return Ok(response);
        }

        // Endpoint de test pour vérifier l'authentification et l'autorisation
        [HttpGet("test")]
        [Authorize]
        public IActionResult TestAuth()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            var claims = identity?.Claims.ToList();
            var response = new
            {
                IsAuthenticated = identity?.IsAuthenticated ?? false,
                Username = identity?.Name,
                Claims = claims?.Select(c => new { c.Type, c.Value }).ToList()
            };
            
            _logger.LogInformation($"Test d'authentification pour l'utilisateur: {identity?.Name}");
            foreach (var claim in claims ?? new List<Claim>())
            {
                _logger.LogInformation($"Claim reçue: {claim.Type} = {claim.Value}");
            }
            
            return Ok(response);
        }

        // Endpoint de test pour vérifier l'autorisation avec différents rôles
        [HttpGet("test-admin")]
        [Authorize(Roles = "admin")]
        public IActionResult TestAdmin()
        {
            return Ok(new { Message = "Vous êtes authentifié en tant qu'admin" });
        }

        [HttpGet("test-professeur")]
        [Authorize(Roles = "professeur")]
        public IActionResult TestProfesseur()
        {
            return Ok(new { Message = "Vous êtes authentifié en tant que professeur" });
        }

        [HttpGet("test-parent")]
        [Authorize(Roles = "parent")]
        public IActionResult TestParent()
        {
            return Ok(new { Message = "Vous êtes authentifié en tant que parent" });
        }

        [HttpGet("test-eleve")]
        [Authorize(Roles = "eleve")]
        public IActionResult TestEleve()
        {
            return Ok(new { Message = "Vous êtes authentifié en tant qu'élève" });
        }

        private string GenerateJwtToken(string username, string role)
        {
            _logger.LogInformation($"Génération du token pour: {username} avec le rôle: {role}");
            
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, username),
                // Ajouter le rôle de plusieurs façons pour s'assurer qu'il est correctement reconnu
                new Claim(ClaimTypes.Role, role),
                new Claim("role", role),
                new Claim("roles", role)
            };

            _logger.LogInformation($"Claims ajoutées au token: {string.Join(", ", claims.Select(c => $"{c.Type}={c.Value}"))}");

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["DurationInMinutes"])),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private bool VerifyPassword(string inputPassword, string hashedPassword)
        {
            using var sha256 = SHA256.Create();
            var inputPasswordHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(inputPassword));
            var inputPasswordHashString = BitConverter.ToString(inputPasswordHash).Replace("-", "").ToLower();
            
            _logger.LogDebug($"Hash du mot de passe saisi: {inputPasswordHashString}");
            _logger.LogDebug($"Hash stocké: {hashedPassword}");
            
            return inputPasswordHashString == hashedPassword;
        }
    }
}

