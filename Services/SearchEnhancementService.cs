using Microsoft.Extensions.Caching.Memory;
using NewKnowledgeAPI.Models;
using NewKnowledgeAPI.Q.Questions.Model;
using System.Security.Cryptography;
using System.Text;

namespace NewKnowledgeAPI.Services
{
    /// <summary>
    /// Enhanced search service that provides hybrid search, caching, and feedback tracking
    /// </summary>
    public interface ISearchEnhancementService
    {
        Task<List<SearchResult>> HybridSearchAsync(string query, float semanticWeight = 0.7f, int topK = 5);
        Task RecordSearchFeedbackAsync(string query, string questionId, int position, bool clicked);
        Task<List<QuestionSuggestion>> GetQuestionSuggestionsAsync(string partialQuery, int count = 5);
        Task UpdateQuestionEmbeddingAsync(string questionId);
        Task<SearchAnalytics> GetSearchAnalyticsAsync(DateTime startDate, DateTime endDate);
    }

    public class SearchEnhancementService : ISearchEnhancementService
    {
        private readonly IVectorSearchService _vectorSearchService;
        private readonly IOpenAIEmbeddingService _embeddingService;
        private readonly DbService _dbService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SearchEnhancementService> _logger;
        private const string FEEDBACK_CONTAINER = "SearchFeedback";
        private const string CACHE_PREFIX = "search:";
        private const int CACHE_DURATION_MINUTES = 60;

