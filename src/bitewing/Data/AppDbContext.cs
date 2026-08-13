using Microsoft.EntityFrameworkCore;

namespace bitewing.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}