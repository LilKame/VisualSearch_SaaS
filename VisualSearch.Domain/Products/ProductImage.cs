using System;
using System.Collections.Generic;
using System.Text;

namespace VisualSearch.Domain.Products
{
    // Entidade modelo "DDD"
    // "Sealded" deixa explicito que não podem herdar dessa classe;
    public sealed class ProductImage
    {
        // Id do registro;
        public Guid Id { get; private set; }
        // Referência do produto;
        public ProductId ProductId { get; private set; }

        // Caminho no MinIO/storage: ex "products/SKU001/front-abc123.jpg"
        public string StoragePath { get; private set; }
        
        // Ângulo da imagem;
        public string Angle { get; private set; }

        // Defino se a imagem é primária;
        public bool IsPrimary { get; private set; }

        // ID do vetor no Qdrant — preenchido APÓS a indexação
        // Null significa que essa imagem ainda não foi processada pela IA
        public Guid? VectorId { get; private set; }

        // Data de criação do registro;
        public DateTime CreatedAt { get; private set; }

        // Referência para o EF Core acessar;
        // Um tipo de backdoor que apenas o EF Core acessa;
        private ProductImage() { }

        // Método para criar o objeto;
        // "Static" diz que essa função pertence a classe e não ao objeto;
        internal static ProductImage Create(
            ProductId productId,
            string storagePath,
            string angle,
            bool isPrimary
        ) =>
            // Retorno da função;
            new()
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            StoragePath = storagePath,
            Angle = angle,
            IsPrimary = isPrimary,
            CreatedAt = DateTime.UtcNow
        };

        // Define se a imagem é a primária;
        internal void SetPrimary(bool value) => IsPrimary = value;

        // Define o VectorId, precisa ser "public" para IA conseguir acessar;
        public void SetVectorId(Guid value) => VectorId = value;
    }
}