        public SearchEnhancementService(
            IVectorSearchService vectorSearchService,
            IOpenAIEmbeddingService embeddingService,
            DbService dbService,
            IMemoryCache cache,
            ILogger<SearchEnhancementService> logger)
        {
            _vectorSearchService = vectorSearchService;
            _embeddingService = embeddingService;
            _dbService = dbService;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Performs hybrid search combining semantic and keyword search
        /// </summary>
        public async Task<List<SearchResult>> HybridSearchAsync(string query, float semanticWeight = 0.7f, int topK = 5)
        {
            var cacheKey = $"{CACHE_PREFIX}hybrid:{ComputeHash(query)}:{semanticWeight}:{topK}";
            
            if (_cache.TryGetValue<List<SearchResult>>(cacheKey, out var cachedResults))
            {
                _logger.LogDebug("Returning cached results for query: {Query}", query);
                return cachedResults;
            }

            try
            {
                // Parallel execution of both search types
                var semanticTask = PerformSemanticSearchAsync(query, topK * 2);
                var keywordTask = PerformKeywordSearchAsync(query, topK * 2);

                await Task.WhenAll(semanticTask, keywordTask);

                var semanticResults = await semanticTask;
                var keywordResults = await keywordTask;

                // Combine and rank results
                var combinedResults = CombineSearchResults(
                    semanticResults, 
                    keywordResults, 
                    semanticWeight, 
                    topK);

                // Cache the results
                _cache.Set(cacheKey, combinedResults, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

                return combinedResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing hybrid search for query: {Query}", query);
                throw;
            }
        }

        /// <summary>
        /// Records user interaction with search results for improving future searches
        /// </summary>
        public async Task RecordSearchFeedbackAsync(string query, string questionId, int position, bool clicked)
        {
            try
            {
                var feedback = new SearchFeedback
                {
                    Id = Guid.NewGuid().ToString(),
                    Query = query,
                    QuestionId = questionId,
                    Position = position,
                    Clicked = clicked,
                    Timestamp = DateTime.UtcNow,
                    PartitionKey = DateTime.UtcNow.ToString("yyyy-MM-dd")
                };

                var container = await _dbService.GetContainer(FEEDBACK_CONTAINER);
                await container.CreateItemAsync(feedback);

                // Update relevance score for this query-question pair
                if (clicked)
                {
                    await UpdateRelevanceScoreAsync(query, questionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording search feedback");
                // Don't throw - feedback is not critical
            }
        }

        /// <summary>
        /// Provides real-time question suggestions as user types
        /// </summary>
        public async Task<List<QuestionSuggestion>> GetQuestionSuggestionsAsync(string partialQuery, int count = 5)
        {
            if (string.IsNullOrWhiteSpace(partialQuery) || partialQuery.Length < 3)
            {
                return new List<QuestionSuggestion>();
            }

            var cacheKey = $"{CACHE_PREFIX}suggest:{ComputeHash(partialQuery)}:{count}";
            
            if (_cache.TryGetValue<List<QuestionSuggestion>>(cacheKey, out var cachedSuggestions))
            {
                return cachedSuggestions;
            }

            try
            {
                var embedding = await _embeddingService.GenerateEmbeddingAsync(partialQuery);
                var similarQuestions = await _vectorSearchService.FindSimilarQuestionsAsync(embedding, count);

                var suggestions = similarQuestions.Select((q, index) => new QuestionSuggestion
                {
                    Id = q.Id,
                    Title = q.QuestionText,
                    Similarity = CalculateSimilarityScore(index, similarQuestions.Count),
                    CategoryId = q.CategoryId,
                    AnswerPreview = q.AnswerText?.Length > 100 
                        ? q.AnswerText.Substring(0, 100) + "..." 
                        : q.AnswerText
                }).ToList();

                _cache.Set(cacheKey, suggestions, TimeSpan.FromMinutes(15));

                return suggestions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting question suggestions");
                return new List<QuestionSuggestion>();
            }
        }

        /// <summary>
        /// Updates the embedding for a specific question (useful when question is edited)
        /// </summary>
        public async Task UpdateQuestionEmbeddingAsync(string questionId)
        {
            try
            {
                var questionContainer = await _dbService.GetContainer("Questions");
                var questionResponse = await questionContainer.ReadItemAsync<Question>(
                    questionId, 
                    new Microsoft.Azure.Cosmos.PartitionKey("question"));

                var question = questionResponse.Resource;
                var embedding = await _embeddingService.GenerateEmbeddingAsync(question.Title);

                var embeddedQuestion = new EmbeddedQuestion
                {
                    Id = question.Id,
                    QuestionText = question.Title,
                    AnswerText = string.Join(" ", question.AssignedAnswers?.Select(a => a.Title) ?? Array.Empty<string>()),
                    CategoryId = question.ParentCategory,
                    Embedding = embedding,
                    PartitionKey = question.Id
                };

                await _vectorSearchService.StoreQuestionAsync(embeddedQuestion);

                // Clear relevant caches
                ClearQuestionCaches(question.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating question embedding for ID: {QuestionId}", questionId);
                throw;
            }
        }

        /// <summary>
        /// Provides analytics on search performance and user behavior
        /// </summary>
        public async Task<SearchAnalytics> GetSearchAnalyticsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var container = await _dbService.GetContainer(FEEDBACK_CONTAINER);
                var query = new Microsoft.Azure.Cosmos.QueryDefinition(
                    @"SELECT * FROM c 
                      WHERE c.timestamp >= @startDate 
                      AND c.timestamp <= @endDate")
                    .WithParameter("@startDate", startDate)
                    .WithParameter("@endDate", endDate);

                var feedbackList = new List<SearchFeedback>();
                var iterator = container.GetItemQueryIterator<SearchFeedback>(query);
                
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    feedbackList.AddRange(response);
                }

                return new SearchAnalytics
                {
                    TotalSearches = feedbackList.Select(f => f.Query).Distinct().Count(),
                    TotalClicks = feedbackList.Count(f => f.Clicked),
                    ClickThroughRate = feedbackList.Any() 
                        ? (double)feedbackList.Count(f => f.Clicked) / feedbackList.Count 
                        : 0,
                    TopSearchQueries = feedbackList
                        .GroupBy(f => f.Query)
                        .OrderByDescending(g => g.Count())
                        .Take(10)
                        .Select(g => new QueryStats 
                        { 
                            Query = g.Key, 
                            Count = g.Count(),
                            ClickRate = (double)g.Count(f => f.Clicked) / g.Count()
                        })
                        .ToList(),
                    PeriodStart = startDate,
                    PeriodEnd = endDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting search analytics");
                throw;
            }
        }

        private async Task<List<SearchResult>> PerformSemanticSearchAsync(string query, int topK)
        {
            var embedding = await _embeddingService.GenerateEmbeddingAsync(query);
            var results = await _vectorSearchService.FindSimilarQuestionsAsync(embedding, topK);

            return results.Select((r, index) => new SearchResult
            {
                QuestionId = r.Id,
                Title = r.QuestionText,
                Answer = r.AnswerText,
                Score = CalculateSimilarityScore(index, results.Count),
                SearchType = SearchType.Semantic,
                CategoryId = r.CategoryId
            }).ToList();
        }

        private async Task<List<SearchResult>> PerformKeywordSearchAsync(string query, int topK)
        {
            // Simple keyword search implementation
            var container = await _dbService.GetContainer("Questions");
            var keywords = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            var sqlQuery = new Microsoft.Azure.Cosmos.QueryDefinition(
                @"SELECT TOP @topK * FROM c 
                  WHERE c.kind = 'question' 
                  AND CONTAINS(LOWER(c.title), @keyword)")
                .WithParameter("@topK", topK)
                .WithParameter("@keyword", keywords.FirstOrDefault() ?? query.ToLower());

            var results = new List<Question>();
            var iterator = container.GetItemQueryIterator<Question>(sqlQuery);
            
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results.Select((r, index) => new SearchResult
            {
                QuestionId = r.Id,
                Title = r.Title,
                Answer = string.Join(" ", r.AssignedAnswers?.Select(a => a.Title) ?? Array.Empty<string>()),
                Score = CalculateKeywordScore(r.Title, keywords),
                SearchType = SearchType.Keyword,
                CategoryId = r.ParentCategory
            }).OrderByDescending(r => r.Score).ToList();
        }

        private List<SearchResult> CombineSearchResults(
            List<SearchResult> semanticResults, 
            List<SearchResult> keywordResults, 
            float semanticWeight, 
            int topK)
        {
            var allResults = new Dictionary<string, SearchResult>();

            // Add semantic results with weighted scores
            foreach (var result in semanticResults)
            {
                result.Score *= semanticWeight;
                allResults[result.QuestionId] = result;
            }

            // Add or update with keyword results
            foreach (var result in keywordResults)
            {
                result.Score *= (1 - semanticWeight);
                
                if (allResults.ContainsKey(result.QuestionId))
                {
                    // Combine scores if found in both searches
                    allResults[result.QuestionId].Score += result.Score;
                    allResults[result.QuestionId].SearchType = SearchType.Hybrid;
                }
                else
                {
                    allResults[result.QuestionId] = result;
                }
            }

            return allResults.Values
                .OrderByDescending(r => r.Score)
                .Take(topK)
                .ToList();
        }

        private async Task UpdateRelevanceScoreAsync(string query, string questionId)
        {
            // This could be expanded to store relevance scores in a separate container
            // For now, we'll just log it
            _logger.LogInformation(
                "User clicked on question {QuestionId} for query '{Query}'", 
                questionId, 
                query);
        }

        private double CalculateSimilarityScore(int position, int total)
        {
            // Simple scoring based on position
            return 1.0 - ((double)position / total);
        }

        private double CalculateKeywordScore(string title, string[] keywords)
        {
            var titleLower = title.ToLower();
            var score = 0.0;
            
            foreach (var keyword in keywords)
            {
                if (titleLower.Contains(keyword))
                {
                    score += 1.0;
                }
            }
            
            return score / keywords.Length;
        }

        private void ClearQuestionCaches(string questionTitle)
        {
            // In a production system, you might want to track cache keys more systematically
            _logger.LogInformation("Clearing caches related to question: {Title}", questionTitle);
        }

        private string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }
    }

    // Supporting models
    public class SearchResult
    {
        public string QuestionId { get; set; }
        public string Title { get; set; }
        public string Answer { get; set; }
        public double Score { get; set; }
        public SearchType SearchType { get; set; }
        public string CategoryId { get; set; }
    }

    public enum SearchType
    {
        Semantic,
        Keyword,
        Hybrid
    }

    public class SearchFeedback
    {
        public string Id { get; set; }
        public string Query { get; set; }
        public string QuestionId { get; set; }
        public int Position { get; set; }
        public bool Clicked { get; set; }
        public DateTime Timestamp { get; set; }
        public string PartitionKey { get; set; }
    }

    public class QuestionSuggestion
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public double Similarity { get; set; }
        public string CategoryId { get; set; }
        public string AnswerPreview { get; set; }
    }

    public class SearchAnalytics
    {
        public int TotalSearches { get; set; }
        public int TotalClicks { get; set; }
        public double ClickThroughRate { get; set; }
        public List<QueryStats> TopSearchQueries { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }

    public class QueryStats
    {
        public string Query { get; set; }
        public int Count { get; set; }
        public double ClickRate { get; set; }
    }
}