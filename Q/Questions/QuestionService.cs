using System.Net;
using Knowledge.Services;
using Microsoft.Azure.Cosmos;
using NewKnowledgeAPI.A.Answers;
using NewKnowledgeAPI.Common;
using NewKnowledgeAPI.Q.Categories.Model;
using NewKnowledgeAPI.Q.Categories;
using NewKnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using NewKnowledgeAPI.A.Answers.Model;
using System.Diagnostics;


namespace NewKnowledgeAPI.Q.Questions
{
    public class QuestionService : IDisposable
    {
        public DbService? Db { get; set; } = null;

        private readonly string containerId = "Questions";
        private Container? _container = null;

        public async Task<Container> container()
        {
            _container ??= await Db!.GetContainer(containerId);
            return _container;
        }


        //public string? PartitionKey { get; set; } = null;
        public QuestionService()
        {
        }

        public QuestionService(DbService Db)
        {
            this.Db = Db;
        }
                 
        public async Task<HttpStatusCode> CheckDuplicate(string? Title, string? Id = null)
        {
            var sqlQuery = Title != null
                ? $"SELECT * FROM c WHERE c.Type = 'question' AND c.Title = '{Title.Replace("\'", "\\'")}' AND IS_NULL(c.Archived)"
                : $"SELECT * FROM c WHERE c.Type = 'question' AND c.Id = '{Id}' AND IS_NULL(c.Archived)";
            QueryDefinition queryDefinition = new(sqlQuery);
            FeedIterator<Question> queryResultSetIterator =
                _container!.GetItemQueryIterator<Question>(queryDefinition);
            if (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<Question> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                if (currentResultSet.Count == 0)
                {
                    throw new CosmosException("Question Title already exists", HttpStatusCode.NotFound, 0, "0", 0);
                }
            }
            return HttpStatusCode.Found;
        }

