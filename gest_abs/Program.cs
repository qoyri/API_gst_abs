using gest_abs.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using gest_abs;

var builder = WebApplication.CreateBuilder(args);

// Ajouter Entity Framework Core
builder.Services.AddDbContext<GestionAbsencesContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(10, 11, 8))
    ));

// Ajouter l'authentification JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]))
    };
});

// Activer Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Ajouter des contrôleurs
builder.Services.AddControllers();

var app = builder.Build();

// Initialiser la base de données
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<GestionAbsencesContext>();
    DbInitializer.Initialize(context);
}

// Configurer Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gestion Absences API v1"));
}

app.MapGet("/home", () => Results.Redirect("/swagger"));

// Configurer le Middleware pour l'authentification et l'autorisation
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Mapper les contrôleurs
app.MapControllers();

app.Run();