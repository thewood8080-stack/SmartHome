using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartHomeApi.Models;
using SmartHomeApi.Services;

namespace SmartHomeApi.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ICurrentUserService? _currentUser;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService? currentUser = null)
        : base(options)
    {
        _currentUser = currentUser;
    }

    /// <summary>
    /// משק הבית של המשתמש המחובר. הפילטרים הגלובליים משווים אליו,
    /// וכשהוא null אף שורה לא מוחזרת — ברירת מחדל בטוחה.
    /// </summary>
    public int? CurrentHouseholdId => _currentUser?.HouseholdId;

    public DbSet<Household> Households => Set<Household>();
    public DbSet<HouseTask> HouseTasks => Set<HouseTask>();
    public DbSet<ShoppingItem> ShoppingItems => Set<ShoppingItem>();
    public DbSet<PhotoAlbum> PhotoAlbums => Set<PhotoAlbum>();
    public DbSet<Photo> Photos => Set<Photo>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Gift> Gifts => Set<Gift>();
    public DbSet<BudgetEntry> BudgetEntries => Set<BudgetEntry>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureHousehold(builder);
        ConfigureUser(builder);
        ConfigureTasks(builder);
        ConfigureShopping(builder);
        ConfigurePhotos(builder);
        ConfigureEventsAndGifts(builder);
        ConfigureBudget(builder);
        ConfigureInventory(builder);
        ConfigureMedical(builder);
        ConfigureVehicles(builder);
        ConfigureNotifications(builder);

        ApplyHouseholdQueryFilters(builder);
    }

    /// <summary>
    /// מחיל סינון לפי משק בית על כל ישות שמממשת IHouseholdOwned.
    /// ApplicationUser מוחרג בכוונה — פילטר עליו היה שובר את ההתחברות
    /// (בזמן Login עוד אין משק בית ב-Session, ו-UserManager לא יודע לעקוף פילטרים).
    /// סינון משתמשים נעשה במפורש בקונטרולרים.
    /// </summary>
    private void ApplyHouseholdQueryFilters(ModelBuilder builder)
    {
        var applyMethod = typeof(ApplicationDbContext)
            .GetMethod(nameof(ApplyHouseholdFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (entityType.BaseType is not null) continue;
            if (!typeof(IHouseholdOwned).IsAssignableFrom(entityType.ClrType)) continue;

            applyMethod.MakeGenericMethod(entityType.ClrType).Invoke(this, [builder]);
        }
    }

    private void ApplyHouseholdFilter<TEntity>(ModelBuilder builder)
        where TEntity : class, IHouseholdOwned
    {
        builder.Entity<TEntity>().HasQueryFilter(e => e.HouseholdId == CurrentHouseholdId);
    }

    private static void ConfigureHousehold(ModelBuilder builder)
    {
        builder.Entity<Household>(e =>
        {
            e.Property(h => h.Name).IsRequired().HasMaxLength(100);
            e.Property(h => h.InviteCode).IsRequired().HasMaxLength(6).IsFixedLength();
            e.HasIndex(h => h.InviteCode).IsUnique();
        });
    }

    private static void ConfigureUser(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            e.Property(u => u.ProfileImageUrl).HasMaxLength(500);

            e.HasOne(u => u.Household)
                .WithMany(h => h.Members)
                .HasForeignKey(u => u.HouseholdId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(u => u.HouseholdId);
        });
    }

    private static void ConfigureTasks(ModelBuilder builder)
    {
        builder.Entity<HouseTask>(e =>
        {
            e.Property(t => t.Title).IsRequired().HasMaxLength(200);
            e.Property(t => t.Description).IsRequired().HasDefaultValue(string.Empty);

            e.HasOne(t => t.AssignedTo)
                .WithMany()
                .HasForeignKey(t => t.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(t => t.CreatedBy)
                .WithMany()
                .HasForeignKey(t => t.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(t => t.Household)
                .WithMany()
                .HasForeignKey(t => t.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(t => t.HouseholdId);
            e.HasIndex(t => new { t.HouseholdId, t.Status });
        });
    }

    private static void ConfigureShopping(ModelBuilder builder)
    {
        builder.Entity<ShoppingItem>(e =>
        {
            e.Property(i => i.Name).IsRequired().HasMaxLength(200);
            e.Property(i => i.Unit).HasMaxLength(30);
            e.Property(i => i.Category).HasMaxLength(50);

            e.HasOne(i => i.AddedBy)
                .WithMany()
                .HasForeignKey(i => i.AddedById)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(i => i.BoughtBy)
                .WithMany()
                .HasForeignKey(i => i.BoughtById)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(i => i.Household)
                .WithMany()
                .HasForeignKey(i => i.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(i => new { i.HouseholdId, i.IsBought });
        });
    }

    private static void ConfigurePhotos(ModelBuilder builder)
    {
        builder.Entity<PhotoAlbum>(e =>
        {
            e.Property(a => a.Name).IsRequired().HasMaxLength(150);
            e.Property(a => a.Description).IsRequired().HasDefaultValue(string.Empty);
            e.Property(a => a.CoverPhotoUrl).HasMaxLength(500);

            e.HasOne(a => a.Owner)
                .WithMany()
                .HasForeignKey(a => a.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.Household)
                .WithMany()
                .HasForeignKey(a => a.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(a => a.HouseholdId);
        });

        builder.Entity<Photo>(e =>
        {
            e.Property(p => p.CloudinaryUrl).IsRequired().HasMaxLength(500);
            e.Property(p => p.CloudinaryPublicId).IsRequired().HasMaxLength(200);
            e.Property(p => p.Caption).HasMaxLength(500);

            e.HasOne(p => p.PhotoAlbum)
                .WithMany(a => a.Photos)
                .HasForeignKey(p => p.PhotoAlbumId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.UploadedBy)
                .WithMany()
                .HasForeignKey(p => p.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Household נשאר Restrict כי מחיקת האלבום כבר גוררת את התמונות,
            // ושני מסלולי cascade לאותה טבלה מסבכים את ה-schema ללא צורך.
            e.HasOne(p => p.Household)
                .WithMany()
                .HasForeignKey(p => p.HouseholdId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(p => new { p.HouseholdId, p.PhotoAlbumId });
        });
    }

    private static void ConfigureEventsAndGifts(ModelBuilder builder)
    {
        builder.Entity<Event>(e =>
        {
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.Property(x => x.Description).IsRequired().HasDefaultValue(string.Empty);
            e.Property(x => x.Color).IsRequired().HasMaxLength(7).HasDefaultValue("#1E3A5F");

            e.HasOne(x => x.CreatedBy)
                .WithMany()
                .HasForeignKey(x => x.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Household)
                .WithMany()
                .HasForeignKey(x => x.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.HouseholdId, x.StartDate });
        });

        builder.Entity<Gift>(e =>
        {
            e.Property(g => g.RecipientName).IsRequired().HasMaxLength(100);
            e.Property(g => g.Occasion).IsRequired().HasMaxLength(100);
            e.Property(g => g.Ideas).IsRequired().HasDefaultValue(string.Empty);
            e.Property(g => g.PurchasedItem).HasMaxLength(200);
            e.Property(g => g.Note).HasMaxLength(500);

            e.HasOne(g => g.Event)
                .WithMany()
                .HasForeignKey(g => g.EventId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(g => g.AddedBy)
                .WithMany()
                .HasForeignKey(g => g.AddedById)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(g => g.Household)
                .WithMany()
                .HasForeignKey(g => g.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(g => new { g.HouseholdId, g.Date });
        });
    }

    private static void ConfigureBudget(ModelBuilder builder)
    {
        builder.Entity<BudgetEntry>(e =>
        {
            // SQLite מאחסן decimal כטקסט; ציון סוג מפורש מונע את אזהרת EF על דיוק.
            e.Property(b => b.Amount).HasColumnType("decimal(18,2)");
            e.Property(b => b.Title).IsRequired().HasMaxLength(200);
            e.Property(b => b.Category).IsRequired().HasMaxLength(50);
            e.Property(b => b.Note).HasMaxLength(500);

            e.HasOne(b => b.AddedBy)
                .WithMany()
                .HasForeignKey(b => b.AddedById)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(b => b.Household)
                .WithMany()
                .HasForeignKey(b => b.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(b => new { b.HouseholdId, b.Date });
        });
    }

    private static void ConfigureInventory(ModelBuilder builder)
    {
        builder.Entity<InventoryItem>(e =>
        {
            e.Property(i => i.Name).IsRequired().HasMaxLength(200);
            e.Property(i => i.Unit).HasMaxLength(30);
            e.Property(i => i.Location).HasMaxLength(100);
            e.Property(i => i.Note).HasMaxLength(500);

            e.HasOne(i => i.AddedBy)
                .WithMany()
                .HasForeignKey(i => i.AddedById)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(i => i.Household)
                .WithMany()
                .HasForeignKey(i => i.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(i => i.HouseholdId);
        });
    }

    private static void ConfigureMedical(ModelBuilder builder)
    {
        builder.Entity<MedicalRecord>(e =>
        {
            e.Property(m => m.Title).IsRequired().HasMaxLength(200);
            e.Property(m => m.Doctor).HasMaxLength(100);
            e.Property(m => m.Clinic).HasMaxLength(100);
            e.Property(m => m.Notes).HasMaxLength(1000);

            e.HasOne(m => m.Member)
                .WithMany()
                .HasForeignKey(m => m.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(m => m.AddedBy)
                .WithMany()
                .HasForeignKey(m => m.AddedById)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(m => m.Household)
                .WithMany()
                .HasForeignKey(m => m.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(m => new { m.HouseholdId, m.MemberId });
        });
    }

    private static void ConfigureVehicles(ModelBuilder builder)
    {
        builder.Entity<Vehicle>(e =>
        {
            e.Property(v => v.Name).IsRequired().HasMaxLength(100);
            e.Property(v => v.LicensePlate).IsRequired().HasMaxLength(20);
            e.Property(v => v.Model).HasMaxLength(100);
            e.Property(v => v.FuelType).HasMaxLength(30);
            e.Property(v => v.Notes).HasMaxLength(1000);

            e.HasOne(v => v.AddedBy)
                .WithMany()
                .HasForeignKey(v => v.AddedById)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(v => v.Household)
                .WithMany()
                .HasForeignKey(v => v.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(v => v.HouseholdId);
        });
    }

    private static void ConfigureNotifications(ModelBuilder builder)
    {
        builder.Entity<Notification>(e =>
        {
            e.Property(n => n.Title).IsRequired().HasMaxLength(200);
            e.Property(n => n.Message).IsRequired().HasMaxLength(1000);

            e.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(n => n.Household)
                .WithMany()
                .HasForeignKey(n => n.HouseholdId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(n => new { n.HouseholdId, n.UserId, n.IsRead });
            e.HasIndex(n => n.ScheduledFor);
        });
    }
}
