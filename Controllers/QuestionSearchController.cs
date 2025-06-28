using Microsoft.AspNetCore.Mvc;
using NewKnowledgeAPI.Models;
using NewKnowledgeAPI.Services;

namespace NewKnowledgeAPI.Controllers
{
    /// <summary>
    /// API controller for adding questions, searching similar questions, and batch processing.
    /// Uses OpenAI embeddings and Cosmos DB vector search.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class QuestionSearchController : ControllerBase
    {
        private readonly IOpenAIEmbeddingService _embeddingService;
        private readonly IVectorSearchService _vectorSearchService;
        private readonly ILogger<QuestionSearchController> _logger;

        public QuestionSearchController(
            IOpenAIEmbeddingService embeddingService,
            IVectorSearchService vectorSearchService,
            ILogger<QuestionSearchController> logger)
        {
            _embeddingService = embeddingService;
            _vectorSearchService = vectorSearchService;
            _logger = logger;
        }

        /// <summary>
        /// Search for questions similar to the input using semantic similarity (vector search).
        /// </summary>
        /// <param name="request">The search request containing the question text and number of results.</param>
        /// <returns>List of similar questions and their answers.</returns>
        [HttpPost("search")]
        public async Task<ActionResult<List<EmbeddedQuestion>>> SearchQuestions([FromBody] SearchRequest request)
        {
            try
            {
                var embedding = await _embeddingService.GenerateEmbeddingAsync(request.Question);
                var results = await _vectorSearchService.FindSimilarQuestionsAsync(embedding, request.TopK);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching questions");
                return StatusCode(500, "An error occurred while searching questions");
            }
        }

        /// <summary>
        /// Add a new question. The embedding is generated and stored for future semantic search.
        /// </summary>
        /// <param name="question">The question to add.</param>
        /// <returns>The stored question with its embedding.</returns>
        [HttpPost]
        public async Task<ActionResult<EmbeddedQuestion>> AddQuestion([FromBody] EmbeddedQuestion question)
        {
            try
            {
                question.Embedding = await _embeddingService.GenerateEmbeddingAsync(question.QuestionText);
                question.PartitionKey = question.Id;
                await _vectorSearchService.StoreQuestionAsync(question);
                return Ok(question);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding question");
                return StatusCode(500, "An error occurred while adding the question");
            }
        }

        /// <summary>
        /// Batch add multiple questions. Embeddings are generated for each and stored.
        /// </summary>
        /// <param name="questions">List of questions to add.</param>
        [HttpPost("batch")]
        public async Task<ActionResult> BatchProcessQuestions([FromBody] List<EmbeddedQuestion> questions)
        {
            try
            {
                foreach (var question in questions)
                {
                    question.Embedding = await _embeddingService.GenerateEmbeddingAsync(question.QuestionText);
                    question.PartitionKey = question.Id;
                }
                await _vectorSearchService.BatchProcessQuestionsAsync(questions);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch processing questions");
                return StatusCode(500, "An error occurred while processing questions");
            }
        }
    }

    /// <summary>
    /// Request model for searching similar questions.
    /// </summary>
    public class SearchRequest
    {
        /// <summary>The question text to search for.</summary>
        public string Question { get; set; } = string.Empty;
        /// <summary>How many similar results to return (default: 3).</summary>
        public int TopK { get; set; } = 3;
    }
} 