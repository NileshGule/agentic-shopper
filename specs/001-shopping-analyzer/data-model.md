# Phase 1: Data Model

**Feature**: Smart Shopping Pattern Analyzer & Recommender  
**Date**: December 13, 2025  
**Status**: Design Complete

## Entity Relationship Diagram

```
┌──────────────────┐
│  FamilyAccount   │
│────────────────  │
│ + Id             │
│ + Name           │
│ + CreatedDate    │
│ + Settings       │
└────────┬─────────┘
         │
         │ 1:N
         │
         ▼
┌──────────────────┐
│  UserProfile     │
│──────────────────│
│ + Id             │
│ + FamilyId       │◄────┐
│ + Name           │     │
│ + Email          │     │
│ + Role           │     │
│ + CreatedDate    │     │
└────────┬─────────┘     │
         │               │
         │ 1:N           │ M:N
         │               │
         ▼               │
┌──────────────────┐     │
│    Receipt       │     │
│──────────────────│     │
│ + Id             │     │
│ + FamilyId       │     │
│ + UploadedBy     │─────┘
│ + StoreName      │
│ + PurchaseDate   │
│ + TotalAmount    │
│ + ImageUrl       │
│ + ConfidenceScore│
│ + Status         │
└────────┬─────────┘
         │
         │ 1:N
         │
         ▼
┌──────────────────┐      ┌──────────────────┐
│    Purchase      │──N:1─│    Product       │
│──────────────────│      │──────────────────│
│ + Id             │      │ + Id             │
│ + ReceiptId      │      │ + Name           │
│ + ProductId      │      │ + NormalizedName │
│ + Quantity       │      │ + CategoryId     │
│ + UnitPrice      │      │ + AveragePrice   │
│ + TotalPrice     │      │ + Frequency      │
│ + PurchaseDate   │      │ + LastPurchased  │
└──────────────────┘      │ + Notes          │
                          │ + Tags           │
                          │ + CreatedBy      │
                          └────────┬─────────┘
                                   │
                                   │ N:1
                                   │
                                   ▼
                          ┌──────────────────┐
                          │    Category      │
                          │──────────────────│
                          │ + Id             │
                          │ + Name           │
                          │ + Description    │
                          │ + IsCustom       │
                          │ + FamilyId       │
                          └──────────────────┘

┌──────────────────┐      ┌──────────────────┐
│  ShoppingList    │──1:N─│ShoppingListItem  │
│──────────────────│      │──────────────────│
│ + Id             │      │ + Id             │
│ + FamilyId       │      │ + ListId         │
│ + Name           │      │ + ProductId      │
│ + CreatedBy      │      │ + Quantity       │
│ + CreatedDate    │      │ + IsPurchased    │
│ + Status         │      │ + Urgency        │
│ + SharedWith     │      │ + AddedBy        │
└────────┬─────────┘      │ + Source         │
         │                └──────────────────┘
         │
         │ N:M
         │
┌────────▼─────────┐
│    Budget        │
│──────────────────│
│ + Id             │
│ + FamilyId       │
│ + CategoryId     │
│ + Amount         │
│ + Period         │
│ + CurrentSpent   │
│ + StartDate      │
│ + EndDate        │
│ + AlertThreshold │
└──────────────────┘

┌──────────────────┐
│    Promotion     │
│──────────────────│
│ + Id             │
│ + ProductName    │
│ + StoreName      │
│ + OriginalPrice  │
│ + SalePrice      │
│ + DiscountPct    │
│ + StartDate      │
│ + EndDate        │
│ + CatalogWeek    │
│ + LastUpdated    │
└──────────────────┘

┌──────────────────┐
│     Store        │
│──────────────────│
│ + Id             │
│ + Name           │
│ + Location       │
│ + Type           │
└──────────────────┘
```

## Entity Definitions

### FamilyAccount

Represents a household account containing multiple user profiles and all shared data.

```csharp
public class FamilyAccount
{
    public Guid Id { get; set; }
    public string Name { get; set; } // e.g., "Smith Family"
    public DateTime CreatedDate { get; set; }
    public string Settings { get; set; } // JSON blob for preferences
    
    // Navigation properties
    public ICollection<UserProfile> Members { get; set; }
    public ICollection<Receipt> Receipts { get; set; }
    public ICollection<ShoppingList> ShoppingLists { get; set; }
    public ICollection<Budget> Budgets { get; set; }
    public ICollection<Category> CustomCategories { get; set; }
}
```

