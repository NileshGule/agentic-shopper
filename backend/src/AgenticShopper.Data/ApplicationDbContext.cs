using AgenticShopper.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AgenticShopper.Data;

/// <summary>
/// Entity Framework Core database context for Agentic Shopper application
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // Entity sets
    public DbSet<FamilyAccount> FamilyAccounts { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<Receipt> Receipts { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Purchase> Purchases { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<ShoppingList> ShoppingLists { get; set; }
    public DbSet<ShoppingListItem> ShoppingListItems { get; set; }
    public DbSet<Budget> Budgets { get; set; }
    public DbSet<Promotion> Promotions { get; set; }
    public DbSet<Store> Stores { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure FamilyAccount relationships
        modelBuilder.Entity<FamilyAccount>()
            .HasMany(f => f.Members)
            .WithOne(u => u.Family)
            .HasForeignKey(u => u.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FamilyAccount>()
            .HasMany(f => f.Receipts)
            .WithOne(r => r.Family)
            .HasForeignKey(r => r.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Product relationships
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.NormalizedName)
            .IsUnique();

        // Configure Receipt relationships
        modelBuilder.Entity<Receipt>()
            .HasMany(r => r.Purchases)
            .WithOne(p => p.Receipt)
            .HasForeignKey(p => p.ReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes for performance
        modelBuilder.Entity<Receipt>()
            .HasIndex(r => new { r.FamilyId, r.PurchaseDate });

        modelBuilder.Entity<Receipt>()
            .HasIndex(r => r.Status);

        modelBuilder.Entity<Purchase>()
            .HasIndex(p => new { p.ProductId, p.PurchaseDate });

        modelBuilder.Entity<ShoppingListItem>()
            .HasIndex(i => new { i.ListId, i.IsPurchased });

        // Seed predefined categories
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = "Dairy", Description = "Milk, cheese, yogurt, butter", IsCustom = false },
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), Name = "Fresh Produce", Description = "Fruits, vegetables, herbs", IsCustom = false },
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), Name = "Meat & Seafood", Description = "Fresh and frozen meat, fish", IsCustom = false },
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-000000000004"), Name = "Bakery", Description = "Bread, pastries, cakes", IsCustom = false },
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-000000000005"), Name = "Pantry & Groceries", Description = "Canned goods, pasta, rice, condiments", IsCustom = false },
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-000000000006"), Name = "Frozen Foods", Description = "Frozen meals, ice cream", IsCustom = false },
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-000000000007"), Name = "Beverages", Description = "Soft drinks, juice, water", IsCustom = false },
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-000000000008"), Name = "Household & Cleaning", Description = "Detergents, cleaning supplies", IsCustom = false },
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-000000000009"), Name = "Personal Care", Description = "Toiletries, hygiene products", IsCustom = false },
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-00000000000a"), Name = "Pet Supplies", Description = "Pet food and accessories", IsCustom = false },
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-00000000000b"), Name = "Other", Description = "Miscellaneous items", IsCustom = false }
        );

        // Seed predefined stores
        modelBuilder.Entity<Store>().HasData(
            new Store { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Name = "Coles", Type = StoreType.Coles },
            new Store { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Name = "Woolworths", Type = StoreType.Woolworths }
        );
    }
}
