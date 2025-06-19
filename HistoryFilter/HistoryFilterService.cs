using System.Net;
using Knowledge.Services;
using Microsoft.Azure.Cosmos;
using NewKnowledgeAPI.Common;
using Newtonsoft.Json;
using NewKnowledgeAPI.Hist.Model;
using NewKnowledgeAPI.Q.Questions.Model;
using Swashbuckle.AspNetCore.SwaggerGen;
using NewKnowledgeAPI.Q.Questions;
using Microsoft.AspNetCore.Http.HttpResults;
using NewKnowledgeAPI.HistFilter.Model;

namespace NewKnowledgeAPI.HistFilter
{
    public class HistoryFilterService : IDisposable
    {
        public DbService? Db { get; set; } = null;

        private readonly string containerId = "Questions";
        private Container? _container = null;

        public async Task<Container> container()
        {
            _container ??= await Db!.GetContainer(containerId);
            return _container;
        }


        public string? PartitionKey { get; set; } = null;
        public HistoryFilterService()
        {
        }

        public HistoryFilterService(DbService Db)
        {
            this.Db = Db;
        }
                 
        public async Task<HttpStatusCode> CheckDuplicate(string? Title, string? Id = null)
        {

            var sqlQuery = Title != null
                ? $"SELECT * FROM c WHERE c.Type = 'history' AND c.Title = '{Title}' "
                : $"SELECT * FROM c WHERE c.Type = 'history' AND c.Id = '{Id}' ";
            QueryDefinition queryDefinition = new(sqlQuery);
            FeedIterator<History> queryResultSetIterator =
                _container!.GetItemQueryIterator<History>(queryDefinition);
            if (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<History> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                if (currentResultSet.Count == 0)
                {
                    throw new CosmosException("History Title already exists", HttpStatusCode.NotFound, 0, "0", 0);
                }
            }
            return HttpStatusCode.Found;
        }
          
