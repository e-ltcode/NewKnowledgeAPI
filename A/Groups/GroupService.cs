using Azure;
using Knowledge.Services;
using Microsoft.Azure.Cosmos;
using NewKnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Net;
using NewKnowledgeAPI.A.Groups.Model;
using NewKnowledgeAPI.A.Answers.Model;
using NewKnowledgeAPI.A.Answers;
using NewKnowledgeAPI.Q.Questions.Model;
using System.Diagnostics;

namespace NewKnowledgeAPI.A.Groups
{
    public class GroupService : IDisposable
    {
        public DbService? Db { get; set; } = null;

        private readonly string containerId = "Answers";
        private Container? _container = null;

        public async Task<Container> container()
        {
            _container ??= await Db!.GetContainer(containerId);
            return _container;
        }

        public GroupService()
        {
        }

        //public Group(IConfiguration configuration)
        //{
        //    Group.Db = new Db(configuration);
        //}

        public GroupService(DbService db)
        {
            Db = db;
        }

        internal async Task<List<Group>> GetAllGroups()
        {
            var myContainer = await container();
            var sqlQuery = "SELECT * FROM c WHERE c.Type = 'group'  ORDER BY c.Title ASC";
            QueryDefinition queryDefinition = new(sqlQuery);
            FeedIterator<Group> queryResultSetIterator = myContainer.GetItemQueryIterator<Group>(queryDefinition);
            //List<GroupDto> subGroups = new List<GroupDto>();
            List<Group> subGroups = [];
            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<Group> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (Group group in currentResultSet)
                {
                    subGroups.Add(group);
                }
            }
            //Console.WriteLine(JsonConvert.SerializeObject(subGroups));
            return subGroups;
        }