**Validations**:
- Name: Required, max 100 characters
- At least one UserProfile required

---

### UserProfile

Individual family member profile within a family account.

```csharp
public class UserProfile
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public UserRole Role { get; set; } // Admin, Member
    public DateTime CreatedDate { get; set; }
    
    // Navigation properties
    public FamilyAccount Family { get; set; }
}

public enum UserRole
{
    Admin,    // Full permissions
    Member    // View + edit shared data
}
```

**Validations**:
- Name: Required, max 50 characters
- Email: Valid email format, unique within family
- Role: Required

---

### Receipt

Represents a shopping transaction with extracted data.

```csharp
public class Receipt
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public Guid UploadedBy { get; set; }
    public string StoreName { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string ImageUrl { get; set; } // Blob storage URL
    public double ConfidenceScore { get; set; } // OCR confidence 0.0-1.0
    public ReceiptStatus Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? VerifiedDate { get; set; }
    
    // Navigation properties
    public FamilyAccount Family { get; set; }
    public UserProfile UploadedByUser { get; set; }
    public ICollection<Purchase> Purchases { get; set; }
}

public enum ReceiptStatus
{
    Pending,     // OCR in progress
    NeedsReview, // Low confidence, needs manual review
    Verified,    // User confirmed
    Archived     // Older receipts
}
```

**Validations**:
- StoreName: Required, max 100 characters
- PurchaseDate: Cannot be future date
- TotalAmount: Must be positive
- ImageUrl: Required, valid URL
- ConfidenceScore: Between 0.0 and 1.0

**Indexes**:
- `IX_Receipt_FamilyId_PurchaseDate` (for date range queries)
- `IX_Receipt_Status` (for filtering pending reviews)

---

### Product

Unique product with purchase history and frequency.

```csharp
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } // Original name
    public string NormalizedName { get; set; } // For matching (lowercase, trimmed)
    public Guid? CategoryId { get; set; }
    public decimal AveragePrice { get; set; }
    public PurchaseFrequency Frequency { get; set; }
    public DateTime? LastPurchased { get; set; }
    public string Notes { get; set; } // User notes (max 500 chars)
    public string Tags { get; set; } // JSON array of tags
    public Guid CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool FrequencyPaused { get; set; }
    
    // Navigation properties
    public Category Category { get; set; }
    public ICollection<Purchase> Purchases { get; set; }
}

public enum PurchaseFrequency
{
    Unknown,       // Not enough data
    Weekly,        // 7 days ± 2
    Fortnightly,   // 14 days ± 3
    Monthly,       // 30 days ± 5
    Quarterly,     // 90 days ± 10
    Annually,      // 365 days ± 30
    Occasional     // Manual/seasonal
}
```

**Validations**:
- Name: Required, max 200 characters
- NormalizedName: Auto-generated, indexed
- AveragePrice: Must be positive
- Notes: Max 500 characters
- Tags: Valid JSON array

**Indexes**:
- `IX_Product_NormalizedName` (unique, for product matching)
- `IX_Product_CategoryId` (for category filtering)
- `IX_Product_Frequency` (for list generation)

**Business Rules**:
- Frequency auto-calculated after 3+ purchases (FR-012)
- NormalizedName: `name.Trim().ToLowerInvariant()`

---

### Purchase

Single product purchase instance linking product to receipt.

```csharp
public class Purchase
{
    public Guid Id { get; set; }
    public Guid ReceiptId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime PurchaseDate { get; set; } // Denormalized for queries
    
    // Navigation properties
    public Receipt Receipt { get; set; }
    public Product Product { get; set; }
}
```

**Validations**:
- Quantity: Must be positive
- UnitPrice: Must be positive
- TotalPrice: Must equal Quantity × UnitPrice (tolerance: 0.01)

**Indexes**:
- `IX_Purchase_ProductId_PurchaseDate` (for frequency calculation)
- `IX_Purchase_ReceiptId` (for receipt details)

---

### Category

Product grouping (predefined + custom).

