using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Domain;

namespace SchoolAdmission.Api.Infrastructure;

public class AdmissionDbContext(DbContextOptions<AdmissionDbContext> options) : DbContext(options)
{
    /// <summary>Name of the partial unique index that backs "one active application per student per class".</summary>
    public const string ActiveHoldPerStudentIndex = "ux_holds_class_student_active";

    public DbSet<SchoolClass> Classes => Set<SchoolClass>();
    public DbSet<Hold> Holds => Set<Hold>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SchoolClass>(e =>
        {
            e.ToTable("classes", t => t.HasCheckConstraint("ck_classes_total_seats_positive", "total_seats > 0"));
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.Name).HasColumnName("name").HasMaxLength(SchoolClass.MaxNameLength);
            e.Property(c => c.TotalSeats).HasColumnName("total_seats");
            e.Property(c => c.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<Hold>(e =>
        {
            e.ToTable("holds");
            e.HasKey(h => h.Id);
            e.Property(h => h.Id).HasColumnName("id");
            e.Property(h => h.ClassId).HasColumnName("class_id");
            e.Property(h => h.StudentId).HasColumnName("student_id").HasMaxLength(StudentId.Length).IsFixedLength();
            e.Property(h => h.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16);
            e.Property(h => h.CreatedAt).HasColumnName("created_at");
            e.Property(h => h.ExpiresAt).HasColumnName("expires_at");
            e.Property(h => h.ConfirmedAt).HasColumnName("confirmed_at");
            e.Property(h => h.CancelledAt).HasColumnName("cancelled_at");

            e.HasOne(h => h.Class).WithMany(c => c.Holds).HasForeignKey(h => h.ClassId).OnDelete(DeleteBehavior.Restrict);

            // Seat counting per class.
            e.HasIndex(h => new { h.ClassId, h.Status, h.ExpiresAt }).HasDatabaseName("ix_holds_class_status_expires");
            // "My applications".
            e.HasIndex(h => h.StudentId).HasDatabaseName("ix_holds_student");
            // Safety net under the service-level check: a student can never have two
            // HELD/CONFIRMED rows for the same class, even if application code has a bug.
            e.HasIndex(h => new { h.ClassId, h.StudentId })
                .IsUnique()
                .HasFilter($"status IN ('{nameof(HoldStatus.Held)}', '{nameof(HoldStatus.Confirmed)}')")
                .HasDatabaseName(ActiveHoldPerStudentIndex);
        });
    }
}
