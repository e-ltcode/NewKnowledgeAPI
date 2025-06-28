using Newtonsoft.Json;
using System.Diagnostics.Metrics;


namespace NewKnowledgeAPI.Q.Questions.Model
{
    public class QuestionKey
    {
        [JsonProperty(PropertyName = "ParentCategory", NullValueHandling = NullValueHandling.Ignore)]
        public string? ParentCategory { get; set; }
        public string Id { get; set; }
        public string PartitionKey { get; set; }

        public  QuestionKey()
        {
        }

        public QuestionKey(string partitionKey, string id)
        {
            PartitionKey = partitionKey;
            Id = id;
        }

        public QuestionKey(Question question)
        {
             PartitionKey = question.PartitionKey;
             Id = question.Id;
        }

        public void Deconstruct(out string partitionKey, out string id)
        {
            partitionKey = PartitionKey;
            id = Id;
        }
    }

}