```csharp
public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsCustom { get; set; }
    public Guid? FamilyId { get; set; } // Null for predefined categories
    
    // Navigation properties
    public FamilyAccount Family { get; set; }
    public ICollection<Product> Products { get; set; }
}
```

**Predefined Categories** (seeded during migration):
- Dairy
- Fresh Produce
- Meat & Seafood
- Bakery
- Pantry & Groceries
- Frozen Foods
- Beverages
- Household & Cleaning
- Personal Care
- Pet Supplies
- Other

**Validations**:
- Name: Required, max 50 characters, unique per family
- Description: Max 200 characters

---

### ShoppingList

Named collection of items for shopping trips.

```csharp
public class ShoppingList
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public string Name { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public ListStatus Status { get; set; }
    public string SharedWith { get; set; } // JSON array of UserProfile IDs
    public DateTime? CompletedDate { get; set; }
    
    // Navigation properties
    public FamilyAccount Family { get; set; }
    public UserProfile Creator { get; set; }
    public ICollection<ShoppingListItem> Items { get; set; }
}

public enum ListStatus
{
    Active,
    InProgress,  // Shopping in progress
    Completed,
    Archived
}
```

**Validations**:
- Name: Required, max 100 characters
- SharedWith: Valid JSON array of GUIDs

---

### ShoppingListItem

Individual item in a shopping list.

```csharp
public class ShoppingListItem
{
    public Guid Id { get; set; }
    public Guid ListId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public bool IsPurchased { get; set; }
    public ItemUrgency Urgency { get; set; }
    public Guid AddedBy { get; set; }
    public ItemSource Source { get; set; }
    public DateTime AddedDate { get; set; }
    public DateTime? PurchasedDate { get; set; }
    
    // Navigation properties
    public ShoppingList List { get; set; }
    public Product Product { get; set; }
    public UserProfile AddedByUser { get; set; }
}

public enum ItemUrgency
{
    Upcoming,      // Due in 3+ days
    DueThisWeek,   // Due in 1-3 days
    Overdue        // Should have been purchased
}

public enum ItemSource
{
    AutoGenerated,  // From frequency analysis
    Manual         // User-added
}
```

**Validations**:
- Quantity: Must be positive

**Indexes**:
- `IX_ShoppingListItem_ListId_IsPurchased` (for active item queries)

---

### Budget

User-defined spending limit per category and period.

```csharp
public class Budget
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public BudgetPeriod Period { get; set; }
    public decimal CurrentSpent { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal AlertThreshold { get; set; } // Percentage (e.g., 0.90 for 90%)
    public DateTime? LastAlertSent { get; set; }
    
    // Navigation properties
    public FamilyAccount Family { get; set; }
    public Category Category { get; set; }
}

public enum BudgetPeriod
{
    Weekly,
    Monthly
}
```

**Validations**:
- Amount: Must be positive
- AlertThreshold: Between 0.0 and 1.0
- EndDate: Must be after StartDate

**Business Rules**:
- CurrentSpent recalculated when new purchases added to category
- Alert triggered when CurrentSpent ≥ Amount × AlertThreshold (FR-033)
- Auto-reset at EndDate: create new budget for next period with same settings

---

### Promotion

Limited-time pricing information from store catalogs.

```csharp
public class Promotion
{
    public Guid Id { get; set; }
    public string ProductName { get; set; }
    public string NormalizedProductName { get; set; } // For matching
    public string StoreName { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string CatalogWeek { get; set; } // e.g., "2025-W50"
    public DateTime LastUpdated { get; set; }
    
    // No navigation properties (standalone reference data)
}
```

**Validations**:
- ProductName: Required
- StoreName: Required (Coles, Woolworths)
- SalePrice: Must be less than OriginalPrice
- EndDate: Must be after StartDate

**Indexes**:
- `IX_Promotion_NormalizedProductName_StoreName` (for price lookups)
- `IX_Promotion_CatalogWeek` (for weekly refreshes)

**Business Rules**:
- Refreshed weekly (FR-027)
- Expired promotions (EndDate < Now) excluded from queries
- Matching: `Product.NormalizedName` fuzzy-matched against `Promotion.NormalizedProductName`

---

### Store

Reference data for retail locations.

