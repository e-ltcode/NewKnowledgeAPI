using NewKnowledgeAPI.A.Answers.Model;
using NewKnowledgeAPI.Q.Questions.Model;
using System.Diagnostics.Metrics;


namespace NewKnowledgeAPI.History.Model
{
    public class HistoryData
    {
        public string? PartitionKey { get; set; }
        public string? Id { get; set; }

        public QuestionKey QuestionKey { get; set; }
        public AnswerKey AnswerKey { get; set; }
        public USER_ANSWER_ACTION UserAction { get; set; }
        public string NickName { get; set; }

        public HistoryData() { 
        }

      
    }

}
