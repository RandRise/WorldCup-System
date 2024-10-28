using Core.Services.Countries;
using Core.Services.Users;
using Data.Context;
using Data.Repos;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IRepositoryManager, RepositoryManager>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICountryService, CountryService>();
builder.Services.AddDbContext<ApplicationDbContext>(option =>
{
    option.UseNpgsql(builder.Configuration["ConnectionStrings:DefaultConnection"]);
});


var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var countryService = scope.ServiceProvider.GetRequiredService<ICountryService>();
    await countryService.LoadCountriesFromExcel("C:\\Users\\Rand-\\Documents\\WorldCup-System\\WorldCup-System\\Core\\all_countries.csv");
}

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
