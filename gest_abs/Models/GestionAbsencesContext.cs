using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace gest_abs.Models;

public partial class GestionAbsencesContext : DbContext
{
    public GestionAbsencesContext()
    {
    }

    public GestionAbsencesContext(DbContextOptions<GestionAbsencesContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Absence> Absences { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<EfmigrationsHistory> EfmigrationsHistories { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Reservation> Reservations { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<Teacher> Teachers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    // Ajouter ces propriétés DbSet à la classe GestionAbsencesContext
    public virtual DbSet<AlertConfig> AlertConfigs { get; set; }
    public virtual DbSet<PointsConfig> PointsConfigs { get; set; }
    public virtual DbSet<PointsHistory> PointsHistory { get; set; }
    public virtual DbSet<AppConfig> AppConfigs { get; set; }
    public virtual DbSet<Schedule> Schedules { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=82.66.203.90;database=gestion_absences;user=user_abs;password=%@8Sm1chel/#$%^3412", Microsoft.EntityFrameworkCore.ServerVersion.Parse("10.11.8-mariadb"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Conserver le code existant
        modelBuilder
            .UseCollation("utf8mb4_general_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Absence>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("absences");

            entity.HasIndex(e => e.StudentId, "student_id");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.AbsenceDate).HasColumnName("absence_date");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Document)
                .HasMaxLength(255)
                .HasColumnName("document");
            entity.Property(e => e.Reason)
                .HasColumnType("text")
                .HasColumnName("reason");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'en attente'")
                .HasColumnType("enum('en attente','justifiée','non justifiée')")
                .HasColumnName("status");
            entity.Property(e => e.StudentId)
                .HasColumnType("int(11)")
                .HasColumnName("student_id");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Student).WithMany(p => p.Absences)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("absences_ibfk_1");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("classes");

            entity.HasIndex(e => e.Name, "name").IsUnique();

            entity.HasIndex(e => e.TeacherId, "teacher_id");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.TeacherId)
                .HasColumnType("int(11)")
                .HasColumnName("teacher_id");

            entity.HasOne(d => d.Teacher).WithMany(p => p.Classes)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("classes_ibfk_1");
        });

        modelBuilder.Entity<EfmigrationsHistory>(entity =>
        {
            entity.HasKey(e => e.MigrationId).HasName("PRIMARY");

            entity.ToTable("__EFMigrationsHistory");

            entity.Property(e => e.MigrationId).HasMaxLength(150);
            entity.Property(e => e.ProductVersion).HasMaxLength(32);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("notifications");

            entity.HasIndex(e => e.UserId, "user_id");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.IsRead)
                .HasDefaultValueSql("'0'")
                .HasColumnName("is_read");
            entity.Property(e => e.Message)
                .HasColumnType("text")
                .HasColumnName("message");
            entity.Property(e => e.UserId)
                .HasColumnType("int(11)")
                .HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("notifications_ibfk_1");
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("reservations");

            entity.HasIndex(e => e.RoomId, "room_id");

            entity.HasIndex(e => e.UserId, "user_id");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.EndTime)
                .HasColumnType("time")
                .HasColumnName("end_time");
            entity.Property(e => e.ReservationDate).HasColumnName("reservation_date");
            entity.Property(e => e.RoomId)
                .HasColumnType("int(11)")
                .HasColumnName("room_id");
            entity.Property(e => e.StartTime)
                .HasColumnType("time")
                .HasColumnName("start_time");
            entity.Property(e => e.UserId)
                .HasColumnType("int(11)")
                .HasColumnName("user_id");

            entity.HasOne(d => d.Room).WithMany(p => p.Reservations)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("reservations_ibfk_2");

            entity.HasOne(d => d.User).WithMany(p => p.Reservations)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("reservations_ibfk_1");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("rooms");

