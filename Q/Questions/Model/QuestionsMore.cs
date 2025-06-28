using System.Collections.Generic;
using System.Net;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NewKnowledgeAPI.Q.Questions.Model
{
    public class QuestionsMore
    {
        public List<QuestionRow> QuestionRows { get; set; }
        public bool HasMoreQuestions { get; set; }
        public QuestionsMore(List<QuestionRow> questionRows, bool hasMore)
        {
            QuestionRows = questionRows;
            HasMoreQuestions = hasMore;
        }
    }
}

