
using Microsoft.Azure.Cosmos;
using System.Net;
using Newtonsoft.Json;
using NewKnowledgeAPI.Q.Categories.Model;
using NewKnowledgeAPI.A.Answers.Model;
using NewKnowledgeAPI.Q;
using NewKnowledgeAPI.A;
using NewKnowledgeAPI.A.Groups.Model;
using NewKnowledgeAPI.Q.Categories;
using NewKnowledgeAPI.A.Groups;

namespace Knowledge.Services
{
    public class DbService : IDisposable
    {
        
        private readonly IConfiguration Configuration;
        private readonly string CosmosDBAccountUri;
        private readonly string CosmosDBAccountPrimaryKey;

        private CosmosClient? cosmosClient = null;
        private Database? database = null;
        readonly Dictionary<string, Container?> containers = [];

        // The name of the database and container we will create
        private readonly string databaseId = "Knowledge";

        public bool Initiated { get; protected set; }

        public DbService(IConfiguration configuration)
        {
            Configuration = configuration;
            // Try the ConnectionStrings approach first, then fallback to the old way
            var connectionString = configuration.GetConnectionString("CosmosDb");
            if (!string.IsNullOrEmpty(connectionString))
            {
                // Parse connection string format: AccountEndpoint=https://...;AccountKey=...;
                var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
                CosmosDBAccountUri = parts.FirstOrDefault(p => p.StartsWith("AccountEndpoint="))?.Replace("AccountEndpoint=", "") ?? "";
                CosmosDBAccountPrimaryKey = parts.FirstOrDefault(p => p.StartsWith("AccountKey="))?.Replace("AccountKey=", "") ?? "";
            }
            else
            {
                // Fallback to the old configuration approach
                CosmosDBAccountUri = configuration["CosmosDBAccountUri"] ?? "";
                CosmosDBAccountPrimaryKey = configuration["CosmosDBAccountPrimaryKey"] ?? "";
            }

            // Only create CosmosClient if configuration is provided
            if (!string.IsNullOrEmpty(CosmosDBAccountUri) && !string.IsNullOrEmpty(CosmosDBAccountPrimaryKey))
            {
                cosmosClient = new CosmosClient(
                    CosmosDBAccountUri,
                    CosmosDBAccountPrimaryKey,
                    new CosmosClientOptions()
                    {
                        ApplicationName = "NewKnowledgeAPI"
                    }
                );
            }
            else
            {
                // Leave cosmosClient as null - will be handled gracefully in methods
                cosmosClient = null;
            }

            Initialize = CreateInstanceAsync();
        }
        public Task Initialize { get; }

        private async Task CreateInstanceAsync()
        {
            await Task.Delay(1);
            //bool created = await CreateDatabaseIfNotExistsAsync();
            //if (created)
            //{
            //    Console.WriteLine("Created Database: {0}\n", database!.Id);
            //    await AddInitialData();
            //}
            Initiated = true;
        }


        public async Task<bool> CreateDatabaseIfNotExistsAsync()
        {
            // Create a new database
            DatabaseResponse response = await cosmosClient!.CreateDatabaseIfNotExistsAsync(databaseId);
            database = response.Database;
            bool created = response.StatusCode == HttpStatusCode.Created;
            if (created) {
                await AddInitialData();
            }
            return created;
        }

        // <CreateContainerAsync>
        /// <summary>
        /// Create the container if it does not exist. 
        /// Specifiy "/partitionKey" as the partition key path since we're storing family information, to ensure good distribution of requests and storage.
        /// </summary>
        /// <returns></returns>
        private async Task<Container> CreateContainerAsync(string containerId)
        {
            // Create a new container
            var container = await database!.CreateContainerIfNotExistsAsync(containerId, "/partitionKey");
            containers.Add(containerId, container);
            //Console.WriteLine("Created Container: {0}\n", containers[containerId]!.Id);
            return container;
        }
        // </CreateContainerAsync>


