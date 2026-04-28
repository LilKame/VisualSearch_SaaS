using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using Qdrant.Client;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using VisualSearch.Application.AiEngine;
using VisualSearch.Infrastructure.AiEngine;
using VisualSearch.Infrastructure.Persistence;
using VisualSearch.Infrastructure.Storage;
using VisualSearch.Infrastructure.VectorStore;

namespace VisualSearch.Infrastructure.Utils
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("Default")));

            // Qdrant
            // Pega a configuração no appsettings.json , se não tiver nada considera "localhost";
            var qdrant = configuration["Qdrant:Host"] ?? "localhost";
            var qdrantPort = int.Parse(configuration["Qdrant:Port"] ?? "6334");

            // Cria uma instancia única de uma classe para todo o sistema;
            services.AddSingleton(new QdrantClient(qdrant, qdrantPort)); // Instância do client do Qdrant
            services.AddScoped<IVectorStoreService, QdrantVectorService>(); // Isso e para chamarem o contrato ao invés deixando o sistema independente do serviço;

            // Client HTTP para o serviço Python;
            services.AddHttpClient<IAiEngineClientService, AiEngineClientService>
            (
                client =>
                {
                    // Definindo a url de destino;
                    client.BaseAddress = new Uri(configuration["AiEngine:BaseUrl"] ?? "http://localhost:8000");
                    // Definindo o tempo de requisição máximo;
                    client.Timeout = TimeSpan.FromSeconds(30);
                }
            );

            services.AddMinio(client => client
            .WithEndpoint(configuration["MinIO:Endpoint"] ?? "localhost:9000")
            .WithCredentials(configuration["MinIO:AcessKey"] ?? "minioadmin"
            , configuration["MinIO:SecretKey"] ?? "minioadmin123")
            .WithSSL(false)
            .Build()
            );

            services.AddScoped<IObjectStorageService, ObjectStorageService>();
            // Envia para as configurações;
            return services;
        }
    }
}