            entity.HasIndex(e => e.Name, "name").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Capacity)
                .HasColumnType("int(11)")
                .HasColumnName("capacity");
            entity.Property(e => e.Location)
                .HasMaxLength(100)
                .HasColumnName("location");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("students");

            entity.HasIndex(e => e.ClassId, "class_id");

            entity.HasIndex(e => e.UserId, "user_id");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Birthdate).HasColumnName("birthdate");
            entity.Property(e => e.ClassId)
                .HasColumnType("int(11)")
                .HasColumnName("class_id");
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasColumnName("first_name");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            entity.Property(e => e.UserId)
                .HasColumnType("int(11)")
                .HasColumnName("user_id");

            entity.HasOne(d => d.Class).WithMany(p => p.Students)
                .HasForeignKey(d => d.ClassId)
                .HasConstraintName("students_ibfk_2");

            entity.HasOne(d => d.User).WithMany(p => p.StudentsNavigation)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("students_ibfk_1");
        });

        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("teachers");

            entity.HasIndex(e => e.UserId, "user_id");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Subject)
                .HasMaxLength(100)
                .HasColumnName("subject");
            entity.Property(e => e.UserId)
                .HasColumnType("int(11)")
                .HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Teachers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("teachers_ibfk_1");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "email").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.Role)
                .HasColumnType("enum('parent','eleve','professeur','admin')")
                .HasColumnName("role");

            entity.HasMany(d => d.Students).WithMany(p => p.Parents)
                .UsingEntity<Dictionary<string, object>>(
                    "ParentStudent",
                    r => r.HasOne<Student>().WithMany()
                        .HasForeignKey("StudentId")
                        .HasConstraintName("parent_student_ibfk_2"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("ParentId")
                        .HasConstraintName("parent_student_ibfk_1"),
                    j =>
                    {
                        j.HasKey("ParentId", "StudentId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("parent_student");
                        j.HasIndex(new[] { "StudentId" }, "student_id");
                        j.IndexerProperty<int>("ParentId")
                            .HasColumnType("int(11)")
                            .HasColumnName("parent_id");
                        j.IndexerProperty<int>("StudentId")
                            .HasColumnType("int(11)")
                            .HasColumnName("student_id");
                    });
        });

        // Ajouter la configuration pour AlertConfig
        modelBuilder.Entity<AlertConfig>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("alert_configs");
            entity.Property(e => e.Id).HasColumnType("int(11)").HasColumnName("id");
            entity.Property(e => e.MaxAbsencesBeforeAlert).HasColumnType("int(11)").HasColumnName("max_absences_before_alert");
            entity.Property(e => e.NotifyParents).HasColumnName("notify_parents");
            entity.Property(e => e.NotifyTeachers).HasColumnName("notify_teachers");
            entity.Property(e => e.NotifyAdmin).HasColumnName("notify_admin");
            entity.Property(e => e.AlertMessage).HasColumnType("text").HasColumnName("alert_message");
        });

        // Ajouter la configuration pour PointsConfig
        modelBuilder.Entity<PointsConfig>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("points_configs");
            entity.Property(e => e.Id).HasColumnType("int(11)").HasColumnName("id");
            entity.Property(e => e.PointsPerJustifiedAbsence).HasColumnType("int(11)").HasColumnName("points_per_justified_absence");
            entity.Property(e => e.PointsPerUnjustifiedAbsence).HasColumnType("int(11)").HasColumnName("points_per_unjustified_absence");
            entity.Property(e => e.PointsPerLateArrival).HasColumnType("int(11)").HasColumnName("points_per_late_arrival");
            entity.Property(e => e.BonusPointsForPerfectAttendance).HasColumnType("int(11)").HasColumnName("bonus_points_for_perfect_attendance");
            entity.Property(e => e.BonusPointsPerMonth).HasColumnType("int(11)").HasColumnName("bonus_points_per_month");
        });

        // Ajouter la configuration pour PointsHistory
        modelBuilder.Entity<PointsHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("points_history");
            entity.HasIndex(e => e.StudentId, "student_id");
            entity.Property(e => e.Id).HasColumnType("int(11)").HasColumnName("id");
            entity.Property(e => e.StudentId).HasColumnType("int(11)").HasColumnName("student_id");
            entity.Property(e => e.Date).HasColumnType("datetime").HasColumnName("date");
            entity.Property(e => e.Points).HasColumnType("int(11)").HasColumnName("points");
            entity.Property(e => e.Reason).HasColumnType("text").HasColumnName("reason");
            entity.Property(e => e.Type).HasMaxLength(50).HasColumnName("type");

            entity.HasOne(d => d.Student).WithMany(p => p.PointsHistory)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("points_history_ibfk_1");
        });

        // Ajouter la configuration pour AppConfig
        modelBuilder.Entity<AppConfig>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("app_configs");
            entity.Property(e => e.Id).HasColumnType("int(11)").HasColumnName("id");
            entity.Property(e => e.Key).HasMaxLength(100).HasColumnName("key");
            entity.Property(e => e.Value).HasColumnType("text").HasColumnName("value");
        });

        // Ajouter la configuration pour Schedule
        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("schedules");
            entity.HasIndex(e => e.ClassId, "class_id");
            entity.Property(e => e.Id).HasColumnType("int(11)").HasColumnName("id");
            entity.Property(e => e.ClassId).HasColumnType("int(11)").HasColumnName("class_id");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.StartTime).HasColumnType("time").HasColumnName("start_time");
            entity.Property(e => e.EndTime).HasColumnType("time").HasColumnName("end_time");
            entity.Property(e => e.Subject).HasMaxLength(100).HasColumnName("subject");
            entity.Property(e => e.Description).HasColumnType("text").HasColumnName("description");

            entity.HasOne(d => d.Class).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.ClassId)
                .HasConstraintName("schedules_ibfk_1");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

