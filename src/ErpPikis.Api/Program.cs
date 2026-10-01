using System.Text;
using System.Text.Json.Serialization;
using ErpPikis.Api.Middleware;
using ErpPikis.Api.Seguridad;
using ErpPikis.Api.WhatsApp;
using ErpPikis.Api.Workers;
using ErpPikis.Service.Alertas;
using ErpPikis.Repository;
using ErpPikis.Service;
using ErpPikis.Service.Impresion;
using ErpPikis.Service.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("ErpPikis")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'ErpPikis'.");

var jwt = builder.Configuration.GetSection(JwtOptions.Seccion).Get<JwtOptions>() ?? new JwtOptions();
if (Encoding.UTF8.GetByteCount(jwt.Key) < 32)
    throw new InvalidOperationException("Jwt:Key debe tener al menos 32 caracteres.");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.Seccion));
builder.Services.AddRepositorio(connectionString);
var whatsApp = builder.Configuration.GetSection(WhatsAppOptions.Seccion).Get<WhatsAppOptions>() ?? new WhatsAppOptions();

builder.Services.AddServicios(
    builder.Configuration.GetSection(TiendaOptions.Seccion).Get<TiendaOptions>() ?? new TiendaOptions(),
    builder.Configuration.GetSection(ImpresoraOptions.Seccion).Get<ImpresoraOptions>() ?? new ImpresoraOptions(),
    builder.Configuration.GetSection(AlertasOptions.Seccion).Get<AlertasOptions>() ?? new AlertasOptions(),
    whatsApp.Envio);

builder.Services.AddSingleton(whatsApp);
switch (whatsApp.Proveedor)
{
    case ProveedorWhatsApp.CallMeBot:
        builder.Services.AddHttpClient<IWhatsAppSender, CallMeBotSender>(c => c.Timeout = TimeSpan.FromSeconds(20));
        break;
    case ProveedorWhatsApp.MetaCloud:
        builder.Services.AddHttpClient<IWhatsAppSender, MetaCloudSender>(c => c.Timeout = TimeSpan.FromSeconds(20));
        break;
    default:
        builder.Services.AddSingleton<IWhatsAppSender, WhatsAppDeshabilitado>();
        break;
}

builder.Services.AddHostedService<RevisionAlertasWorker>();
builder.Services.AddHostedService<EnvioWhatsAppWorker>();
builder.Services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    var origenes = builder.Configuration.GetSection("Cors:Origenes").Get<string[]>() ?? [];
    options.AddDefaultPolicy(policy => policy.WithOrigins(origenes).AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddExceptionHandler<ManejadorErrores>();
builder.Services.AddProblemDetails();

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ERP-PIKIS API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pegue el token obtenido en /api/auth/login"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (builder.Configuration.GetValue("BaseDatos:AplicarMigracionesAlIniciar", true))
{
    await app.Services.AplicarMigracionesAsync();

    using var scope = app.Services.CreateScope();
    var semilla = scope.ServiceProvider.GetRequiredService<IDatosInicialesService>();
    await semilla.SembrarAsync(
        builder.Configuration["AdminInicial:Username"] ?? "admin",
        builder.Configuration["AdminInicial:Password"] ?? "Pikis2026*");
}

app.Run();
