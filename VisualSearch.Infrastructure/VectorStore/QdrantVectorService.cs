using System;
using System.Collections.Generic;
using System.Text;
using Qdrant;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using Microsoft.EntityFrameworkCore.Query;
using System.Xml.Serialization;

namespace VisualSearch.Infrastructure.VectorStore
{
    public class QdrantVectorService(QdrantClient client) : IVectorStoreService
    {
        // Nome da coleção no Qdrant - como uma "tabela" para os vetores;
        private const string CollectionName = "products";

        // Criar a coleção caso ela não exista;
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

        // Atualizar o banco do Qdrant;
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
            Filter? filter = null;
            if( request.CategoryFilter is not null)
            {
                filter = new Filter
                {
                    Must =
                    {
                        new Condition
                        {
                            Field = new FieldCondition
                            {
                                Key = "category",
                                Match = new Match {Text = request.CategoryFilter}
                            }
                        }
                    }

                };


            }
                var results = await client.SearchAsync
                (
                   collectionName : CollectionName,
                   vector : request.Vector,
                   limit : (ulong)request.Limit,
                   scoreThreshold : request.ScoreThreshold,
                   filter : filter,
                   cancellationToken : ct
                );

                return results.Select(r => new VectorSearchResult
                (
                    Id: Guid.Parse(r.Id.Uuid),
                    Score: r.Score,
                    Payload: r.Payload.ToDictionary
                    (
                        kv => kv.Key,
                        kv => (object)kv.Value.StringValue
                    )

                )).ToList();
        }

        // Deletar registros
        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            await client.DeleteAsync
            (
                CollectionName,
                id : new PointId { Uuid = id.ToString()},
                cancellationToken: ct
            );
        }
    }
}
