using Newtonsoft.Json;

namespace NewKnowledgeAPI.Q.Categories.Model
{
    public class CategoryKey
    {
        public string PartitionKey { get; set; }
        public string Id { get; set; }

        public CategoryKey()
        {
        }

        public CategoryKey(string partitionKey, string id)
        {
            PartitionKey = partitionKey;
            Id = id;
        }

        public CategoryKey(Category category)
        {
            PartitionKey = category.PartitionKey;
            Id = category.Id;
        }


        public CategoryKey(CategoryRowDto rowDto)
        {
            PartitionKey = rowDto.PartitionKey;
            Id = rowDto.Id;
        }

        public CategoryKey(CategoryDto categoryDto)
        {
            PartitionKey = categoryDto.PartitionKey;
            Id = categoryDto.Id;
        }

        public void Deconstruct(out string partitionKey, out string id)
        {
            partitionKey = PartitionKey;
            id = Id;
        }
    }

}
