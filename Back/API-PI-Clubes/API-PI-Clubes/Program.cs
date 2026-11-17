using API_PI_Clubes.Api.Filters;
using API_PI_Clubes.Application;
using API_PI_Clubes.Application.Auth;
using API_PI_Clubes.Application.DependencyInjection;
using API_PI_Clubes.Application.Email;
using API_PI_Clubes.Application.Interfaces.IServices;
using API_PI_Clubes.Application.Services;
using API_PI_Clubes.Application.Validators;
using API_PI_Clubes.Hubs;
using API_PI_Clubes.Infrastructure.Configuration;
using API_PI_Clubes.Infrastructure.Data;
using API_PI_Clubes.Infrastructure.Jobs;
using API_PI_Clubes.Infrastructure.Middlewares;
using API_PI_Clubes.Infrastructure.Security;
using API_PI_Clubes.Infrastructure.Services;
using API_PI_Clubes.Infrastructure.Settings;
using API_PI_Clubes.Model;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection não configurada.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
        sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

if (!EF.IsDesignTime)
{
    var masterKey = Convert.FromBase64String(
        builder.Configuration["SETTINGS_MASTER_KEY"]
        ?? throw new InvalidOperationException("SETTINGS_MASTER_KEY não configurada."));

    if (masterKey.Length != 32)
        throw new InvalidOperationException("SETTINGS_MASTER_KEY deve ter 32 bytes (Base64).");

    await MigrateDatabaseAsync(connectionString);
    await SeedSettingsAsync(connectionString, builder.Configuration, masterKey);

    ((IConfigurationBuilder)builder.Configuration).Add(new DbConfigurationSource
    {
        ConnectionString = connectionString,
        MasterKey = masterKey,
        ReloadInterval = TimeSpan.FromMinutes(1)
    });
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>())
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddValidatorsFromAssemblyContaining<CreateClubDTOValidator>();

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddTransient<TokenService>();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
                  ?? throw new InvalidOperationException("Jwt settings not configured.");

builder.Services.AddOptions<ShareSettings>()
    .Bind(builder.Configuration.GetSection(ShareSettings.SectionName))
    .Validate(s => Uri.TryCreate(s.FrontendBaseUrl, UriKind.Absolute, out _)
                   && Uri.TryCreate(s.ApiPublicBaseUrl, UriKind.Absolute, out _),
        "Share:FrontendBaseUrl e Share:ApiPublicBaseUrl precisam ser URLs absolutas.")
    .ValidateOnStart();

builder.Services.AddHttpClient();
builder.Services.AddHttpClient(CoverImageLoader.HttpClientName, c => c.Timeout = TimeSpan.FromSeconds(4))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

builder.Services.AddHttpClient<IMercadoPagoOrdersClient, MercadoPagoOrdersClient>((sp, client) =>
{
    var token = sp.GetRequiredService<IConfiguration>()["MercadoPago:AccessToken"]
                ?? throw new InvalidOperationException("MercadoPago:AccessToken não configurado.");

    client.BaseAddress = new Uri("https://api.mercadopago.com/");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("share-image", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "clubera_auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(jwtSettings.Expiration);
        options.SlidingExpiration = true;

        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddSignalR()
    .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddCors(options =>
{
    options.AddPolicy("CluberaPolicy", policy =>
        policy.WithOrigins(
                "https://www.clubera.dev",
                "https://clubera.dev",
                "https://api.clubera.dev",
                "http://localhost:4200")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

builder.Services.AddHostedService<SubscriptionExpiryJob>();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var wwwrootPath = app.Environment.WebRootPath
                  ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(wwwrootPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(wwwrootPath)
});

app.UseRouting();
app.UseRateLimiter();
app.UseCors("CluberaPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<CourtAvailabilityHub>("api/hubs/court-availability");

app.Run();

static async Task MigrateDatabaseAsync(string connectionString)
{
    const int maxAttempts = 10;
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlServer(connectionString)
        .Options;

    for (var attempt = 1;; attempt++)
    {
        try
        {
            await using var db = new AppDbContext(options);
            await db.Database.MigrateAsync();
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            Console.WriteLine(
                $"[Startup] Falha ao migrar (tentativa {attempt}/{maxAttempts}): {ex.Message}. Nova tentativa em 5s...");
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}

static async Task SeedSettingsAsync(string connectionString, IConfiguration config, byte[] masterKey)
{
    string[] keys =
    [
        "Jwt:Key", "Jwt:Issuer", "Jwt:Audience", "Jwt:Expiration",
        "Jwt:EmailVerificationKey", "Jwt:ExpirationEmailVerification",
        "Jwt:ResetPasswordKey", "Jwt:ExpirationResetPassword",
        "Email:SenderEmail", "Email:SenderName", "Email:SmtpServer", "Email:SmtpPort",
        "Email:SmtpUsername", "Email:SmtpPassword", "Email:EnableSsl", "Email:BaseUrl",
        "AzureStorage:ConnectionString", "AzureStorage:ContainerName",
        "Google:ClientId",
        "MercadoPago:AccessToken", "MercadoPago:WebhookUrl", "MercadoPago:WebhookSecret",
        "MercadoPago:BackUrls:Success", "MercadoPago:BackUrls:Failure", "MercadoPago:BackUrls:Pending",
        "LocalStorage:BaseUrl", "Storage:Provider",
        "Booking:BookingCutoffMinutes", "Booking:CancellationCutoffMinutes", "Booking:TimeZoneId",
        "Share:FrontendBaseUrl", "Share:ApiPublicBaseUrl", "Share:AllowedImageHosts:0",
        "ReserveCleanup:RetentionMonths"
    ];

    var secrets = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Jwt:Key", "Jwt:EmailVerificationKey", "Jwt:ResetPasswordKey",
        "Email:SmtpPassword", "AzureStorage:ConnectionString",
        "MercadoPago:AccessToken", "MercadoPago:WebhookSecret"
    };

    var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options;
    await using var db = new AppDbContext(options);

    var existing = await db.AppSettings.Select(s => s.Key).ToListAsync();

    foreach (var key in keys.Except(existing, StringComparer.OrdinalIgnoreCase))
    {
        var value = config[key];
        if (string.IsNullOrWhiteSpace(value)) continue;

        var isSecret = secrets.Contains(key);
        db.AppSettings.Add(new AppSetting
        {
            Key = key,
            Value = isSecret ? SettingsCrypto.Encrypt(value, masterKey) : value,
            IsSecret = isSecret,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = "seed"
        });
    }

    await db.SaveChangesAsync();
}