using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VisualSearch.Application;
using VisualSearch.Application.Features.Catalog;
using VisualSearch.Application.Features.Search;
using VisualSearch.Domain.Products;
using VisualSearch.Infrastructure;
using VisualSearch.Infrastructure.AiEngine;
using VisualSearch.Infrastructure.Persistence;
using VisualSearch.Infrastructure.VectorStore;

namespace VisualSearch.Api.Features.Search
{
    public static class SearchProductEndpoint
    {
        public static void MapSearchEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/v1/search", HandleAsync)
                .DisableAntiforgery()
                .WithName("SearchProduct");
        }

        private static async Task<IResult> HandleAsync
        (
            [FromForm] IFormFile iamge,
            [FromForm] string? category,
            SearchProductService searchService,
            CatalogProductService catalogService,
            CancellationToken ct
        )
        {
            // Primeira coisa a se fazer e ler os bytes da imagem;
            await using var stream = iamge.OpenReadStream();

            // Declaramos o array onde vão ficar os dados
            float[] embeddding;

            // Processamos a imagem;
            try
            {
                // Processamos a imagem e pegamos o vetor
                embeddding = await catalogService.ProcessImageAsync(stream, ct);
            }
            catch(HttpRequestException)
            {
                return Results.Problem
                (
                    detail: "Erro na requisição",
                    statusCode: 502
                );
            }

            // Fazemos a comparação com os vetores salvos;
            var vectorResults = await searchService.SearchSimilarVectors(embeddding, category, ct);

            // Verificamos se tem algum produto similar com o da imagem no sistema;
            if(vectorResults.Count <= 0)
            {
                return Results.Ok(new { results = Array.Empty<object>(), message = "Nenhum produto similar encontrado." });
            }

            // Agora pegamos o produto relacionado e enviamos os 5 com o maior score;
            var results = await searchService.BuildProductSearchResultsAsync(vectorResults, 5, ct);

            // Por fim retornamos o resultado;
            return Results.Ok(new { results, total = results.Count });

        }
    }
}
