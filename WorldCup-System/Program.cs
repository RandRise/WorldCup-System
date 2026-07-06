using Serilog;
using Serilog.Formatting.Compact;
using Core.Services.Bets;
using Core.Services.Cards;
using Core.Services.Cities;
using Core.Services.Coaches;
using Core.Services.Countries;
using Core.Services.Goals;
using Core.Services.Groups;
using Core.Services.Matches;
using Core.Services.PlayerPositions;
using Core.Services.Players;
using Core.Services.Seeding;
using Core.Services.Stadiums;
using Core.Services.Standings;
using Core.Services.Stats;
using Core.Services.Teams;
using Core.Services.Users;
using Core.Services.WorldCups;
using Data.Context;
using Data.Entities;
using Data.Repos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(new RenderedCompactJsonFormatter());
});

string? jwtSecret = configuration["JWT:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new InvalidOperationException(
        "JWT:Secret is not configured. Set it via User Secrets (dotnet user-secrets set \"JWT:Secret\" \"<value>\") or an environment variable.");
}

string? connectionString = configuration["ConnectionStrings:DefaultConnection"];
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection is not configured. Set it via User Secrets or an environment variable.");
}

// Add services to the container.

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDev", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(e =>
{
    e.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Scheme = "Bearer",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.Http

    });

    e.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddScoped<IRepositoryManager, RepositoryManager>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICountryService, CountryService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<IStadiumService, StadiumService>();
builder.Services.AddScoped<IWorldCupService, WorldCupService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<ICoachService, CoachService>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IPlayerPositionService, PlayerPositionService>();
builder.Services.AddScoped<IMatchService, MatchService>();
builder.Services.AddScoped<IGoalService, GoalService>();
builder.Services.AddScoped<ICardService, CardService>();
builder.Services.AddScoped<ITeamStatsService, TeamStatsService>();
builder.Services.AddScoped<IStandingsService, StandingsService>();
builder.Services.AddScoped<IBetService, BetService>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
builder.Services.AddScoped<IDemoSeedService, DemoSeedService>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})

// Adding Jwt Bearer
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = configuration["JWT:ValidAudience"],
        ValidIssuer = configuration["JWT:ValidIssuer"],
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
});

builder.Services.AddDbContext<ApplicationDbContext>(option =>
{
    option.UseNpgsql(builder.Configuration["ConnectionStrings:DefaultConnection"]);
});

IHealthChecksBuilder healthChecksBuilder = builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database");

if (!builder.Environment.IsEnvironment("Testing"))
{
    healthChecksBuilder.AddNpgSql(connectionString, name: "postgresql");
}
builder.Services.AddIdentity<User, IdentityRole<long>>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings.
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

}).AddEntityFrameworkStores<ApplicationDbContext>();

var app = builder.Build();

// Seed identity roles and dev admin user.
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<long>>>();
    string[] roles = { "Admin", "User" };

    foreach (var roleName in roles)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole<long>(roleName));
        }
    }

    if (app.Environment.IsDevelopment())
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        string? adminEmail = configuration["DevAdmin:Email"];
        string? adminPassword = configuration["DevAdmin:Password"];

        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            User? adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new User
                {
                    Email = adminEmail,
                    UserName = adminEmail,
                    Name = "Admin",
                    SecurityStamp = Guid.NewGuid().ToString()
                };
                IdentityResult createResult = await userManager.CreateAsync(adminUser, adminPassword);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
            else if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }

    ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (app.Environment.IsEnvironment("Testing"))
    {
        await dbContext.Database.EnsureCreatedAsync();
    }
    else
    {
        await dbContext.Database.MigrateAsync();
    }

    string[] playerPositions = { "Goalkeeper", "Defender", "Midfielder", "Forward" };
    foreach (string positionName in playerPositions)
    {
        bool positionExists = dbContext.PlayerPositions.Any(position => position.Name == positionName);
        if (!positionExists)
        {
            dbContext.PlayerPositions.Add(new PlayerPosition { Name = positionName });
        }
    }

    await dbContext.SaveChangesAsync();

    if (app.Environment.IsDevelopment() && configuration.GetValue<bool>("DevSeed:WorldCup2026"))
    {
        IDemoSeedService demoSeedService = scope.ServiceProvider.GetRequiredService<IDemoSeedService>();
        await demoSeedService.SeedWorldCup2026DemoAsync();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

}

app.UseHttpsRedirection();

app.UseCors("AngularDev");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

public partial class Program { }


