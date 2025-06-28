using NewKnowledgeAPI.A.Answers.Model;
using NewKnowledgeAPI.Common;
using NewKnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;

namespace NewKnowledgeAPI.Hist.Model
{
    public enum USER_ANSWER_ACTION { NotFixed = 0, Fixed = 1, NotClicked = 2 };

    public class History : /*Record,*/ IDisposable
    {
        public string Type { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "partitionKey")]
        public string PartitionKey { get; set; }

        public QuestionKey QuestionKey { get; set; }
        public AnswerKey AnswerKey { get; set; }
        public short UserAction { get; set; }
        public WhoWhen Created { get; set; }


        public static DateTime centuryBegin = new DateTime(2025, 1, 1);
        public static string GeneratedId {  
            get
            {
                long elapsedTicks = DateTime.Now.Ticks - History.centuryBegin.Ticks;
                TimeSpan elapsedSpan = new TimeSpan(elapsedTicks);
                return elapsedSpan.Ticks.ToString();
            }
        }  

        public History()
        {
        }

  
        public History(HistoryData historyData)
        {
            Type = "history";
            PartitionKey = historyData.PartitionKey ?? "history";
            Id = History.GeneratedId;
            QuestionKey = historyData.QuestionKey;
            AnswerKey = historyData.AnswerKey;
            //Fixed = (short)historyData.UserAction;
            Created = new WhoWhen(historyData.NickName ?? "Admin");
            UserAction = (short)historyData.UserAction;
        }

        public History(HistoryDto historyDto)
        {
            Type = "history";
            PartitionKey = historyDto.PartitionKey ?? "history";
            Id = History.GeneratedId;
            QuestionKey = historyDto.QuestionKey;
            AnswerKey = historyDto.AnswerKey;
            UserAction = historyDto.UserAction;
            Created = new WhoWhen(historyDto.Created);
        }

        //public override string ToString() => 
        //    $"{PartitionKey}/{Id}, {Title} {ParentCategory} ";

        public void Deconstruct(out string partitionKey, out string id, out QuestionKey questionKey, out AnswerKey answerKey, out short userAction, out WhoWhen created)
        {
            partitionKey = PartitionKey;
            id = Id;
            questionKey = QuestionKey;
            answerKey = AnswerKey;
            userAction = UserAction;
            created = Created;
        }

        public void Dispose()
        {
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
