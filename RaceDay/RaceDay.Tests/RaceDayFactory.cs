using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Tests;

public class RaceDayFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<RaceDayContext>)
                            || d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration"))
                .ToList();

            foreach (var descriptor in toRemove)
                services.Remove(descriptor);

            services.AddDbContext<RaceDayContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}
