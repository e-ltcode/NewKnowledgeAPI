using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using NewKnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Net;

namespace NewKnowledgeAPI.Q.Questions.Model
{
   

    public class QuestionDtoEx
    {
        //public QuestionDtoEx(QuestionDto? questionDto, string msg)
        //{
        //    this.questionDto = questionDto;
        //    this.msg = msg;
        //}
        public QuestionDtoEx(QuestionEx questionEx)
        {
            var (question, msg) = questionEx;
            questionDto = question != null ? new QuestionDto(question) : null;
            this.msg = msg;
        }

        public QuestionDtoEx(QuestionDto questionDto)
        {
            this.questionDto = questionDto;
            this.msg = String.Empty;
        }

        public QuestionDtoEx(string msg)
        {
            questionDto = null;
            this.msg = msg;
        }


        public QuestionDto? questionDto { get; set; }
        public string msg { get; set; }

        internal void Deconstruct(out QuestionDto questionDto, out string msg)
        {
            questionDto = this.questionDto;
            msg = this.msg;
        }
    }

}



