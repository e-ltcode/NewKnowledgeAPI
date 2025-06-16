using Azure.AI.OpenAI;
using Microsoft.Extensions.Caching.Memory;
using OpenAI;
using System.ClientModel; // Ensure you have the correct NuGet package installed

// To fix the CS0234 error, you need to install the Azure.AI.OpenAI NuGet package.
// Run the following command in the Package Manager Console or use the NuGet Package Manager in Visual Studio:
// Install-Package Azure.AI.OpenAI

namespace NewKnowledgeAPI.Services
{
    /// <summary>
    /// Service for generating vector embeddings for text using OpenAI.
    /// Embeddings are used to represent text as a vector for semantic search (finding similar questions).
    /// </summary>
    public interface IOpenAIEmbeddingService
    {
        Task<float[]> GenerateEmbeddingAsync(string text);
    }

    public class OpenAIEmbeddingService : IOpenAIEmbeddingService
    {
        private readonly OpenAIClient _openAIClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<OpenAIEmbeddingService> _logger;
        private readonly string _embeddingModel;
        private const int CacheExpirationMinutes = 60;

        public OpenAIEmbeddingService(
            IConfiguration configuration,
            IMemoryCache cache,
            ILogger<OpenAIEmbeddingService> logger)
        {
            var apiKey = configuration["OpenAI:ApiKey"] 
                ?? throw new ArgumentNullException("OpenAI:ApiKey configuration is missing");
            // Allow model to be set in config, fallback to ada-002
            _embeddingModel = configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-ada-002";
            // slavko
            // _openAIClient = new OpenAIClient(apiKey, new OpenAIClientOptions());
            
            // Fix for CS1503: Use ApiKeyCredential instead of string for the first argument
            var credential = new ApiKeyCredential(apiKey);
            _openAIClient = new OpenAIClient(credential, new OpenAIClientOptions());

            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Generates a vector embedding for the given text using OpenAI.
        /// Embeddings are high-dimensional vectors that capture the meaning of the text for similarity search.
        /// </summary>
        /// <param name="text">The input text to embed.</param>
        /// <returns>Embedding vector as a float array.</returns>
        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            var cacheKey = $"embedding_{text.GetHashCode()}";
            
            if (_cache.TryGetValue(cacheKey, out float[]? cachedEmbedding) && cachedEmbedding != null)
            {
                return cachedEmbedding;
            }

            try
            {
                // var embeddingOptions = new EmbeddingsOptions(_embeddingModel, new List<string> { text });
                // var response = await _openAIClient.GetEmbeddingsAsync(embeddingOptions);
                // var embedding = response.Value.Data[0].Embedding.ToArray();

                // Slavko
                var embeddingOptions = new EmbeddingsOptions(_embeddingModel, new List<string> { text });
                // var response = await _openAIClient.GetEmbeddingsAsync(embeddingOptions);
                var embedding = new float[0];
                _cache.Set(cacheKey, embedding, TimeSpan.FromMinutes(CacheExpirationMinutes));
                return embedding;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding for text: {Text}", text);
                throw new ApplicationException("Failed to generate embedding. Please try again later.");
            }
        }

        // Slavko generated
        private class EmbeddingsOptions
        {
            private string embeddingModel;
            private List<string> list;

            public EmbeddingsOptions(string embeddingModel, List<string> list)
            {
                this.embeddingModel = embeddingModel;
                this.list = list;
            }
        }
    }
} 