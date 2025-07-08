using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NewKnowledgeAPI.Models;
using NewKnowledgeAPI.Q.Questions.Model;
using Knowledge.Services;

namespace NewKnowledgeAPI.Services
{
    /// <summary>
    /// Background service that automatically generates embeddings for questions without them
    /// </summary>
    public class EmbeddingBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmbeddingBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);
        private readonly int _batchSize = 10;

        public EmbeddingBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<EmbeddingBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Embedding Background Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessUnembeddedQuestions(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in embedding background service");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task ProcessUnembeddedQuestions(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbService = scope.ServiceProvider.GetRequiredService<DbService>();
            var embeddingService = scope.ServiceProvider.GetRequiredService<IOpenAIEmbeddingService>();
            var vectorSearchService = scope.ServiceProvider.GetRequiredService<IVectorSearchService>();

            try
            {
                // Get questions without embeddings
                var unembeddedQuestions = await GetQuestionsWithoutEmbeddings(dbService);
                
                if (!unembeddedQuestions.Any())
                {
                    _logger.LogDebug("No questions found without embeddings");
                    return;
                }

                _logger.LogInformation("Found {Count} questions without embeddings", unembeddedQuestions.Count);

                // Process in batches
                foreach (var batch in unembeddedQuestions.Chunk(_batchSize))
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    await ProcessBatch(batch, embeddingService, vectorSearchService, cancellationToken);
                    
                    // Add delay between batches to avoid rate limiting
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing unembedded questions");
            }
        }

        private async Task<List<Question>> GetQuestionsWithoutEmbeddings(DbService dbService)
        {
            var questionContainer = await dbService.GetContainer("Questions");
            var embeddingContainer = await dbService.GetContainer("EmbeddedQuestions");

            // Get all question IDs
            var questionQuery = new Microsoft.Azure.Cosmos.QueryDefinition(
                "SELECT c.id, c.title, c.parentCategory, c.assignedAnswers FROM c WHERE c.kind = 'question'");

            var questions = new List<Question>();
            var questionIterator = questionContainer.GetItemQueryIterator<Question>(questionQuery);

            while (questionIterator.HasMoreResults)
            {
                var response = await questionIterator.ReadNextAsync();
                questions.AddRange(response);
            }

            // Get all embedded question IDs
            var embeddedQuery = new Microsoft.Azure.Cosmos.QueryDefinition(
                "SELECT c.id FROM c WHERE c.partitionKey != null");

            var embeddedIds = new HashSet<string>();
            var embeddedIterator = embeddingContainer.GetItemQueryIterator<dynamic>(embeddedQuery);

            while (embeddedIterator.HasMoreResults)
            {
                var response = await embeddedIterator.ReadNextAsync();
                foreach (var item in response)
                {
                    embeddedIds.Add(item.id.ToString());
                }
            }

            // Return questions without embeddings
            return questions.Where(q => !embeddedIds.Contains(q.Id)).ToList();
        }

        private async Task ProcessBatch(
            Question[] batch,
            IOpenAIEmbeddingService embeddingService,
            IVectorSearchService vectorSearchService,
            CancellationToken cancellationToken)
        {
            var embeddedQuestions = new List<EmbeddedQuestion>();

            foreach (var question in batch)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    // Generate embedding for question title
                    var embedding = await embeddingService.GenerateEmbeddingAsync(question.Title);

                    // Create embedded question
                    var embeddedQuestion = new EmbeddedQuestion
                    {
                        Id = question.Id,
                        QuestionText = question.Title,
                        AnswerText = question.AssignedAnswers != null && question.AssignedAnswers.Any()
                            ? string.Join(" ", question.AssignedAnswers.Select(a => a.Title))
                            : "",
                        CategoryId = question.ParentCategory,
                        Embedding = embedding,
                        PartitionKey = question.Id,
                        CreatedAt = DateTime.UtcNow
                    };

                    embeddedQuestions.Add(embeddedQuestion);
                    
                    _logger.LogDebug("Generated embedding for question: {QuestionId}", question.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating embedding for question: {QuestionId}", question.Id);
                }
            }

            // Batch store the embeddings
            if (embeddedQuestions.Any())
            {
                try
                {
                    await vectorSearchService.BatchProcessQuestionsAsync(embeddedQuestions);
                    _logger.LogInformation("Stored {Count} embeddings successfully", embeddedQuestions.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error storing batch embeddings");
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Embedding Background Service is stopping");
            await base.StopAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Extension methods for registering the embedding service
    /// </summary>
    public static class EmbeddingServiceExtensions
    {
        public static IServiceCollection AddEmbeddingBackgroundService(this IServiceCollection services)
        {
            services.AddHostedService<EmbeddingBackgroundService>();
            return services;
        }
    }
}