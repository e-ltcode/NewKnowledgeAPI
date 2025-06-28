using NewKnowledgeAPI.A.Answers.Model;
using NewKnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;

namespace NewKnowledgeAPI.Q.Questions.Model
{
    public class AssignedAnswerData: IDisposable
    {
        public AnswerKey AnswerKey { get; set; }

        //public AssignedAnswerData(AnswerKey answerKey)
        //{
        //    AnswerKey = answerKey;
        //}

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
