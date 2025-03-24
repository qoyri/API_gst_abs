using System.Security.Cryptography;
using System.Text;
using gest_abs.Models;
using static gest_abs.Services.HasherPassword;

namespace gest_abs;

public static class DbInitializer
{
    public static void Initialize(GestionAbsencesContext context)
    {
        context.Database.EnsureCreated();

        if (context.Users.Any(u => u.Email == "admin"))
        {
            return;
        }

        var adminUser = new User
        {
            Email = "admin",
            Role = "admin",
            Password = HashPassword("admin123")
        };
    
        var parentUser = new User
        {
            Email = "parent@parent.fr",
            Role = "parent",
            Password = HashPassword("parent123")
        };
    
        var teacherUser = new User
        {
            Email = "teacher@teacher.fr",
            Role = "professeur",
            Password = HashPassword("teacher123")
        };
    
        var studentUser = new User
        {
            Email = "student@student.fr",
            Role = "eleve",
            Password = HashPassword("student123")
        };

        context.Users.AddRange(adminUser, parentUser, teacherUser, studentUser);
        context.SaveChanges();
    }
    
}