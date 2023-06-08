using Azure;
using Azure.AI.OpenAI;

namespace App
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers();

            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("local.settings.json", true)
                .Build();

            var appConfig = new AppConfiguration(config);

            // AppConfiguration
            builder.Services.AddSingleton(provider => appConfig);

            // OpenAIClient
            builder.Services.AddSingleton(provider =>
            {
                var endpoint = new Uri($"https://{appConfig.OpenAIServiceName}.openai.azure.com/");
                var credential = new AzureKeyCredential(appConfig.OpenAIServiceKey);
                return new OpenAIClient(endpoint, credential);
            });

            var app = builder.Build();

            app.UseHttpsRedirection();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}