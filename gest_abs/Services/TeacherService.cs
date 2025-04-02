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

        // 🔹 Récupérer le profil d'un enseignant
        public async Task<TeacherProfileDTO> GetTeacherProfile(string email)
        {
            try
            {
                var teacher = await _context.Teachers
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.User.Email == email);

                if (teacher == null)
                    return null;

                return new TeacherProfileDTO
                {
                    Id = teacher.Id,
                    Email = teacher.User.Email,
                    Subject = teacher.Subject,
                    CreatedAt = teacher.User.CreatedAt
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la récupération du profil enseignant : {ex.Message}");
                return null;
            }
        }

        // 🔹 Mettre à jour le profil d'un enseignant
        public async Task<bool> UpdateTeacherProfile(string email, TeacherUpdateProfileDTO updateDTO)
        {
            try
            {
                var teacher = await _context.Teachers
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.User.Email == email);

                if (teacher == null)
                    return false;

                // Mise à jour de la matière enseignée
                teacher.Subject = updateDTO.Subject;

                // Mise à jour du mot de passe si fourni
                if (!string.IsNullOrEmpty(updateDTO.CurrentPassword) && !string.IsNullOrEmpty(updateDTO.NewPassword))
                {
                    // Vérifier l'ancien mot de passe
                    var currentPasswordHash = HasherPassword.HashPassword(updateDTO.CurrentPassword);
                    if (currentPasswordHash != teacher.User.Password)
                        return false;

                    // Mettre à jour le mot de passe
                    teacher.User.Password = HasherPassword.HashPassword(updateDTO.NewPassword);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la mise à jour du profil enseignant : {ex.Message}");
                return false;
            }
        }

        // 🔹 Récupérer les classes d'un enseignant
        public async Task<List<ClassDTO>> GetTeacherClasses(string email)
        {
            try
            {
                var teacherId = await _context.Teachers
                    .Where(t => t.User.Email == email)
                    .Select(t => t.Id)
                    .FirstOrDefaultAsync();

                if (teacherId == 0)
                    return new List<ClassDTO>();

                var classes = await _context.Classes
                    .Where(c => c.TeacherId == teacherId)
                    .Select(c => new ClassDTO
                    {
                        Id = c.Id,
                        Name = c.Name,
                        StudentCount = c.Students.Count
                    })
                    .ToListAsync();

                return classes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la récupération des classes : {ex.Message}");
                return new List<ClassDTO>();
            }
        }

        // 🔹 Récupérer les détails d'une classe
        public async Task<ClassDetailDTO> GetClassDetails(string email, int classId)
        {
            try
            {
                var teacherId = await _context.Teachers
                    .Where(t => t.User.Email == email)
                    .Select(t => t.Id)
                    .FirstOrDefaultAsync();

                if (teacherId == 0)
                    return null;

                var classEntity = await _context.Classes
                    .Include(c => c.Students)
                    .FirstOrDefaultAsync(c => c.Id == classId && c.TeacherId == teacherId);

                if (classEntity == null)
                    return null;

                var classDTO = new ClassDetailDTO
                {
                    Id = classEntity.Id,
                    Name = classEntity.Name,
                    Students = classEntity.Students.Select(s => new StudentDTO
                    {
                        Id = s.Id,
                        ClassId = s.ClassId,
                        FirstName = s.FirstName,
                        LastName = s.LastName,
                        Birthdate = s.Birthdate
                    }).ToList()
                };

                return classDTO;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la récupération des détails de la classe : {ex.Message}");
                return null;
            }
        }

        // 🔹 Notifier les parents d'une absence
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
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la notification des parents : {ex.Message}");
                return false;
            }
        }

        // 🔹 Générer un rapport d'absences
        public async Task<AbsenceReportDTO> GenerateAbsenceReport(int teacherId, ReportFilterDTO filter)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la génération du rapport : {ex.Message}");
                return null;
            }
        }

        // 🔹 Récupérer les statistiques d'une classe
        public async Task<ClassStatisticsDTO> GetClassStatistics(int classId)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la récupération des statistiques de classe : {ex.Message}");
                return null;
            }
        }

        // 🔹 Récupérer les statistiques d'un étudiant
        public async Task<StudentStatisticsDTO> GetStudentStatistics(int studentId)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la récupération des statistiques d'étudiant : {ex.Message}");
                return null;
            }
        }

        // 🔹 Vérifier si une salle est disponible
        public async Task<bool> IsRoomAvailable(int roomId, DateOnly date, TimeOnly startTime, TimeOnly endTime)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la vérification de disponibilité : {ex.Message}");
                return false;
            }
        }

        // 🔹 Vérifier si une salle est disponible (en excluant une réservation existante)
        public async Task<bool> IsRoomAvailableExcludingReservation(int roomId, DateOnly date, TimeOnly startTime, TimeOnly endTime, int excludeReservationId)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la vérification de disponibilité : {ex.Message}");
                return false;
            }
        }

        // 🔹 Récupérer les réservations d'un enseignant
        public async Task<List<ReservationDTO>> GetTeacherReservations(string email, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var userId = await _context.Users
                    .Where(u => u.Email == email)
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync();

                if (userId == 0)
                    return new List<ReservationDTO>();

                var query = _context.Reservations
                    .Include(r => r.Room)
                    .Where(r => r.UserId == userId);

                if (startDate.HasValue)
                    query = query.Where(r => r.ReservationDate >= DateOnly.FromDateTime(startDate.Value));

                if (endDate.HasValue)
                    query = query.Where(r => r.ReservationDate <= DateOnly.FromDateTime(endDate.Value));

                var reservations = await query
                    .Select(r => new ReservationDTO
                    {
                        Id = r.Id,
                        RoomId = r.RoomId,
                        RoomName = r.Room.Name,
                        ReservationDate = r.ReservationDate,
                        StartTime = r.StartTime,
                        EndTime = r.EndTime
                    })
                    .OrderBy(r => r.ReservationDate)
                    .ThenBy(r => r.StartTime)
                    .ToListAsync();

                return reservations;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la récupération des réservations : {ex.Message}");
                return new List<ReservationDTO>();
            }
        }

        // 🔹 Créer une réservation de salle
        public async Task<Reservation> CreateReservation(string email, ReservationCreateDTO createDTO)
        {
            try
            {
                var userId = await _context.Users
                    .Where(u => u.Email == email)
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync();

                if (userId == 0)
                    return null;

                // Vérifier que la salle existe
                var room = await _context.Rooms.FindAsync(createDTO.RoomId);
                if (room == null)
                    return null;

                // Vérifier que la salle n'est pas déjà réservée
                var isRoomAvailable = await IsRoomAvailable(
                    createDTO.RoomId,
                    DateOnly.FromDateTime(createDTO.ReservationDate),
                    TimeOnly.FromTimeSpan(createDTO.StartTime),
                    TimeOnly.FromTimeSpan(createDTO.EndTime));

                if (!isRoomAvailable)
                    return null;

                var reservation = new Reservation
                {
                    UserId = userId,
                    RoomId = createDTO.RoomId,
                    ReservationDate = DateOnly.FromDateTime(createDTO.ReservationDate),
                    StartTime = TimeOnly.FromTimeSpan(createDTO.StartTime),
                    EndTime = TimeOnly.FromTimeSpan(createDTO.EndTime)
                };

                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();

                return reservation;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la création de la réservation : {ex.Message}");
                return null;
            }
        }

        // 🔹 Mettre à jour une réservation de salle
        public async Task<bool> UpdateReservation(string email, int reservationId, ReservationUpdateDTO updateDTO)
        {
            try
            {
                var userId = await _context.Users
                    .Where(u => u.Email == email)
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync();

                if (userId == 0)
                    return false;

                var reservation = await _context.Reservations
                    .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId);

                if (reservation == null)
                    return false;

                // Vérifier que la salle n'est pas déjà réservée (en excluant la réservation actuelle)
                var isRoomAvailable = await IsRoomAvailableExcludingReservation(
                    reservation.RoomId,
                    DateOnly.FromDateTime(updateDTO.ReservationDate),
                    TimeOnly.FromTimeSpan(updateDTO.StartTime),
                    TimeOnly.FromTimeSpan(updateDTO.EndTime),
                    reservationId);

                if (!isRoomAvailable)
                    return false;

                // Mise à jour des informations
                reservation.ReservationDate = DateOnly.FromDateTime(updateDTO.ReservationDate);
                reservation.StartTime = TimeOnly.FromTimeSpan(updateDTO.StartTime);
                reservation.EndTime = TimeOnly.FromTimeSpan(updateDTO.EndTime);

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la mise à jour de la réservation : {ex.Message}");
                return false;
            }
        }

        // 🔹 Supprimer une réservation de salle
        public async Task<bool> DeleteReservation(string email, int reservationId)
        {
            try
            {
                var userId = await _context.Users
                    .Where(u => u.Email == email)
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync();

                if (userId == 0)
                    return false;

                var reservation = await _context.Reservations
                    .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId);

                if (reservation == null)
                    return false;

                _context.Reservations.Remove(reservation);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la suppression de la réservation : {ex.Message}");
                return false;
            }
        }

        // 🔹 Récupérer les notifications d'un enseignant
        public async Task<List<NotificationDTO>> GetTeacherNotifications(string email)
        {
            try
            {
                var userId = await _context.Users
                    .Where(u => u.Email == email)
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync();

                if (userId == 0)
                    return new List<NotificationDTO>();

                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId)
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
                return new List<NotificationDTO>();
            }
        }

        // 🔹 Marquer une notification comme lue
        public async Task<bool> MarkNotificationAsRead(string email, int notificationId)
        {
            try
            {
                var userId = await _context.Users
                    .Where(u => u.Email == email)
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync();

                if (userId == 0)
                    return false;

                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

                if (notification == null)
                    return false;

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

        // 🔹 Créer une absence
        public async Task<Absence> CreateAbsence(string email, AbsenceCreateDTO createDTO)
        {
            try
            {
                var teacherId = await _context.Teachers
                    .Where(t => t.User.Email == email)
                    .Select(t => t.Id)
                    .FirstOrDefaultAsync();

                if (teacherId == 0)
                    return null;

                // Vérifier que l'étudiant existe et appartient à une classe de l'enseignant
                var student = await _context.Students
                    .Include(s => s.Class)
                    .FirstOrDefaultAsync(s => s.Id == createDTO.StudentId && s.Class.TeacherId == teacherId);

                if (student == null)
                    return null;

                var absence = new Absence
                {
                    StudentId = createDTO.StudentId,
                    AbsenceDate = DateOnly.FromDateTime(createDTO.AbsenceDate),
                    Status = createDTO.Status ?? "en attente",
                    Reason = createDTO.Reason,
                    Document = createDTO.Document,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Absences.Add(absence);
                await _context.SaveChangesAsync();

                // Notifier les parents
                await NotifyParentsAboutAbsence(absence.Id);

                return absence;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la création de l'absence : {ex.Message}");
                return null;
            }
        }

        // 🔹 Récupérer le tableau de bord de l'enseignant
        public async Task<object> GetTeacherDashboard(string email)
        {
            try
            {
                var teacher = await _context.Teachers
                    .Include(t => t.User)
                    .Include(t => t.Classes)
                    .ThenInclude(c => c.Students)
                    .FirstOrDefaultAsync(t => t.User.Email == email);

                if (teacher == null)
                    return null;

                // Récupérer les classes de l'enseignant
                var classes = teacher.Classes.ToList();
                var classIds = classes.Select(c => c.Id).ToList();
                var studentIds = classes.SelectMany(c => c.Students.Select(s => s.Id)).ToList();

                // Récupérer les absences récentes
                var recentAbsences = await _context.Absences
                    .Include(a => a.Student)
                    .Where(a => studentIds.Contains(a.StudentId))
                    .OrderByDescending(a => a.AbsenceDate)
                    .Take(5)
                    .Select(a => new
                    {
                        Id = a.Id,
                        StudentName = $"{a.Student.FirstName} {a.Student.LastName}",
                        AbsenceDate = a.AbsenceDate,
                        Status = a.Status
                    })
                    .ToListAsync();

                // Récupérer les notifications non lues
                var unreadNotifications = await _context.Notifications
                    .Where(n => n.UserId == teacher.UserId && (n.IsRead == false || n.IsRead == null))
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(5)
                    .Select(n => new
                    {
                        Id = n.Id,
                        Message = n.Message,
                        CreatedAt = n.CreatedAt
                    })
                    .ToListAsync();

                // Récupérer les réservations à venir
                var upcomingReservations = await _context.Reservations
                    .Include(r => r.Room)
                    .Where(r => r.UserId == teacher.UserId && r.ReservationDate >= DateOnly.FromDateTime(DateTime.Today))
                    .OrderBy(r => r.ReservationDate)
                    .ThenBy(r => r.StartTime)
                    .Take(3)
                    .Select(r => new
                    {
                        Id = r.Id,
                        RoomName = r.Room.Name,
                        ReservationDate = r.ReservationDate,
                        StartTime = r.StartTime,
                        EndTime = r.EndTime
                    })
                    .ToListAsync();

                // Calculer les statistiques globales
                var allAbsences = await _context.Absences
                    .Where(a => studentIds.Contains(a.StudentId))
                    .ToListAsync();

                var dashboardData = new
                {
                    TeacherName = teacher.User.Email,
                    Subject = teacher.Subject,
                    TotalClasses = classes.Count,
                    TotalStudents = classes.Sum(c => c.Students.Count),
                    TotalAbsences = allAbsences.Count,
                    JustifiedAbsences = allAbsences.Count(a => a.Status == "justifiée"),
                    UnjustifiedAbsences = allAbsences.Count(a => a.Status == "non justifiée"),
                    PendingAbsences = allAbsences.Count(a => a.Status == "en attente"),
                    RecentAbsences = recentAbsences,
                    UnreadNotifications = unreadNotifications,
                    UnreadNotificationsCount = unreadNotifications.Count,
                    UpcomingReservations = upcomingReservations,
                    Classes = classes.Select(c => new
                    {
                        Id = c.Id,
                        Name = c.Name,
                        StudentCount = c.Students.Count
                    }).ToList()
                };

                return dashboardData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la récupération du tableau de bord : {ex.Message}");
                return null;
            }
        }
    }
}

