using Azure;
using Knowledge.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using NewKnowledgeAPI.Common;
using NewKnowledgeAPI.Q.Categories.Model;
using NewKnowledgeAPI.Q.Questions;
using NewKnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

namespace NewKnowledgeAPI.Q.Categories
{
    public class CategoryService : IDisposable
    {
        public DbService? Db { get; set; } = null;

        private readonly string containerId = "Questions";
        private Container? _container = null;

        public async Task<Container> container()
        {
            _container ??= await Db!.GetContainer(containerId);
            return _container;
        }

        public CategoryService()
        {
        }

        //public Category(IConfiguration configuration)
        //{
        //    Category.Db = new Db(configuration);
        //}

        public CategoryService(DbService db)
        {
            Db = db;
        }

        internal async Task<List<CategoryRowDto>> GetAllCats()
        {
            var myContainer = await container();
            var sqlQuery = "SELECT * FROM c WHERE c.Type = 'category' AND IS_NULL(c.Archived) ORDER BY c.Title ASC";
            QueryDefinition queryDefinition = new(sqlQuery);
            FeedIterator<Category> queryResultSetIterator = myContainer.GetItemQueryIterator<Category>(queryDefinition);
            //List<CategoryDto> subCategories = new List<CategoryDto>();
            List<CategoryRowDto> dtos = [];
            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<Category> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (Category category in currentResultSet)
                {
                    dtos.Add(new CategoryRowDto(new CategoryRow(category)));
                }
            }
            return dtos;
        }
        
        public async Task<CategoryEx> GetCategoryHidrated(CategoryKey categoryKey, int pageSize, string? includeQuestionId)
        {
            var (PartitionKey, Id) = categoryKey;
            var myContainer = await container();
            try
            {
                // Read the item to see if it exists.  
                //ItemResponse<Category> aResponse =
                Category category = await myContainer!.ReadItemAsync<Category>(Id, new PartitionKey(PartitionKey));
                //Console.WriteLine(JsonConvert.SerializeObject(category));

                if (category != null)
                {
                    /////////////////////
                    //// subCategoryRows
                    //List<CategoryRow> subCategories = await GetSubCategoryRows(myContainer, PartitionKey, Id);
                    //category.SubCategories = subCategories;
                    category.SubCategories = [];

                    ///////////////////
                    // questions
                    if (pageSize > 0)
                    {
                        // hidrate collections except questions, like  category.x = hidrate;  
                        if (category.NumOfQuestions > 0)
                        {
                            var questionService = new QuestionService(Db);
                            QuestionsMore questionsMore = await questionService.GetQuestions(Id, 0, pageSize, includeQuestionId??"null");
                            category.QuestionRows = questionsMore.QuestionRows/*.Select(questionRow => new Question(questionRow))*/.ToList();
                            category.HasMoreQuestions = questionsMore.HasMoreQuestions;
                        }
                    }
                }
                return new CategoryEx(category, "");
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                return new CategoryEx(null, ex.Message);
            }
        }

