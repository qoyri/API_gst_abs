using gest_abs.Models;
using gest_abs.DTO;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace gest_abs.Services
{
    public class AdminConfigService
    {
        private readonly GestionAbsencesContext _context;

        public AdminConfigService(GestionAbsencesContext context)
        {
            _context = context;
        }

        // 🔹 Récupérer la configuration des alertes
        public async Task<AlertConfigDTO> GetAlertConfig()
        {
            try
            {
                var config = await _context.AlertConfigs.FirstOrDefaultAsync();
                if (config == null)
                {
                    // Créer une configuration par défaut si elle n'existe pas
                    config = new AlertConfig
                    {
                        MaxAbsencesBeforeAlert = 5,
                        NotifyParents = true,
                        NotifyTeachers = true,
                        NotifyAdmin = true,
                        AlertMessage = "L'élève a dépassé le seuil d'absences autorisées."
                    };

                    _context.AlertConfigs.Add(config);
                    await _context.SaveChangesAsync();
                }

                return new AlertConfigDTO
                {
                    Id = config.Id,
                    MaxAbsencesBeforeAlert = config.MaxAbsencesBeforeAlert,
                    NotifyParents = config.NotifyParents,
                    NotifyTeachers = config.NotifyTeachers,
                    NotifyAdmin = config.NotifyAdmin,
                    AlertMessage = config.AlertMessage
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la récupération de la configuration des alertes : {ex.Message}");
                return null;
            }
        }

        // 🔹 Mettre à jour la configuration des alertes
        public async Task<bool> UpdateAlertConfig(AlertConfigDTO configDTO)
        {
            try
            {
                var config = await _context.AlertConfigs.FirstOrDefaultAsync(c => c.Id == configDTO.Id);
                if (config == null)
                {
                    config = new AlertConfig();
                    _context.AlertConfigs.Add(config);
                }

                config.MaxAbsencesBeforeAlert = configDTO.MaxAbsencesBeforeAlert;
                config.NotifyParents = configDTO.NotifyParents;
                config.NotifyTeachers = configDTO.NotifyTeachers;
                config.NotifyAdmin = configDTO.NotifyAdmin;
                config.AlertMessage = configDTO.AlertMessage;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la mise à jour de la configuration des alertes : {ex.Message}");
                return false;
            }
        }

        // 🔹 Récupérer la configuration du système de points
        public async Task<PointsSystemDTO> GetPointsConfig()
        {
            try
            {
                var config = await _context.PointsConfigs.FirstOrDefaultAsync();
                if (config == null)
                {
                    // Créer une configuration par défaut si elle n'existe pas
                    config = new PointsConfig
                    {
                        PointsPerJustifiedAbsence = -1,
                        PointsPerUnjustifiedAbsence = -3,
                        PointsPerLateArrival = -1,
                        BonusPointsForPerfectAttendance = 5,
                        BonusPointsPerMonth = 10
                    };

                    _context.PointsConfigs.Add(config);
                    await _context.SaveChangesAsync();
                }

                return new PointsSystemDTO
                {
                    Id = config.Id,
                    PointsPerJustifiedAbsence = config.PointsPerJustifiedAbsence,
                    PointsPerUnjustifiedAbsence = config.PointsPerUnjustifiedAbsence,
                    PointsPerLateArrival = config.PointsPerLateArrival,
                    BonusPointsForPerfectAttendance = config.BonusPointsForPerfectAttendance,
                    BonusPointsPerMonth = config.BonusPointsPerMonth
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la récupération de la configuration des points : {ex.Message}");
                return null;
            }
        }

        // 🔹 Mettre à jour la configuration du système de points
        public async Task<bool> UpdatePointsConfig(PointsSystemDTO configDTO)
        {
            try
            {
                var config = await _context.PointsConfigs.FirstOrDefaultAsync(c => c.Id == configDTO.Id);
                if (config == null)
                {
                    config = new PointsConfig();
                    _context.PointsConfigs.Add(config);
                }

                config.PointsPerJustifiedAbsence = configDTO.PointsPerJustifiedAbsence;
                config.PointsPerUnjustifiedAbsence = configDTO.PointsPerUnjustifiedAbsence;
                config.PointsPerLateArrival = configDTO.PointsPerLateArrival;
                config.BonusPointsForPerfectAttendance = configDTO.BonusPointsForPerfectAttendance;
                config.BonusPointsPerMonth = configDTO.BonusPointsPerMonth;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la mise à jour de la configuration des points : {ex.Message}");
                return false;
            }
        }

        // 🔹 Mettre à jour la configuration de la base de données
        public async Task<bool> UpdateDatabaseConfig(DatabaseConfigDTO configDTO)
        {
            try
            {
                // Construire la chaîne de connexion
                var connectionString = $"server={configDTO.Server};database={configDTO.Database};user={configDTO.Username};password={configDTO.Password};port={configDTO.Port}";

                // Enregistrer la configuration dans un fichier ou une table de configuration
                // Note: Dans un environnement de production, il faudrait utiliser une méthode plus sécurisée
                var config = await _context.AppConfigs.FirstOrDefaultAsync(c => c.Key == "ConnectionString");
                if (config == null)
                {
                    config = new AppConfig
                    {
                        Key = "ConnectionString",
                        Value = connectionString
                    };
                    _context.AppConfigs.Add(config);
                }
                else
                {
                    config.Value = connectionString;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la mise à jour de la configuration de la base de données : {ex.Message}");
                return false;
            }
        }

        // 🔹 Vérifier et générer les alertes pour les absences dépassant le seuil
        public async Task<(int AlertsGenerated, List<StudentAlertDTO> StudentsWithExcessiveAbsences)> CheckAndGenerateAbsenceAlerts()
        {
            try
            {
                var alertConfig = await GetAlertConfig();
                if (alertConfig == null)
                {
                    return (0, new List<StudentAlertDTO>());
                }

                // Récupérer tous les étudiants avec leurs absences
                var students = await _context.Students
                    .Include(s => s.Absences)
                    .Include(s => s.Class)
                    .Include(s => s.Parents)
                    .ToListAsync();

                var studentsWithExcessiveAbsences = new List<StudentAlertDTO>();
                var alertsGenerated = 0;

                foreach (var student in students)
                {
                    var unjustifiedAbsences = student.Absences.Count(a => a.Status == "non justifiée");
                    var pendingAbsences = student.Absences.Count(a => a.Status == "en attente");
                    var totalAbsences = unjustifiedAbsences + pendingAbsences;

                    if (totalAbsences >= alertConfig.MaxAbsencesBeforeAlert)
                    {
                        // Créer une alerte pour cet étudiant
                        var alertMessage = alertConfig.AlertMessage
                            .Replace("{StudentName}", $"{student.FirstName} {student.LastName}")
                            .Replace("{AbsenceCount}", totalAbsences.ToString())
                            .Replace("{Threshold}", alertConfig.MaxAbsencesBeforeAlert.ToString());

                        // Notifier les parents si configuré
                        if (alertConfig.NotifyParents)
                        {
                            foreach (var parent in student.Parents)
                            {
                                var notification = new Notification
                                {
                                    UserId = parent.Id,
                                    Message = alertMessage,
                                    IsRead = false,
                                    CreatedAt = DateTime.UtcNow
                                };
                                _context.Notifications.Add(notification);
                                alertsGenerated++;
                            }
                        }

                        // Notifier l'enseignant si configuré
                        if (alertConfig.NotifyTeachers && student.Class.TeacherId.HasValue)
                        {
                            var teacher = await _context.Teachers
                                .Include(t => t.User)
                                .FirstOrDefaultAsync(t => t.Id == student.Class.TeacherId);

                            if (teacher != null)
                            {
                                var notification = new Notification
                                {
                                    UserId = teacher.UserId,
                                    Message = alertMessage,
                                    IsRead = false,
                                    CreatedAt = DateTime.UtcNow
                                };
                                _context.Notifications.Add(notification);
                                alertsGenerated++;
                            }
                        }

                        // Notifier les administrateurs si configuré
                        if (alertConfig.NotifyAdmin)
                        {
                            var admins = await _context.Users
                                .Where(u => u.Role == "admin")
                                .ToListAsync();

                            foreach (var admin in admins)
                            {
                                var notification = new Notification
                                {
                                    UserId = admin.Id,
                                    Message = alertMessage,
                                    IsRead = false,
                                    CreatedAt = DateTime.UtcNow
                                };
                                _context.Notifications.Add(notification);
                                alertsGenerated++;
                            }
                        }

                        // Ajouter l'étudiant à la liste des étudiants avec des absences excessives
                        studentsWithExcessiveAbsences.Add(new StudentAlertDTO
                        {
                            StudentId = student.Id,
                            StudentName = $"{student.FirstName} {student.LastName}",
                            ClassId = student.ClassId,
                            ClassName = student.Class.Name,
                            TotalAbsences = totalAbsences,
                            UnjustifiedAbsences = unjustifiedAbsences,
                            PendingAbsences = pendingAbsences,
                            AlertMessage = alertMessage
                        });
                    }
                }

                await _context.SaveChangesAsync();
                return (alertsGenerated, studentsWithExcessiveAbsences);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la vérification des alertes : {ex.Message}");
                return (0, new List<StudentAlertDTO>());
            }
        }

        // 🔹 Générer un rapport au format spécifié
        public async Task<byte[]> GenerateReport(ReportExportDTO reportDTO)
        {
            try
            {
                // Récupérer les données nécessaires selon le type de rapport
                switch (reportDTO.ReportType)
                {
                    case "Student":
                        return await GenerateStudentReport(reportDTO);
                    case "Class":
                        return await GenerateClassReport(reportDTO);
                    case "Teacher":
                        return await GenerateTeacherReport(reportDTO);
                    case "Global":
                        return await GenerateGlobalReport(reportDTO);
                    default:
                        throw new ArgumentException("Type de rapport non reconnu.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR lors de la génération du rapport : {ex.Message}");
                return null;
            }
        }

        // Méthodes privées pour générer les différents types de rapports
        private async Task<byte[]> GenerateStudentReport(ReportExportDTO reportDTO)
        {
            // Implémentation de la génération de rapport pour un étudiant
            // Cette méthode devrait récupérer les données de l'étudiant et les formater selon le format demandé
            
            // Pour l'exemple, nous retournons simplement un texte formaté
            var student = await _context.Students
                .Include(s => s.Class)
                .Include(s => s.Absences)
                .FirstOrDefaultAsync(s => s.Id == reportDTO.StudentId);

            if (student == null)
                return null;

            var sb = new StringBuilder();
            sb.AppendLine($"Rapport pour l'étudiant: {student.FirstName} {student.LastName}");
            sb.AppendLine($"Classe: {student.Class.Name}");
            sb.AppendLine($"Période: {reportDTO.StartDate?.ToString("dd/MM/yyyy") ?? "Début"} - {reportDTO.EndDate?.ToString("dd/MM/yyyy") ?? "Aujourd'hui"}");
            sb.AppendLine();

            if (reportDTO.IncludeAbsences)
            {
                var absences = student.Absences
                    .Where(a => (!reportDTO.StartDate.HasValue || a.AbsenceDate >= DateOnly.FromDateTime(reportDTO.StartDate.Value)) &&
                                (!reportDTO.EndDate.HasValue || a.AbsenceDate <= DateOnly.FromDateTime(reportDTO.EndDate.Value)))
                    .OrderBy(a => a.AbsenceDate)
                    .ToList();

                sb.AppendLine("Absences:");
                foreach (var absence in absences)
                {
                    sb.AppendLine($"- Date: {absence.AbsenceDate}, Statut: {absence.Status}, Raison: {absence.Reason ?? "Non spécifiée"}");
                }
                sb.AppendLine();
            }

            if (reportDTO.IncludeStatistics)
            {
                var absences = student.Absences
                    .Where(a => (!reportDTO.StartDate.HasValue || a.AbsenceDate >= DateOnly.FromDateTime(reportDTO.StartDate.Value)) &&
                                (!reportDTO.EndDate.HasValue || a.AbsenceDate <= DateOnly.FromDateTime(reportDTO.EndDate.Value)))
                    .ToList();

                sb.AppendLine("Statistiques:");
                sb.AppendLine($"- Total des absences: {absences.Count}");
                sb.AppendLine($"- Absences justifiées: {absences.Count(a => a.Status == "justifiée")}");
                sb.AppendLine($"- Absences non justifiées: {absences.Count(a => a.Status == "non justifiée")}");
                sb.AppendLine($"- Absences en attente: {absences.Count(a => a.Status == "en attente")}");
                sb.AppendLine();
            }

            // Convertir le rapport au format demandé
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private async Task<byte[]> GenerateClassReport(ReportExportDTO reportDTO)
        {
            // Implémentation similaire pour les rapports de classe
            return null;
        }

        private async Task<byte[]> GenerateTeacherReport(ReportExportDTO reportDTO)
        {
            // Implémentation similaire pour les rapports d'enseignant
            return null;
        }

        private async Task<byte[]> GenerateGlobalReport(ReportExportDTO reportDTO)
        {
            // Implémentation similaire pour les rapports globaux
            return null;
        }
    }
}

