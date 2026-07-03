using ADS2026.Data;
using ADS2026.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// SERVICES
builder.Services.AddControllersWithViews();
builder.Services.AddControllers();

// DB CONTEXT
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// SIGNALR
builder.Services.AddSignalR();

// CORS � AllowAnyOrigin() breaks SignalR, use WithOrigins() + AllowCredentials()
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(

                "http://localhost:5173",   // React (Vite)
                "http://localhost:5138"    // MVC DisplayScreen (kiosk)
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();         // Required for SignalR
    });
});

// 100GB UPLOAD LIMIT
const long MAX_UPLOAD_SIZE = 100L * 1024 * 1024 * 1024;

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = MAX_UPLOAD_SIZE;
});

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = MAX_UPLOAD_SIZE;
});

// SWAGGER
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "ADS2026 Media API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Description = "Enter JWT token"
    });

    c.AddSecurityRequirement(doc =>
    {
        var requirement = new Microsoft.OpenApi.OpenApiSecurityRequirement();
        requirement.Add(new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", doc, null), []);
        return requirement;
    });
});

// JWT AUTHENTICATION
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/mediaHub"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

// PIPELINE
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ADS2026 API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// CORS must be before auth & endpoints
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// ROUTES
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=DisplayScreen}/{action=Index}/{id?}");

app.MapHub<MediaHub>("/mediaHub");
app.MapControllers();

// AUTO-OPEN KIOSK BROWSER
//app.Lifetime.ApplicationStarted.Register(() =>
//{
//    try
//    {
//        Process.Start(new ProcessStartInfo
//        {
//            FileName = "msedge.exe",
//            Arguments = "--kiosk http://localhost:5138 --edge-kiosk-type=fullscreen",
//            UseShellExecute = true
//        });
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine($"[Kiosk] Failed to open browser: {ex.Message}");
//    }
//});

app.Run();