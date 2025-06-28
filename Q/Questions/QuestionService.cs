using Knowledge.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Cosmos;
using NewKnowledgeAPI.A.Answers;
using NewKnowledgeAPI.A.Answers.Model;
using NewKnowledgeAPI.Common;
using NewKnowledgeAPI.Q.Categories;
using NewKnowledgeAPI.Q.Categories.Model;
using NewKnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Net;


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
                ? $"SELECT * FROM c WHERE c.Type = 'question' AND c.Title = '{Title.Trim().Replace("\'", "\\'")}' "
                : $"SELECT * FROM c WHERE c.Type = 'question' AND c.Id = '{Id}' ";
            QueryDefinition queryDefinition = new(sqlQuery);
            FeedIterator<Question> queryResultSetIterator =
                _container!.GetItemQueryIterator<Question>(queryDefinition);
            if (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<Question> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                if (currentResultSet.Count == 0)
                {
                    throw new CosmosException("Question with Title doesn't exist", HttpStatusCode.NotFound, 0, "0", 0);
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
                QuestionEx questionEx = await AddNewQuestion(myContainer, q);
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


        public async Task<QuestionEx> AddNewQuestion(Container? cntr, Question question)
        {
            var (partitionKey, id, title, parentCategory, type, source, status, assignedAnswers, relatedFilters) = question;
            var myContainer = cntr != null ? cntr : await container();
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
                var q = new Question(questionDto);
                QuestionEx questionEx = await AddNewQuestion(myContainer, q);
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

        public async Task<QuestionDtoEx> UpdateQuestion(QuestionDto questionDto, CategoryService categoryService)
        {
            var (partitionKey, id, oldParentCategory, newParentCategory, title, source, status, modified) = questionDto;

            Console.WriteLine(JsonConvert.SerializeObject(questionDto));
            Console.WriteLine("========================UpdateQuestion-3");

            var myContainer = await container();
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Question> aResponse =
                    await myContainer.ReadItemAsync<Question>(
                        id,
                        new PartitionKey(partitionKey!)
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
                        return new QuestionDtoEx(msg);
                    }
                    catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                    {
                        //question.Title = q.Title;
                    }
                }
                if (doUpdate)
                {
                    // Update the item fields
                    question.Title = title;
                    question.Source = source;
                    question.Status = status;
                    question.ParentCategory = newParentCategory;
                    question.PartitionKey = newParentCategory!;

                    if (modified != null) {
                        question.Modified = new WhoWhen(modified.NickName);
                    }

                    if (oldParentCategory != newParentCategory)
                    {
                        // parent category changed
                        String msg = await ArchiveQuestion(myContainer, partitionKey, id);
                        if (msg.Equals(String.Empty))
                        {
                            QuestionEx ex = await AddNewQuestion(myContainer, question);
                            if (ex.question != null)
                            {
                                Console.WriteLine("DODAO QUESTION  sa novom praentCategory");

                                await categoryService.UpdateNumOfQuestions(
                                    new CategoryKey(oldParentCategory!, oldParentCategory!),
                                    new WhoWhen(modified!), -1);
                                await categoryService.UpdateNumOfQuestions(
                                    new CategoryKey(newParentCategory!, newParentCategory!),
                                    new WhoWhen(modified!), 1);
                            }
                            else
                            {
                                Console.WriteLine("AddNewQuestion PROBLEMOS: " + ex.msg);
                            }
                        }
                    }
                    else
                    {
                        aResponse = await myContainer.ReplaceItemAsync(question, question.Id);
                        question = aResponse.Resource;
                    }
                    Console.WriteLine(JsonConvert.SerializeObject(question, Formatting.Indented));
                    var questionEx = new QuestionEx(question, string.Empty);
                    return new QuestionDtoEx(questionEx);
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                var msg = $"Question Id: \"{questionDto.Id}\" Not Found in database.";
                Console.WriteLine(msg); 
                return new QuestionDtoEx(msg);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
            }
            return new QuestionDtoEx("Server Problemos Update");
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


        public async Task<string> ArchiveQuestion(Container? cntr, string partitionKey, string id)
        {
            var myContainer = cntr != null ? cntr : await container();
            Question? question = null;
            var message = string.Empty;
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Question> aResponse = 
                    await myContainer.ReadItemAsync<Question>(
                        id,
                        new PartitionKey(partitionKey)
                    );
                question = aResponse.Resource;
                Console.WriteLine("NASAOOOOOOOOOOOOOOOOOOOOOO ARCHIVED");

                // now delete question
                await myContainer.DeleteItemAsync<Question>(
                        id,
                        new PartitionKey(question.PartitionKey)
                    );
                Console.WriteLine("OBRISAO");

                // Partition keys are crucial for how data is distributed across partitions.
                // PartitionKey is immutable.
                // 1) Item should be deleted
                // 2) Recreated with the new partitionKey
                question.PartitionKey = "questionArchived";
                question.Type += "_archived";

                aResponse = 
                    await myContainer.ReadItemAsync<Question>(
                        id, 
                        new PartitionKey(question.PartitionKey)
                    );

                await myContainer.ReplaceItemAsync(question, id);
                Console.WriteLine("ZAMENIO  ARCHIVED");
                Console.WriteLine("Question Archived [{0},{1}].", question.Title, question.Id);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                if (question != null)
                {
                    //question.Title += " ARCHIVED"; // otherwise updated Question can't be added
                    await AddNewQuestion(myContainer, question);
                    Console.WriteLine("DODAO ARCHIVED");
                }
                // message = $"Question item {id} NotFound in database.";
                //Console.WriteLine(message); //, aResponse.RequestCharge);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                message = ex.Message;   
                Console.WriteLine(ex.Message);
            }
            return message;
        }

        public async Task<QuestionsMore> GetQuestions(string parentCategory, int startCursor, int pageSize, string includeQuestionId)
        {
            var myContainer = await container();
            try
            {
                string sqlQuery = $"SELECT c.id, c.partitionKey, c.ParentCategory, c.Title FROM c " +
                    $" WHERE c.Type = 'question'  AND " +
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
                            questionRow.Included = true;
                        }
                        else
                        {
                            questionRow.Included = false;
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
                                "WHERE c.Type = 'question'  AND ";
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
