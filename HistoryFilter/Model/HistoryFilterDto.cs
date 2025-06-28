using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using NewKnowledgeAPI.A.Answers.Model;
using NewKnowledgeAPI.Common;
using NewKnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System.Net;

namespace NewKnowledgeAPI.HistFilter.Model
{
    public class HistoryFilterDto //: RecordDto
    {
        public QuestionKey QuestionKey { get; set; }
        public string Filter { get; set; }
        public WhoWhenDto Created { get; set; }

        public HistoryFilterDto()
        {
        }
    }
 }



