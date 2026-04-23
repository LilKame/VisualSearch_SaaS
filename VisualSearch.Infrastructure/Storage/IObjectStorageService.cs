using System;
using System.Collections.Generic;
using System.Text;

namespace VisualSearch.Infrastructure.Storage
{
    public interface IObjectStorageService
    {
        // Faz upload de um arquivo. Cria o bucket automaticamente se não existir.
        Task UploadAsync(string objectPath, Stream data, string contentType, CancellationToken ct = default);

        // Retorna o stream do arquivo para leitura;
        Task<Stream> DownloadAsync(string objectPath, CancellationToken ct = default);

        // Remove o arquivo do storage;
        Task DeleteAsync(string objectPath, CancellationToken ct = default);

        // Gerar uma url temporária da imagem para acesso sem passar pela API;
        // Por padrão definimos 1 hora;
        Task<string> GetPresignedUrlAsync(string objectPath, int expiredTime = 3600, CancellationToken ct = default);
    }
}
