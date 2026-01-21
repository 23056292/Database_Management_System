using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using JournalApp.Data;
using JournalApp.Services;
using Microsoft.Data.Sqlite;

namespace JournalApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

        builder.Services.AddMauiBlazorWebView();

        // Configure SQLite connection
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "journalapp.db");
        var connectionString = $"DataSource={dbPath}";
        
        builder.Services.AddDbContext<MyAppContext>(options =>
        {
            options.UseSqlite(connectionString);
        });

        // Register authentication services
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddSingleton<StateContainer>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        
        using (var scope = app.Services.CreateScope())
        {
            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
            authService.ResetDatabaseAsync().Wait(); // Initialize and reset database
        }
        return app;
    }
} 