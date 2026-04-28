using System;
using System.Collections.Generic;
using System.Text;

namespace VisualSearch.Infrastructure.VectorStore
{
    // Salvar no banco;
    public record VectorPoint(Guid Id, float[] Vector, Dictionary<string, object> Payload);

    // Envio para API;
    public record VectorSearchRequest(float[] Vector, int Limit = 5, float ScoreThreshold = 0.65f, string? CategoryFilter = null);

    // Retorno da API;
    public record VectorSearchResult(Guid Id, float Score, Dictionary<string, object> Payload);

    // Retorno da busca da API;
    public class ProductSearchResult()
    {
        public Guid productId { get; set; }
        public string productCode { get; set; }
        public string productName { get; set; }
        public string? category { get; set; }
        public double score { get; set; }
        public Object bestAngle { get; set; }
        public DateTime? indexedAt { get; set; }
    }


    public interface IVectorStoreService
    {
        // Função para salvar no banco;
        Task UpsertAsync(VectorPoint point, CancellationToken ct = default);

        // Enviar consulta para a API;
        Task<List<VectorSearchResult>> SearchAsync(VectorSearchRequest request, CancellationToken ct = default);

        // Deletar;
        Task DeleteAsync(Guid id, CancellationToken ct = default);

        // Criar coleção;
        Task EnsureCollectionExistsAsync(CancellationToken ct = default);
    }

}
