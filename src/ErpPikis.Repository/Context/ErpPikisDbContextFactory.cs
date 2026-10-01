using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ErpPikis.Repository.Context;

public class ErpPikisDbContextFactory : IDesignTimeDbContextFactory<ErpPikisDbContext>
{
    public ErpPikisDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ErpPikisDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=erp_pikis;Username=postgres;Password=postgres")
            .Options;

        return new ErpPikisDbContext(options);
    }
}
