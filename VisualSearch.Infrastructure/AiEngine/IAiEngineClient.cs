using System;
using System.Collections.Generic;
using System.Text;

namespace VisualSearch.Infrastructure.AiEngine
{
    public interface IAiEngineClientService
    {
        Task<float[]> ExtractEmbeddingAsync(Stream ImageStram, CancellationToken ct);
    }
}
