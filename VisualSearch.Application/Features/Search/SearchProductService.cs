using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using VisualSearch.Domain.Products;
using VisualSearch.Infrastructure.AiEngine;
using VisualSearch.Infrastructure.Persistence;
using VisualSearch.Infrastructure.Storage;
using VisualSearch.Infrastructure.VectorStore;

namespace VisualSearch.Application.Features.Search
{
    public sealed class SearchProductService
    {
        /// <summary>
        /// Dependency Inject;
        /// </summary>
        private readonly AppDbContext _db;
        private readonly IAiEngineClientService _ai;
        private readonly IVectorStoreService _vs;
        private readonly IObjectStorageService _os;
        public SearchProductService(AppDbContext db, IAiEngineClientService ai, IVectorStoreService vs, IObjectStorageService os)
        {
            _db = db;
            _ai = ai;
            _vs = vs;
            _os = os;
        }

        /// <summary>
        /// Busca no Qdrant quais são os vetores parecidos e retorna uma lista com o produto é o score;
        /// </summary>
        /// <param name="embedding"></param>
        /// <param name="category"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<List<VectorSearchResult>> SearchSimilarVectors(float[] embedding,string? category, CancellationToken ct)
        {
            var vectorResults = await _vs.SearchAsync(

                // Vamos utilizar alguns valores padrões;
                new VectorSearchRequest(Vector: embedding,CategoryFilter: category)
                , ct
            );

            // Retornamos os itens;
            return vectorResults;
        }

        /// <summary>
        /// Ordena e retorna os N produtos mais parecidos com a imagem enviada;
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<List<ProductSearchResult>> BuildProductSearchResultsAsync(List<VectorSearchResult> request, int numberOfResults, CancellationToken ct)
        {
            var productIds = request
                .Select(r => ProductId.From(Guid.Parse(r.Payload["product_id"].ToString()!)))
                .Distinct()
                .ToList();

            var products = await _db.Products
                .Include(p => p.Images)
                .Where(p => productIds.Contains(p.Id))
                .AsNoTracking()
                .ToDictionaryAsync(p => p.Id.Value, ct);

            // Monta o resultado agrupando por produto( melhor score estre as imagens)
            var tasks = request
                .GroupBy(r => r.Payload["product_id"].ToString()) //Aqui juntamos os produtos que tem o mesmo id e convertemos para string
                .Select
                (async g =>
                {
                    var productId = Guid.Parse(g.Key);
                    if (!products.TryGetValue(productId, out var product)) return null;

                    var primaryImage = product.Images.FirstOrDefault(p => p.IsPrimary) // Pegamos a primeira imagem que for primária;
                    ?? product.Images.FirstOrDefault(); // Se não tiver nenhuma definida como primaria pegamos a primeira imagem;

                    // Pegamos a URL
                    string? imageUrl = null;

                    if (primaryImage is not null)
                    {
                        imageUrl = await _os.GetPresignedUrlAsync(primaryImage.StoragePath);
                    }

                    var bestMatch = g.OrderByDescending(r => r.Score).First();

                    return new ProductSearchResult
                    {
                        productId = productId,
                        productCode = product.Code,
                        productName = product.Name,
                        category = product.Category,
                        score = Math.Round(bestMatch.Score * 100, 1), // Transforma o score em porcentagem, ex: 90.8%
                        bestAngle = bestMatch.Payload["angle"],
                        indexedAt = product.IndexedAt,
                        imageUrl = imageUrl
                    };
                }
                );
            var results = (await Task.WhenAll(tasks))
                .Where(r => r is not null)
                .OrderByDescending(r => r!.score)
                .Take(numberOfResults) // Define a quantidade de produtos retornados;
                .ToList();

            return results!;
        }
    }
}
