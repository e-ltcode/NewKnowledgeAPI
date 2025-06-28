using Azure.AI.OpenAI;
using Microsoft.Extensions.Caching.Memory;

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
            _openAIClient = new OpenAIClient(apiKey, new OpenAIClientOptions());
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
                var embeddingOptions = new EmbeddingsOptions(_embeddingModel, new List<string> { text });
                var response = await _openAIClient.GetEmbeddingsAsync(embeddingOptions);
                var embedding = response.Value.Data[0].Embedding.ToArray();

                _cache.Set(cacheKey, embedding, TimeSpan.FromMinutes(CacheExpirationMinutes));
                return embedding;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding for text: {Text}", text);
                throw new ApplicationException("Failed to generate embedding. Please try again later.");
            }
        }
    }
} 