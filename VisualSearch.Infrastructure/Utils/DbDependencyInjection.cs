using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using VisualSearch.Infrastructure.Persistence;

namespace VisualSearch.Infrastructure.Utils
{
    public static class DbDependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("Default")));
            return services;
        }
    }
}
