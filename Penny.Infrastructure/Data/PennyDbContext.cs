using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Penny.Domain.Account;
using Penny.Domain.Category;
using Penny.Domain.Movement;


namespace Penny.Infrastructure.Data;

public class PennyDbContext : IdentityDbContext
{
    public PennyDbContext(DbContextOptions<PennyDbContext> options) : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Subcategory> Subcategories => Set<Subcategory>();
    public DbSet<Movement> Movements => Set<Movement>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Account>(account =>
        {
            account.ToTable(nameof(Account));
            account.Property(entity => entity.InitialBalance)
                .HasPrecision(18, 2);

            account.HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(entity => entity.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Category>(category =>
        {
            category.ToTable(nameof(Category));

            category.HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(entity => entity.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Subcategory>(subcategory =>
        {
            subcategory.ToTable(nameof(Subcategory));

            subcategory.HasOne<Category>()
                .WithMany()
                .HasForeignKey(entity => entity.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Movement>(movement =>
        {
            movement.ToTable(nameof(Movement), table =>
            {
                table.HasCheckConstraint("CK_Movement_Amount_Positive", "[Amount] > 0");
                table.HasCheckConstraint("CK_Movement_Category_Required",
                    $"([Type] <> {(int)MovementType.Expense} AND [SubcategoryId] IS NULL) OR [CategoryId] IS NOT NULL");
            });

            movement.Property(entity => entity.Amount)
                .HasPrecision(18, 2);

            movement.HasOne<Account>()
                .WithMany()
                .HasForeignKey(entity => entity.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            movement.HasOne<Category>()
                .WithMany()
                .HasForeignKey(entity => entity.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            movement.HasOne<Subcategory>()
                .WithMany()
                .HasForeignKey(entity => new { entity.CategoryId, entity.SubcategoryId })
                .HasPrincipalKey(entity => new { entity.CategoryId, entity.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