        public async Task<QuestionEx?> AddQuestion(QuestionData questionData)
        {
            var myContainer = await container();
            //Console.WriteLine(JsonConvert.SerializeObject(questionData));
            string msg = string.Empty;
            try
            {
                var question = new Question(questionData);
                //Console.WriteLine("----->>>>> " + JsonConvert.SerializeObject(question));
                // Read the item to see if it exists.  
                await CheckDuplicate(questionData.Title);
                msg = $":::::: Item in database with Title: {questionData.Title} already exists";
                Console.WriteLine(msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                var q = new Question(questionData);
                QuestionEx questionEx = await AddNewQuestion(q);
                return questionEx;
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                msg = ex.Message;
                Console.WriteLine(msg);
            }
            return new QuestionEx(null, msg);
        }


        public async Task<QuestionEx> AddNewQuestion(Question question)
        {
            var (partitionKey, id, title, parentCategory, type, source, status, assignedAnswers, relatedFilters) = question;

            var myContainer = await container();
            string msg = string.Empty;
            try
            {
                // Check if the id already exists
                ItemResponse<Question> aResponse =
                    await myContainer!.ReadItemAsync<Question>(
                        id,
                        new PartitionKey(partitionKey)
                    );
                msg = $"Question in database with id: {id} already exists\n";
                Console.WriteLine(msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                try
                {
                    // Check if the title already exists
                    HttpStatusCode statusCode = await CheckDuplicate(title);
                    msg = $"Question in database with Title: {title} already exists";
                    Console.WriteLine(msg);
                }
                catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
                {
                    ItemResponse<Question> aResponse =
                    await myContainer!.CreateItemAsync(
                            question,
                            new PartitionKey(partitionKey)
                        );
                    // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                    // Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", aResponse.Resource.Id, aResponse.RequestCharge);
                    return new QuestionEx(aResponse.Resource, "");
                }
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                msg = ex.Message;
            }
            return new QuestionEx(null, msg);
        }


        public async Task<QuestionEx> CreateQuestion(QuestionDto questionDto)
        {
            var myContainer = await container();
            try
            {
                Question q = new(questionDto);
                QuestionEx questionEx = await AddNewQuestion(q);
                return questionEx;
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                return new QuestionEx(null, ex.Message);
            }
        }

        public async Task<QuestionEx> GetQuestion(QuestionKey questionKey)
        {
            var myContainer = await container();
            Question? question = null;
            string msg = string.Empty;
            try
            {
                var (PartitionKey, Id) = questionKey;
                Console.WriteLine($"*****************************  {PartitionKey}/{Id}");
                // Read the item to see if it exists.  
                question = await myContainer.ReadItemAsync<Question>(
                    Id,
                    new PartitionKey(PartitionKey)
                );
                return new QuestionEx(question, msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                msg = "NotFound";
                Console.WriteLine(msg);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                msg = ex.Message;
                Console.WriteLine(msg);
            }
            //Console.WriteLine(JsonConvert.SerializeObject(question));
            Console.WriteLine("*****************************");
            return new QuestionEx(null, msg);
        }

        public async Task<QuestionEx> UpdateQuestion(QuestionDto dto, CategoryService categoryService)
        {
            var (partitionKey, id, parentCategory, title, source, status, modified) = dto;
            Console.WriteLine("========================UpdateQuestion-1");
            Console.WriteLine(JsonConvert.SerializeObject(dto));
            // Console.WriteLine("========================UpdateQuestion-2");
            // Console.WriteLine(JsonConvert.SerializeObject(assignedAnswers));
            Console.WriteLine("========================UpdateQuestion-3");
            var myContainer = await container();
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Question> aResponse =
                    await myContainer!.ReadItemAsync<Question>(
                        id,
                        new PartitionKey(partitionKey)
                    );
                Question question = aResponse.Resource;

                var doUpdate = true;
                if (!question.Title.Equals(title, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        HttpStatusCode statusCode = await CheckDuplicate(title);
                        doUpdate = false;
                        var msg = $"Question with Title: \"{title}\" already exists in database.";
                        Console.WriteLine(msg);
                        return new QuestionEx(null, msg);
                    }
                    catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                    {
                        //question.Title = q.Title;
                    }
                }
                if (doUpdate)
                {
                    if (question.ParentCategory != parentCategory)
                    {
                        // changed Category
                    }
                    // Update the item fields
                    question.Title = title;
                    question.Source = source;
                    question.Status = status;
                    question.ParentCategory = parentCategory;   
                    if (modified != null) {
                        question.Modified = new WhoWhen(modified.NickName);
                    }
                    aResponse = await myContainer.ReplaceItemAsync(question, question.Id, new PartitionKey(question.PartitionKey));
                    return new QuestionEx(aResponse.Resource, "");
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                var msg = $"Question Id: \"{dto.Id}\" Not Found in database.";
                Console.WriteLine(msg); 
                return new QuestionEx(null, msg);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
            }
            return new QuestionEx(null, "Server Problem Update");
        }


        public async Task<QuestionEx> UpdateQuestion(Question q, List<AssignedAnswer> assignedAnswers)
        {
            var myContainer = await container();
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Question> aResponse =
                    await myContainer!.ReadItemAsync<Question>(
                        q.Id,
                        new PartitionKey(q.PartitionKey)
                    );
                Question question = aResponse.Resource;
                question.AssignedAnswers = assignedAnswers;
                question.NumOfAssignedAnswers = assignedAnswers.Count;
                question.Modified = q.Modified;
                aResponse = await myContainer.ReplaceItemAsync(question, question.Id, new PartitionKey(question.PartitionKey));
                return new QuestionEx(aResponse.Resource, "");
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                var msg = $"Question Id: \"{q.Id}\" Not Found in database.";
                Console.WriteLine(msg);
                return new QuestionEx(null, msg);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
            }
            return new QuestionEx(null, "Server Problem Update");
        }


        public async Task<QuestionEx> UpdateQuestionFilters(Question q, List<RelatedFilter> relatedFilters)
        {
            var (PartitionKey, Id, Title, ParentCategory, Type, Source, Status, _, _ /*relatedFilters*/) = q;
            var myContainer = await container();
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Question> aResponse =
                    await myContainer!.ReadItemAsync<Question>(
                        Id,
                        new PartitionKey(PartitionKey)
                    );
                Question question = aResponse.Resource;
                question.RelatedFilters = relatedFilters;
                question.NumOfRelatedFilters = relatedFilters.Count;
                question.Modified = q.Modified!;
                aResponse = await myContainer.ReplaceItemAsync(question, Id, new PartitionKey(PartitionKey));
                return new QuestionEx(aResponse.Resource, "");
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                var msg = $"Question Id: \"{Id}\" Not Found in database.";
                Console.WriteLine(msg);
                return new QuestionEx(null, msg);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
            }
            return new QuestionEx(null, "Server Problem Update");
        }


        public async Task<QuestionEx> DeleteQuestion(QuestionDto questionDto)
        {
            var myContainer = await container();
            var duplicateTitle = false;
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Question> aResponse =
                    await myContainer!.ReadItemAsync<Question>(
                        questionDto.Id,
                        new PartitionKey(questionDto.PartitionKey)
                    );
                Question question = aResponse.Resource;

                //duplicateTitle = true;
                //if (!question.Title.Equals(questionDto.Title, StringComparison.OrdinalIgnoreCase))
                //{
                //    HttpStatusCode statusCode = await CheckDuplicate(questionDto.Title);
                //}
                // TODO check if is it already Archived
                question.Archived = new WhoWhen(questionDto.Modified!.NickName);

                aResponse = await myContainer.ReplaceItemAsync(question, question.Id, new PartitionKey(question.PartitionKey));
                Console.WriteLine("Updated Question [{0},{1}].\n \tBody is now: {2}\n", question.Title, question.Id, question);
                return new QuestionEx(aResponse.Resource, "");
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                var msg = $"Question item {questionDto.Id} NotFound in database.";
                if (duplicateTitle)
                {
                    msg = $"Question Title: {questionDto.Title} aleready exists in database.";
                }
                Console.WriteLine(msg); //, aResponse.RequestCharge);
                return new QuestionEx(null, msg);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
            }
            return new QuestionEx(null, "Server Problem Delete");
        }

