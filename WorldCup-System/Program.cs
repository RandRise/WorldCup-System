using Serilog;
using Serilog.Formatting.Compact;
using Core.Options;
using Core.Services.Bets;
using Core.Services.Cards;
using Core.Services.Cities;
using Core.Services.Coaches;
using Core.Services.Companies;
using Core.Services.Countries;
using Core.Services.Goals;
using Core.Services.Groups;
using Core.Services.Knockout;
using Core.Services.Matches;
using Core.Services.MatchSync;
using Core.Services.PlayerPositions;
using Core.Services.Players;
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
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IPlayerPositionService, PlayerPositionService>();
builder.Services.AddScoped<IMatchService, MatchService>();
builder.Services.AddScoped<IKnockoutService, KnockoutService>();
builder.Services.AddScoped<IGoalService, GoalService>();
builder.Services.AddScoped<ICardService, CardService>();
builder.Services.AddScoped<ITeamStatsService, TeamStatsService>();
builder.Services.AddScoped<IStandingsService, StandingsService>();
builder.Services.AddScoped<IBetService, BetService>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
builder.Services.AddScoped<IMatchResultSyncService, MatchResultSyncService>();
builder.Services.AddScoped<ITimelinePlayerResolver, TimelinePlayerResolver>();
builder.Services.AddScoped<ITimelineScorerApplyService, TimelineScorerApplyService>();
builder.Services.Configure<MatchResultSyncOptions>(
    builder.Configuration.GetSection(MatchResultSyncOptions.SectionName));
builder.Services.AddHttpClient<IExternalMatchResultProvider, FifaCalendarMatchResultProvider>((serviceProvider, client) =>
{
    MatchResultSyncOptions options = serviceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<MatchResultSyncOptions>>()
        .Value;
    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
});
builder.Services.AddHttpClient<IExternalMatchEventsProvider, FifaTimelineEventsProvider>((serviceProvider, client) =>
{
    MatchResultSyncOptions options = serviceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<MatchResultSyncOptions>>()
        .Value;
    client.BaseAddress = new Uri(options.TimelineBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
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

// JWT must be registered after AddIdentity so cookie defaults do not overwrite the API scheme.
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = true;
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

var app = builder.Build();

// Migrate schema first, then seed identity roles and dev users.
using (var scope = app.Services.CreateScope())
{
    ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (app.Environment.IsEnvironment("Testing"))
    {
        await dbContext.Database.EnsureCreatedAsync();
    }
    else
    {
        await dbContext.Database.MigrateAsync();
        // Some local DBs recorded AddMatchStage as applied before Feeder* columns existed.
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE "Match" ADD COLUMN IF NOT EXISTS "FeederMatchOneId" integer NULL;
            ALTER TABLE "Match" ADD COLUMN IF NOT EXISTS "FeederMatchTwoId" integer NULL;
            ALTER TABLE "Match" ADD COLUMN IF NOT EXISTS "FeederOneTakesLoser" boolean NOT NULL DEFAULT FALSE;
            ALTER TABLE "Match" ADD COLUMN IF NOT EXISTS "FeederTwoTakesLoser" boolean NOT NULL DEFAULT FALSE;
            ALTER TABLE "Match" ALTER COLUMN "TeamOneId" DROP NOT NULL;
            ALTER TABLE "Match" ALTER COLUMN "TeamTwoId" DROP NOT NULL;
            CREATE INDEX IF NOT EXISTS "IX_Match_FeederMatchOneId" ON "Match" ("FeederMatchOneId");
            CREATE INDEX IF NOT EXISTS "IX_Match_FeederMatchTwoId" ON "Match" ("FeederMatchTwoId");
            CREATE INDEX IF NOT EXISTS "IX_Match_Stage" ON "Match" ("Stage");
            ALTER TABLE "Match" ADD COLUMN IF NOT EXISTS "ExternalMatchId" character varying(64) NULL;
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Match_ExternalMatchId" ON "Match" ("ExternalMatchId") WHERE "ExternalMatchId" IS NOT NULL;
            ALTER TABLE "Match" ADD COLUMN IF NOT EXISTS "ExternalStageId" character varying(64) NULL;
            ALTER TABLE "Player" ADD COLUMN IF NOT EXISTS "ExternalPlayerId" character varying(64) NULL;
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Player_ExternalPlayerId" ON "Player" ("ExternalPlayerId") WHERE "ExternalPlayerId" IS NOT NULL;
            DROP INDEX IF EXISTS "IX_Team_CountryId";
            CREATE INDEX IF NOT EXISTS "IX_Team_CountryId" ON "Team" ("CountryId");
            CREATE TABLE IF NOT EXISTS "Company" (
                "Id" bigint GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                "Name" character varying(128) NOT NULL,
                "Slug" character varying(64) NULL,
                "InviteCode" character varying(32) NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "CreatedByUserId" bigint NULL,
                CONSTRAINT "PK_Company" PRIMARY KEY ("Id"),
                CONSTRAINT "CK_Company_Name_Length_Less_Than_128" CHECK (Length("Name") <= 128),
                CONSTRAINT "CK_Company_InviteCode_Length_Less_Than_32" CHECK (Length("InviteCode") <= 32),
                CONSTRAINT "CK_Company_Slug_Length_Less_Than_64" CHECK ("Slug" IS NULL OR Length("Slug") <= 64),
                CONSTRAINT "FK_Company_CreatedByUser" FOREIGN KEY ("CreatedByUserId") REFERENCES "AspNetUsers" ("Id")
            );
            CREATE INDEX IF NOT EXISTS "IX_Company_CreatedByUserId" ON "Company" ("CreatedByUserId");
            CREATE UNIQUE INDEX IF NOT EXISTS "Uq_Company_InviteCode" ON "Company" ("InviteCode");
            CREATE UNIQUE INDEX IF NOT EXISTS "Uq_Company_Slug" ON "Company" ("Slug") WHERE "Slug" IS NOT NULL;
            ALTER TABLE "AspNetUsers" ADD COLUMN IF NOT EXISTS "CompanyId" bigint NULL;
            CREATE INDEX IF NOT EXISTS "IX_AspNetUsers_CompanyId" ON "AspNetUsers" ("CompanyId");
            DO $$ BEGIN
                ALTER TABLE "AspNetUsers" ADD CONSTRAINT "FK_User_Company"
                    FOREIGN KEY ("CompanyId") REFERENCES "Company" ("Id");
            EXCEPTION WHEN duplicate_object THEN NULL;
            END $$;
            """);
    }

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<long>>>();
    string[] roles = { "Admin", "User", "CompanyAdmin" };

    foreach (var roleName in roles)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole<long>(roleName));
        }
    }

    if (app.Environment.IsDevelopment())
    {
        UserManager<User> userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        await EnsureDevUserInRoleAsync(
            userManager,
            email: configuration["DevAdmin:Email"],
            password: configuration["DevAdmin:Password"] ?? "Admin123!",
            displayName: "Admin",
            roleName: "Admin");

        await EnsureDevUserInRoleAsync(
            userManager,
            email: configuration["DevUser:Email"],
            password: configuration["DevUser:Password"] ?? "User123!",
            displayName: "Demo User",
            roleName: "User");
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
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseCors("AngularDev");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

public partial class Program
{
    private static async Task EnsureDevUserInRoleAsync(
        UserManager<User> userManager,
        string? email,
        string? password,
        string displayName,
        string roleName)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        User? user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new User
            {
                Email = email,
                UserName = email,
                Name = displayName,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            IdentityResult createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                return;
            }
        }

        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            await userManager.AddToRoleAsync(user, roleName);
        }
    }
}