```csharp
public class Store
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Location { get; set; } // Address or suburb
    public StoreType Type { get; set; }
}

public enum StoreType
{
    Coles,
    Woolworths,
    Other
}
```

**Validations**:
- Name: Required, max 100 characters

**Initial Data** (seeded):
- Coles (generic)
- Woolworths (generic)

---

## Database Schema (PostgreSQL)

### Table: FamilyAccounts

```sql
CREATE TABLE FamilyAccounts (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(100) NOT NULL,
    CreatedDate TIMESTAMP NOT NULL DEFAULT NOW(),
    Settings JSONB
);
```

### Table: UserProfiles

```sql
CREATE TABLE UserProfiles (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    FamilyId UUID NOT NULL REFERENCES FamilyAccounts(Id) ON DELETE CASCADE,
    Name VARCHAR(50) NOT NULL,
    Email VARCHAR(100) NOT NULL,
    Role VARCHAR(20) NOT NULL,
    CreatedDate TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT UQ_UserProfiles_Email_FamilyId UNIQUE (Email, FamilyId)
);

CREATE INDEX IX_UserProfiles_FamilyId ON UserProfiles(FamilyId);
```

### Table: Receipts

```sql
CREATE TABLE Receipts (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    FamilyId UUID NOT NULL REFERENCES FamilyAccounts(Id) ON DELETE CASCADE,
    UploadedBy UUID NOT NULL REFERENCES UserProfiles(Id),
    StoreName VARCHAR(100) NOT NULL,
    PurchaseDate DATE NOT NULL,
    TotalAmount DECIMAL(10,2) NOT NULL CHECK (TotalAmount > 0),
    ImageUrl VARCHAR(500) NOT NULL,
    ConfidenceScore DECIMAL(3,2) CHECK (ConfidenceScore BETWEEN 0 AND 1),
    Status VARCHAR(20) NOT NULL,
    CreatedDate TIMESTAMP NOT NULL DEFAULT NOW(),
    VerifiedDate TIMESTAMP
);

CREATE INDEX IX_Receipts_FamilyId_PurchaseDate ON Receipts(FamilyId, PurchaseDate DESC);
CREATE INDEX IX_Receipts_Status ON Receipts(Status) WHERE Status IN ('Pending', 'NeedsReview');
```

### Table: Categories

```sql
CREATE TABLE Categories (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(50) NOT NULL,
    Description VARCHAR(200),
    IsCustom BOOLEAN NOT NULL DEFAULT FALSE,
    FamilyId UUID REFERENCES FamilyAccounts(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_Categories_Name_FamilyId UNIQUE (Name, FamilyId)
);

-- Seed predefined categories
INSERT INTO Categories (Name, Description, IsCustom) VALUES
    ('Dairy', 'Milk, cheese, yogurt, butter', false),
    ('Fresh Produce', 'Fruits, vegetables, herbs', false),
    ('Meat & Seafood', 'Fresh and frozen meat, fish', false),
    ('Bakery', 'Bread, pastries, cakes', false),
    ('Pantry & Groceries', 'Canned goods, pasta, rice, condiments', false),
    ('Frozen Foods', 'Frozen meals, ice cream', false),
    ('Beverages', 'Soft drinks, juice, water', false),
    ('Household & Cleaning', 'Detergents, cleaning supplies', false),
    ('Personal Care', 'Toiletries, hygiene products', false),
    ('Pet Supplies', 'Pet food and accessories', false),
    ('Other', 'Miscellaneous items', false);
```

### Table: Products

```sql
CREATE TABLE Products (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(200) NOT NULL,
    NormalizedName VARCHAR(200) NOT NULL,
    CategoryId UUID REFERENCES Categories(Id),
    AveragePrice DECIMAL(10,2) CHECK (AveragePrice >= 0),
    Frequency VARCHAR(20),
    LastPurchased DATE,
    Notes VARCHAR(500),
    Tags JSONB,
    CreatedBy UUID NOT NULL REFERENCES UserProfiles(Id),
    CreatedDate TIMESTAMP NOT NULL DEFAULT NOW(),
    FrequencyPaused BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE UNIQUE INDEX IX_Products_NormalizedName ON Products(NormalizedName);
CREATE INDEX IX_Products_CategoryId ON Products(CategoryId);
CREATE INDEX IX_Products_Frequency ON Products(Frequency) WHERE FrequencyPaused = FALSE;
```

