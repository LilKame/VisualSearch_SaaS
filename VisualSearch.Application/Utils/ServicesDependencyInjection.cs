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
using VisualSearch.Application;
using VisualSearch.Application.Features.Search;
using VisualSearch.Application.Features.Catalog;

namespace VisualSearch.Application.DependencyInjection
{
    public static class ServicesDependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Adicionando os serviços
            services.AddScoped<SearchProductService>();
            services.AddScoped<CatalogProductService>();

            return services;
        }
    }
}