        public async Task<QuestionsMore> GetQuestions(string parentCategory, int startCursor, int pageSize, string includeQuestionId)
        {
            var myContainer = await container();
            try
            {
                string sqlQuery = $"SELECT c.id, c.partitionKey, c.ParentCategory, c.Title FROM c " +
                    $" WHERE c.Type = 'question' AND IS_NULL(c.Archived) AND " +
                    $" c.ParentCategory = '{parentCategory}' ORDER BY c.Title OFFSET {startCursor} ";
                sqlQuery += includeQuestionId == "null"
                    ? $"LIMIT {pageSize}"
                    : $"LIMIT 9999";

                //Console.WriteLine("************ sqlQuery{0}", sqlQuery);

                int n = 0;
                bool included = false;

                List<QuestionRow> list = [];
                QueryDefinition queryDefinition = new QueryDefinition(sqlQuery);
                FeedIterator<QuestionRow> queryResultSetIterator = myContainer!.GetItemQueryIterator<QuestionRow>(queryDefinition);
                while (queryResultSetIterator.HasMoreResults)
                {
                    FeedResponse<QuestionRow> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                    foreach (QuestionRow questionRow in currentResultSet)
                    {
                        if (includeQuestionId != null && questionRow.Id == includeQuestionId)
                        {
                            included = true;
                        }
                        //Console.WriteLine(">>>>>>>> question is: {0}", JsonConvert.SerializeObject(question));
                        list.Add(questionRow);
                        n++;
                        if (n >= pageSize && (includeQuestionId == null || included))
                        {
                            return new QuestionsMore(list, true);
                        }
                    }
                    return new QuestionsMore(list, false);
                }
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
            }
            return new QuestionsMore([], false);
        }

