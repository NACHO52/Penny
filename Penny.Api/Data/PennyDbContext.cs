using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Penny.Api.Data;

public class PennyDbContext : IdentityDbContext
{
    public PennyDbContext(DbContextOptions<PennyDbContext> options) : base(options)
    {
    }
}