using System;
using System.Collections.Generic;
using System.Text;
using VisualSearch.Infrastructure.VectorStore;
using Qdrant;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using Microsoft.EntityFrameworkCore.Query;

namespace VisualSearch.Application.Qdrant
{
    public class QdrantVectorService(QdrantClient client) : IVectorStoreService
    {
        // Nome da coleção no Qdrant - como uma "tabela" para os vetores;
        private const string CollectionName = "products";

        public async Task EnsureCollectionExistsAsync(CancellationToken ct = default)
        {
            // Verifica se já existe uma "tabela" com esse nome;
            var collections = await client.ListCollectionsAsync(ct);
            if (collections.Any(c => c == CollectionName))
                return;

            // Cria a coleção com configurações otimizadas para CLIP (512 dims, distância cosseno)
            await client.CreateCollectionAsync(CollectionName,
        
                    new VectorParams
                    {
                        Size = 512,                      // CLIP ViT-B/32 sempre gera 512 dims
                        Distance = Distance.Cosine,      // Melhor para embeddings normalizados
                        OnDisk = false                   // Manter em RAM para velocidade máxima
                    
                }, cancellationToken: ct);
        }

        // Atualizar o banco de dados;
        public async Task UpsertAsync(VectorPoint point, CancellationToken ct = default)
        {
            var payload = point.Payload.ToDictionary(
                kv => kv.Key,
                // Converte o valor recebido no payload para string.
                kv => new Value { StringValue = kv.Value?.ToString() ?? "" }
            );

            await client.UpsertAsync(CollectionName,
                points:
                [
                    new PointStruct
                    {
                        Id = new PointId { Uuid = point.Id.ToString()},
                        Vectors = point.Vector,
                        Payload = {payload}
                    }
                ],
                cancellationToken : ct
            );
        }

        // Proucurar pelas imagens;
        public async Task<List<VectorSearchResult>> SearchAsync(VectorSearchRequest request, CancellationToken ct = default)
        {

        }

        // Deletar registros
        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {

        }
    }
}
