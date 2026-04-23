using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using VisualSearch.Infrastructure.AiEngine;
using VisualSearch.Infrastructure.VectorStore;
using VisualSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Update;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using VisualSearch.Domain.Products;

namespace VisualSearch.Application.Features.Catalog
{
    // Obs: Sempre chame o contrato da função;
    public sealed class CatalogProductService
    {
        // Dependency Inject;
        private readonly AppDbContext _db;
        private readonly IAiEngineClientService _ai;
        private readonly IVectorStoreService _vs;
        public CatalogProductService(AppDbContext db, IAiEngineClientService ai, IVectorStoreService vs)
        {
            _db = db;
            _ai = ai;
            _vs = vs;
        }

        public async Task<float[]> ProcessImageAsync(Stream imageStream,CancellationToken ct)
        {
            // Alocando espaço na memória;
            using var ms = new MemoryStream();
            // Enviamos os bytes para memória;
            await imageStream.CopyToAsync(ms, ct);
            // Voltamos o ponteiro para o inicio dos bytes;
            // Ele inicia no final, oque impede a leitura;
            ms.Position = 0;

            // Garante que a coleção existe no Qdrant
            await _vs.EnsureCollectionExistsAsync(ct);

            // Declaramos a variável de retorno;
            float[] embedding;

            try
            {
                // Transformamos os bytes da imagem no embedding que será salvo no Qdrant;
                return embedding = await _ai.ExtractEmbeddingAsync(ms,ct);
            }
            // Exceções;
            catch (HttpRequestException ex)
            {
                throw new HttpRequestException("Erro ao chamar o serviço de IA", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new TimeoutException("Timeout ao gerar embedding", ex);
            }
        }

        public async Task <Product?> SaveEmbeddingInDbAsync(string productCode,string productName, string? productCategory, CancellationToken ct = default)
        {
            // Ver se já tem algum produto cadastrado;
            var product = await _db.Products.FirstOrDefaultAsync();

            if(product is null)
            {
                // Se tiver vazio eu crio;
                product = Product.Create(productCode, productName, productCategory);
                // Adiciono ao banco;
                _db.Add(product);
                await _db.SaveChangesAsync(ct);
            }
            // Retorna o objeto salvo;
            return product;
        }

        public async Task SaveEmbeddingInQdrantAsync
        (
            Guid id, float[] embedding, Product product, ProductImage productImage, string? category
            ,CancellationToken ct
        )
        {
            // Aqui indexamos no Qdrant;
            await _vs.UpsertAsync
            (
                new VectorPoint
                (
                    Id: id,
                    Vector: embedding,
                    Payload: new Dictionary<string, object>
                    {
                        ["product_id"] = product.Id.Value.ToString(),
                        ["product_code"] = product.Code,
                        ["image_id"] = productImage.Id.ToString(),
                        ["angle"] = productImage.Angle,
                        ["category"] = category ?? "geral"
                    }
                )
            ,ct
            );
        }
        public async Task SaveVectorReferenceAsync(ProductImage productImage,Product product,Guid vectorId, CancellationToken ct = default)
        {
            // Definimos o Id para essa imagem;
            productImage.SetVectorId(vectorId);
            // Marcamos que esse produto está com a imagem indexada;
            product.MarkAsIndexed();
            await _db.SaveChangesAsync(ct);
        }
    }
}
