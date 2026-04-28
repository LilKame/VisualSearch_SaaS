using Microsoft.Extensions.Logging;
using Minio.DataModel.Args;
using Minio;
using System;
using System.Collections.Generic;
using System.Text;
using Qdrant.Client.Grpc;

namespace VisualSearch.Infrastructure.Storage
{
    public class ObjectStorageService(IMinioClient client, ILogger<ObjectStorageService> logger): IObjectStorageService
    {
        // Nome do bucket onde as imagens vão ficar;
        private const string BucketName = "visualsearch";

        // Faz upload de um arquivo. Cria o bucket automaticamente se não existir.
        public async Task UploadAsync(string objectPath, Stream data, string contentType, CancellationToken ct = default)
        {
            // Garante que o bucket existe antes de fazer upload
            await EnsureBucketExistsAsync(ct);

            // Criando o objeto a ser depositado no bucket;
            var args = new PutObjectArgs()
                .WithBucket(BucketName)
                .WithObject(objectPath)
                .WithStreamData(data)
                .WithObjectSize(data.Length > 0 ? data.Length : -1) // Se o tamanho for menor que 0 (ou seja vazio) deixa como -1;
                .WithContentType(contentType);

            // Enviamos para o bucket
            await client.PutObjectAsync(args, ct);

            // Envia uma mensagem para o console;
            logger.LogDebug("Uploaded {Path}", BucketName);
        }

        // Retorna o stream do arquivo para leitura;
        public async Task<Stream> DownloadAsync(string objectPath, CancellationToken ct = default)
        {
            // Aqui alocamos espaço na memória.
            // Obs: A memória é alocada automaticamente começando por pouca memória e ajustando dinamicamente;
            var memoryStream = new MemoryStream();

            // Criamos os argumentos
            var args = new GetObjectArgs()
                .WithBucket(BucketName)
                .WithObject(objectPath)
                .WithCallbackStream(stream => stream.CopyToAsync(memoryStream)); // Quando o arquivo chegar envie ele para a memória;

            // Transformamos em objeto
            await client.GetObjectAsync(args, ct);
            // Voltamos o ponteiro da memória para o inicio
            memoryStream.Position = 0;

            return memoryStream;
        }

        // Remove o arquivo do storage;
        public async Task DeleteAsync(string objectPath, CancellationToken ct = default)
        {
            var args = new RemoveObjectArgs()
                .WithBucket(BucketName)
                .WithObject(objectPath);

            await client.RemoveObjectAsync(args, ct);
            logger.LogDebug("Deleted {Path}", BucketName);
        }

        public async Task<string> GetPresignedUrlAsync(string objectPath, int expiredTime = 3600 , CancellationToken ct = default)
        {
            var args = new PresignedGetObjectArgs()
                .WithBucket(BucketName)
                .WithObject(objectPath)
                .WithExpiry(expiredTime);

            

            logger.LogDebug("Url created {Path}", BucketName);

            // Enviamos a string aqui, o retorno da função já retorna uma string, não precisamos converter;
            return await client.PresignedGetObjectAsync(args); ;
        }

        // Verifica se o bucket existe, se não cria automáticamente;
        private async Task EnsureBucketExistsAsync(CancellationToken ct)
        {
            var exists = await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(BucketName), ct);

            // Se não existir cria automáticamente como manda o contrato;
            if(!exists)
            {
                await client.MakeBucketAsync(new MakeBucketArgs().WithBucket(BucketName), ct);
                logger.LogInformation("Created MinIO bucket : {Bucket}", BucketName);
            }
        }

    }
}
