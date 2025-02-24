using gest_abs.Models;
using gest_abs.DTO;
using Microsoft.EntityFrameworkCore;

namespace gest_abs.Services
{
    public class ParentService
    {
        private readonly GestionAbsencesContext _context;

        public ParentService(GestionAbsencesContext context)
        {
            _context = context;
        }

        // 🔹 Récupérer les absences des enfants d’un parent
        public List<ParentAbsenceDTO> GetParentAbsences(string parentEmail)
        {
            try
            {
                Console.WriteLine($"🔎 Recherche du parent avec email: {parentEmail}");

                var parent = _context.Users
                    .Include(p => p.Students)
                    .FirstOrDefault(u => u.Email == parentEmail);

                if (parent == null || !parent.Students.Any())
                {
                    Console.WriteLine("❌ Parent introuvable ou aucun élève associé.");
                    return new List<ParentAbsenceDTO>();
                }

                var studentIds = parent.Students.Select(s => s.Id).ToList();
                Console.WriteLine($"✅ Élèves liés : {string.Join(", ", studentIds)}");

                var absences = _context.Absences
                    .Where(a => studentIds.Contains(a.StudentId))
                    .Include(a => a.Student)
                    .ToList();

                if (!absences.Any())
                {
                    Console.WriteLine("❌ Aucune absence trouvée.");
                    return new List<ParentAbsenceDTO>();
                }

                Console.WriteLine($"✅ Absences trouvées : {absences.Count}");

                return absences.Select(a => new ParentAbsenceDTO
                {
                    Id = a.Id,
                    StudentName = $"{a.Student.FirstName} {a.Student.LastName}",
                    AbsenceDate = a.AbsenceDate.ToDateTime(TimeOnly.MinValue),
                    Reason = a.Reason ?? "Non spécifié",
                    Status = a.Status ?? "En attente"
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR : {ex.Message}");
                return new List<ParentAbsenceDTO>();
            }
        }

        // 🔹 Justifier une absence
        public bool JustifyAbsence(string parentEmail, int absenceId, JustifyAbsenceDTO justification)
        {
            try
            {
                Console.WriteLine($"🔎 Recherche du parent avec email: {parentEmail}");

                var parent = _context.Users
                    .Include(p => p.Students)
                    .FirstOrDefault(u => u.Email == parentEmail);

                if (parent == null || !parent.Students.Any())
                {
                    Console.WriteLine("❌ Parent introuvable ou aucun élève associé.");
                    return false;
                }

                var studentIds = parent.Students.Select(s => s.Id).ToList();
                Console.WriteLine($"✅ Élèves liés au parent: {string.Join(", ", studentIds)}");

                var absence = _context.Absences
                    .AsEnumerable() // Force le filtrage en mémoire pour éviter un bug EF Core
                    .FirstOrDefault(a => a.Id == absenceId && studentIds.Contains(a.StudentId));

                if (absence == null)
                {
                    Console.WriteLine($"❌ Absence ID {absenceId} introuvable.");
                    return false;
                }

                if (absence.Status == "justifiée")
                {
                    Console.WriteLine("❌ Absence déjà justifiée.");
                    return false;
                }

                absence.Status = "justifiée"; // Utiliser la bonne valeur ENUM
                absence.Document = justification.Document ?? "Aucun document fourni";

                _context.SaveChanges();
                _context.Database.CloseConnection(); // ✅ Fermeture de la connexion

                Console.WriteLine("✅ Absence justifiée avec succès.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la justification : {ex.Message}");
                return false;
            }
        }



        // 🔹 Récupérer les notifications non lues
        public List<NotificationDTO> GetParentNotifications(string parentEmail)
        {
            try
            {
                var parent = _context.Users.FirstOrDefault(u => u.Email == parentEmail);

                if (parent == null)
                {
                    Console.WriteLine("❌ Parent introuvable.");
                    return new List<NotificationDTO>();
                }

                return _context.Notifications
                    .Where(n => n.UserId == parent.Id && (n.IsRead == false || n.IsRead == null))
                    .OrderByDescending(n => n.CreatedAt ?? DateTime.MinValue)
                    .Select(n => new NotificationDTO
                    {
                        Id = n.Id,
                        Message = n.Message,
                        IsRead = n.IsRead ?? false,
                        CreatedAt = n.CreatedAt ?? DateTime.UtcNow
                    }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la récupération des notifications : {ex.Message}");
                return new List<NotificationDTO>();
            }
        }

        // 🔹 Récupérer les statistiques des absences d’un enfant
        public StatsDTO GetStudentStats(string parentEmail, int studentId)
        {
            try
            {
                var parent = _context.Users
                    .Include(p => p.Students)
                    .FirstOrDefault(u => u.Email == parentEmail);

                if (parent == null || !parent.Students.Any(s => s.Id == studentId))
                {
                    Console.WriteLine("❌ Parent introuvable ou accès refusé.");
                    return null;
                }

                var absences = _context.Absences
                    .Where(a => a.StudentId == studentId)
                    .ToList();

                if (!absences.Any())
                {
                    Console.WriteLine("❌ Aucune absence trouvée pour cet élève.");
                    return null;
                }

                Console.WriteLine($"✅ Statistiques récupérées pour l'élève {studentId}.");

                return new StatsDTO
                {
                    StudentId = studentId,
                    StudentName = _context.Students
                        .Where(s => s.Id == studentId)
                        .Select(s => s.FirstName + " " + s.LastName)
                        .FirstOrDefault(),
                    TotalAbsences = absences.Count,
                    JustifiedAbsences = absences.Count(a => a.Status == "justifié"),
                    UnjustifiedAbsences = absences.Count(a => a.Status == "non justifié"),
                    AbsencesByMonth = absences
                        .GroupBy(a => a.AbsenceDate.Month)
                        .ToDictionary(g => g.Key.ToString(), g => g.Count())
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la récupération des statistiques : {ex.Message}");
                return null;
            }
        }
    }
}