        // TODO make CtrlController as the base class for:  CategoryRowController and CategoryController
        internal async Task<List<CategoryRow>> GetSubCategoryRows(Container myContainer, string PartitionKey, string id)
        {
            var sqlQuery = $"SELECT * FROM c WHERE c.Type = 'category' AND IS_NULL(c.Archived) AND "
            + (
                id == "null"
                    ? $" IS_NULL(c.ParentCategory)"
                    : $" c.ParentCategory = '{id}'"
            );
            QueryDefinition queryDefinition = new(sqlQuery);
            FeedIterator<Category> queryResultSetIterator = myContainer!.GetItemQueryIterator<Category>(queryDefinition);
            List<CategoryRow> subCategorRows = [];
            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<Category> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (Category category in currentResultSet)
                {
                    subCategorRows.Add(new CategoryRow(category));
                }
            }
            return subCategorRows;
        }


        public async Task<CategoryEx> GetCategoryWithSubCategories(CategoryKey categoryKey)
        {
            var myContainer = await container();

            var (PartitionKey, Id) = categoryKey;
            try
            {
                Category category = await myContainer!.ReadItemAsync<Category>(Id, new PartitionKey(PartitionKey));
                var categoryRow = new CategoryRow(category);
                List<CategoryRow> subCategories = await GetSubCategoryRows(myContainer, PartitionKey, Id);
                    // bio neki []
                categoryRow.SubCategories = subCategories;
                return new CategoryEx(category, "");
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                return new CategoryEx(null, ex.Message);
            }
        }



        public async Task<HttpStatusCode> CheckDuplicate(string title, string? id = null) //QuestionData questionData)
        {
            var sqlQuery = "SELECT * FROM c WHERE c.Type = 'category' AND " +
                $"(c.Title = '{title.Replace("\'", "\\'")}'" + 
                   (id != null ? $" OR c.Id = '{id}')" : $")");
            QueryDefinition queryDefinition = new(sqlQuery);
            FeedIterator<Question> queryResultSetIterator =
                _container!.GetItemQueryIterator<Question>(queryDefinition);
            if (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<Question> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                if (currentResultSet.Count == 0)
                {
                    throw new CosmosException("", HttpStatusCode.NotFound, 0, "0", 0);
                }
            }
            return HttpStatusCode.OK;
        }

        public async Task AddCategory(CategoryData categoryData)
        {
            var (partitionKey, id, title, link, header, parentCategory, kind, level, variations, categories, questions) = categoryData;

            //Console.WriteLine(JsonConvert.SerializeObject(categoryData));
            var myContainer = await container();

            if (questions != null && id == "DOMAIN")
            {
                for (var i = 1; i <= 500; i++)
                    questions!.Add(new QuestionData(id, $"Test row for DOMAIN " + i.ToString("D3")));
            }

            try
            {
                var c = new Category(categoryData);
                CategoryEx categoryEx = await AddNewCategory(c);
                if (categoryEx.category != null)
                {
                    Category category = categoryEx.category;
                    if (categories != null)
                    {
                        foreach (var subCategoryData in categories)
                        {
                            subCategoryData.PartitionKey = subCategoryData.Id;
                            subCategoryData.ParentCategory = category.Id;
                            subCategoryData.Level = category.Level + 1;
                            await AddCategory(subCategoryData);
                        }
                    }
                    if (questions != null)
                    {
                        QuestionService questionService = new(Db!);
                        foreach (var questionData in questions)
                        {
                            questionData.ParentCategory = category.Id;
                            await questionService.AddQuestion(questionData);
                        }
                    }
                    // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                    // Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", aResponse.Resource.Id, aResponse.RequestCharge);
                }
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
            }
        }

        public async Task<CategoryEx> AddNewCategory(Category category)
        {
            //var (PartitionKey, Id, Title, ParentCategory, Kind, Level, Variations, Questions) = category;
            var (partitionKey, id, parentCategory, title, link, header, level, kind,
                hasSubCategories, subCategories,
                hasMoreQuestions, numOfQuestions, questionRows, variations, isExpanded, rootId) = category;

            var myContainer = await container();
            string msg = string.Empty;
            try
            {
                // Check if the id already exists
                ItemResponse<Category> aResponse =
                    await myContainer!.ReadItemAsync<Category>(
                        id,
                        new PartitionKey(partitionKey)
                    );
                msg = $"Category in database with Id: {id} already exists"; //, aResponse.Resource.Id
                Console.WriteLine(msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                try
                {
                    // Check if the title already exists
                    HttpStatusCode statusCode = await CheckDuplicate(title, id);
                    msg = $"Category in database with Id: {id} or Title: {title} already exists";
                    Console.WriteLine(msg);
                }
                catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
                {
                    // Create an item in container.Note we provide the value of the partition key for this item
                    ItemResponse<Category> aResponse =
                        await myContainer!.CreateItemAsync(
                            category,
                            new PartitionKey(partitionKey)
                        );
                    // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                    Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", aResponse.Resource.Id, aResponse.RequestCharge);
                    return new CategoryEx(aResponse.Resource, "");
                }
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                msg = ex.Message;
            }
            return new CategoryEx(null, msg);
        }

        public async Task<CategoryEx> CreateCategory(CategoryDto categoryDto)
        {
            //var (Id, PartitionKey) = categoryDto;
            categoryDto.Id = categoryDto.Title.Trim().Replace(' ', '_').ToUpper();
            categoryDto.PartitionKey = categoryDto.Id;
            var myContainer = await container();
            var category = new Category(categoryDto);
            CategoryEx categoryEx = await AddNewCategory(category);

            // update parentCategory
            categoryDto.Modified = categoryDto.Modified;
            await UpdateHasSubCategories(categoryDto);

            return categoryEx;
        }

        /*
        public async Task<CategoryEx> UpdateCategory(CategoryDto categoryDto)
        {
            var myContainer = await container();
            string msg = string.Empty;
            try
            {
                var (partitionKey, id, parentCategory, title, link, level, kind, variations, modified) = categoryDto;
                // Read the item to see if it exists.  
                ItemResponse<Category> aResponse =
                    await myContainer!.ReadItemAsync<Category>(
                        id,
                        new PartitionKey(partitionKey)
                    );
                Category category = aResponse.Resource;
                // Update the item fields
                category.Title = title;
                category.Link = link;
                category.Kind = kind;
                category.Variations = variations;
                category.ParentCategory = parentCategory;
                if (modified != null)
                {
                    category.Modified = new WhoWhen(modified.NickName);
                }
                aResponse = await myContainer.ReplaceItemAsync(category, id, new PartitionKey(partitionKey));
                Console.WriteLine("Updated Category [{0},{1}].\n \tBody is now: {2}\n", title, id, category);

                // update parentCategory
                //categoryDto.Modified = categoryDto.Modified;
                //await UpdateHasSubCategories(categoryDto);

                return new CategoryEx(category, msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                msg = $"Category Id: {categoryDto.Id} NotFound in database."; //, aResponse.RequestCharge);
                Console.WriteLine(msg); //, aResponse.RequestCharge);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                msg = ex.Message;
                Console.WriteLine(ex.Message);
            }
            return new CategoryEx(null, msg);
        }
        */


        public async Task<CategoryEx> UpdateCategory(CategoryDto categoryDto)
        {
            var myContainer = await container();
            string msg = string.Empty;
            try
            {
                var (partitionKey, id, parentCategory, title, link, level, kind, variations, modified) = categoryDto;
                // Read the item to see if it exists.  
                ItemResponse<Category> aResponse =
                    await myContainer!.ReadItemAsync<Category>(
                        id,
                        new PartitionKey(partitionKey)
                    );
                Category category = aResponse.Resource;
                var doUpdate = true;
                if (!category.Title.Equals(title, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        HttpStatusCode statusCode = await CheckDuplicate(title);
                        doUpdate = false;
                        msg = $"Question with Title: \"{title}\" already exists in database.";
                        Console.WriteLine(msg);
                        return new CategoryEx(null, msg);
                    }
                    catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                    {
                        //question.Title = q.Title;
                    }
                }
                if (doUpdate)
                {
                    if (category.ParentCategory != parentCategory)
                    {
                        // changed Category
                    }
                    // Update the item fields
                    category.Title = title;
                    category.Link = link;
                    category.Kind = kind;
                    category.Variations = variations;
                    category.ParentCategory = parentCategory;
                    category.Modified = new WhoWhen(modified!.NickName);
                    aResponse = await myContainer.ReplaceItemAsync(category, id, new PartitionKey(partitionKey));
                    return new CategoryEx(aResponse.Resource, "");
                }
                return new CategoryEx(category, msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                msg = $"Category Id: {categoryDto.Id} NotFound in database."; //, aResponse.RequestCharge);
                Console.WriteLine(msg); //, aResponse.RequestCharge);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                msg = ex.Message;
                Console.WriteLine(ex.Message);
            }
            return new CategoryEx(null, msg);
        }



        public async Task<Category> UpdateNumOfQuestions(CategoryKey categoryKey, WhoWhen modified, int incr)
        {
            var ( partitionKey, id ) = categoryKey;
            var myContainer = await container();
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Category> aResponse =
                    await myContainer!.ReadItemAsync<Category>(
                        id,
                        new PartitionKey(partitionKey)
                    );
                Category category = aResponse.Resource;
                
                // Update the item fields
                if (incr == 1)
                    category.NumOfQuestions++;
                else
                    category.NumOfQuestions--;
                category.Modified = new WhoWhen(modified!.NickName); //new WhoWhen(questionDto.Modified!);

                aResponse = await myContainer.ReplaceItemAsync(category, category.Id, new PartitionKey(category.PartitionKey));
                Console.WriteLine("Updated Category [{0},{1}].\n \tBody is now: {2}\n", category.Title, category.Id, category);
                return category;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine("Category item {0}/{1} NotFound in database.\n", partitionKey, id); //, aResponse.RequestCharge);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
            }
            return null;
        }

        public async Task<Category> UpdateHasSubCategories(CategoryDto categoryDto)
        {
            var (_, _, parentCategory, _, _, _, _, _, modified) = categoryDto;
            var myContainer = await container();
            try
            {
                var PartitionKey = parentCategory;
                var Id = parentCategory;
                // Read the item to see if it exists.  
                ItemResponse<Category> aResponse =
                    await myContainer!.ReadItemAsync<Category>(
                        Id,
                        new PartitionKey(PartitionKey)
                    );
                Category category = aResponse.Resource;

                var sql = $"SELECT value count(1) FROM c WHERE c.Type = 'category' " +
                    "AND c.partitionKey='{PartitionKey} " +
                    "AND Parentcategory='{Id}' " + 
                    "AND IS_NULL(c.Archived)";
                int num = await CountItems(myContainer, sql);
                Console.WriteLine($"============================ num: {num}");

                category.HasSubCategories = num > 0;
                category.Modified = new WhoWhen(modified!.NickName);

                aResponse = await myContainer.ReplaceItemAsync(category, Id, new PartitionKey(PartitionKey));
                Console.WriteLine("Updated Category [{0},{1}].\n \tBody is now: {2}\n", category.Title, Id, category);
                return category;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine("Category item {0}/{1} NotFound in database.\n", categoryDto.PartitionKey, categoryDto.Id); //, aResponse.RequestCharge);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
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

        public async Task<CategoryEx> GetCategory(CategoryKey categoryKey)
        {
            var (partitionKey, id) = categoryKey;
            string msg = string.Empty;
            var myContainer = await container();
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Category> aResponse =
                    await myContainer.ReadItemAsync<Category>(
                        id,
                        new PartitionKey(partitionKey)
                    );
                Category category = aResponse.Resource;
                return new CategoryEx(category, msg);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                msg = $"Category {partitionKey}/{id} NotFound in database.";
                Console.WriteLine(msg); //, aResponse.RequestCharge);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
            }
            return new CategoryEx(null, msg);
        }

        public async Task<CategoryEx> DeleteCategory(CategoryDto categoryDto)
        {
            var myContainer = await container();
            string msg = string.Empty;
            try
            {

                var (partitionKey, id, parentCategory, _, _, _, _, _, modified) = categoryDto; 
                // Read the item to see if it exists.

                ItemResponse<Category> aResponse =
                    await myContainer!.ReadItemAsync<Category>(id, new PartitionKey(partitionKey));
                Category category = aResponse.Resource;
                if (category.HasSubCategories)
                {
                    return new CategoryEx(null, "HasSubCategories");
                }
                else if (category.NumOfQuestions > 0 )
                {
                    return new CategoryEx(null, "HasQuestions");
                }
                category.Archived = new WhoWhen(categoryDto.Modified!.NickName);
                aResponse = await myContainer.ReplaceItemAsync(category, category.Id, new PartitionKey(category.PartitionKey));
                msg = $"Archived Category {category.PartitionKey}/{category.Id}. {category.Title}";
                Console.WriteLine(msg);

                // update parentCategory
                await UpdateHasSubCategories(categoryDto);
                return new CategoryEx(aResponse.Resource, "OK");
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                msg =$"Category {categoryDto.Id} NotFound in database."; //, aResponse.RequestCharge);
                Console.WriteLine(msg);
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                msg = ex.Message;
            }
            return new CategoryEx(null, msg);
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