        public async Task<List<QuestionRowDto>> SearchQuestionRows(List<string> words, int count)
        {
            var myContainer = await container();
            try
            {
                // order of fields matters
                var sqlQuery = $"SELECT c.partitionKey, c.id, c.Title, c.ParentCategory FROM c " +
                                "WHERE c.Type = 'question' AND IS_NULL(c.Archived) AND ";
                if (words.Count == 1)
                {
                    sqlQuery += $" CONTAINS(c.Title, \"{words[0]}\", true) ";
                }
                else
                {
                    sqlQuery += "(";
                    for (var i=0; i < words.Count; i++)
                    {
                        if (i > 0)
                            sqlQuery += " OR ";
                        sqlQuery += $" CONTAINS(c.Title, \"{words[i]}\", true) ";
                    }
                    sqlQuery += ")";

                }
                sqlQuery += $" ORDER BY c.Title OFFSET 0 LIMIT {count}";
                Console.WriteLine(sqlQuery);   

                QueryDefinition queryDefinition = new QueryDefinition(sqlQuery);    
                using (FeedIterator<QuestionRowDto> queryResultSetIterator   = 
                    myContainer!.GetItemQueryIterator<QuestionRowDto>(queryDefinition))
                {
                    while (queryResultSetIterator.HasMoreResults)
                    {
                        FeedResponse<QuestionRowDto> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                        return currentResultSet.ToList();
                       
                    }
                }
                return [];
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
            }
            return [];
        }


        // --------------------------------------------------------
        //                  Assigned Answers
        // --------------------------------------------------------

        public async Task<QuestionEx> AssignAnswer(AssignedAnswerDto assignedAnswerDto)
        {
            var (questionKey, answerKey, answerTitle, answerLink, created, modified) = assignedAnswerDto;
            QuestionEx questionEx = await GetQuestion(questionKey!);
            var (question, msg) = questionEx;
            if (question != null)
            {
                var assignedAnswers = question.AssignedAnswers ?? new List<AssignedAnswer>();
                assignedAnswers.Add(new AssignedAnswer(assignedAnswerDto));
                question.Modified = new WhoWhen(created);
                questionEx = await UpdateQuestion(question, assignedAnswers);
            }
            return questionEx;
        }

        public async Task<QuestionEx> UnAssignAnswer(AssignedAnswerDto assignedAnswerDto)
        {
            var (questionKey, answerKey, answerTitle, answerLink, created, modified) = assignedAnswerDto;

            QuestionEx questionEx = await GetQuestion(questionKey!);
            var (question, msg) = questionEx;
            if (question != null)
            {
                var assignedAnswers = question.AssignedAnswers.FindAll(a => a.AnswerKey.Id != answerKey.Id);
                question.Modified = new WhoWhen(created);
                questionEx = await UpdateQuestion(question, assignedAnswers);
            }
            return questionEx;
        }


        public async Task<Question> SetAnswerTitles(Question question, 
            CategoryService categoryService, AnswerService answerService)
        {
            var (PartitionKey, Id, Title, ParentCategory, Type, Source, Status, AssignedAnswers, relatedFilters) = question;
            CategoryKey categoryKey = new(PartitionKey, question.ParentCategory!);
            // get category Title
            CategoryEx categoryEx = await categoryService.GetCategory(categoryKey);
            var (category, message) = categoryEx;
            question.CategoryTitle = category != null ? category.Title : "NotFound Category";
            await SetAnswerTitles(question, answerService);
            return question;
        }

        public async Task<Question> SetAnswerTitles(Question question, AnswerService answerService)
        {
            var (PartitionKey, Id, _, _, _, Source, Status, assignedAnswers, _) = question;
            if (assignedAnswers != null && assignedAnswers.Count > 0)
            {
                var answerIds = assignedAnswers.Select(a => a.AnswerKey.Id).Distinct().ToList();
                Dictionary<string, AnswerTitleLink> dict = await answerService.GetTitlesAndLinks(answerIds);
                Console.WriteLine(JsonConvert.SerializeObject(dict));
                foreach (var assignedAnswer in assignedAnswers)
                {
                    AnswerTitleLink titleLink = dict[assignedAnswer.AnswerKey.Id];
                    assignedAnswer.AnswerTitle = titleLink.Title;
                    assignedAnswer.AnswerLink = titleLink.Link;
                }
            }
            return question;
        }

        // --------------------------------------------------------
        //                  Filters
        // --------------------------------------------------------

