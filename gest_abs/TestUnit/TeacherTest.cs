using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using gest_abs.DTO;
using gest_abs.Models;
using gest_abs.Controllers;

namespace gest_abs_unit
{
    public class TeachersControllerTests
    {
        private GestionAbsencesContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<GestionAbsencesContext>()
                .UseInMemoryDatabase(databaseName: "TestDb") // In-Memory Database
                .Options;

            return new GestionAbsencesContext(options);
        }

        [Fact]
        public async Task CreateTeacher_ShouldReturn_CreatedTeacher()
        {
            // Arrange
            var context = CreateInMemoryContext();
            context.Database.EnsureDeleted(); // Nettoyer la base si nécessaire
            context.Database.EnsureCreated();

            // Ajouter un User dans la base pour la relation UserId
            context.Users.Add(new User
            {
                Id = 1,
                Email = "teacher@test.com",
                Password = "hashedpassword",
                Role = "teacher"
            });
            await context.SaveChangesAsync();

            var controller = new TeachersController(context);

            var teacherCreateDto = new TeacherDTO.TeacherCreateDto
            {
                UserId = 1, // User existant
                Subject = "Mathématiques"
            };

            // Act
            var actionResult = await controller.CreateTeacher(teacherCreateDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            var createdTeacher = Assert.IsType<TeacherDTO.TeacherDto>(createdResult.Value);

            Assert.Equal(teacherCreateDto.UserId, createdTeacher.UserId);
            Assert.Equal(teacherCreateDto.Subject, createdTeacher.Subject);
        }
    }
}