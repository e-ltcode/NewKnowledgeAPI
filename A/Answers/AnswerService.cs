using System.Net;
using Knowledge.Services;
using Microsoft.Azure.Cosmos;
using NewKnowledgeAPI.Common;
using Newtonsoft.Json;
using NewKnowledgeAPI.A.Answers.Model;
using System.Collections.Generic;
using System.Diagnostics;


namespace NewKnowledgeAPI.A.Answers
{
    public class AnswerService : IDisposable
    {
        public DbService? Db { get; set; } = null;

        private readonly string containerId = "Answers";
        private Container? _container = null;

        public async Task<Container> container()
        {
            _container ??= await Db!.GetContainer(containerId);
            return _container;
        }


        public string? PartitionKey { get; set; } = null;
        public AnswerService()
        {
        }

        public AnswerService(DbService Db)
        {
            this.Db = Db;
        }
                 
        public async Task<HttpStatusCode> CheckDuplicate(string? Title, string? Id = null)
        {

            var sqlQuery = Title != null
                ? $"SELECT * FROM c WHERE c.Type = 'answer' AND c.Title = '{Title.Replace("\'", "\\'")}' AND IS_NULL(c.Archived)"
                : $"SELECT * FROM c WHERE c.Type = 'answer' AND c.Id = '{Id}' AND IS_NULL(c.Archived)";
            QueryDefinition queryDefinition = new(sqlQuery);
            FeedIterator<Answer> queryResultSetIterator =
                _container!.GetItemQueryIterator<Answer>(queryDefinition);
            if (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<Answer> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                if (currentResultSet.Count == 0)
                {
                    throw new CosmosException("Answer Title already exists", HttpStatusCode.NotFound, 0, "0", 0);
                }
            }
            return HttpStatusCode.Found;
        }

