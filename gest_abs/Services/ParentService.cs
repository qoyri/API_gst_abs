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

        // 🔹 Récupérer les absences des enfants d'un parent
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
                absence.Reason = justification.Reason;
                absence.Document = justification.Document ?? "Aucun document fourni";
                absence.UpdatedAt = DateTime.UtcNow;

                _context.SaveChanges();
                _context.Database.CloseConnection(); // ✅ Fermeture de la connexion

                // Créer une notification pour informer les enseignants
                var student = _context.Students
                    .Include(s => s.Class)
                    .ThenInclude(c => c.Teacher)
                    .FirstOrDefault(s => s.Id == absence.StudentId);

                if (student?.Class?.Teacher != null)
                {
                    var teacherId = student.Class.Teacher.UserId;
                    var notification = new Notification
                    {
                        UserId = teacherId,
                        Message = $"L'absence de {student.FirstName} {student.LastName} du {absence.AbsenceDate.ToString("dd/MM/yyyy")} a été justifiée.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Notifications.Add(notification);
                    _context.SaveChanges();
                }

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
                    .Where(n => n.UserId == parent.Id)
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

        // 🔹 Récupérer les statistiques des absences d'un enfant
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

                var student = _context.Students
                    .Include(s => s.Class)
                    .FirstOrDefault(s => s.Id == studentId);

                if (student == null)
                {
                    Console.WriteLine("❌ Étudiant introuvable.");
                    return null;
                }

                var absences = _context.Absences
                    .Where(a => a.StudentId == studentId)
                    .ToList();

                if (!absences.Any())
                {
                    Console.WriteLine("❌ Aucune absence trouvée pour cet élève.");
                    // Retourner des statistiques vides plutôt que null
                    return new StatsDTO
                    {
                        StudentId = studentId,
                        StudentName = $"{student.FirstName} {student.LastName}",
                        TotalAbsences = 0,
                        JustifiedAbsences = 0,
                        UnjustifiedAbsences = 0,
                        AbsencesByMonth = new Dictionary<string, int>()
                    };
                }

                Console.WriteLine($"✅ Statistiques récupérées pour l'élève {studentId}.");

                // Calculer les absences par mois avec des noms de mois
                var absencesByMonth = absences
                    .GroupBy(a => a.AbsenceDate.Month)
                    .ToDictionary(
                        g => GetMonthName(g.Key), 
                        g => g.Count()
                    );

                return new StatsDTO
                {
                    StudentId = studentId,
                    StudentName = $"{student.FirstName} {student.LastName}",
                    TotalAbsences = absences.Count,
                    JustifiedAbsences = absences.Count(a => a.Status == "justifiée"),
                    UnjustifiedAbsences = absences.Count(a => a.Status == "non justifiée"),
                    AbsencesByMonth = absencesByMonth
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la récupération des statistiques : {ex.Message}");
                return null;
            }
        }

        // Méthode utilitaire pour obtenir le nom du mois
        private string GetMonthName(int month)
        {
            return month switch
            {
                1 => "Janvier",
                2 => "Février",
                3 => "Mars",
                4 => "Avril",
                5 => "Mai",
                6 => "Juin",
                7 => "Juillet",
                8 => "Août",
                9 => "Septembre",
                10 => "Octobre",
                11 => "Novembre",
                12 => "Décembre",
                _ => "Inconnu"
            };
        }

        // 🔹 Récupérer les enfants d'un parent
        public List<StudentDTO> GetParentStudents(string parentEmail)
        {
            try
            {
                var parent = _context.Users
                    .Include(p => p.Students)
                    .ThenInclude(s => s.Class)
                    .FirstOrDefault(u => u.Email == parentEmail);

                if (parent == null || !parent.Students.Any())
                {
                    Console.WriteLine("❌ Parent introuvable ou aucun élève associé.");
                    return new List<StudentDTO>();
                }

                return parent.Students.Select(s => new StudentDTO
                {
                    Id = s.Id,
                    ClassId = s.ClassId,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    Birthdate = s.Birthdate
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la récupération des élèves : {ex.Message}");
                return new List<StudentDTO>();
            }
        }
    }
}

