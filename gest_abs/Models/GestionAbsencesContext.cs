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

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Reservation> Reservations { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<Teacher> Teachers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Charger les configurations depuis appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) // Chemin de base du projet
                .AddJsonFile("appsettings.json") // Fichier JSON à charger
                .Build();

            // Lire la chaîne de connexion dans le fichier
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Configurer le DbContext avec la chaîne de connexion
            optionsBuilder.UseMySql(
                connectionString,
                Microsoft.EntityFrameworkCore.ServerVersion.Parse("10.11.8-mariadb")
            );
        }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
