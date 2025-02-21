using System.Collections.Generic;
using System.Linq;
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
    [Authorize]
    public class TeachersController : ControllerBase
    {
        private readonly GestionAbsencesContext _context;

        public TeachersController(GestionAbsencesContext context)
        {
            _context = context;
        }

        // GET: api/Teachers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TeacherDTO.TeacherDto>>> GetTeachers()
        {
            var teachers = await _context.Teachers
                .Include(t => t.User)
                .ToListAsync();

            var teacherDtos = teachers.Select(t => new TeacherDTO.TeacherDto
            {
                Id = t.Id,
                UserId = t.UserId,
                Subject = t.Subject,
                Email = t.User.Email
            }).ToList();

            return Ok(teacherDtos);
        }

        // GET: api/Teachers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TeacherDTO.TeacherDto>> GetTeacher(int id)
        {
            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (teacher == null)
            {
                return NotFound();
            }

            var teacherDto = new TeacherDTO.TeacherDto
            {
                Id = teacher.Id,
                UserId = teacher.UserId,
                Subject = teacher.Subject,
                Email = teacher.User.Email
            };

            return Ok(teacherDto);
        }

        // POST: api/Teachers
        // Seul un administrateur peut créer un enseignant
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<TeacherDTO.TeacherDto>> CreateTeacher(TeacherDTO.TeacherCreateDto teacherCreateDto)
        {
            var teacher = new Teacher
            {
                UserId = teacherCreateDto.UserId,
                Subject = teacherCreateDto.Subject
            };

            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            // Recharger l'entité pour obtenir les données liées
            teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == teacher.Id);

            var teacherDto = new TeacherDTO.TeacherDto
            {
                Id = teacher.Id,
                UserId = teacher.UserId,
                Subject = teacher.Subject,
                Email = teacher.User.Email
            };

            return CreatedAtAction(nameof(GetTeacher), new { id = teacher.Id }, teacherDto);
        }

        // PUT: api/Teachers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTeacher(int id, TeacherDTO.TeacherUpdateDto teacherUpdateDto)
        {
            if (id != teacherUpdateDto.Id)
            {
                return BadRequest("L'identifiant ne correspond pas.");
            }

            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher == null)
            {
                return NotFound();
            }

            teacher.Subject = teacherUpdateDto.Subject;
            teacher.UserId = teacherUpdateDto.UserId;

            _context.Entry(teacher).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TeacherExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Teachers/5
        // Seul un administrateur peut supprimer un enseignant
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteTeacher(int id)
        {
            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher == null)
            {
                return NotFound();
            }

            _context.Teachers.Remove(teacher);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TeacherExists(int id)
        {
            return _context.Teachers.Any(e => e.Id == id);
        }
    }
}