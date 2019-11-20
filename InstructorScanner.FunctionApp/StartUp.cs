using InstructorScanner.FunctionApp;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SendGrid;
using System.IO;

[assembly: FunctionsStartup(typeof(Startup))]
namespace InstructorScanner.FunctionApp
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                                .AddEnvironmentVariables()
                                .Build();

            builder.Services.Configure<AppSettings>(config.GetSection("AppSettings"));

            ConfigureServices(builder);
        }

        private void ConfigureServices(IFunctionsHostBuilder builder)
        {
            var appSettings = builder
                .Services
                .BuildServiceProvider()
                .GetRequiredService<IOptions<AppSettings>>();

            builder.Services.AddScoped<IPreviousInstructorCalendarService, PreviousInstructorCalendarService>();
            builder.Services.AddScoped<ISendEmailService, SendEmailService>();
            builder.Services.AddScoped<IStorageHelper, StorageHelper>();
            builder.Services.AddScoped<ISendGridClient>(ifact => new SendGridClient(appSettings.Value.SendGridApiKey));
        }
    }
}