using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using NewKnowledgeAPI.A.Answers.Model;
using NewKnowledgeAPI.Common;
using NewKnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System.Net;

namespace NewKnowledgeAPI.History.Model
{
    public class HistoryDto //: RecordDto
    {

        public string? Type { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string? Id { get; set; }

        [JsonProperty(PropertyName = "partitionKey")]
        public string? PartitionKey { get; set; }

        public QuestionKey QuestionKey { get; set; }
        public AnswerKey AnswerKey { get; set; }
        public short UserAction { get; set; }
        public WhoWhenDto Created { get; set; }


        public HistoryDto()
        {
        }

        public HistoryDto(History history)
        {
            //Console.WriteLine(JsonConvert.SerializeObject(history));
            //PartitionKey = history.PartitionKey;
            //Id = history.Id;
            QuestionKey = history.QuestionKey;
            AnswerKey = history.AnswerKey;
            UserAction = history.UserAction;
            Created = new WhoWhenDto(history.Created);
        }
    }
 }



