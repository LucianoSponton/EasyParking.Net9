using EasyParking.API;
using EasyParkingAPI.Data;
using EasyParkingAPI.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.IO;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


// ======================================================
// ?? Leer configuración
// ======================================================
var config = builder.Configuration;
string connectionString = config.GetValue<string>("ConnectionString");

// ======================================================
// ?? Servicios
// ======================================================
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContextPool<EasyParkingAuthContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(3),
            errorNumbersToAdd: null);
    })
    .EnableSensitiveDataLogging(false)
);

// ======================================================
// ?? Identity con Token Providers (SOLUCIÓN AL ERROR)
// ======================================================
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(config =>
{
    config.SignIn.RequireConfirmedEmail = false;
    config.Password.RequireDigit = false;
    config.Password.RequireLowercase = false;
    config.Password.RequireUppercase = false;
    config.Password.RequireNonAlphanumeric = false;
    config.Password.RequiredLength = 8;

    // Configuración de tokens para reseteo de contraseña y confirmación de email
    config.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultProvider;
    config.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultProvider;
})
.AddEntityFrameworkStores<EasyParkingAuthContext>()
.AddDefaultTokenProviders(); // ? LÍNEA CRÍTICA AGREGADA

// Configuración del tiempo de vida de los tokens (opcional pero recomendado)
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(3); // Los tokens expiran en 3 horas
});

builder.Services.AddAuthorization();

builder.Services.AddControllers(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    options.OutputFormatters.RemoveType<Microsoft.AspNetCore.Mvc.Formatters.HttpNoContentOutputFormatter>();
})
.AddNewtonsoftJson(x => x.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

// ======================================================
// ?? Swagger + Seguridad
// ======================================================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EasyParking.API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header usando Bearer.\nEjemplo: 'Bearer 12345abcdef'",
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// ======================================================
// ?? CORS
// ======================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// ======================================================
// ?? Autenticación JWT
// ======================================================
string? key = config.GetValue<string>("Security:SymmetricSecurityKey");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "easyparking.ep@gmail.com",
            ValidAudience = "easyparking.ep@gmail.com",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ClockSkew = TimeSpan.Zero
        });

// ======================================================
// ??? IIS Config
// ======================================================
builder.Services.Configure<IISServerOptions>(options =>
{
    options.AutomaticAuthentication = false;
});

var app = builder.Build();

// ======================================================
// ?? Middleware y pipeline
// ======================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Ejecución de lógica solo en desarrollo
    bool CreatingUsersAndRoles = false;
    if (CreatingUsersAndRoles)
    {
        using var scope = app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        CreateUsersAndRoles createUsersAndRoles = new(userManager, roleManager);
        await createUsersAndRoles.CreateRolesAsync();
        await createUsersAndRoles.CreateUsersAsync();
    }
}

// Habilitar archivos estáticos para Estacionamientos
var estacionamientosFolder = builder.Configuration.GetValue<string>("EasyParkingAPI:Images:Estacionamientos_Folder");

if (!string.IsNullOrEmpty(estacionamientosFolder) && Directory.Exists(estacionamientosFolder))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(estacionamientosFolder),
        RequestPath = "/images/estacionamientos"
    });
}

// Habilitar archivos estáticos para Usuarios
var usuariosFolder = builder.Configuration.GetValue<string>("EasyParkingAPI:Images:Usuarios_Folder");

if (!string.IsNullOrEmpty(usuariosFolder) && Directory.Exists(usuariosFolder))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(usuariosFolder),
        RequestPath = "/images/usuarios"
    });
}

app.UseHttpsRedirection();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
//app.UseMiddleware<LogUserNameMiddleware>();

app.MapControllers();

app.Run();