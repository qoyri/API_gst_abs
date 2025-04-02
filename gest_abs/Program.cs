using gest_abs.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using gest_abs;
using Microsoft.OpenApi.Models;
using gest_abs.Services;

var builder = WebApplication.CreateBuilder(args);

// Configurer la journalisation
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Modifier la configuration CORS pour accepter les connexions depuis n'importe quelle origine
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

// Ajouter Entity Framework Core
builder.Services.AddDbContext<GestionAbsencesContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(10, 11, 8))
    ));

// Jeter la mappage par défaut des claims pour éviter toute modification du Role
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"])),
        // Spécifier le type de claim contenant le rôle
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.Name
    };
    
    // Ajouter des logs pour le débogage de l'authentification JWT
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Token validé pour l'utilisateur: {User}", context.Principal?.Identity?.Name);
            
            if (context.Principal?.Identity is ClaimsIdentity identity)
            {
                foreach (var claim in identity.Claims)
                {
                    logger.LogInformation("Claim: {Type} = {Value}", claim.Type, claim.Value);
                }
                
                var roles = identity.Claims
                    .Where(c => c.Type == ClaimTypes.Role || c.Type == "role" || c.Type == "roles")
                    .Select(c => c.Value)
                    .ToList();
                
                logger.LogInformation("Rôles trouvés: {Roles}", string.Join(", ", roles));
            }
            
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError("Échec de l'authentification: {Error}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("Challenge d'authentification déclenché: {Error}", context.Error);
            return Task.CompletedTask;
        }
    };
});

// Enregistrer les services
builder.Services.AddScoped<ParentService>();
builder.Services.AddScoped<TeacherService>();
builder.Services.AddScoped<StudentService>();
builder.Services.AddScoped<AdminConfigService>();
builder.Services.AddScoped<PointsService>();

// Activer Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Entrez 'Bearer' suivi d'un espace et de votre token JWT. Ex: \"Bearer 12345abcdef\""
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] { }
        }
    });
});

var app = builder.Build();


// Initialiser la base de données
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<GestionAbsencesContext>();
    // Ajouter cette ligne après la création du contexte dans la méthode Initialize
    context.Database.Migrate();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        DbInitializer.Initialize(context);
        logger.LogInformation("Base de données initialisée avec succès");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erreur lors de l'initialisation de la base de données");
    }
}

// Configurer Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gestion Absences API v1"));
}

app.MapGet("/home", () => Results.Redirect("/swagger"));

// Utiliser CORS avant l'authentification et l'autorisation
app.UseCors("AllowAll");

app.UseMiddleware<BearerPrefixMiddleware>();

// Configurer le Middleware pour l'authentification et l'autorisation
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Mapper les contrôleurs
app.MapControllers();

app.Run();

