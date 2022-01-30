using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.DependencyInjection;

//https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection
//https://peter.intheazuresky.com/2020/02/12/azure-functions-dependency-injection-the-forgotten-greatness/

[assembly: FunctionsStartup(typeof(Module.Startup))]
namespace Module
{
    public class Startup : FunctionsStartup
    {
        //public static readonly IConfigurationRoot Config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").AddJsonFile("appsettings.local.json", optional: true).AddEnvironmentVariables().Build();

        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();
            builder.Services.AddDurableClientFactory();
            builder.Services.AddLogging();


            //builder.Services.AddSingleton(_ => Config);
            //var context = builder.GetContext();

            //builder.Services.AddOptions<MyOptions>().Configure<IConfiguration>((settings, configuration) =>
            //{

            //    var a = configuration
            //    configuration.GetSection("MyOptions").Bind(settings);
            //});


            //builder.ConfigurationBuilder
            //    .AddJsonFile(Path.Combine(context.ApplicationRootPath, "appsettings.json"), optional: true, reloadOnChange: false)
            //    .AddJsonFile(Path.Combine(context.ApplicationRootPath, $"appsettings.{context.EnvironmentName}.json"), optional: true, reloadOnChange: false)
            //    .AddEnvironmentVariables();


            //public static readonly IConfigurationRoot Config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").AddJsonFile("appsettings.local.json", optional: true).AddEnvironmentVariables().Build();


            //builder.Services.AddSingleton<IDurableClient>(o =>
            //    {
            //        return clientFactory.CreateClient(new DurableClientOptions
            //        {
            //            TaskHub = config["ModuleName"]
            //        });
            //    });

            //builder.Services.AddSingleton<IMyService>((s) => {
            //    return new MyService();
            //});
        }
    }
}
