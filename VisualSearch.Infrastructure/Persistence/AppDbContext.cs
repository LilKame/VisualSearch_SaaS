using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using VisualSearch.Domain;
using VisualSearch.Domain.Products;

namespace VisualSearch.Infrastructure.Persistence
{
    public sealed class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Criação das tabelas

        // Essa forma substitui o 'public DbSet<Product> Products {get; set;}
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductImage> ProductImages => Set<ProductImage>();

        // Alterar e definir regras das tabelas;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>(e =>
            {
                // Definir chave primária;
                e.HasKey(p => p.Id);

                // Ensina o EF Core a converter ProductId => Guid
                e.Property(p => p.Id)
                .HasConversion
                (
                    // Salva no banco;
                    id => id.Value,
                    // Retorna do banco
                    value => ProductId.From(value)
                )
                .HasColumnName("id");

                // Limite de caractéres;
                e.Property(p => p.Code)
                .HasMaxLength(100)
                .IsRequired();

                e.Property(p => p.Name)
                .HasMaxLength(500)
                .IsRequired();

                // Não obrigatórios;
                e.Property(p => p.Category)
                .HasMaxLength(100);
                e.Property(p => p.CategoryDescription)
                .HasMaxLength(500);

                // Relação entre Produto e imagens;
                // Um produto tem várias imagens e cada imagem só tem um produto;
                e.HasMany(p => p.Images)
                .WithOne()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

                // Sempre que for navegar por Images use _images
                e.Navigation(p => p.Images).HasField("_images");
            });

            modelBuilder.Entity<ProductImage>(i =>
            {
                // Definir chave primária;
                i.HasKey(p => p.Id);

                // Ensina o EF Core a converter ProductId => Guid
                i.Property(p => p.ProductId)
                .HasConversion(
                    p => p.Value,
                    value => ProductId.From(value)
                 )
                .HasColumnName("product_id");

                // Definir limite de caractéres;
                i.Property(p => p.StoragePath)
                .HasMaxLength(1000)
                .IsRequired();

                i.Property(p => p.Angle)
                .HasMaxLength(50)
                .IsRequired();
            });
        }
    }
}
    