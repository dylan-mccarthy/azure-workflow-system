using AzureWorkflowSystem.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AzureWorkflowSystem.Api.Data;

public class WorkflowDbContext : DbContext
{
    public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<Attachment> Attachments { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<SlaConfiguration> SlaConfigurations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Role).HasConversion<string>();
        });

        // Ticket configuration
        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.Priority).HasConversion<string>();
            entity.Property(e => e.Category).HasConversion<string>();
            
            // Relationships
            entity.HasOne(e => e.CreatedBy)
                  .WithMany(u => u.CreatedTickets)
                  .HasForeignKey(e => e.CreatedById)
                  .OnDelete(DeleteBehavior.Restrict);
                  
            entity.HasOne(e => e.AssignedTo)
                  .WithMany(u => u.AssignedTickets)
                  .HasForeignKey(e => e.AssignedToId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Attachment configuration
        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.BlobUrl).IsRequired();
            
            entity.HasOne(e => e.Ticket)
                  .WithMany(t => t.Attachments)
                  .HasForeignKey(e => e.TicketId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.UploadedBy)
                  .WithMany()
                  .HasForeignKey(e => e.UploadedById)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            
            entity.HasOne(e => e.Ticket)
                  .WithMany(t => t.AuditLogs)
                  .HasForeignKey(e => e.TicketId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.User)
                  .WithMany(u => u.AuditLogs)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // SlaConfiguration configuration
        modelBuilder.Entity<SlaConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Priority).HasConversion<string>();
            entity.Property(e => e.Category).HasConversion<string>();
            
            // Ensure unique combination of Priority and Category
            entity.HasIndex(e => new { e.Priority, e.Category }).IsUnique();
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed default SLA configurations
        modelBuilder.Entity<SlaConfiguration>().HasData(
            new { Id = 1, Priority = TicketPriority.Critical, Category = TicketCategory.Incident, ResponseTimeMinutes = 15, ResolutionTimeMinutes = 240, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new { Id = 2, Priority = TicketPriority.High, Category = TicketCategory.Incident, ResponseTimeMinutes = 30, ResolutionTimeMinutes = 480, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new { Id = 3, Priority = TicketPriority.Medium, Category = TicketCategory.Incident, ResponseTimeMinutes = 60, ResolutionTimeMinutes = 1440, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new { Id = 4, Priority = TicketPriority.Low, Category = TicketCategory.Incident, ResponseTimeMinutes = 120, ResolutionTimeMinutes = 2880, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new { Id = 5, Priority = TicketPriority.Critical, Category = TicketCategory.Alert, ResponseTimeMinutes = 10, ResolutionTimeMinutes = 120, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new { Id = 6, Priority = TicketPriority.High, Category = TicketCategory.Alert, ResponseTimeMinutes = 15, ResolutionTimeMinutes = 240, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new { Id = 7, Priority = TicketPriority.Medium, Category = TicketCategory.Access, ResponseTimeMinutes = 240, ResolutionTimeMinutes = 1440, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new { Id = 8, Priority = TicketPriority.Medium, Category = TicketCategory.NewResource, ResponseTimeMinutes = 480, ResolutionTimeMinutes = 2880, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );

        // Seed default admin user
        modelBuilder.Entity<User>().HasData(
            new { Id = 1, Email = "admin@azureworkflow.com", FirstName = "System", LastName = "Administrator", Role = UserRole.Admin, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
    }
}