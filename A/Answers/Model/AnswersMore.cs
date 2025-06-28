using System.Collections.Generic;
using System.Net;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NewKnowledgeAPI.A.Answers.Model
{
    public class AnswersMore
    {
        public List<AnswerRow> AnswerRows { get; set; }
        public bool hasMoreAnswers { get; set; }
        public AnswersMore(List<AnswerRow> answerRows, bool hasMore)
        {
            AnswerRows = answerRows;
            hasMoreAnswers = hasMore;
        }
    }
}