        // <ScaleContainerAsync>
        /// <summary>
        /// Scale the throughput provisioned on an existing Container.
        /// You can scale the throughput (RU/s) of your container up and down to meet the needs of the workload. Learn more: https://aka.ms/cosmos-request-units
        /// </summary>
        /// <returns></returns>
        private async Task ScaleContainerAsync(string containerId)
        {
            // Read the current throughput
            try
            {
                Container? container = containers[containerId];
                int? throughput = await container!.ReadThroughputAsync();
                if (throughput.HasValue)
                {
                    Console.WriteLine("Current provisioned throughput : {0}\n", throughput.Value);
                    int newThroughput = throughput.Value + 100;
                    // Update throughput
                    await container.ReplaceThroughputAsync(newThroughput);
                    Console.WriteLine("New provisioned throughput : {0}\n", newThroughput);
                }
            }
            catch (CosmosException cosmosException) when (cosmosException.StatusCode == HttpStatusCode.BadRequest)
            {
                Console.WriteLine("Cannot read container throuthput.");
                Console.WriteLine(cosmosException.ResponseBody);
            }

        }
        // </ScaleContainerAsync>

        private async Task<bool> AddInitialData()
        {
            try
            {
                var categoryService = new CategoryService(this);
                using StreamReader r = new("InitialData/categories-questions.json");
                string json = r.ReadToEnd();
                CategoriesData? categoriesData = JsonConvert.DeserializeObject<CategoriesData>(json);
                foreach (var categoryData in categoriesData!.Categories)
                {
                    categoryData.PartitionKey = categoryData.Id;
                    categoryData.ParentCategory = null;
                    categoryData.Level = 1;
                    Console.WriteLine("ADDING CATEGORIESSS {0}", categoryData.Id);
                    await categoryService.AddCategory(categoryData);
                }
                //return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            try
            {
                var groupService = new GroupService(this);
                using StreamReader r = new("InitialData/groups-answers.json");
                string json = r.ReadToEnd();
                GroupsData? groupsData = JsonConvert.DeserializeObject<GroupsData>(json);
                foreach (var groupData in groupsData!.Groups)
                {
                    groupData.PartitionKey = groupData.Id;
                    groupData.ParentGroup = null;
                    groupData.Level = 1;
                    Console.WriteLine("ADDING GROUPSSSS {0}", groupData.Id);
                    await groupService.AddGroup(groupData);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

        }


        public async Task<Container> GetContainer(string containerId)
        {
            if (cosmosClient == null)
            {
                throw new InvalidOperationException("CosmosDB is not configured. Please check your connection string in appsettings.json");
            }
            
            if (database == null)
            {
                database = cosmosClient.GetDatabase(databaseId);
                await database.ReadAsync(); // TODO treba li ovo?
                //    bool created = await this.CreateDatabaseIfNotExistsAsync();
                //    if (created)
                //    {
                //        Console.WriteLine("Created Database: {0}\n", this.database.Id);
                //        await this.AddInitialData();
                //    }
            }
            Container? container;
            if (containers.ContainsKey(containerId))
            {
                container = containers[containerId];
            }
            else 
            {
                container = await CreateContainerAsync(containerId);
                //await ScaleContainerAsync();
            }
            return container!;
        }

        // <GetStartedAsync>
        /// <summary>
        /// Entry point to call methods that operate on Azure Cosmos DB resources in this sample
        /// </summary>
        //public async Task GetStartedAsync()
        //{
            //// Create a new instance of the Cosmos Client
            //this.cosmosClient = new CosmosClient(
            //    CosmosDBAccountUri,
            //    CosmosDBAccountPrimaryKey,
            //    new CosmosClientOptions()
            //    {
            //        ApplicationName = "KnowledgeCosmos"
            //    }
            //);

            //await this.CreateDatabaseAsync();
            //await this.CreateContainerAsync();
            //await this.ScaleContainerAsync();
            //await this.AddItemsToContainerAsync();
            //await this.QueryItemsAsync();
            //await this.ReplaceFamilyItemAsync();
            // await this.DeleteFamilyItemAsync();
            // await this.DeleteDatabaseAndCleanupAsync();
        //}


        public void Dispose()
        {
            foreach (var container in containers) {
                containers[container.Key] = null;
            }   
            containers.Clear();

            database = null;
            if (cosmosClient != null) {
                cosmosClient.Dispose();
                cosmosClient = null;
            }

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        // </GetStartedDemoAsync>

    }
}
