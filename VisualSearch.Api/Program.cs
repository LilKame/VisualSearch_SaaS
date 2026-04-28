using Microsoft.EntityFrameworkCore;
using VisualSearch.Api.Features.Catalog;
using VisualSearch.Api.Features.Search;
using VisualSearch.Application.DependencyInjection;
using VisualSearch.Infrastructure;
using VisualSearch.Infrastructure.DependencyInjection;
using VisualSearch.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Iniciando Qdrant,Serviço Python e serviços internos;
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Endpoints
app.MapCatalogEndpoints();
app.MapSearchEndpoints();

// Garante que as migrations foram aplicadas ao subir
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();