using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotifyBot;
using NotifyBot.Database;
using NotifyBot.Secrets;
using NotifyBot.Services;

using var host = Host.CreateDefaultBuilder(args).ConfigureServices(services =>
{
    services.AddSingleton<IDopplerService, DopplerService>();
    services.AddSingleton<IDatabaseClient, DatabaseClient>();
    services.AddSingleton<IExpiredVotesService, ExpiredVotesService>();
    services.AddSingleton<INotifyService, NotifyService>();
    services.AddTransient<App>();
    
    
}).ConfigureAppConfiguration((hostingContext, configuration) =>
{
    configuration.Sources.Clear();
    var env = hostingContext.HostingEnvironment;
    configuration
        .AddJsonFile(@"C:\Users\c\git\NotifyBot\NotifyBot\appsettings.json", optional: true, reloadOnChange: true)
        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true);
}).Build();

static async void StartApp(IServiceProvider hostProvider)
{
    using var serviceScope = hostProvider.CreateScope();
    var provider = serviceScope.ServiceProvider;
    var app = provider.GetRequiredService<App>();
    await app.Run();
    Environment.Exit(0);
}

StartApp(host.Services);
await host.RunAsync();