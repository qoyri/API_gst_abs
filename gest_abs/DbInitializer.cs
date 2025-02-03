using System.Security.Cryptography;
using System.Text;
using gest_abs.Models;

namespace gest_abs;

public static class DbInitializer
{
    public static void Initialize(GestionAbsencesContext context)
    {
        // Vérifier qu'il y a bien une base de données connectée
        context.Database.EnsureCreated();

        // Vérifier si l'utilisateur "Admin" existe déjà
        if (context.Users.Any(u => u.Email == "admin"))
        {
            return; // Si l'administrateur existe déjà, arrêter l'initialisation
        }

        // Créer un nouvel utilisateur Admin par défaut
        var adminUser = new User
        {
            Email = "admin",
            Role = "admin",
            Password = HashPassword("admin123") // Mot de passe hashé
        };

        context.Users.Add(adminUser);
        context.SaveChanges();
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create(); // Utilisation de l'algorithme SHA-256
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return BitConverter.ToString(bytes).Replace("-", "").ToLower(); // Convertir le hachage en chaîne hexadécimale
    }
}