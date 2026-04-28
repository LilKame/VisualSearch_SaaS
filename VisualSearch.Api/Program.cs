using Microsoft.EntityFrameworkCore;
using VisualSearch.Api.Features.Catalog;
using VisualSearch.Infrastructure;
using VisualSearch.Infrastructure.Utils;

var builder = WebApplication.CreateBuilder(args);

// Serviços e DI;
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();

// Swagger (TEM QUE VIR ANTES DO BUILD)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Instanciar os Endpoints das Minimal APIs;
app.MapCatalogEndpoints();
app.Ma

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();