### Table: Purchases

```sql
CREATE TABLE Purchases (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ReceiptId UUID NOT NULL REFERENCES Receipts(Id) ON DELETE CASCADE,
    ProductId UUID NOT NULL REFERENCES Products(Id),
    Quantity DECIMAL(10,2) NOT NULL CHECK (Quantity > 0),
    UnitPrice DECIMAL(10,2) NOT NULL CHECK (UnitPrice > 0),
    TotalPrice DECIMAL(10,2) NOT NULL CHECK (TotalPrice > 0),
    PurchaseDate DATE NOT NULL
);

CREATE INDEX IX_Purchases_ProductId_PurchaseDate ON Purchases(ProductId, PurchaseDate DESC);
CREATE INDEX IX_Purchases_ReceiptId ON Purchases(ReceiptId);
```

### Table: ShoppingLists

```sql
CREATE TABLE ShoppingLists (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    FamilyId UUID NOT NULL REFERENCES FamilyAccounts(Id) ON DELETE CASCADE,
    Name VARCHAR(100) NOT NULL,
    CreatedBy UUID NOT NULL REFERENCES UserProfiles(Id),
    CreatedDate TIMESTAMP NOT NULL DEFAULT NOW(),
    Status VARCHAR(20) NOT NULL,
    SharedWith JSONB,
    CompletedDate TIMESTAMP
);

CREATE INDEX IX_ShoppingLists_FamilyId_Status ON ShoppingLists(FamilyId, Status);
```

### Table: ShoppingListItems

```sql
CREATE TABLE ShoppingListItems (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ListId UUID NOT NULL REFERENCES ShoppingLists(Id) ON DELETE CASCADE,
    ProductId UUID NOT NULL REFERENCES Products(Id),
    Quantity DECIMAL(10,2) NOT NULL CHECK (Quantity > 0),
    IsPurchased BOOLEAN NOT NULL DEFAULT FALSE,
    Urgency VARCHAR(20) NOT NULL,
    AddedBy UUID NOT NULL REFERENCES UserProfiles(Id),
    Source VARCHAR(20) NOT NULL,
    AddedDate TIMESTAMP NOT NULL DEFAULT NOW(),
    PurchasedDate TIMESTAMP
);

CREATE INDEX IX_ShoppingListItems_ListId_IsPurchased ON ShoppingListItems(ListId, IsPurchased);
```

### Table: Budgets

```sql
CREATE TABLE Budgets (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    FamilyId UUID NOT NULL REFERENCES FamilyAccounts(Id) ON DELETE CASCADE,
    CategoryId UUID NOT NULL REFERENCES Categories(Id),
    Amount DECIMAL(10,2) NOT NULL CHECK (Amount > 0),
    Period VARCHAR(20) NOT NULL,
    CurrentSpent DECIMAL(10,2) NOT NULL DEFAULT 0,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL CHECK (EndDate > StartDate),
    AlertThreshold DECIMAL(3,2) NOT NULL DEFAULT 0.90 CHECK (AlertThreshold BETWEEN 0 AND 1),
    LastAlertSent TIMESTAMP
);

CREATE INDEX IX_Budgets_FamilyId_CategoryId ON Budgets(FamilyId, CategoryId);
```

### Table: Promotions

```sql
CREATE TABLE Promotions (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ProductName VARCHAR(200) NOT NULL,
    NormalizedProductName VARCHAR(200) NOT NULL,
    StoreName VARCHAR(100) NOT NULL,
    OriginalPrice DECIMAL(10,2) NOT NULL,
    SalePrice DECIMAL(10,2) NOT NULL CHECK (SalePrice < OriginalPrice),
    DiscountPercentage DECIMAL(5,2),
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL CHECK (EndDate >= StartDate),
    CatalogWeek VARCHAR(10),
    LastUpdated TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX IX_Promotions_NormalizedProductName_StoreName 
    ON Promotions(NormalizedProductName, StoreName) 
    WHERE EndDate >= CURRENT_DATE;
CREATE INDEX IX_Promotions_CatalogWeek ON Promotions(CatalogWeek);
```

### Table: Stores

