using System.Text.Json;

namespace AlertApiServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Rejestracja kontrolerów
            builder.Services.AddControllers();

            // Swagger/OpenAPI
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Konfiguracja serwera Kestrel
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(5136); // HTTP
                options.ListenAnyIP(7270, listenOptions =>
                {
                    listenOptions.UseHttps(); // HTTPS
                });
            });

            var app = builder.Build();

            // Konfiguracja middleware
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            // Mapowanie kontrolerów
            app.MapControllers();

            // Uruchomienie aplikacji
            app.Run();
        }

    }
}
