using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace VisualSearch.Domain.Products
{
    public sealed class Product
    {
        // Id de refência única;
        public ProductId Id { get; private set; }

        // Codigo do produto;
        // "string.Empty" diz que a variável vai iniciar vazia propositalmente;
        public string Code { get; private set; } = string.Empty;

        // Nome do produto;
        public string Name { get; private set; } = string.Empty;

        // Categoria do produto;
        // "?" pode ter ou não;
        public string ? Category { get; private set; }

        // Descrição da categoria;
        public string ? CategoryDescription { get; private set; }

        // Data de criação do registro;
        public DateTime CreatedAt { get; private set; }

        // Está ativo o produto?
        public bool IsActive { get; private set; }

        // Preenchido quando os vetores de todas as imagens foram indexados pelo Qdrant;
        public DateTime? IndexedAt { get; private set; }

        // Imagens pertencentes a esse produto
        private readonly List<ProductImage> _images = [];
        public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();

        // EF Core acessar;
        private Product() { }

        public static Product Create(string code,string name,string? category = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(code);
            ArgumentException.ThrowIfNullOrEmpty(name);

            return new Product
            {
                Id = ProductId.New(),
                Code = code,
                Name = name,
                // "Trim" remove os espaços do início e do fim;
                Category = category?.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
        }

        public ProductImage AddImage(string storagePath, string angle, bool isPrimary = false)
        {
            if (isPrimary)
            {
                _images.ForEach(i => i.SetPrimary(false));
            }

            var image = ProductImage.Create(Id, storagePath, angle, isPrimary);
            _images.Add(image);

            return image;
        }

        // Marcar que o produto foi indexado;
        public void MarkAsIndexed() => IndexedAt = DateTime.UtcNow;
    }
}
