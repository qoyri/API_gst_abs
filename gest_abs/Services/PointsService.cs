using gest_abs.Models;
using gest_abs.DTO;
using Microsoft.EntityFrameworkCore;

namespace gest_abs.Services
{
    public class PointsService
    {
        private readonly GestionAbsencesContext _context;

        public PointsService(GestionAbsencesContext context)
        {
            _context = context;
        }

        // 🔹 Récupérer les points d'un étudiant
        public async Task<StudentPointsDTO> GetStudentPoints(int studentId)
        {
            try
            {
                var student = await _context.Students
                    .Include(s => s.PointsHistory)
                    .FirstOrDefaultAsync(s => s.Id == studentId);

                if (student == null)
                {
                    return null;
                }

                // Calculer le classement de l'étudiant
                var allStudents = await _context.Students
                    .Include(s => s.PointsHistory)
                    .ToListAsync();

                var rankedStudents = allStudents
                    .OrderByDescending(s => s.PointsHistory.Sum(p => p.Points))
                    .ToList();

                var rank = rankedStudents.FindIndex(s => s.Id == studentId) + 1;

                // Calculer les points du mois en cours
                var currentMonth = DateTime.UtcNow.Month;
                var currentYear = DateTime.UtcNow.Year;
                var currentMonthPoints = student.PointsHistory
                    .Where(p => p.Date.Month == currentMonth && p.Date.Year == currentYear)
                    .Sum(p => p.Points);

                return new StudentPointsDTO
                {
                    StudentId = student.Id,
                    StudentName = $"{student.FirstName} {student.LastName}",
                    TotalPoints = student.PointsHistory.Sum(p => p.Points),
                    CurrentMonthPoints = currentMonthPoints,
                    Rank = rank,
                    PointsHistory = student.PointsHistory.Select(p => new PointsHistoryDTO
                    {
                        Id = p.Id,
                        StudentId = p.StudentId,
                        Date = p.Date,
                        Points = p.Points,
                        Reason = p.Reason ?? string.Empty,
                        Type = p.Type ?? "Regular"
                    }).OrderByDescending(p => p.Date).ToList()
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la récupération des points de l'étudiant : {ex.Message}");
                return null;
            }
        }

        // 🔹 Récupérer les points d'une classe
        public async Task<ClassPointsDTO> GetClassPoints(int classId)
        {
            try
            {
                var classEntity = await _context.Classes
                    .Include(c => c.Students)
                    .ThenInclude(s => s.PointsHistory)
                    .FirstOrDefaultAsync(c => c.Id == classId);

                if (classEntity == null)
                {
                    return null;
                }

                var studentPoints = classEntity.Students.Select(s => new StudentPointsDTO
                {
                    StudentId = s.Id,
                    StudentName = $"{s.FirstName} {s.LastName}",
                    TotalPoints = s.PointsHistory.Sum(p => p.Points),
                    CurrentMonthPoints = s.PointsHistory
                        .Where(p => p.Date.Month == DateTime.UtcNow.Month && p.Date.Year == DateTime.UtcNow.Year)
                        .Sum(p => p.Points),
                    PointsHistory = s.PointsHistory.Select(p => new PointsHistoryDTO
                    {
                        Id = p.Id,
                        StudentId = p.StudentId,
                        Date = p.Date,
                        Points = p.Points,
                        Reason = p.Reason ?? string.Empty,
                        Type = p.Type ?? "Regular"
                    }).OrderByDescending(p => p.Date).ToList()
                }).OrderByDescending(s => s.TotalPoints).ToList();

                // Attribuer les rangs
                for (int i = 0; i < studentPoints.Count; i++)
                {
                    studentPoints[i].Rank = i + 1;
                }

                return new ClassPointsDTO
                {
                    ClassId = classEntity.Id,
                    ClassName = classEntity.Name,
                    TotalPoints = studentPoints.Sum(s => s.TotalPoints),
                    AveragePoints = studentPoints.Count > 0 ? studentPoints.Sum(s => s.TotalPoints) / studentPoints.Count : 0,
                    TopStudents = studentPoints.Take(5).ToList()
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la récupération des points de la classe : {ex.Message}");
                return null;
            }
        }

        // 🔹 Ajouter des points à un étudiant
        public async Task<bool> AddPointsToStudent(int studentId, PointsAddDTO pointsDTO)
        {
            try
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.Id == studentId);

                if (student == null)
                {
                    return false;
                }

                var pointsHistory = new PointsHistory
                {
                    StudentId = studentId,
                    Date = DateTime.UtcNow,
                    Points = pointsDTO.Points,
                    Reason = pointsDTO.Reason,
                    Type = pointsDTO.Type
                };

                _context.PointsHistory.Add(pointsHistory);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de l'ajout de points à l'étudiant : {ex.Message}");
                return false;
            }
        }