        internal async Task<List<Group>> GetSubGroups(string PartitionKey, string id)
        {
            var myContainer = await container();
            var sqlQuery = $"SELECT * FROM c WHERE c.Type = 'group'  AND "
            // for groups partitionKey is same as Id
            //+ (
            //    PartitionKey == "null"
            //        ? $""
            //        : $" c.partitionKey = '{PartitionKey}' AND "  
            //)
            + (
                id == "null"
                    ? $" IS_NULL(c.ParentGroup)"
                    : $" c.ParentGroup = '{id}'"
            );
            QueryDefinition queryDefinition = new(sqlQuery);
            FeedIterator<Group> queryResultSetIterator = myContainer!.GetItemQueryIterator<Group>(queryDefinition);
            //List<GroupDto> subGroups = new List<GroupDto>();
            List<Group> subGroups = [];
            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<Group> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (Group group in currentResultSet)
                {
                    //subGroups.Add(new GroupDto(group));
                    subGroups.Add(group);
                }
            }
            return subGroups;
        }

        public async Task<GroupEx> GetGroup(GroupKey groupKey, bool hidrate, int pageSize, string? includeAnswerId)
        {
            var (PartitionKey, Id) = groupKey;
            var myContainer = await container();
            try
            {
                // Read the item to see if it exists.  
                //ItemResponse<Group> aResponse =
                Group group = await myContainer!.ReadItemAsync<Group>(Id, new PartitionKey(PartitionKey));
                Console.WriteLine(JsonConvert.SerializeObject(group));

                if (hidrate && group != null)
                {
                    // hidrate collections except answers, like  group.x = hidrate;  
                    if (pageSize > 0 && group.NumOfAnswers > 0)
                    {
                        var answerService = new AnswerService(Db);
                        AnswersMore answersMore = await answerService.GetAnswers(Id, 0, pageSize, includeAnswerId);

                        group.Answers = answersMore.AnswerRows.Select(answerRow => new Answer(answerRow)).ToList();
                        group.HasMoreAnswers = answersMore.hasMoreAnswers;
                    }
                }
                return new GroupEx(group, "");
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this reans.
                Console.WriteLine(ex.Message);
                return new GroupEx(null, ex.Message);
            }
        }

        public async Task<HttpStatusCode> CheckDuplicate(string title, string id) //AnswerData answerData)
        {
            var sqlQuery = $"SELECT * FROM c WHERE c.Type = 'group' AND (c.Title = '{title.Replace("\'", "\\'")}' OR c.id = '{id}')";
            QueryDefinition queryDefinition = new(sqlQuery);
            FeedIterator<Answer> queryResultSetIterator =
                _container!.GetItemQueryIterator<Answer>(queryDefinition);
            if (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<Answer> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                if (currentResultSet.Count == 0)
                {
                    throw new CosmosException("Group Title already exists", HttpStatusCode.NotFound, 0, "0", 0);
                }
            }
            return HttpStatusCode.OK;
        }

        public async Task AddGroup(GroupData groupData)
        {
            var (PartitionKey, Id, Title, ParentGroup, Kind, Level, Variations, Groups, Answers) = groupData;
            //Console.WriteLine(JsonConvert.SerializeObject(groupData));
            var myContainer = await container();

            if (Answers != null && Id == "DOMAIN")
            {
                for (var i = 1; i <= 500; i++)
                    Answers!.Add(new AnswerData(Id, $"Test row for DOMAIN " + i.ToString("D3")));
            }

            try
            {
                Group c = new(groupData);
                GroupEx groupEx = await AddNewGroup(c);
                if (groupEx.group != null)
                {
                    Group group = groupEx.group;
                    if (Groups != null)
                    {
                        foreach (var subGroupData in Groups)
                        {
                            subGroupData.PartitionKey = subGroupData.Id;
                            subGroupData.ParentGroup = group.Id;
                            subGroupData.Level = group.Level + 1;
                            await AddGroup(subGroupData);
                        }
                    }
                    if (Answers != null)
                    {
                        AnswerService answerService = new(Db!);
                        foreach (var answerData in Answers)
                        {
                            answerData.ParentGroup = group.Id;
                            await answerService.AddAnswer(answerData);
                        }
                    }
                    // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this reans.
                    // Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", aResponse.Resource.Id, aResponse.RequestCharge);
                }
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this reans.
                Console.WriteLine(ex.Message);
            }
        }

        public async Task<GroupEx> AddNewGroup(Group group)
        {
            var (PartitionKey, Id, ParentGroup, Title, Level, Kind, Variations, Answers) = group;
            var myContainer = await container();
            string msg = string.Empty;
            try
            {
                // Check if the id already exists
                ItemResponse<Group> aResponse =
                    await myContainer!.ReadItemAsync<Group>(
                        Id,
                        new PartitionKey(PartitionKey)
                    );
                msg = $"Group in database with Id: {Id} already exists"; //, aResponse.Resource.Id
                Console.WriteLine(msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                try
                {
                    // Check if the title already exists
                    HttpStatusCode statusCode = await CheckDuplicate(Title, Id);
                    msg = $"Group in database with Id: {Id} or Title: {Title} already exists";
                    Debug.WriteLine(msg);
                }
                catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
                {
                    // Create an item in container.Note we provide the value of the partition key for this item
                    ItemResponse<Group> aResponse =
                        await myContainer!.CreateItemAsync(
                            group,
                            new PartitionKey(PartitionKey)
                        );
                    // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this reans.
                    Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", aResponse.Resource.Id, aResponse.RequestCharge);
                    return new GroupEx(aResponse.Resource, "");
                }
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this reans.
                Debug.WriteLine(ex.Message);
                msg = ex.Message;
            }
            return new GroupEx(null, msg);
        }

        public async Task<GroupEx> CreateGroup(GroupDto groupDto)
        {
            var (Id, PartitionKey) = groupDto;
            var myContainer = await container();
            Group c = new(groupDto);
            GroupEx groupEx = await AddNewGroup(c);

            // update parentGroup
            groupDto.Modified = groupDto.Modified;
            await UpdateHasSubGroups(groupDto);

            return groupEx;
        }



        public async Task<GroupEx> UpdateGroup(GroupDto groupDto)
        {
            var myContainer = await container();
            string msg = string.Empty;
            try
            {
                var (PartitionKey, Id, ParentGroup, Title, Level, Kind, Variations) = groupDto;
                // Read the item to see if it exists.  
                ItemResponse<Group> aResponse =
                    await myContainer!.ReadItemAsync<Group>(
                        Id,
                        new PartitionKey(PartitionKey)
                    );
                Group group = aResponse.Resource;
                // Update the item fields
                group.Title = Title;
                group.Kind = Kind;
                group.Variations = Variations;
                group.ParentGroup = ParentGroup;
                //group.Modified = new WhoWhen(groupDto.Modified!.NickName);

                aResponse = await myContainer.ReplaceItemAsync(group, group.Id, new PartitionKey(group.PartitionKey));
                Console.WriteLine("Updated Group [{0},{1}].\n \tBody is now: {2}\n", group.Title, group.Id, group);

                // update parentGroup
                groupDto.Modified = groupDto.Modified;
                await UpdateHasSubGroups(groupDto);

                return new GroupEx(group, msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                msg = $"Group Id: {groupDto.Id} NotFound in database."; //, aResponse.RequestCharge);
                Console.WriteLine(msg); //, aResponse.RequestCharge);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this reans.
                msg = ex.Message;
                Console.WriteLine(ex.Message);
            }
            return new GroupEx(null, msg);
        }

        public async Task<Group> UpdateNumOfAnswers(AnswerDto answerDto, int incr)
        {
            var myContainer = await container();
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Group> aResponse =
                    await myContainer!.ReadItemAsync<Group>(
                        answerDto.ParentGroup,
                        new PartitionKey(answerDto.PartitionKey)
                    );
                Group group = aResponse.Resource;
                
                // Update the item fields
                if (incr == 1)
                    group.NumOfAnswers++;
                else
                    group.NumOfAnswers--;
                group.Modified = new WhoWhen(answerDto.Modified!);

                aResponse = await myContainer.ReplaceItemAsync(group, group.Id, new PartitionKey(group.PartitionKey));
                Console.WriteLine("Updated Group [{0},{1}].\n \tBody is now: {2}\n", group.Title, group.Id, group);
                return group;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine("Group item {0}/{1} NotFound in database.\n", answerDto.PartitionKey, answerDto.Id); //, aResponse.RequestCharge);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this reans.
                Console.WriteLine(ex.Message);
            }
            return null;
        }

        public async Task<Group> UpdateHasSubGroups(GroupDto groupDto)
        {
            var (PartitionKey, Id, ParentGroup, Title, Level, Kind, Variations) = groupDto;
            var myContainer = await container();
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Group> aResponse =
                    await myContainer!.ReadItemAsync<Group>(
                        ParentGroup,
                        new PartitionKey(PartitionKey)
                    );
                Group group = aResponse.Resource;

                var sql = $"SELECT value count(1) FROM c WHERE c.Type = 'group' " +
                    "AND c.partitionKey='{PartitionKey} " +
                    "AND Parentgroup='{ParentGroup}' " + 
                    "";
                int num = await CountItems(myContainer, sql);
                Console.WriteLine($"============================ num: {num}");

                group.HasSubGroups = num > 0;
                group.Modified = new WhoWhen(groupDto.Modified!);

                aResponse = await myContainer.ReplaceItemAsync(group, group.Id, new PartitionKey(group.PartitionKey));
                Console.WriteLine("Updated Group [{0},{1}].\n \tBody is now: {2}\n", group.Title, group.Id, group);
                return group;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine("Group item {0}/{1} NotFound in database.\n", groupDto.PartitionKey, groupDto.Id); //, aResponse.RequestCharge);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this reans.
                Console.WriteLine(ex.Message);
            }
            return null;
        }


        public async Task<int> CountItems(Container myContainer, string sqlQuery)
        {
            int count = 0;
            var query = myContainer.GetItemQueryIterator<int>(new QueryDefinition(sqlQuery));
            while (query.HasMoreResults)
            {
                FeedResponse<int> response = await query.ReadNextAsync();
                count += response.Resource.FirstOrDefault();
            }
            return count;
        }

        public async Task<GroupEx> GetGroup(GroupKey groupKey)
        {
            var (partitionKey, id) = groupKey;
            string msg = string.Empty;
            var myContainer = await container();
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Group> aResponse =
                    await myContainer.ReadItemAsync<Group>(
                        id,
                        new PartitionKey(partitionKey)
                    );
                Group group = aResponse.Resource;
                return new GroupEx(group, msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                msg = $"Group {partitionKey}/{id} NotFound in database.";
                Console.WriteLine(msg); //, aResponse.RequestCharge);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this reans.
                Console.WriteLine(ex.Message);
            }
            return new GroupEx(null, msg);
        }

        public async Task<GroupEx> DeleteGroup(GroupDto groupDto)
        {
            var myContainer = await container();
            string msg = string.Empty;
            try
            {
                // Read the item to see if it exists.
                
                ItemResponse<Group> aResponse =
                    await myContainer!.ReadItemAsync<Group>(
                        groupDto.Id,
                        new PartitionKey(groupDto.PartitionKey)
                    );
                Group group = aResponse.Resource;
                aResponse = await myContainer.ReplaceItemAsync(group, group.Id, new PartitionKey(group.PartitionKey));
                msg = $"Updated Answer {group.PartitionKey}/{group.Id}. {group.Title}";
                Console.WriteLine(msg);

                // update parentGroup
                //groupDto.Modified = groupDto.Archived;
                await UpdateHasSubGroups(groupDto);

                return new GroupEx(aResponse.Resource, msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                msg =$"Group {groupDto.Id} NotFound in database."; //, aResponse.RequestCharge);
                Console.WriteLine(msg);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this reans.
                Console.WriteLine(ex.Message);
                msg = ex.Message;
            }
            return new GroupEx(null, msg);
        }

        public async Task<GroupListEx> GetGroupsUpTheTree(GroupKey groupKey)
        {
            string message = string.Empty;
            try {
                Group? group;
                string? parentGroup;
                List<Group> list = [];
                do
                {
                    GroupEx groupEx = await GetGroup(groupKey, false, 0, null);
                    Console.WriteLine("---------------------------------------------------");
                    Console.WriteLine(JsonConvert.SerializeObject(groupEx)); 
                    group = groupEx.group;
                    if (group != null)
                    {
                        list.Add(group);
                        parentGroup = group.ParentGroup;
                        // partitionKey is the same as Id
                        groupKey = new GroupKey(group.ParentGroup, group.ParentGroup);
                    }
                    else
                    {
                        message = groupEx.msg;
                        parentGroup = null;
                    }
                } while (parentGroup != null);
                return new GroupListEx(list, message);
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new GroupListEx(null, message);
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



