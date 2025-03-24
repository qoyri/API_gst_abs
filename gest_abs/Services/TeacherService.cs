using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using gest_abs.Models;
using gest_abs.DTO;

namespace gest_abs.Services
{
    public class TeacherService
    {
        private readonly GestionAbsencesContext _context;

        public TeacherService(GestionAbsencesContext context)
        {
            _context = context;
        }

        public async Task<bool> NotifyParentsAboutAbsence(int absenceId)
        {
            try
            {
                var absence = await _context.Absences
                    .Include(a => a.Student)
                    .ThenInclude(s => s.Parents)
                    .FirstOrDefaultAsync(a => a.Id == absenceId);

                if (absence == null)
                    return false;

                // Créer une notification pour chaque parent
                foreach (var parent in absence.Student.Parents)
                {
                    var notification = new Notification
                    {
                        UserId = parent.Id,
                        Message = $"Votre enfant {absence.Student.FirstName} {absence.Student.LastName} a été absent le {absence.AbsenceDate}.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Notifications.Add(notification);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<AbsenceReportDTO> GenerateAbsenceReport(int teacherId, ReportFilterDTO filter)
        {
            // Récupérer les classes de l'enseignant
            var classIds = await _context.Classes
                .Where(c => c.TeacherId == teacherId)
                .Select(c => c.Id)
                .ToListAsync();

            // Récupérer les étudiants de ces classes
            var studentIds = await _context.Students
                .Where(s => classIds.Contains(s.ClassId))
                .Select(s => s.Id)
                .ToListAsync();

            // Construire la requête de base
            var query = _context.Absences
                .Include(a => a.Student)
                .ThenInclude(s => s.Class)
                .Where(a => studentIds.Contains(a.StudentId));

            // Appliquer les filtres
            if (filter.ClassId.HasValue)
            {
                var studentsInClass = await _context.Students
                    .Where(s => s.ClassId == filter.ClassId)
                    .Select(s => s.Id)
                    .ToListAsync();
                
                query = query.Where(a => studentsInClass.Contains(a.StudentId));
            }

            if (filter.StudentId.HasValue)
                query = query.Where(a => a.StudentId == filter.StudentId);

            if (filter.StartDate.HasValue)
                query = query.Where(a => a.AbsenceDate >= DateOnly.FromDateTime(filter.StartDate.Value));

            if (filter.EndDate.HasValue)
                query = query.Where(a => a.AbsenceDate <= DateOnly.FromDateTime(filter.EndDate.Value));

            // Exécuter la requête
            var absences = await query.ToListAsync();

            // Construire le rapport
            var report = new AbsenceReportDTO
            {
                GeneratedAt = DateTime.UtcNow,
                ReportPeriod = $"{filter.StartDate?.ToString("dd/MM/yyyy") ?? "Début"} - {filter.EndDate?.ToString("dd/MM/yyyy") ?? "Aujourd'hui"}",
                TotalAbsences = absences.Count,
                JustifiedAbsences = absences.Count(a => a.Status == "justifiée"),
                UnjustifiedAbsences = absences.Count(a => a.Status == "non justifiée"),
                PendingAbsences = absences.Count(a => a.Status == "en attente"),
                Absences = absences.Select(a => new AbsenceDTO
                {
                    Id = a.Id,
                    StudentId = a.StudentId,
                    StudentName = $"{a.Student.FirstName} {a.Student.LastName}",
                    ClassId = a.Student.ClassId,
                    ClassName = a.Student.Class.Name,
                    AbsenceDate = a.AbsenceDate,
                    Status = a.Status,
                    Reason = a.Reason,
                    Document = a.Document
                }).ToList(),
                AbsencesByClass = absences
                    .GroupBy(a => a.Student.Class.Name)
                    .ToDictionary(g => g.Key, g => g.Count()),
                AbsencesByStudent = absences
                    .GroupBy(a => $"{a.Student.FirstName} {a.Student.LastName}")
                    .ToDictionary(g => g.Key, g => g.Count()),
                AbsencesByMonth = absences
                    .GroupBy(a => a.AbsenceDate.ToString("MM/yyyy"))
                    .ToDictionary(g => g.Key, g => g.Count()),
                AbsencesByDay = absences
                    .GroupBy(a => a.AbsenceDate.DayOfWeek.ToString())
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return report;
        }

        public async Task<ClassStatisticsDTO> GetClassStatistics(int classId)
        {
            var classEntity = await _context.Classes
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (classEntity == null)
                return null;

            var studentIds = classEntity.Students.Select(s => s.Id).ToList();
            var absences = await _context.Absences
                .Where(a => studentIds.Contains(a.StudentId))
                .ToListAsync();

            var stats = new ClassStatisticsDTO
            {
                ClassId = classEntity.Id,
                ClassName = classEntity.Name,
                StudentCount = classEntity.Students.Count,
                TotalAbsences = absences.Count,
                JustifiedAbsences = absences.Count(a => a.Status == "justifiée"),
                UnjustifiedAbsences = absences.Count(a => a.Status == "non justifiée"),
                PendingAbsences = absences.Count(a => a.Status == "en attente"),
                AbsenceRate = classEntity.Students.Count > 0 
                    ? (double)absences.Count / classEntity.Students.Count 
                    : 0,
                StudentStats = new List<StudentAbsenceStatDTO>(),
                AbsencesByMonth = absences
                    .GroupBy(a => a.AbsenceDate.ToString("MM/yyyy"))
                    .ToDictionary(g => g.Key, g => g.Count()),
                AbsencesByDay = absences
                    .GroupBy(a => a.AbsenceDate.DayOfWeek.ToString())
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            // Statistiques par étudiant
            foreach (var student in classEntity.Students)
            {
                var studentAbsences = absences.Where(a => a.StudentId == student.Id).ToList();
                stats.StudentStats.Add(new StudentAbsenceStatDTO
                {
                    StudentId = student.Id,
                    StudentName = $"{student.FirstName} {student.LastName}",
                    TotalAbsences = studentAbsences.Count,
                    JustifiedAbsences = studentAbsences.Count(a => a.Status == "justifiée"),
                    UnjustifiedAbsences = studentAbsences.Count(a => a.Status == "non justifiée"),
                    PendingAbsences = studentAbsences.Count(a => a.Status == "en attente"),
                    AbsenceRate = absences.Count > 0 ? (double)studentAbsences.Count / absences.Count : 0
                });
            }

            return stats;
        }

        public async Task<StudentStatisticsDTO> GetStudentStatistics(int studentId)
        {
            var student = await _context.Students
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                return null;

            var absences = await _context.Absences
                .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.AbsenceDate)
                .ToListAsync();

            var stats = new StudentStatisticsDTO
            {
                StudentId = student.Id,
                StudentName = $"{student.FirstName} {student.LastName}",
                ClassId = student.ClassId,
                ClassName = student.Class.Name,
                TotalAbsences = absences.Count,
                JustifiedAbsences = absences.Count(a => a.Status == "justifiée"),
                UnjustifiedAbsences = absences.Count(a => a.Status == "non justifiée"),
                PendingAbsences = absences.Count(a => a.Status == "en attente"),
                AbsenceRate = 0, // À calculer en fonction du nombre total de jours de cours
                RecentAbsences = absences.Take(5).Select(a => new AbsenceDTO
                {
                    Id = a.Id,
                    StudentId = a.StudentId,
                    StudentName = $"{student.FirstName} {student.LastName}",
                    ClassId = student.ClassId,
                    ClassName = student.Class.Name,
                    AbsenceDate = a.AbsenceDate,
                    Status = a.Status,
                    Reason = a.Reason,
                    Document = a.Document
                }).ToList(),
                AbsencesByMonth = absences
                    .GroupBy(a => a.AbsenceDate.ToString("MM/yyyy"))
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return stats;
        }

        public async Task<bool> IsRoomAvailable(int roomId, DateOnly date, TimeOnly startTime, TimeOnly endTime)
        {
            // Vérifier s'il existe déjà une réservation pour cette salle à cette date et qui chevauche l'horaire demandé
            var conflictingReservation = await _context.Reservations
                .AnyAsync(r => r.RoomId == roomId && 
                               r.ReservationDate == date && 
                               ((r.StartTime <= startTime && r.EndTime > startTime) || 
                                (r.StartTime < endTime && r.EndTime >= endTime) ||
                                (r.StartTime >= startTime && r.EndTime <= endTime)));

            return !conflictingReservation;
        }

        public async Task<bool> IsRoomAvailableExcludingReservation(int roomId, DateOnly date, TimeOnly startTime, TimeOnly endTime, int excludeReservationId)
        {
            // Vérifier s'il existe déjà une réservation pour cette salle à cette date et qui chevauche l'horaire demandé
            var conflictingReservation = await _context.Reservations
                .AnyAsync(r => r.Id != excludeReservationId &&
                               r.RoomId == roomId && 
                               r.ReservationDate == date && 
                               ((r.StartTime <= startTime && r.EndTime > startTime) || 
                                (r.StartTime < endTime && r.EndTime >= endTime) ||
                                (r.StartTime >= startTime && r.EndTime <= endTime)));

            return !conflictingReservation;
        }
    }
}

