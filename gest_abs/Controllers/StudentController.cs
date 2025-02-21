using System.Linq;
using System.Threading.Tasks;
using gest_abs.DTO;
using gest_abs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace gest_abs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "professeur, admin")]
    public class StudentController : ControllerBase
    {
        private readonly GestionAbsencesContext _context;

        public StudentController(GestionAbsencesContext context)
        {
            _context = context;
        }

        // GET: api/student
        [HttpGet]
        public async Task<IActionResult> GetStudents()
        {
            var students = await _context.Students
                .Select(s => new StudentDTO
                {
                    Id = s.Id,
                    ClassId = s.ClassId,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    Birthdate = s.Birthdate
                })
                .ToListAsync();

            return Ok(students);
        }

        // GET: api/student/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetStudent(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
                return NotFound();

            var dto = new StudentDTO
            {
                Id = student.Id,
                ClassId = student.ClassId,
                FirstName = student.FirstName,
                LastName = student.LastName,
                Birthdate = student.Birthdate
            };

            return Ok(dto);
        }

        // POST: api/student
        [HttpPost]
        public async Task<IActionResult> CreateStudent([FromBody] StudentCreateDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var student = new Student
            {
                UserId = dto.UserId,
                ClassId = dto.ClassId,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Birthdate = dto.Birthdate
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            // Retourner l’étudiant créé au format DTO
            var studentDto = new StudentDTO
            {
                Id = student.Id,
                ClassId = student.ClassId,
                FirstName = student.FirstName,
                LastName = student.LastName,
                Birthdate = student.Birthdate
            };

            return CreatedAtAction(nameof(GetStudent), new { id = student.Id }, studentDto);
        }

        // PUT: api/student/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateStudent(int id, [FromBody] StudentUpdateDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var student = await _context.Students.FindAsync(id);
            if (student == null)
                return NotFound();

            student.ClassId = dto.ClassId;
            student.FirstName = dto.FirstName;
            student.LastName = dto.LastName;
            student.Birthdate = dto.Birthdate;

            _context.Students.Update(student);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/student/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
                return NotFound();

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}