using gest_abs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using gest_abs.DTO;

namespace gest_abs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly GestionAbsencesContext _context;

        public AuthController(IConfiguration configuration, GestionAbsencesContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] UserLoginDTO userLoginDTO)
        {
            // Rechercher l'utilisateur dans la base de données par Email/Username
            var user = _context.Users.SingleOrDefault(u => u.Email == userLoginDTO.Username);
            if (user == null)
                return Unauthorized(new ErrorResponseDTO
                {
                    Message = "Nom d'utilisateur ou mot de passe incorrect."
                });

            // Vérification du mot de passe haché
            if (!VerifyPassword(userLoginDTO.Password, user.Password))
                return Unauthorized(new ErrorResponseDTO
                {
                    Message = "Nom d'utilisateur ou mot de passe incorrect."
                });

            // Générer un token JWT pour l'utilisateur
            var token = GenerateJwtToken(user.Email, user.Role);

            // Retourner l'utilisateur et le token via un DTO
            var response = new AuthResponseDTO
            {
                Token = token,
                Username = user.Email,
                Role = user.Role
            };

            return Ok(response);
        }

        private string GenerateJwtToken(string username, string role)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim("role", role) // Ajouter le rôle en minuscule
            };

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
            return inputPasswordHashString == hashedPassword;
        }
    }
}