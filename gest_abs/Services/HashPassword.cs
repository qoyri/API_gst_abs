using System.Security.Cryptography;
using System.Text;

namespace gest_abs.Services;

public class HasherPassword
{
    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create(); // Utilisation de l'algorithme SHA-256
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return BitConverter.ToString(bytes).Replace("-", "").ToLower(); // Convertir le hachage en chaîne hexadécimale
    }
}