```sql
CREATE TABLE Stores (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(100) NOT NULL,
    Location VARCHAR(200),
    Type VARCHAR(20) NOT NULL
);

-- Seed initial stores
INSERT INTO Stores (Name, Type) VALUES
    ('Coles', 'Coles'),
    ('Woolworths', 'Woolworths');
```

---

## Entity Framework Core Configuration

### DbContext

```csharp
public class ApplicationDbContext : DbContext
{
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
        // FamilyAccount
        modelBuilder.Entity<FamilyAccount>()
            .HasMany(f => f.Members)
            .WithOne(u => u.Family)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Product - Category
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .OnDelete(DeleteBehavior.SetNull);
        
        // Product unique index
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.NormalizedName)
            .IsUnique();
        
        // Receipt - Purchases
        modelBuilder.Entity<Receipt>()
            .HasMany(r => r.Purchases)
            .WithOne(p => p.Receipt)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Seed predefined categories (shown above in SQL)
        SeedCategories(modelBuilder);
    }
}
```

---

## Key Business Rules & Calculations

### Frequency Calculation Algorithm

```csharp
public class FrequencyCalculator
{
    public PurchaseFrequency CalculateFrequency(List<DateTime> purchaseDates)
    {
        if (purchaseDates.Count < 3)
            return PurchaseFrequency.Unknown;
        
        // Calculate average interval between purchases
        var intervals = new List<int>();
        for (int i = 1; i < purchaseDates.Count; i++)
        {
            intervals.Add((purchaseDates[i] - purchaseDates[i-1]).Days);
        }
        
        var avgInterval = intervals.Average();
        
        return avgInterval switch
        {
            <= 9 => PurchaseFrequency.Weekly,        // 7 ± 2 days
            <= 17 => PurchaseFrequency.Fortnightly,  // 14 ± 3 days
            <= 35 => PurchaseFrequency.Monthly,      // 30 ± 5 days
            <= 100 => PurchaseFrequency.Quarterly,   // 90 ± 10 days
            <= 395 => PurchaseFrequency.Annually,    // 365 ± 30 days
            _ => PurchaseFrequency.Occasional
        };
    }
}
```

### Urgency Classification

```csharp
public class UrgencyClassifier
{
    public ItemUrgency ClassifyUrgency(Product product, DateTime today)
    {
        if (!product.LastPurchased.HasValue || product.FrequencyPaused)
            return ItemUrgency.Upcoming;
        
        var daysSinceLastPurchase = (today - product.LastPurchased.Value).Days;
        var expectedInterval = GetExpectedInterval(product.Frequency);
        
        if (daysSinceLastPurchase > expectedInterval)
            return ItemUrgency.Overdue;
        
        if (daysSinceLastPurchase >= expectedInterval - 3)
            return ItemUrgency.DueThisWeek;
        
        return ItemUrgency.Upcoming;
    }
    
    private int GetExpectedInterval(PurchaseFrequency frequency)
    {
        return frequency switch
        {
            PurchaseFrequency.Weekly => 7,
            PurchaseFrequency.Fortnightly => 14,
            PurchaseFrequency.Monthly => 30,
            PurchaseFrequency.Quarterly => 90,
            PurchaseFrequency.Annually => 365,
            _ => int.MaxValue
        };
    }
}
```

### Budget Tracking

```csharp
public class BudgetTracker
{
    public void UpdateBudgetSpending(Guid budgetId, decimal newPurchaseAmount)
    {
        var budget = _dbContext.Budgets.Find(budgetId);
        budget.CurrentSpent += newPurchaseAmount;
        
        // Check alert threshold
        if (budget.CurrentSpent >= budget.Amount * budget.AlertThreshold 
            && !budget.LastAlertSent.HasValue)
        {
            SendBudgetAlert(budget);
            budget.LastAlertSent = DateTime.UtcNow;
        }
        
        _dbContext.SaveChanges();
    }
}
```

---

## Next Steps

1. **API Contracts** (`contracts/` directory) - Define OpenAPI specs
2. **Quickstart Guide** (`quickstart.md`) - Setup and development instructions
3. **Agent Context Update** - Run update script to add model info to AI context

**Status**: ✅ **DATA MODEL COMPLETE - READY FOR API CONTRACT DESIGN**