        public async Task<AnswerEx?> AddAnswer(AnswerData answerData)
        {
            var myContainer = await container();
            //Console.WriteLine(JsonConvert.SerializeObject(answerData));
            string msg = string.Empty;
            try
            {
                var answer = new Answer(answerData);
                //Console.WriteLine("----->>>>> " + JsonConvert.SerializeObject(answer));
                // Read the item to see if it exists.  
                await CheckDuplicate(answerData.Title);
                msg = $":::::: Item in database with Title: {answerData.Title} already exists";
                Debug.WriteLine(msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                var q = new Answer(answerData);
                AnswerEx answerEx = await AddNewAnswer(q);
                return answerEx;
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this reans.
                msg = ex.Message;
                Debug.WriteLine(msg);
            }
            return new AnswerEx(null, msg);
        }


        public async Task<AnswerEx> AddNewAnswer(Answer answer)
        {
            var (PartitionKey, Id, Title, Link, ParentGroup, Type, Source, Status) = answer;

            var myContainer = await container();
            string msg = string.Empty;
            try
            {
                // Check if the id already exists
                ItemResponse<Answer> aResponse =
                    await myContainer!.ReadItemAsync<Answer>(
                        Id,
                        new PartitionKey(PartitionKey)
                    );
                msg = $"Answer in database with id: {Id} already exists\n";
                Debug.WriteLine(msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                try
                {
                    // Check if the title already exists
                    HttpStatusCode statusCode = await CheckDuplicate(Title);
                    msg = $"Answer in database with Title: {Title} already exists";
                    Debug.WriteLine(msg);
                }
                catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
                {
                    ItemResponse<Answer> aResponse =
                    await myContainer!.CreateItemAsync(
                            answer,
                            new PartitionKey(PartitionKey)
                        );
                    // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this reans.
                    //Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", aResponse.Resource.Id, aResponse.RequestCharge);
                    return new AnswerEx(aResponse.Resource, "");
                }
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this reans.
                Debug.WriteLine(ex.Message);
                msg = ex.Message;
            }
            return new AnswerEx(null, msg);
        }


        public async Task<AnswerEx> CreateAnswer(AnswerDto answerDto)
        {
            var myContainer = await container();
            try
            {
                Answer a = new(answerDto);
                AnswerEx answerEx = await AddNewAnswer(a);
                return answerEx;
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this reans.
                Debug.WriteLine(ex.Message);
                return new AnswerEx(null, ex.Message);
            }
        }

        public async Task<AnswerEx> GetAnswer(string PartitionKey, string Id)
        {
            var myContainer = await container();
            Answer? answer = null;
            string msg = string.Empty;
            try
            {
                Console.WriteLine($"*****************************  {PartitionKey}/{Id}");
                // Read the item to see if it exists.  
                answer = await myContainer.ReadItemAsync<Answer>(
                    Id,
                    new PartitionKey(PartitionKey)
                );
                return new AnswerEx(answer, msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                msg = "NotFound";
                Console.WriteLine(msg);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this reans.
                msg = ex.Message;
                Console.WriteLine(msg);
            }
            Console.WriteLine(JsonConvert.SerializeObject(answer));
            Console.WriteLine("*****************************");
            return new AnswerEx(null, msg);
        }

        public async Task<AnswerEx> UpdateAnswer(AnswerDto dto)
        {
            var myContainer = await container();
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Answer> aResponse =
                    await myContainer!.ReadItemAsync<Answer>(
                        dto.Id,
                        new PartitionKey(dto.PartitionKey)
                    );
                Answer answer = aResponse.Resource;
                var doUpdate = true;
                if (!answer.Title.Equals(dto.Title, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        HttpStatusCode statusCode = await CheckDuplicate(dto.Title);
                        doUpdate = false;
                        var msg = $"Answer with Title: \"{dto.Title}\" already exists in database.";
                        Debug.WriteLine(msg);
                        return new AnswerEx(null, msg);
                    }
                    catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                    {
                        //answer.Title = a.Title;
                    }
                }
                if (doUpdate)
                {
                    answer = new Answer(dto);
                    answer.Modified = new WhoWhen(dto.Modified);
                    aResponse = await myContainer.ReplaceItemAsync(answer, answer.Id, new PartitionKey(answer.PartitionKey));
                    Console.WriteLine($"Updated Answer \"{answer.Id}\" / \"{answer.Title}\"");
                    return new AnswerEx(aResponse.Resource, "");
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                var msg = $"Answer Id: \"{dto.Id}\" Not Found in database.";
                Debug.WriteLine(msg); 
                return new AnswerEx(null, msg);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this reans.
                Debug.WriteLine(ex.Message);
            }
            return new AnswerEx(null, "Server Problem");
        }

        public async Task<AnswerEx> DeleteAnswer(AnswerDto answerDto)
        {
            var myContainer = await container();
            var duplicateTitle = false;
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Answer> aResponse =
                    await myContainer!.ReadItemAsync<Answer>(
                        answerDto.Id,
                        new PartitionKey(answerDto.PartitionKey)
                    );
                Answer answer = aResponse.Resource;

                //duplicateTitle = true;
                //if (!answer.Title.Equals(answerDto.Title, StringComparison.OrdinalIgnoreCase))
                //{
                //    HttpStatusCode statusCode = await CheckDuplicate(answerDto.Title);
                //}
                // TODO check if is it already Archived
                answer.Archived = new WhoWhen(answerDto.Modified!.NickName);

                aResponse = await myContainer.ReplaceItemAsync(answer, answer.Id, new PartitionKey(answer.PartitionKey));
                Console.WriteLine("Updated Answer [{0},{1}].\n \tBody is now: {2}\n", answer.Title, answer.Id, answer);
                return new AnswerEx(aResponse.Resource, "");
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                var msg = $"Answer item {answerDto.Id} NotFound in database.";
                if (duplicateTitle)
                {
                    msg = $"Answer Title: {answerDto.Title} aleready exists in database.";
                }
                Debug.WriteLine(msg); //, aResponse.RequestCharge);
                return new AnswerEx(null, msg);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this reans.
                Debug.WriteLine(ex.Message);
            }
            return new AnswerEx(null, "Server Problem");
        }

        public async Task<AnswersMore> GetAnswers(string parentGroup, int startCursor, int pageSize, string includeAnswerId)
        {
            var myContainer = await container();
            try { 

                string sqlQuery = $"SELECT c.id, c.partitionKey, c.ParentGroup, c.Title, c.Link FROM c WHERE c.Type = 'answer' AND IS_NULL(c.Archived) AND " +
                    $" c.ParentGroup = '{parentGroup}' ORDER BY c.Title OFFSET {startCursor} ";
                sqlQuery += includeAnswerId == "null"
                    ? $"LIMIT {pageSize}"
                    : $"LIMIT 9999";

                Console.WriteLine("************ sqlQuery{0}", sqlQuery);

                int n = 0;
                bool included = false;

                List<AnswerRow> list = [];
                QueryDefinition queryDefinition = new QueryDefinition(sqlQuery);
                FeedIterator<AnswerRow> queryResultSetIterator = myContainer!.GetItemQueryIterator<AnswerRow>(queryDefinition);
                while (queryResultSetIterator.HasMoreResults)
                {
                    FeedResponse<AnswerRow> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                    foreach (AnswerRow answerRow in currentResultSet)
                    {
                        if (includeAnswerId != null && answerRow.Id == includeAnswerId)
                        {
                            included = true;
                        }
                        Console.WriteLine(">>>>>>>> answer is: {0}", JsonConvert.SerializeObject(answerRow));
                        list.Add(answerRow);
                        n++;
                        if (n >= pageSize && (includeAnswerId == null || included))
                        {
                            return new AnswersMore(list, true);
                        }
                    }
                    return new AnswersMore(list, false);
                }
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this reans.
                Debug.WriteLine(ex.Message);
            }
            return new AnswersMore([], false);
        }

        public async Task<List<AnswerRowDto>> SearchAnswerRows(List<string> words, int count)
        {
            var myContainer = await container();
            try
            {
                // order of fields matters
                var sqlQuery = $"SELECT c.partitionKey, c.id, c.ParentGroup, c.Title  FROM c WHERE c.Type = 'answer' AND IS_NULL(c.Archived) AND ";
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

                QueryDefinition queryDefinition = new QueryDefinition(sqlQuery);
                using (FeedIterator<AnswerRowDto> queryResultSetIterator = 
                    myContainer!.GetItemQueryIterator<AnswerRowDto>(queryDefinition))
                {
                    while (queryResultSetIterator.HasMoreResults)
                    {
                        FeedResponse<AnswerRowDto> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                        return currentResultSet.ToList();
                    }
                }
                return [];
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this reans.
                Debug.WriteLine(ex.Message);
            }
            return [];
        }


        public async Task<Dictionary<string, AnswerTitleLink>> GetTitlesAndLinks(List<string> answerIds)
        {
            var myContainer = await container();
            try
            {
                string str = string.Join("','", answerIds.ToArray());

                // OR c.ParentGroup = ''
                string sqlQuery = $"SELECT c.id, c.Title, c.Link FROM c " + 
                    $" WHERE c.Type = 'answer' AND IS_NULL(c.Archived) AND " +
                    $" ARRAY_CONTAINS(['{str}'], c.id, false)";
                //Console.WriteLine("************ sqlQuery{0}", sqlQuery);

                List<AnswerTitleLink> list = [];
                QueryDefinition queryDefinition = new QueryDefinition(sqlQuery);
                FeedIterator<AnswerTitleLink> queryResultSetIterator = myContainer!.GetItemQueryIterator<AnswerTitleLink>(queryDefinition);
                while (queryResultSetIterator.HasMoreResults)
                {
                    FeedResponse<AnswerTitleLink> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                    foreach (AnswerTitleLink answerTitleLink in currentResultSet)
                    {
                        //Console.WriteLine(">>>>>>>> answer is: {0}", JsonConvert.SerializeObject(answer));
                        list.Add(answerTitleLink);
                    }
                    return list.ToDictionary(x => x.Id!, x => new AnswerTitleLink(x));
                }
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this reans.
                Debug.WriteLine(ex.Message);
            }
            return answerIds.ToDictionary(id => id, id => new AnswerTitleLink("unk", "unk"));
        }


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
