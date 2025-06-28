using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using NewKnowledgeAPI.A.Answers.Model;
using NewKnowledgeAPI.Common;
using NewKnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System.Net;

namespace NewKnowledgeAPI.Hist.Model
{
    public class AnswerRatedDtoListEx
    {

        public List<AnswerRatedDto> list { get; set; }
        public string msg { get; set; }


        public AnswerRatedDtoListEx(List<AnswerRatedDto>? list, string msg)
        {
            this.list = list ?? new List<AnswerRatedDto>();
            this.msg = msg;
        }

    }
 }