        // 🔹 Récupérer le classement des élèves d'une classe
        public async Task<List<StudentPointsDTO>> GetClassRanking(int classId)
        {
            try
            {
                var classEntity = await _context.Classes
                    .Include(c => c.Students)
                    .ThenInclude(s => s.PointsHistory)
                    .FirstOrDefaultAsync(c => c.Id == classId);

                if (classEntity == null)
                {
                    return null;
                }

                var studentPoints = classEntity.Students.Select(s => new StudentPointsDTO
                {
                    StudentId = s.Id,
                    StudentName = $"{s.FirstName} {s.LastName}",
                    TotalPoints = s.PointsHistory.Sum(p => p.Points),
                    CurrentMonthPoints = s.PointsHistory
                        .Where(p => p.Date.Month == DateTime.UtcNow.Month && p.Date.Year == DateTime.UtcNow.Year)
                        .Sum(p => p.Points),
                    PointsHistory = null // Ne pas inclure l'historique des points pour alléger la réponse
                }).OrderByDescending(s => s.TotalPoints).ToList();

                // Attribuer les rangs
                for (int i = 0; i < studentPoints.Count; i++)
                {
                    studentPoints[i].Rank = i + 1;
                }

                return studentPoints;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la récupération du classement de la classe : {ex.Message}");
                return null;
            }
        }

        // 🔹 Récupérer le classement global des élèves
        public async Task<List<StudentPointsDTO>> GetGlobalRanking()
        {
            try
            {
                var students = await _context.Students
                    .Include(s => s.PointsHistory)
                    .Include(s => s.Class)
                    .ToListAsync();

                var studentPoints = students.Select(s => new StudentPointsDTO
                {
                    StudentId = s.Id,
                    StudentName = $"{s.FirstName} {s.LastName} ({s.Class.Name})",
                    TotalPoints = s.PointsHistory.Sum(p => p.Points),
                    CurrentMonthPoints = s.PointsHistory
                        .Where(p => p.Date.Month == DateTime.UtcNow.Month && p.Date.Year == DateTime.UtcNow.Year)
                        .Sum(p => p.Points),
                    PointsHistory = null // Ne pas inclure l'historique des points pour alléger la réponse
                }).OrderByDescending(s => s.TotalPoints).ToList();

                // Attribuer les rangs
                for (int i = 0; i < studentPoints.Count; i++)
                {
                    studentPoints[i].Rank = i + 1;
                }

                return studentPoints;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la récupération du classement global : {ex.Message}");
                return null;
            }
        }

        // 🔹 Calculer et attribuer les points pour les absences
        public async Task<bool> CalculateAbsencePoints()
        {
            try
            {
                var pointsConfig = await _context.PointsConfigs.FirstOrDefaultAsync();
                if (pointsConfig == null)
                {
                    return false;
                }

                // Récupérer toutes les absences qui n'ont pas encore été traitées pour les points
                var absences = await _context.Absences
                    .Where(a => !a.PointsProcessed)
                    .ToListAsync();

                foreach (var absence in absences)
                {
                    int points = 0;

                    // Attribuer les points selon le statut de l'absence
                    if (absence.Status == "justifiée")
                    {
                        points = pointsConfig.PointsPerJustifiedAbsence;
                    }
                    else if (absence.Status == "non justifiée")
                    {
                        points = pointsConfig.PointsPerUnjustifiedAbsence;
                    }
                    else
                    {
                        // Ne pas attribuer de points pour les absences en attente
                        continue;
                    }

                    // Créer l'historique des points
                    var pointsHistory = new PointsHistory
                    {
                        StudentId = absence.StudentId,
                        Date = DateTime.UtcNow,
                        Points = points,
                        Reason = $"Absence du {absence.AbsenceDate} - {absence.Status}",
                        Type = points >= 0 ? "Bonus" : "Malus"
                    };

                    _context.PointsHistory.Add(pointsHistory);

                    // Marquer l'absence comme traitée pour les points
                    absence.PointsProcessed = true;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors du calcul des points pour les absences : {ex.Message}");
                return false;
            }
        }

        // 🔹 Attribuer des bonus pour l'assiduité parfaite
        public async Task<bool> AwardPerfectAttendanceBonuses()
        {
            try
            {
                var pointsConfig = await _context.PointsConfigs.FirstOrDefaultAsync();
                if (pointsConfig == null)
                {
                    return false;
                }

                // Récupérer tous les étudiants
                var students = await _context.Students
                    .Include(s => s.Absences)
                    .ToListAsync();

                // Déterminer la période (mois précédent)
                var lastMonth = DateTime.UtcNow.AddMonths(-1);
                var startOfLastMonth = new DateOnly(lastMonth.Year, lastMonth.Month, 1);
                var endOfLastMonth = startOfLastMonth.AddMonths(1).AddDays(-1);

                foreach (var student in students)
                {
                    // Vérifier si l'étudiant n'a aucune absence pour le mois précédent
                    var hasNoAbsences = !student.Absences.Any(a => 
                        a.AbsenceDate >= startOfLastMonth && 
                        a.AbsenceDate <= endOfLastMonth);

                    if (hasNoAbsences)
                    {
                        // Attribuer le bonus pour l'assiduité parfaite
                        var pointsHistory = new PointsHistory
                        {
                            StudentId = student.Id,
                            Date = DateTime.UtcNow,
                            Points = pointsConfig.BonusPointsForPerfectAttendance,
                            Reason = $"Bonus pour assiduité parfaite - {lastMonth:MMMM yyyy}",
                            Type = "Bonus"
                        };

                        _context.PointsHistory.Add(pointsHistory);
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de l'attribution des bonus pour assiduité parfaite : {ex.Message}");
                return false;
            }
        }
    }
}

