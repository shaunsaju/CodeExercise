using System.Text.Json.Serialization;
using NotificationsApp.Configuration;
using NotificationsApp.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration ---
builder.Services
    .AddOptions<DiscordSettings>()
    .Bind(builder.Configuration.GetSection(DiscordSettings.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<RateLimitingSettings>()
    .Bind(builder.Configuration.GetSection(RateLimitingSettings.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// --- Services ---
builder.Services.AddSingleton<IRateLimiter, SlidingWindowRateLimiter>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddHttpClient<INotificationForwarder, DiscordNotificationForwarder>();

// --- Controllers & JSON ---
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Make the implicit Program class accessible to integration tests
public partial class Program;
