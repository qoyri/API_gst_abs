using gest_abs.Models;
using gest_abs.DTO;
using Microsoft.EntityFrameworkCore;

namespace gest_abs.Services
{
    public class StudentService
    {
        private readonly GestionAbsencesContext _context;

        public StudentService(GestionAbsencesContext context)
        {
            _context = context;
        }

        // 🔹 Récupérer les absences d'un élève
        public async Task<List<StudentAbsenceDTO>> GetStudentAbsences(string studentEmail)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == studentEmail && u.Role == "eleve");

                if (user == null)
                {
                    return null;
                }

                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.UserId == user.Id);

                if (student == null)
                {
                    return null;
                }

                var absences = await _context.Absences
                    .Where(a => a.StudentId == student.Id)
                    .OrderByDescending(a => a.AbsenceDate)
                    .Select(a => new StudentAbsenceDTO
                    {
                        Id = a.Id,
                        AbsenceDate = a.AbsenceDate,
                        Status = a.Status ?? "en attente",
                        Reason = a.Reason ?? "Non spécifié",
                        Document = a.Document ?? "Aucun document"
                    })
                    .ToListAsync();

                return absences;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la récupération des absences : {ex.Message}");
                return null;
            }
        }

        // 🔹 Récupérer les détails d'une absence
        public async Task<StudentAbsenceDetailDTO> GetAbsenceDetails(string studentEmail, int absenceId)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == studentEmail && u.Role == "eleve");

                if (user == null)
                {
                    return null;
                }

                var student = await _context.Students
                    .Include(s => s.Class)
                    .FirstOrDefaultAsync(s => s.UserId == user.Id);

                if (student == null)
                {
                    return null;
                }

                var absence = await _context.Absences
                    .FirstOrDefaultAsync(a => a.Id == absenceId && a.StudentId == student.Id);

                if (absence == null)
                {
                    return null;
                }

                return new StudentAbsenceDetailDTO
                {
                    Id = absence.Id,
                    AbsenceDate = absence.AbsenceDate,
                    Status = absence.Status ?? "en attente",
                    Reason = absence.Reason ?? "Non spécifié",
                    Document = absence.Document ?? "Aucun document",
                    CreatedAt = absence.CreatedAt,
                    UpdatedAt = absence.UpdatedAt,
                    ClassName = student.Class.Name
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la récupération des détails de l'absence : {ex.Message}");
                return null;
            }
        }

        // 🔹 Récupérer les notifications d'un élève
        public async Task<List<NotificationDTO>> GetStudentNotifications(string studentEmail)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == studentEmail && u.Role == "eleve");

                if (user == null)
                {
                    return null;
                }

                var notifications = await _context.Notifications
                    .Where(n => n.UserId == user.Id)
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new NotificationDTO
                    {
                        Id = n.Id,
                        Message = n.Message,
                        IsRead = n.IsRead ?? false,
                        CreatedAt = n.CreatedAt ?? DateTime.UtcNow
                    })
                    .ToListAsync();

                return notifications;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la récupération des notifications : {ex.Message}");
                return null;
            }
        }

        // 🔹 Marquer une notification comme lue
        public async Task<bool> MarkNotificationAsRead(string studentEmail, int notificationId)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == studentEmail && u.Role == "eleve");

                if (user == null)
                {
                    return false;
                }

                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == user.Id);

                if (notification == null)
                {
                    return false;
                }

                notification.IsRead = true;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors du marquage de la notification : {ex.Message}");
                return false;
            }
        }

        // 🔹 Récupérer les statistiques d'un élève
        public async Task<StatsDTO> GetStudentStats(string studentEmail)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == studentEmail && u.Role == "eleve");

                if (user == null)
                {
                    return null;
                }

                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.UserId == user.Id);

                if (student == null)
                {
                    return null;
                }

                var absences = await _context.Absences
                    .Where(a => a.StudentId == student.Id)
                    .ToListAsync();

                if (!absences.Any())
                {
                    // Retourner des statistiques vides plutôt que null
                    return new StatsDTO
                    {
                        StudentId = student.Id,
                        StudentName = $"{student.FirstName} {student.LastName}",
                        TotalAbsences = 0,
                        JustifiedAbsences = 0,
                        UnjustifiedAbsences = 0,
                        AbsencesByMonth = new Dictionary<string, int>()
                    };
                }

                // Calculer les absences par mois avec des noms de mois
                var absencesByMonth = absences
                    .GroupBy(a => a.AbsenceDate.Month)
                    .ToDictionary(
                        g => GetMonthName(g.Key), 
                        g => g.Count()
                    );

                return new StatsDTO
                {
                    StudentId = student.Id,
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

        // 🔹 Récupérer l'emploi du temps d'un élève
        public async Task<List<StudentScheduleDTO>> GetStudentSchedule(string studentEmail, DateTime startDate, DateTime endDate)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == studentEmail && u.Role == "eleve");

                if (user == null)
                {
                    return null;
                }

                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.UserId == user.Id);

                if (student == null)
                {
                    return null;
                }

                var startDateOnly = DateOnly.FromDateTime(startDate);
                var endDateOnly = DateOnly.FromDateTime(endDate);

                var schedules = await _context.Schedules
                    .Where(s => s.ClassId == student.ClassId && s.Date >= startDateOnly && s.Date <= endDateOnly)
                    .OrderBy(s => s.Date)
                    .ThenBy(s => s.StartTime)
                    .Select(s => new StudentScheduleDTO
                    {
                        Id = s.Id,
                        Date = s.Date,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        Subject = s.Subject,
                        Description = s.Description ?? string.Empty
                    })
                    .ToListAsync();

                return schedules;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la récupération de l'emploi du temps : {ex.Message}");
                return new List<StudentScheduleDTO>();
            }
        }

        // 🔹 Récupérer le classement de la classe
        public async Task<List<StudentRankingDTO>> GetClassRanking(string studentEmail)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == studentEmail && u.Role == "eleve");

                if (user == null)
                {
                    return null;
                }

                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.UserId == user.Id);

                if (student == null)
                {
                    return null;
                }

                // Récupérer tous les étudiants de la classe
                var classStudents = await _context.Students
                    .Where(s => s.ClassId == student.ClassId)
                    .ToListAsync();

                // Récupérer toutes les absences pour ces étudiants
                var studentIds = classStudents.Select(s => s.Id).ToList();
                var allAbsences = await _context.Absences
                    .Where(a => studentIds.Contains(a.StudentId))
                    .ToListAsync();

                // Calculer les statistiques pour chaque étudiant
                var studentStats = classStudents.Select(s => {
                    var absences = allAbsences.Where(a => a.StudentId == s.Id).ToList();
                    return new
                    {
                        StudentId = s.Id,
                        StudentName = $"{s.FirstName} {s.LastName}",
                        TotalAbsences = absences.Count,
                        JustifiedAbsences = absences.Count(a => a.Status == "justifiée"),
                        UnjustifiedAbsences = absences.Count(a => a.Status == "non justifiée"),
                        IsCurrentStudent = s.Id == student.Id
                    };
                }).ToList();

                // Trier par nombre d'absences non justifiées (croissant)
                var rankedStudents = studentStats
                    .OrderBy(s => s.UnjustifiedAbsences)
                    .ThenBy(s => s.TotalAbsences)
                    .Select((s, index) => new StudentRankingDTO
                    {
                        StudentId = s.StudentId,
                        StudentName = s.StudentName,
                        TotalAbsences = s.TotalAbsences,
                        JustifiedAbsences = s.JustifiedAbsences,
                        UnjustifiedAbsences = s.UnjustifiedAbsences,
                        Rank = index + 1,
                        IsCurrentStudent = s.IsCurrentStudent
                    })
                    .ToList();

                return rankedStudents;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la récupération du classement : {ex.Message}");
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
    }
}

