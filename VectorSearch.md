# Vector Search Implementation Guide

## Overview
This document explains the vector search implementation in NewKnowledgeAPI, which enables semantic search capabilities for finding similar questions based on meaning rather than exact keyword matches.

## Current Implementation

### Components

1. **OpenAIEmbeddingService** (`Services/OpenAIEmbeddingService.cs`)
   - Generates vector embeddings using OpenAI's text-embedding-ada-002 model
   - Produces 1536-dimensional vectors
   - Handles API communication and error handling

2. **VectorSearchService** (`Services/VectorSearchService.cs`)
   - Stores embedded questions in Cosmos DB
   - Performs vector similarity search
   - Manages the "embedded-questions" container

3. **EmbeddedQuestion** (`Models/EmbeddedQuestion.cs`)
   - Model for questions with vector embeddings
   - Stores original question data plus embedding array

4. **QuestionSearchController** (`Controllers/QuestionSearchController.cs`)
   - REST API endpoint for semantic search
   - Returns ranked results based on similarity

### How It Works

1. **Embedding Generation**
   ```csharp
   // When a question is created/updated
   var embedding = await _embeddingService.GetEmbeddingAsync(question.Title);
   ```

2. **Storage**
   ```csharp
   var embeddedQuestion = new EmbeddedQuestion
   {
       Id = question.Id,
       Title = question.Title,
       CategoryId = question.CategoryId,
       Embedding = embedding
   };
   await _vectorSearchService.StoreEmbeddedQuestionAsync(embeddedQuestion);
   ```

3. **Search**
   ```csharp
   // When user searches
   var results = await _vectorSearchService.SearchSimilarQuestionsAsync(
       searchQuery, 
       topK: 5
   );
   ```

## Enhancing the Implementation

### 1. Automatic Embedding Generation

Create a background service to automatically generate embeddings:

```csharp
public class EmbeddingBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Find questions without embeddings
            var unembeddedQuestions = await _questionService
                .GetQuestionsWithoutEmbeddingsAsync();
            
            foreach (var question in unembeddedQuestions)
            {
                var embedding = await _embeddingService
                    .GetEmbeddingAsync(question.Title);
                    
                await _vectorSearchService
                    .StoreEmbeddedQuestionAsync(question, embedding);
            }
            
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

### 2. Hybrid Search (Keyword + Semantic)

Combine traditional keyword search with vector search:

```csharp
public async Task<IEnumerable<Question>> HybridSearchAsync(
    string query, 
    float semanticWeight = 0.7f)
{
    // Semantic search
    var semanticResults = await _vectorSearchService
        .SearchSimilarQuestionsAsync(query, topK: 20);
    
    // Keyword search
    var keywordResults = await _questionService
        .SearchByKeywordsAsync(query);
    
    // Combine and rank
    return CombineResults(semanticResults, keywordResults, semanticWeight);
}
```

### 3. Smart Question Suggestion

When users type questions, suggest similar existing questions:

```csharp
[HttpGet("api/questions/suggest")]
public async Task<IActionResult> SuggestQuestions(
    [FromQuery] string partial, 
    [FromQuery] int count = 5)
{
    if (string.IsNullOrWhiteSpace(partial) || partial.Length < 3)
        return Ok(new List<QuestionSuggestion>());
    
    var embedding = await _embeddingService.GetEmbeddingAsync(partial);
    var similar = await _vectorSearchService
        .SearchSimilarQuestionsAsync(partial, topK: count);
    
    return Ok(similar.Select(q => new QuestionSuggestion
    {
        Id = q.Id,
        Title = q.Title,
        Similarity = q.Similarity,
        Category = q.Category
    }));
}
```

### 4. Learning from User Behavior

Track which search results users click and improve rankings:

```csharp
public class SearchFeedbackService
{
    public async Task RecordClickAsync(
        string query, 
        string questionId, 
        int position)
    {
        var feedback = new SearchFeedback
        {
            Query = query,
            QuestionId = questionId,
            Position = position,
            Clicked = true,
            Timestamp = DateTime.UtcNow
        };
        
        await _dbService.StoreFeedbackAsync(feedback);
        
        // Update question relevance score
        await UpdateRelevanceScoreAsync(query, questionId);
    }
    
    private async Task UpdateRelevanceScoreAsync(
        string query, 
        string questionId)
    {
        // Increase relevance score for this query-question pair
        // This can be used to boost results in future searches
    }
}
```

### 5. Multi-Language Support

Extend to support multiple languages:

```csharp
public class MultilingualEmbeddingService
{
    public async Task<float[]> GetEmbeddingAsync(
        string text, 
        string language = "en")
    {
        // Detect language if not provided
        if (language == null)
            language = await DetectLanguageAsync(text);
        
        // Translate to English if needed (OpenAI embeddings work best with English)
        if (language != "en")
            text = await TranslateAsync(text, from: language, to: "en");
        
        return await _openAIService.GetEmbeddingAsync(text);
    }
}
```

### 6. Caching Strategy

Implement intelligent caching for embeddings:

```csharp
public class CachedEmbeddingService : IEmbeddingService
{
    private readonly IMemoryCache _cache;
    private readonly IEmbeddingService _innerService;
    
    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        var cacheKey = $"embedding:{ComputeHash(text)}";
        
