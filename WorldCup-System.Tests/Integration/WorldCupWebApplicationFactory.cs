using Data.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WorldCup_System.Tests.Integration
{
    public class WorldCupWebApplicationFactory : WebApplicationFactory<Program>
    {
        public WorldCupWebApplicationFactory()
        {
            Environment.SetEnvironmentVariable("JWT__Secret", "integration-test-jwt-secret-key-32chars");
            Environment.SetEnvironmentVariable(
                "ConnectionStrings__DefaultConnection",
                "Host=localhost;Database=test;Username=test;Password=test");
            Environment.SetEnvironmentVariable("DevSeed__WorldCup2026", "false");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("JWT:Secret", "integration-test-jwt-secret-key-32chars");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=localhost;Database=test;Username=test;Password=test");
            builder.UseSetting("DevSeed:WorldCup2026", "false");

            builder.ConfigureAppConfiguration((context, configurationBuilder) =>
            {
                Dictionary<string, string?> settings = new Dictionary<string, string?>
                {
                    ["JWT:Secret"] = "integration-test-jwt-secret-key-32chars",
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test",
                    ["DevSeed:WorldCup2026"] = "false"
                };
                configurationBuilder.AddInMemoryCollection(settings);
            });

            builder.ConfigureServices(services =>
            {
                List<ServiceDescriptor> descriptors = services
                    .Where(serviceDescriptor =>
                        serviceDescriptor.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                        || serviceDescriptor.ServiceType == typeof(DbContextOptions)
                        || serviceDescriptor.ServiceType == typeof(ApplicationDbContext))
                    .ToList();

                foreach (ServiceDescriptor descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("WorldCupIntegrationTests");
                });
            });
        }
    }
}
