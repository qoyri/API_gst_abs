using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using gest_abs.DTO;
using gest_abs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace gest_abs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin")]
    public class AdminController : ControllerBase
    {
        private readonly GestionAbsencesContext _context;

        public AdminController(GestionAbsencesContext context)
        {
            _context = context;
        }

        // GET: api/Admin/users
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<AdminDTO.UserDto>>> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            var userDtos = users.Select(u => new AdminDTO.UserDto
            {
                Id = u.Id,
                Email = u.Email,
                Role = u.Role,
                CreatedAt = u.CreatedAt
            }).ToList();

            return Ok(userDtos);
        }

        // GET: api/Admin/users/5
        [HttpGet("users/{id}")]
        public async Task<ActionResult<AdminDTO.UserDto>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            var userDto = new AdminDTO.UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            return Ok(userDto);
        }

        // POST: api/Admin/users
        [HttpPost("users")]
        public async Task<ActionResult<AdminDTO.UserDto>> CreateUser(AdminDTO.UserCreateDto userCreateDto)
        {
            var user = new User
            {
                Email = userCreateDto.Email,
                Role = userCreateDto.Role,
                Password = HashPassword(userCreateDto.Password), // Hasher le mot de passe
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userDto = new AdminDTO.UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userDto);
        }

        // PUT: api/Admin/users/5
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, AdminDTO.UserUpdateDto userUpdateDto)
        {
            if (id != userUpdateDto.Id)
            {
                return BadRequest("L'identifiant ne correspond pas.");
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.Email = userUpdateDto.Email;
            user.Role = userUpdateDto.Role;

            if (!string.IsNullOrWhiteSpace(userUpdateDto.Password))
            {
                user.Password = HashPassword(userUpdateDto.Password);
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Users.Any(u => u.Id == id))
                {
                    return NotFound();
                }

                throw;
            }

            return NoContent();
        }

        // DELETE: api/Admin/users/5
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Admin/users/5/reset-password
        [HttpPost("users/{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id, AdminDTO.ResetPasswordDto resetPasswordDto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            if (resetPasswordDto.NewPassword != resetPasswordDto.ConfirmPassword)
            {
                return BadRequest("Les mots de passe ne correspondent pas.");
            }

            user.Password = HashPassword(resetPasswordDto.NewPassword);
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Méthode utilitaire pour hasher le mot de passe en utilisant SHA-256
        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }
}