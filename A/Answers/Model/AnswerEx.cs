using NewKnowledgeAPI.Common;
using Newtonsoft.Json;

namespace NewKnowledgeAPI.A.Answers.Model
{

    public class AnswerEx
    {
        public AnswerEx(Answer? answer, string msg)
        {
            this.answer = answer;
            this.msg = msg;
        }

        public Answer? answer { get; set; }
        public string msg { get; set; }

        internal void Deconstruct(out Answer? answer, out string msg)
        {
            answer = this.answer;
            msg = this.msg;
        }
    }
}
