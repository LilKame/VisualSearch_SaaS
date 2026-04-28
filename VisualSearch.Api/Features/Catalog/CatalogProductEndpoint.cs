using Microsoft.AspNetCore.Mvc;
using VisualSearch.Infrastructure;
using VisualSearch.Infrastructure.AiEngine;
using VisualSearch.Infrastructure.Persistence;
using VisualSearch.Infrastructure.VectorStore;
using VisualSearch.Application;
using VisualSearch.Application.Features.Catalog;
using VisualSearch.Domain.Products;

namespace VisualSearch.Api.Features.Catalog
{
    public static class CatalogProductEndpoint
    {
        public static void MapCatalogEndpoints(this IEndpointRouteBuilder app)
        {
            // Rota para catalogar itens;
            // MapPost( "rota" , função chamada);
            app.MapPost("/api/v1/catalog", HandleAsync)
                .DisableAntiforgery()
                .WithName("CatalogProduct");
        }

         private static async Task<IResult> HandleAsync
         (
                [FromForm] string productCode,
                [FromForm] string productName,
                [FromForm] string? categoryItem,
                [FromForm] string angle,
                IFormFile image,
                [FromServices]CatalogProductService service,
                CancellationToken ct
         )
         {
            
            // Lê os bytes da imagem
            await using var imageStream = image.OpenReadStream(); //Aqui iniciamos a leitura da imagem;

            // Declaramos a variável que vai armazenar o embedding;
            float[] embedding;

            // Salvamos a imagem no MinIO e pegamos o caminho gerado;
            var storagePath = await service.ProcessImageInMinio("products", imageStream, productCode, angle, "jpg", ct);

            // Voltamos o ponteiro para o inicio;
            imageStream.Position = 0;

            // Processamos e geramos o embedding;
            try
            {
                embedding = await service.ProcessImageAsync(imageStream, ct);
            }
            catch(HttpRequestException)
            {
                // Se der erro em processar a imagem retiramos ela do MinIO;
                await service.DeleteImageInMinio(storagePath, ct);

                return Results.Problem(
                    detail: "Erro na requisição."
                    ,statusCode: 502
                );
            }

            // Salvamos o produto no banco;
            var product = await service.SaveEmbeddingInDbAsync(productCode, productName,angle,categoryItem,storagePath,ct);
            var productImage = product.Images.Last();

            // Aqui criamos um novo ID;
            Guid vectorId = Guid.NewGuid();

            // Agora salvamos no Qdrant;
            await service.SaveEmbeddingInQdrantAsync(vectorId, embedding, product, productImage, categoryItem, ct);

            // Salva o ID do vetor na imagem e persiste no PostgreSql;
            await service.SaveVectorReferenceAsync(productImage, product, vectorId, storagePath, 3600, ct); // 3600 segundos, expira em 1 hora a url de preview;

            return Results.Ok(new
            { 
                productId = product!.Id.Value,
                productCode = product.Code,
                vectorId = vectorId,
                dimensions = embedding.Length,
                message = "Produto catalogado e indexado com sucesso"
            }

            );
        }
    }
}
