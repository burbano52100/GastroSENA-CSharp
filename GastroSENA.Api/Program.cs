using System.Security.Claims;
using System.Text;
using GastroSENA.Api.Data;
using GastroSENA.Api.Endpoints;
using GastroSENA.Api.Servicios;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Base de datos PostgreSQL (EF Core + Npgsql)
builder.Services.AddDbContext<GastroSenaDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("GastroSena")));

// Servicios propios
builder.Services.AddSingleton<ServicioJwt>();

// Autenticación JWT
var jwt = builder.Configuration.GetSection("Jwt");
var clave = jwt["Clave"];
if (string.IsNullOrWhiteSpace(clave) || clave.Length < 32)
    throw new InvalidOperationException("Configura Jwt:Clave con al menos 32 caracteres (dotnet user-secrets).");

var llaveBytes = Encoding.UTF8.GetBytes(clave);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Emisor"],
            ValidateAudience = true,
            ValidAudience = jwt["Audiencia"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(llaveBytes),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization(opt =>
    opt.AddPolicy("Admin", p => p.RequireRole("Admin")));

// OpenAPI (JSON en /openapi/v1.json) y health checks (/health)
// El esquema "Bearer" permite pegar el token JWT en la interfaz de Scalar.
builder.Services.AddOpenApi(opt => opt.AddDocumentTransformer((doc, _, _) =>
{
    doc.Components ??= new OpenApiComponents();
    doc.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
    doc.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Token que devuelve POST /api/auth/login"
    };
    doc.Security ??= [];
    doc.Security.Add(new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", doc)] = [] });
    return Task.CompletedTask;
}));
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { servicio = "GastroSENA API", version = "1.0" }));
app.MapHealthChecks("/health");
app.MapOpenApi();
app.MapScalarApiReference(); // Interfaz visual en /scalar

// Módulos de la API
app.MapAuthEndpoints();
app.MapUsuariosEndpoints();
app.MapFichasEndpoints();
app.MapReservasEndpoints();
app.MapEntradasEndpoints();
app.MapAdminEndpoints();

// Aplicar migraciones y crear el administrador inicial (si está configurado)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GastroSenaDbContext>();
    await db.Database.MigrateAsync();
    await DatosIniciales.SembrarAdminAsync(db, app.Configuration);
}

app.Run();
