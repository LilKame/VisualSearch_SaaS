using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using VisualSearch.Infrastructure.AiEngine;

namespace VisualSearch.Application.AiEngine
{
    // Deixa como parâmetro o serviço para instanciar ele depois;
    public sealed class AiEngineClientService(HttpClient httpClient) : IAiEngineClientService
    {
        public async Task<float[]> ExtractEmbeddingAsync(Stream imageStream,CancellationToken ct)
        {
            // Isso simula o envio de um formulário;
            using var content = new MultipartFormDataContent();

            // Isso coloca a imagem para envio;
            using var streamContent = new StreamContent(imageStream);

            // Definir o tipo do arquivo de imagem.
            streamContent.Headers.ContentType =
                new MediaTypeHeaderValue("image/jpeg");

            // Adiciona a imagem;
            content.Add(streamContent, "file", "image.jpg");

            // Envia a requisição;
            // PostAsync("rota" , body , CancellationToken);
            var response = await httpClient.PostAsync("/embed", content, ct);
            // Se o servidor retornar algum erro, gera imediatamente uma exception;
            response.EnsureSuccessStatusCode();

            // Ler o corpo da requisição e transformar em json;
            var json = await response.Content.ReadAsStringAsync();
            // Transformando em algo manipulável, tipo um objeto;
            var doc = JsonDocument.Parse(json);

            // Lê o array "embedding" do retorno da API Python;
            var embedding = doc.RootElement.GetProperty("embedding")
                .EnumerateArray()
                .Select(e => e.GetSingle())
                .ToArray();
            // Agora temos o array que devemos retornar;

            return embedding;
        }
    }
    
}
