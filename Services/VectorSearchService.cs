using Knowledge.Services;
using Microsoft.Azure.Cosmos;
using NewKnowledgeAPI.Models;

namespace NewKnowledgeAPI.Services
{
    /// <summary>
    /// Service for storing and searching questions in Cosmos DB using vector search.
    /// Vector search allows finding semantically similar questions using embeddings.
    /// </summary>
    public interface IVectorSearchService
    {
        Task InitializeAsync();
        Task StoreQuestionAsync(EmbeddedQuestion question);
        Task<List<EmbeddedQuestion>> FindSimilarQuestionsAsync(float[] embedding, int topK = 3);
        Task BatchProcessQuestionsAsync(IEnumerable<EmbeddedQuestion> questions);
    }

    public class VectorSearchService : IVectorSearchService
    {
        private readonly DbService _dbService;
        private readonly ILogger<VectorSearchService> _logger;
        private readonly string _containerId;
        private readonly int _embeddingDimension;

        /// <summary>
        /// Initializes the vector search service.
        /// </summary>
        /// <param name="dbService">Database service for Cosmos DB.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="containerId">Cosmos DB container name (default: EmbeddedQuestions).</param>
        /// <param name="embeddingDimension">Embedding vector dimension (default: 1536 for OpenAI ada-002).</param>
        public VectorSearchService(
            DbService dbService,
            ILogger<VectorSearchService> logger,
            string containerId = "EmbeddedQuestions",
            int embeddingDimension = 1536)
        {
            _dbService = dbService;
            _logger = logger;
            _containerId = containerId;
            _embeddingDimension = embeddingDimension;
        }

        /// <summary>
        /// Ensures the Cosmos DB container is set up for vector search.
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                var container = await _dbService.GetContainer(_containerId);
                var containerProperties = new ContainerProperties
                {
                    Id = _containerId,
                    PartitionKeyPath = "/partitionKey",
                    // TODO Slavko commented out
                    //VectorIndexingPolicy = new VectorIndexingPolicy
                    //{
                    //    VectorIndexes = new List<VectorIndex>
                    //    {
                    //        new VectorIndex
                    //        {
                    //            Path = "/embedding",
                    //            Type = VectorIndexType.COSMOS_DOT_PRODUCT,
                    //            Dimensions = _embeddingDimension
                    //        }
                    //    }
                    //}
                };
                await container.ReplaceContainerAsync(containerProperties);
                _logger.LogInformation("Vector search container initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing vector search container");
                throw;
            }
        }

        /// <summary>
        /// Stores a question and its embedding in Cosmos DB.
        /// </summary>
        public async Task StoreQuestionAsync(EmbeddedQuestion question)
        {
            try
            {
                var container = await _dbService.GetContainer(_containerId);
                await container.CreateItemAsync(question, new PartitionKey(question.PartitionKey));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing question: {QuestionId}", question.Id);
                throw;
            }
        }

        /// <summary>
        /// Finds the most similar questions to the given embedding using vector search.
        /// </summary>
        public async Task<List<EmbeddedQuestion>> FindSimilarQuestionsAsync(float[] embedding, int topK = 3)
        {
            try
            {
                var container = await _dbService.GetContainer(_containerId);
                var query = new QueryDefinition(
                    "SELECT TOP @topK c.id, c.partitionKey, c.question, c.answer, c.embedding, c.categoryId, c.groupId, VectorDistance(c.embedding, @embedding) as similarity FROM c ORDER BY VectorDistance(c.embedding, @embedding) DESC")
                    .WithParameter("@topK", topK)
                    .WithParameter("@embedding", embedding);

                var results = new List<EmbeddedQuestion>();
                var iterator = container.GetItemQueryIterator<EmbeddedQuestion>(query);
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    results.AddRange(response);
                }
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding similar questions");
                throw;
            }
        }

        /// <summary>
        /// Batch stores multiple questions and their embeddings in Cosmos DB.
        /// </summary>
        public async Task BatchProcessQuestionsAsync(IEnumerable<EmbeddedQuestion> questions)
        {
            try
            {
                var container = await _dbService.GetContainer(_containerId);
                var tasks = questions.Select(q => 
                    container.CreateItemAsync(q, new PartitionKey(q.PartitionKey)));
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch processing questions");
                throw;
            }
        }
    }
} 