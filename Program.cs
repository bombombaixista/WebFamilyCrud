using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebFamilyCrud.Data;
using WebFamilyCrud.DTOs;
using WebFamilyCrud.Models;

var builder = WebApplication.CreateBuilder(args);

// Configuração do PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebFamilyCrud API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT. Exemplo: Bearer {seu token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "chave-super-secreta";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "WebFamilyCrud";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// ✅ Configuração de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Endpoint de Login
app.MapPost("/login", (LoginRequest request, AppDbContext db) =>
{
    if (request.Username == "admin" && request.Password == "123")
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, request.Username)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: null,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return Results.Ok(new { token = tokenString });
    }

    return Results.Unauthorized();
});

// CRUD de Grupos
app.MapGet("/grupos", async (AppDbContext db) =>
{
    return await db.Grupos.ToListAsync();
}).RequireAuthorization();

app.MapGet("/grupos/{id}", async (int id, AppDbContext db) =>
{
    var g = await db.Grupos.FindAsync(id);
    return g is not null ? Results.Ok(g) : Results.NotFound();
}).RequireAuthorization();

app.MapPost("/grupos", async (Grupo grupo, AppDbContext db) =>
{
    db.Grupos.Add(grupo);
    await db.SaveChangesAsync();
    return Results.Created($"/grupos/{grupo.Id}", grupo);
}).RequireAuthorization();

app.MapPut("/grupos/{id}", async (int id, Grupo input, AppDbContext db) =>
{
    var grupo = await db.Grupos.FindAsync(id);
    if (grupo is null) return Results.NotFound();
    grupo.Nome = input.Nome;
    await db.SaveChangesAsync();
    return Results.Ok(grupo);
}).RequireAuthorization();

app.MapDelete("/grupos/{id}", async (int id, AppDbContext db) =>
{
    var grupo = await db.Grupos.FindAsync(id);
    if (grupo is null) return Results.NotFound();
    db.Grupos.Remove(grupo);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

// CRUD de Clientes
app.MapGet("/clientes", async (AppDbContext db) =>
{
    return await db.Clientes.Include(c => c.Grupo).ToListAsync();
}).RequireAuthorization();

app.MapGet("/clientes/{id}", async (int id, AppDbContext db) =>
{
    var c = await db.Clientes.Include(x => x.Grupo).FirstOrDefaultAsync(x => x.Id == id);
    return c is not null ? Results.Ok(c) : Results.NotFound();
}).RequireAuthorization();

app.MapPost("/clientes", async (Cliente cliente, AppDbContext db) =>
{
    db.Clientes.Add(cliente);
    await db.SaveChangesAsync();
    return Results.Created($"/clientes/{cliente.Id}", cliente);
}).RequireAuthorization();

app.MapPut("/clientes/{id}", async (int id, Cliente input, AppDbContext db) =>
{
    var cliente = await db.Clientes.FindAsync(id);
    if (cliente is null) return Results.NotFound();
    cliente.Nome = input.Nome;
    cliente.Email = input.Email;
    cliente.GrupoId = input.GrupoId;
    await db.SaveChangesAsync();
    return Results.Ok(cliente);
}).RequireAuthorization();

app.MapDelete("/clientes/{id}", async (int id, AppDbContext db) =>
{
    var cliente = await db.Clientes.FindAsync(id);
    if (cliente is null) return Results.NotFound();
    db.Clientes.Remove(cliente);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.Run();
