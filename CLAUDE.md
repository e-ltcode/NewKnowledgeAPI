# Claude Code Assistant Documentation

## Project Context
You are working on NewKnowledgeAPI, a Knowledge Management API that provides intelligent Q&A capabilities with semantic search using OpenAI embeddings and Azure Cosmos DB.

## Key Commands and Scripts

### Build and Test
```bash
# Build the project
dotnet build

# Run tests
dotnet test

# Run the application
dotnet run

# Run with hot reload
dotnet watch run
```

### Code Quality
```bash
# Format code
dotnet format

# Analyze code
dotnet build /p:AnalysisMode=AllEnabledByDefault
```

### Database Operations
```bash
# Initialize Cosmos DB collections
# This happens automatically on startup in Program.cs

# Seed initial data
# Place JSON files in InitialData/ folder
```

## Important Code Patterns

### Adding a New Entity
1. Create domain model in appropriate namespace (Q/ or A/)
2. Create DTO and DtoEx classes
3. Create service class with CRUD operations
4. Create controller with RESTful endpoints
5. Register service in Program.cs

### Implementing Vector Search for New Content
```csharp
// 1. Generate embedding
var embedding = await _embeddingService.GetEmbeddingAsync(content);

// 2. Store with content
var embeddedItem = new EmbeddedItem
{
    Id = Guid.NewGuid().ToString(),
    Content = content,
    Embedding = embedding,
    Kind = "embedded-item"
};

// 3. Search similar items
var results = await _vectorSearchService.SearchSimilarAsync(query, topK: 5);
```

### Working with Hierarchical Data
```csharp
// Categories and Groups follow parent-child pattern
category.ParentCategory = parentId;
category.Level = parentLevel + 1;
category.HasSubCategories = true;
```

## Common Tasks

### Add Vector Search to Existing Entity
1. Create embedded version of the model
2. Add embedding property (float[])
3. Generate embeddings on create/update
4. Implement similarity search endpoint
5. Add caching for performance

### Track User Interactions
```csharp
await _historyService.TrackInteractionAsync(new History
{
    UserId = userId,
    QuestionId = questionId,
    Action = HistoryAction.Clicked,
    Timestamp = DateTime.UtcNow
});
```

### Add Caching
```csharp
var cacheKey = $"question_{id}";
if (!_cache.TryGetValue(cacheKey, out Question question))
{
    question = await _dbService.GetQuestionAsync(id);
    _cache.Set(cacheKey, question, TimeSpan.FromMinutes(10));
}
```

## Environment Setup
Create `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "CosmosDb": "YOUR_COSMOS_CONNECTION_STRING"
  },
  "OpenAI": {
    "ApiKey": "YOUR_OPENAI_API_KEY",
    "EmbeddingModel": "text-embedding-ada-002"
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID"
  }
}
```

## Testing Vector Search
```csharp
// Test similar question search
var testQueries = new[] {
    "How do I reset my password?",
    "Where can I change my password?",
    "I forgot my password"
};

foreach (var query in testQueries)
{
    var results = await _searchController.SearchQuestionsAsync(query);
    // Results should be semantically similar
}
```

## Performance Optimization

### Embedding Generation
- Batch multiple texts in single API call
- Cache embeddings to avoid regeneration
- Use background jobs for bulk processing

### Vector Search
- Limit search scope with filters
- Use appropriate topK values (5-10)
- Index embedding fields in Cosmos DB

### Cosmos DB
- Use partition keys effectively
- Implement pagination for large results
- Use point reads when possible

## Debugging Tips

### Vector Search Issues
1. Check embedding dimensions match (1536 for ada-002)
2. Verify embeddings are normalized
3. Test with known similar content
4. Monitor OpenAI API usage

### Cosmos DB Issues
1. Check partition key usage
2. Verify connection string
3. Monitor RU consumption
4. Check indexing policy

### Authentication Issues
1. Verify JWT token format
2. Check Azure AD configuration
3. Validate audience and issuer
4. Test with Postman/Thunder Client

## Architecture Decisions

### Why Cosmos DB?
- Native vector search support
- Hierarchical data modeling
- Global distribution capability
- Integrated with Azure ecosystem

### Why OpenAI Embeddings?
- High quality semantic understanding
- Good performance for Q&A scenarios
- Easy integration via API
- Cost-effective for moderate usage

### Why Separate Q and A Namespaces?
- Clear domain boundaries
- Independent scaling
- Easier to understand and maintain
- Supports different access patterns

## Future Enhancements
1. Implement semantic caching
2. Add multi-language support
3. Implement feedback loop for search quality
4. Add analytics dashboard
5. Implement auto-categorization using AI