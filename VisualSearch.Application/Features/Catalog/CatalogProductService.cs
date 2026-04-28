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
using VisualSearch.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace VisualSearch.Application.Features.Catalog
{
    // Obs: Sempre chame o contrato da função;
    public sealed class CatalogProductService
    {
        /// <summary>
        /// Dependency Inject;
        /// </summary>
        private readonly AppDbContext _db;
        private readonly IAiEngineClientService _ai;
        private readonly IVectorStoreService _vs;
        private readonly IObjectStorageService _os;
        public CatalogProductService(AppDbContext db, IAiEngineClientService ai, IVectorStoreService vs, IObjectStorageService os)
        {
            _db = db;
            _ai = ai;
            _vs = vs;
            _os = os;
        }

        /// <summary>
        /// Processar e retornar o embedding da imagem;
        /// </summary>
        /// <param name="imageStream"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="HttpRequestException"></exception>
        /// <exception cref="TimeoutException"></exception>
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

        /// <summary>
        /// Processar a imagem no MinIO;
        /// </summary>
        /// <param name="pathName"></param>
        /// <param name="data"></param>
        /// <param name="productCode"></param>
        /// <param name="angle"></param>
        /// <param name="contentType"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task <string> ProcessImageInMinio(string pathName, Stream data, string productCode, string angle , string contentType, CancellationToken ct)
        {
            // Garantimos que o ponteiro do Stream está no início;
            data.Position = 0;
            // Gerar o caminho;
            var storagePath = $"{pathName}/{productCode.ToUpperInvariant}/{angle}-{Guid.NewGuid()}.{contentType}";

            // Salvamos no MinIO;
            await _os.UploadAsync(storagePath, data, contentType, ct);

            // Voltamos o ponteiro para o início após processarmos;
            data.Position = 0;

            // Enviamos o caminho da alteração;
            return storagePath;
        }

        /// <summary>
        /// Deletar a imagem do MinIO;
        /// </summary>
        /// <param name="storagePath"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task DeleteImageInMinio(string storagePath, CancellationToken ct)
        {
            // Deletamos a imagem no caminho selecionado;
            await _os.DeleteAsync(storagePath, ct);
        }

        /// <summary>
        /// Salvar o embedding no banco;
        /// </summary>
        /// <param name="productCode"></param>
        /// <param name="productName"></param>
        /// <param name="angle"></param>
        /// <param name="productCategory"></param>
        /// <param name="storagePath"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task <Product> SaveEmbeddingInDbAsync(string productCode,string productName, string angle,string? productCategory, string storagePath,CancellationToken ct = default)
        {
            // Ver se já tem algum produto cadastrado com as condições definidas;
            var product = await _db.Products.Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Code == productCode.ToUpperInvariant(), ct);

            if(product is null)
            {
                // Se tiver vazio eu crio;
                product = Product.Create(productCode, productName, productCategory);
                // Adiciono ao banco;
                _db.Add(product);
                await _db.SaveChangesAsync(ct);
            }

            // Referenciamos a imagem salva no MinIO ao produto;
            product.AddImage(storagePath, angle, isPrimary: angle == "front");

            // Retorna o objeto salvo;
            return product;
        }

        /// <summary>
        /// Salvar o embedding no Qdrant;
        /// </summary>
        /// <param name="id"></param>
        /// <param name="embedding"></param>
        /// <param name="product"></param>
        /// <param name="productImage"></param>
        /// <param name="category"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
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

        // Atualiza a imagem como o vectorId e persiste tudo;
        /// <summary>
        /// Retorna a URL de preview para confirmar o upload da imagem;
        /// </summary>
        /// <param name="productImage"></param>
        /// <param name="product"></param>
        /// <param name="vectorId"></param>
        /// <param name="objectPath"></param>
        /// <param name="urlExpiredAt"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<string> SaveVectorReferenceAsync(ProductImage productImage,Product product,Guid vectorId, string objectPath, int urlExpiredAt, CancellationToken ct = default)
        {
            // Definimos o Id para essa imagem;
            productImage.SetVectorId(vectorId);
            // Marcamos que esse produto está com a imagem indexada;
            product.MarkAsIndexed();
            await _db.SaveChangesAsync(ct);

            // Geramos a url paara confirmar o salvamento;
            return await _os.GetPresignedUrlAsync(objectPath, urlExpiredAt, ct);
        }
    }
}
