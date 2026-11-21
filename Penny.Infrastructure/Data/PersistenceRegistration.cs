using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Penny.Infrastructure.Data;

public static class PersistenceRegistration
{
    public static IServiceCollection AddPennyPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        return services.AddDbContext<PennyDbContext>(options => options.UseSqlServer(connectionString));
    }
}
