using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using NewKnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Net;

namespace NewKnowledgeAPI.A.Answers.Model
{
   

    public class AnswerDtoEx
    {
        //public AnswerDtoEx(AnswerDto? answerDto, string msg)
        //{
        //    this.answerDto = answerDto;
        //    this.msg = msg;
        //}
        public AnswerDtoEx(AnswerEx answerEx)
        {
            answerDto = answerEx.answer != null ? new AnswerDto(answerEx.answer!) : null;
            msg = answerEx.msg!;
        }

        public AnswerDtoEx(string msg)
        {
            answerDto = null;
            this.msg = msg;
        }



        public AnswerDto? answerDto { get; set; }
        public string msg { get; set; }
    }

}



