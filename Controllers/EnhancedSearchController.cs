using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewKnowledgeAPI.Services;

namespace NewKnowledgeAPI.Controllers
{
    /// <summary>
    /// Enhanced search controller with hybrid search, suggestions, and analytics
    /// </summary>
    [ApiController]
    [Route("api/search")]
    public class EnhancedSearchController : ControllerBase
    {
        private readonly ISearchEnhancementService _searchService;
        private readonly ILogger<EnhancedSearchController> _logger;

        public EnhancedSearchController(
            ISearchEnhancementService searchService,
            ILogger<EnhancedSearchController> logger)
        {
            _searchService = searchService;
            _logger = logger;
        }

        /// <summary>
        /// Performs hybrid search combining semantic and keyword search
        /// </summary>
        /// <param name="request">Search request with query and optional parameters</param>
        /// <returns>Ranked list of search results</returns>
        [HttpPost("hybrid")]
        public async Task<ActionResult<List<SearchResult>>> HybridSearch([FromBody] HybridSearchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest("Query cannot be empty");
            }

            try
            {
                var results = await _searchService.HybridSearchAsync(
                    request.Query,
                    request.SemanticWeight ?? 0.7f,
                    request.TopK ?? 5);

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing hybrid search");
                return StatusCode(500, "An error occurred while searching");
            }
        }

        /// <summary>
        /// Provides real-time question suggestions as user types
        /// </summary>
        /// <param name="partial">Partial query string (minimum 3 characters)</param>
        /// <param name="count">Number of suggestions to return</param>
        /// <returns>List of question suggestions</returns>
        [HttpGet("suggestions")]
        public async Task<ActionResult<List<QuestionSuggestion>>> GetSuggestions(
            [FromQuery] string partial,
            [FromQuery] int count = 5)
        {
            if (string.IsNullOrWhiteSpace(partial))
            {
                return Ok(new List<QuestionSuggestion>());
            }

            try
            {
                var suggestions = await _searchService.GetQuestionSuggestionsAsync(partial, count);
                return Ok(suggestions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting suggestions");
                return Ok(new List<QuestionSuggestion>()); // Return empty list on error
            }
        }

        /// <summary>
        /// Records user interaction with search results for improving future searches
        /// </summary>
        /// <param name="feedback">Feedback data about user interaction</param>
        [HttpPost("feedback")]
        public async Task<ActionResult> RecordFeedback([FromBody] FeedbackRequest feedback)
        {
            if (string.IsNullOrWhiteSpace(feedback.Query) || string.IsNullOrWhiteSpace(feedback.QuestionId))
            {
                return BadRequest("Query and QuestionId are required");
            }

            try
            {
                await _searchService.RecordSearchFeedbackAsync(
                    feedback.Query,
                    feedback.QuestionId,
                    feedback.Position,
                    feedback.Clicked);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording feedback");
                return Ok(); // Don't fail on feedback errors
            }
        }

        /// <summary>
        /// Gets search analytics for the specified date range
        /// </summary>
        /// <param name="startDate">Start date for analytics period</param>
        /// <param name="endDate">End date for analytics period</param>
        /// <returns>Search analytics data</returns>
        [HttpGet("analytics")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<SearchAnalytics>> GetAnalytics(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            if (start > end)
            {
                return BadRequest("Start date must be before end date");
            }

            try
            {
                var analytics = await _searchService.GetSearchAnalyticsAsync(start, end);
                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting search analytics");
                return StatusCode(500, "An error occurred while retrieving analytics");
            }
        }

        /// <summary>
        /// Updates the embedding for a specific question
        /// </summary>
        /// <param name="questionId">ID of the question to update</param>
        [HttpPost("questions/{questionId}/update-embedding")]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<ActionResult> UpdateQuestionEmbedding(string questionId)
        {
            if (string.IsNullOrWhiteSpace(questionId))
            {
                return BadRequest("Question ID is required");
            }

            try
            {
                await _searchService.UpdateQuestionEmbeddingAsync(questionId);
                return Ok(new { message = "Embedding updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating question embedding");
                return StatusCode(500, "An error occurred while updating the embedding");
            }
        }
    }

    // Request/Response models
    public class HybridSearchRequest
    {
        public string Query { get; set; } = string.Empty;
        public float? SemanticWeight { get; set; } = 0.7f;
        public int? TopK { get; set; } = 5;
    }

    public class FeedbackRequest
    {
        public string Query { get; set; } = string.Empty;
        public string QuestionId { get; set; } = string.Empty;
        public int Position { get; set; }
        public bool Clicked { get; set; }
    }
}