        if (_cache.TryGetValue<float[]>(cacheKey, out var cached))
            return cached;
        
        var embedding = await _innerService.GetEmbeddingAsync(text);
        
        _cache.Set(cacheKey, embedding, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(24),
            Size = 1 // Manage cache size
        });
        
        return embedding;
    }
}
```

## Best Practices

### 1. Batch Processing
```csharp
// Process multiple texts in batches to reduce API calls
public async Task<Dictionary<string, float[]>> GetBatchEmbeddingsAsync(
    IEnumerable<string> texts)
{
    var batches = texts.Chunk(20); // OpenAI allows up to 20 texts per request
    var results = new Dictionary<string, float[]>();
    
    foreach (var batch in batches)
    {
        var embeddings = await _openAIService.GetBatchEmbeddingsAsync(batch);
        // Add to results...
    }
    
    return results;
}
```

### 2. Error Handling
```csharp
public async Task<float[]> GetEmbeddingWithRetryAsync(string text)
{
    var retryPolicy = Policy
        .Handle<HttpRequestException>()
        .OrResult<float[]>(r => r == null)
        .WaitAndRetryAsync(
            3,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                _logger.LogWarning($"Retry {retryCount} after {timespan}");
            });
    
    return await retryPolicy.ExecuteAsync(async () =>
        await _embeddingService.GetEmbeddingAsync(text)
    );
}
```

### 3. Monitoring
```csharp
public class EmbeddingMetrics
{
    private readonly IMetrics _metrics;
    
    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        using var timer = _metrics.Measure.Timer.Time("embedding.generation");
        
        try
        {
            var embedding = await _innerService.GetEmbeddingAsync(text);
            _metrics.Measure.Counter.Increment("embedding.success");
            return embedding;
        }
        catch (Exception ex)
        {
            _metrics.Measure.Counter.Increment("embedding.failure");
            throw;
        }
    }
}
```

## Testing Vector Search

### Unit Tests
```csharp
[Fact]
public async Task SearchSimilarQuestions_FindsSemanticMatches()
{
    // Arrange
    var testQuestions = new[]
    {
        "How do I reset my password?",
        "Where can I change my password?",
        "What is the capital of France?",
        "How to update my login credentials?"
    };
    
    // Act
    var results = await _searchService
        .SearchSimilarQuestionsAsync("forgot password", topK: 2);
    
    // Assert
    Assert.Equal(2, results.Count());
    Assert.Contains(results, r => r.Title.Contains("password"));
    Assert.DoesNotContain(results, r => r.Title.Contains("France"));
}
```

### Integration Tests
```csharp
[Fact]
public async Task EndToEnd_QuestionCreationAndSearch()
{
    // Create question
    var question = await _questionService.CreateAsync(new Question
    {
        Title = "How to reset forgotten password?",
        CategoryId = "auth"
    });
    
    // Wait for embedding generation
    await Task.Delay(1000);
    
    // Search for similar
    var results = await _searchController
        .SearchQuestionsAsync("password reset steps");
    
    // Verify
    Assert.Contains(results, r => r.Id == question.Id);
}
```

## Performance Optimization

### 1. Cosmos DB Indexing
```json
{
  "indexingPolicy": {
    "vectorIndexes": [
      {
        "path": "/embedding",
        "type": "flat"
      }
    ]
  }
}
```

### 2. Reduce Embedding Size
```csharp
// Use PCA or other dimensionality reduction
public float[] ReduceDimensions(float[] embedding, int targetDims = 512)
{
    // Implementation of PCA or similar
    return reduced;
}
```

### 3. Pre-compute Common Queries
```csharp
public class CommonQueryCache
{
    private readonly Dictionary<string, SearchResults> _cache = new()
    {
        ["password reset"] = precomputedResults1,
        ["login help"] = precomputedResults2,
        // etc.
    };
    
    public bool TryGetCached(string query, out SearchResults results)
    {
        return _cache.TryGetValue(query.ToLower(), out results);
    }
}
```

## Troubleshooting

### Common Issues

1. **Slow Search Performance**
   - Check Cosmos DB RU consumption
   - Verify vector indexing is enabled
   - Consider caching frequent queries

2. **Poor Search Quality**
   - Verify embeddings are generated correctly
   - Check if question titles are descriptive
   - Consider including question body in embeddings

3. **High OpenAI Costs**
   - Implement caching
   - Batch embedding requests
   - Only embed new/updated content

4. **Dimension Mismatch Errors**
   - Ensure all embeddings use same model
   - Validate embedding dimensions (1536)
   - Check for null/empty embeddings

## Future Enhancements

1. **Semantic Answer Matching**
   - Embed answers as well as questions
   - Match user queries to best answers directly

2. **Contextual Search**
   - Include user history in search
   - Personalize results based on role/department

3. **Query Expansion**
   - Use synonyms and related terms
   - Implement query understanding

4. **Feedback Loop**
   - Learn from user selections
   - Continuously improve rankings
   - A/B test different approaches

5. **Advanced Analytics**
   - Track search performance metrics
   - Identify content gaps
   - Suggest new questions to create