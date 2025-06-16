using Azure;
using Knowledge.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
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
    public class CategoryRowService : IDisposable
    {
        public DbService? Db { get; set; } = null;

        private readonly string containerId = "Questions";
        private Container? _container = null;

        public async Task<Container> container()
        {
            _container ??= await Db!.GetContainer(containerId);
            return _container;
        }

        public CategoryRowService()
        {
        }

        //public Category(IConfiguration configuration)
        //{
        //    Category.Db = new Db(configuration);
        //}

        public CategoryRowService(DbService db)
        {
            Db = db;
        }

        internal async Task<List<CategoryRowDto>> GetAllCategoryRows()
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
                    // TODO  One nice day, replace "SELECT *" with fields needed for CategoryRow only
                    dtos.Add(new CategoryRowDto(new CategoryRow(category)));
                }
            }
            return dtos;
        }

        internal async Task<List<CategoryRow>> GetSubCategoryRows(Container? cntr, string PartitionKey, string id)
        {
            var myContainer = cntr != null ? cntr : await container();
            var sqlQuery = $"SELECT * FROM c WHERE c.Type = 'category' AND IS_NULL(c.Archived) AND "
            // for categories partitionKey is same as Id
            //+ (
            //    PartitionKey == "null"
            //        ? $""
            //        : $" c.partitionKey = '{PartitionKey}' AND "  
            //)
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

        public async Task<CategoryRowEx> GetCategoryRowWithSubCategories(Container container, CategoryKey categoryKey, bool hidrate)
        {
            var (PartitionKey, Id) = categoryKey;
            try
            {
                Category category = await container!.ReadItemAsync<Category>(Id, new PartitionKey(PartitionKey));
                var categoryRow = new CategoryRow(category);
                categoryRow.SubCategories = hidrate 
                    ? await GetSubCategoryRows(container, PartitionKey, Id)
                    : [];
                return new CategoryRowEx(categoryRow, "");
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                return new CategoryRowEx((CategoryRow?)null, ex.Message);
            }
        }


        public async Task<CategoryRowEx> GetCategoryRowsUpTheTree(CategoryKey categoryKey)
        {
            var myContainer = await container();
            string message = string.Empty;
            try
            {
                string? parentCategory = null;
                CategoryRow? categoryRow = null;
                CategoryRow? child = null;
                do
                {
                    bool hidrate = categoryRow != null; // do not hidrate row at the bottom
                    CategoryRowEx categoryRowEx = await GetCategoryRowWithSubCategories(myContainer, categoryKey, hidrate);
                    // Console.WriteLine("---------------------------------------------------");
                    // Console.WriteLine(JsonConvert.SerializeObject(categoryEx)); 
                    var (catRow, msg) = categoryRowEx;
                    if (catRow != null)
                    {
                        //child = cat;
                        if (catRow.HasSubCategories)
                        {
                            int index = catRow.SubCategories!.FindIndex(x => x.Id == child!.Id);
                            if (index >= 0)
                            {
                                catRow.SubCategories[index] = child!.ShallowCopy();
                            }
                        }
                        catRow.IsExpanded = hidrate;
                        child = catRow.ShallowCopy();
                        parentCategory = catRow.ParentCategory!;
                        // partitionKey is the same as Id
                        categoryKey = new CategoryKey(parentCategory, parentCategory);
                        categoryRow = catRow.DeepCopy();
                    }
                    else
                    {
                        message = msg;
                        parentCategory = null;
                    }
                } while (parentCategory != null);
                // put root id to each Category
                if (categoryRow != null) {
                    SetRootId(categoryRow, categoryRow.Id);
                }
                return new CategoryRowEx(categoryRow, message);
            }
            catch (Exception ex)
            {
                message = ex.Message;
                Console.WriteLine(message);
            }
            return new CategoryRowEx((CategoryRow?)null, message);
        }

        void SetRootId(CategoryRow categoryRow, string rootId)
        {
            categoryRow.RootId = rootId;
            Debug.Assert(categoryRow.SubCategories != null);
            categoryRow.SubCategories.ForEach(c => {
                SetRootId(c, rootId);
            });
        }

        /*
        public async Task<CategoryRowDtoEx> GetCategoryRow(CategoryKey categoryKey)
        {
            // used for node collapse
            var (PartitionKey, Id) = categoryKey;
            var myContainer = await container();
            var msg = string.Empty;
            try
            {
                // Read the item to see if it exists.  
                //ItemResponse<Category> aResponse =
                Category category = await myContainer!.ReadItemAsync<Category>(Id, new PartitionKey(PartitionKey));
                //Console.WriteLine(JsonConvert.SerializeObject(category));
                if (category == null)
                {
                    msg = "Not Found Bre";
                }
                else
                {
                    var categoryRowDto = new CategoryRowDto(new CategoryRow(category));
                    return new CategoryRowDtoEx(categoryRowDto, "");
                }
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                msg = ex.Message;
            }
            return new CategoryRowDtoEx(null, msg);
        }
        */

        public async Task<CategoryRowDtoEx> GetCategoryRow(CategoryKey categoryKey, 
            bool hidrate, int pageSize, string? includeQuestionId)
        {
            // used for node expand
            var (PartitionKey, Id) = categoryKey;
            var myContainer = await container();
            var msg = string.Empty;
            try
            {
                // Read the item to see if it exists.  
                //ItemResponse<Category> aResponse =
                Category category = await myContainer!.ReadItemAsync<Category>(Id, new PartitionKey(PartitionKey));
                //Console.WriteLine(JsonConvert.SerializeObject(category));
                if (category == null)
                {
                    msg = "Not Found Bre";
                }
                else
                {
                    if (hidrate)
                    {
                        ///////////////////
                        // subCategoryRows
                        List<CategoryRow> subCategories = await GetSubCategoryRows(myContainer, PartitionKey, Id);
                        category.SubCategories = subCategories;

                        ///////////////////
                        // questionsRows
                        if (pageSize > 0)
                        {
                            if (category.NumOfQuestions > 0)
                            {
                                var questionService = new QuestionService(Db);
                                QuestionsMore questionsMore = await questionService.GetQuestions(Id, 0, pageSize, includeQuestionId ?? "null");
                                category.QuestionRows = questionsMore.QuestionRows/*.Select(questionRow => new Question(questionRow))*/.ToList();
                                category.HasMoreQuestions = questionsMore.HasMoreQuestions;
                            }
                        }
                    }
                    var categoryRow = new CategoryRow(category);
                    var categoryRowDto = new CategoryRowDto(categoryRow);
                    return new CategoryRowDtoEx(categoryRowDto, "");
                }
            }
            catch (Exception ex)
            {
                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine(ex.Message);
                msg = ex.Message;
            }
            return new CategoryRowDtoEx(null, msg);
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