        public async Task<HistoryFilterEx> AddNewHistory(HistoryFilter history)
        {
            var (partitionKey, id, questionKey, filter, created) = history;
            var myContainer = await container();
            string msg = string.Empty;
            try
            {
                // Check if the id already exists
                ItemResponse<HistoryFilter> aResponse =
                    await myContainer!.ReadItemAsync<HistoryFilter>(
                        id,
                        new PartitionKey(partitionKey)
                    );
                msg = $"History in database with id: {id} already exists\n";
                Console.WriteLine(msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                    ItemResponse<HistoryFilter> aResponse =
                    await myContainer!.CreateItemAsync(
                            history,
                            new PartitionKey(partitionKey)
                        );
                    // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                    Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", aResponse.Resource.Id, aResponse.RequestCharge);
                    return new HistoryFilterEx(aResponse.Resource, "");
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                msg = ex.Message;
            }
            return new HistoryFilterEx(null, msg);
        }


        public async Task<QuestionEx> CreateHistoryFilter(HistoryFilter historyFilter, QuestionService questionService)
        {
            var(_, _, questionKey, filter, created) = historyFilter;
            var myContainer = await container();
            try
            {
                HistoryFilterEx historyFilterEx = await AddNewHistory(historyFilter);
                var (history, msg) = historyFilterEx;
                if (history == null)
                    return new QuestionEx(null, msg);

                QuestionEx questionEx = await questionService.GetQuestion(questionKey);
                var (question, message) = questionEx;
                //Console.WriteLine(JsonConvert.SerializeObject(question));
                if (question != null)
                {
                    List<RelatedFilter> relatedFilters = question.RelatedFilters ?? [];
                    var relatedFilter = relatedFilters.FirstOrDefault(relatedFilter => relatedFilter.IsSimmilar(filter));
                    if (relatedFilter != null)
                    {
                        relatedFilter.NumOfUsages++;
                        relatedFilter.LastUsed = historyFilter.Created;
                        Console.WriteLine("RELATED SIMMILAR");
                    }
                    else
                    { 
                        if (relatedFilters.Count > 5)
                        {
                            relatedFilters.Sort(RelatedFilter.Comparer); // put the most rated RelatedFilters to the top
                            relatedFilters = relatedFilters.Take(5).ToList();
                        }
                        relatedFilters.Add(new RelatedFilter(historyFilter.Filter, created));
                        Console.WriteLine("RELATED NOVI");
                    }
                    question.Modified = created;
                    questionEx = await questionService.UpdateQuestionFilters(question, relatedFilters);
                }
                return questionEx;
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                return new QuestionEx(null, ex.Message);
            }
        }

        public class QAKey
        {
            public QAKey(AnswerRated answerRated) {
                key = answerRated.QuestionKey.Id + "/" + answerRated.AnswerKey.Id;
            }

            public string key { get; set; }
        }

        public async Task<List<AnswerRatedDto>> GetAnswersRated(Question question)
        {
            var questionKey = new QuestionKey(question);
            var myContainer = await container();

            //var list = new List<AnswerRated>();
            var dict = new Dictionary<QAKey, AnswerRatedDto>();
            foreach (AssignedAnswer assignedAnswer in question.AssignedAnswers)
            {
                var answerRated = new AnswerRated(questionKey, assignedAnswer);
                var key = new QAKey(answerRated);
                if (!dict.ContainsKey(key))
                {
                    dict[key] = new AnswerRatedDto(questionKey, assignedAnswer);
                }
                dict[key].Incr(answerRated);
                    //list.Add(new AnswerRated(questionKey, assignedAnswer));
            }
            return dict.Values.ToList();
            //History? history = null;
            //string msg = string.Empty;
            //try
            //{
            //    Console.WriteLine($"*****************************  {PartitionKey}/{Id}");
            //    // Read the item to see if it exists.  
            //    history = await myContainer.ReadItemAsync<History>(
            //        Id,
            //        new PartitionKey(PartitionKey)
            //    );
            //    return new HistoryEx(history, msg);
            //}
            //catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            //{
            //    msg = "NotFound";
            //    Console.WriteLine(msg);
            //}
            //catch (Exception ex)
            //{
            //    // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
            //    msg = ex.Message;
            //    Console.WriteLine(msg);
            //}
            //Console.WriteLine(JsonConvert.SerializeObject(history));
            //Console.WriteLine("*****************************");
            //return new HistoryEx(null, msg);
        }

        public async Task<HistoryEx> GetHistory(string PartitionKey, string Id)
        {
            var myContainer = await container();
            History? history = null;
            string msg = string.Empty;
            try
            {
                Console.WriteLine($"*****************************  {PartitionKey}/{Id}");
                // Read the item to see if it exists.  
                history = await myContainer.ReadItemAsync<History>(
                    Id,
                    new PartitionKey(PartitionKey)
                );
                return new HistoryEx(history, msg);
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
            Console.WriteLine(JsonConvert.SerializeObject(history));
            Console.WriteLine("*****************************");
            return new HistoryEx(null, msg);
        }

        public async Task<HistoryListEx> GetHistories(string QuestionId)
        {
            List<History> histories = [];
            string msg = string.Empty;
            var myContainer = await container();
            try
            {
                // OR c.ParentCategory = ''
                string sqlQuery = $"SELECT * FROM c WHERE c.partitionKey = 'history' AND c.Type = 'history' AND " +
                    $" c.QuestionId = '{QuestionId}' OFFSET 0 LIMIT 999 "; // TODO
                //sqlQuery += includeHistoryId == "null"
                //    ? $"LIMIT {pageSize}"
                //    : $"LIMIT 9999";

                Console.WriteLine("************ sqlQuery{0}", sqlQuery);

                QueryDefinition queryDefinition = new QueryDefinition(sqlQuery);
                FeedIterator<History> queryResultSetIterator = myContainer!.GetItemQueryIterator<History>(queryDefinition);
                while (queryResultSetIterator.HasMoreResults)
                {
                    FeedResponse<History> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                    foreach (History history in currentResultSet)
                    {
                        Console.WriteLine(">>>>>>>> history is: {0}", JsonConvert.SerializeObject(history));
                        histories.Add(history);
                    }
                }
                return new HistoryListEx(histories, msg);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                msg = ex.Message;
            }
            return new HistoryListEx(histories, "");
        }
        /*

        public async Task<List<QuestDto>> GetQuests(List<string> words, int count)
        {
            var myContainer = await container();
            try
            {
                var sqlQuery = $"SELECT c.ParentCategory, c.Title, c.id FROM c WHERE c.Type = 'history'  AND ";
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


                List<QuestDto> quests = [];
                QueryDefinition queryDefinition = new QueryDefinition(sqlQuery);
                using (FeedIterator<History> queryResultSetIterator = 
                    myContainer!.GetItemQueryIterator<History>(queryDefinition))
                {
                    while (queryResultSetIterator.HasMoreResults)
                    {
                        FeedResponse<History> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                        foreach (History history in currentResultSet)
                        {
                            quests.Add(new QuestDto(history));
                        }
                    }
                }
                return quests;
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
            }
            return [];
        }
        */
           

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
