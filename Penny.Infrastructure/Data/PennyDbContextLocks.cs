using Microsoft.EntityFrameworkCore;

namespace Penny.Infrastructure.Data;

public static class PennyDbContextLocks
{
    public static Task<bool> LockUserAsync(
        this PennyDbContext db, string userId, CancellationToken cancellationToken)
    {
        // Call inside a transaction so competing requests wait until it completes.
        return db.Users
            .FromSqlInterpolated($"SELECT * FROM [AspNetUsers] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {userId}")
            .AnyAsync(cancellationToken);
    }
}
