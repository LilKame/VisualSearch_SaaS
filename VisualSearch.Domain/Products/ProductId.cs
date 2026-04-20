using System;
using System.Collections.Generic;
using System.Text;

namespace VisualSearch.Domain.Products
{
    // Id Fortemente tipado para evitar erros;
    public readonly record struct ProductId(Guid Value)
    {
        public static ProductId New() => new (Guid.NewGuid());
        public static ProductId From(Guid value) => new(value);
        public override string ToString() => Value.ToString();

    }
}