        public async Task<QuestionEx> AssignFilter(RelatedFilterDto dto)
        {
            var (questionKey, filter, created, modified, numOfUsage) = dto;
            QuestionEx questionEx = await GetQuestion(questionKey!);
            var (question, msg) = questionEx;
            if (question != null)
            {
                var relatedFilters = question.RelatedFilters ?? new List<RelatedFilter>();
                relatedFilters.Add(new RelatedFilter(dto));
                question.Modified = new WhoWhen(created);
                questionEx = await UpdateQuestionFilters(question, relatedFilters);
            }
            return questionEx;
        }

        //public async Task<QuestionEx> UnAssignFilter(RelatedFilterDto dto)
        //{
        //    var (questionKey, filter, created, modified) = dto;

        //    QuestionEx questionEx = await GetQuestion(questionKey!);
        //    var (question, msg) = questionEx;
        //    if (question != null)
        //    {
        //        var relatedFilters = question.RelatedFilters.FindAll(a => a.FilterKey.Id != filterKey.Id);
        //        question.Modified = new WhoWhen(created);
        //        questionEx = await UpdateQuestionFilters(question, relatedFilters);
        //    }
        //    return questionEx;
        //}


        //public async Task<Question> SetFilterTitles(Question question,
        //    CategoryService categoryService, FilterService filterService)
        //{
        //    var (PartitionKey, Id, Title, ParentCategory, Type, Source, Status, RelatedFilters) = question;
        //    CategoryKey categoryKey = new(PartitionKey, question.ParentCategory!);
        //    // get category Title
        //    CategoryEx categoryEx = await categoryService.GetCategory(categoryKey);
        //    var (category, message) = categoryEx;
        //    question.CategoryTitle = category != null ? category.Title : "NotFound Category";
        //    //if (RelatedFilters.Count > 0)
        //    //{
        //    //    var filterIds = RelatedFilters.Select(a => a.FilterKey.Id).Distinct().ToList();
        //    //    Dictionary<string, FilterTitleLink> dict = await filterService.GetTitlesAndLinks(filterIds);
        //    //    Console.WriteLine(JsonConvert.SerializeObject(dict));
        //    //    foreach (var relatedFilters in RelatedFilters)
        //    //    {
        //    //        FilterTitleLink titleLink = dict[relatedFilters.FilterKey.Id];
        //    //        relatedFilters.FilterTitle = titleLink.Title;
        //    //        relatedFilters.FilterLink = titleLink.Link;
        //    //    }
        //    //}
        //    await SetFilterTitles(question, filterService);
        //    return question;
        //}

        //public async Task<Question> SetFilterTitles(Question question, FilterService filterService)
        //{
        //    var (PartitionKey, Id, Title, ParentCategory, Type, Source, Status, RelatedFilters) = question;
        //    if (RelatedFilters.Count > 0)
        //    {
        //        //var filterIds = RelatedFilters.Select(a => a.FilterKey.Id).Distinct().ToList();
        //        //Dictionary<string, string> filterTitles = await filterService.GetTitlesAndLinks(filterIds);
        //        //Console.WriteLine(JsonConvert.SerializeObject(filterTitles));
        //        //foreach (var relatedFilters in RelatedFilters)
        //        //    relatedFilters.FilterTitle = filterTitles[relatedFilters.FilterKey.Id];
        //        var filterIds = RelatedFilters.Select(a => a.FilterKey.Id).Distinct().ToList();
        //        Dictionary<string, FilterTitleLink> dict = await filterService.GetTitlesAndLinks(filterIds);
        //        Console.WriteLine(JsonConvert.SerializeObject(dict));
        //        foreach (var relatedFilters in RelatedFilters)
        //        {
        //            FilterTitleLink titleLink = dict[relatedFilters.FilterKey.Id];
        //            relatedFilters.FilterTitle = titleLink.Title;
        //            relatedFilters.FilterLink = titleLink.Link;
        //        }
        //    }

        //    return question;
        //}

        public void Dispose()
        {
            _container = null;
            Db = null;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }
    }
}
