using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using NewKnowledgeAPI.A.Answers.Model;
using NewKnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Net;

namespace NewKnowledgeAPI.Q.Questions.Model
{
    public class RelatedFilterData
    {
        public string Filter { get; set; }

        public RelatedFilterData()
        {
        }
    }
}
