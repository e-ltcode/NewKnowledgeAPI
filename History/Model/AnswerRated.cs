using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using NewKnowledgeAPI.A.Answers.Model;
using NewKnowledgeAPI.Common;
using NewKnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System.Net;

namespace NewKnowledgeAPI.History.Model
{
    public class AnswerRated 
    {
        public QuestionKey QuestionKey { get; set; }
        public AnswerKey AnswerKey { get; set; }
        public string AnswerTitle { get; set; }

        public Boolean Fixed { get; set; }
        public Boolean NotFixed { get; set; }
        public Boolean NotClicked { get; set; }


        public AnswerRated()
        {
        }


        public AnswerRated(History history)
        {
            //Console.WriteLine(JsonConvert.SerializeObject(history));
            QuestionKey = history.QuestionKey;
            AnswerKey = history.AnswerKey;
            Fixed = history.UserAction == (short)USER_ANSWER_ACTION.Fixed;
            NotFixed = history.UserAction == (short)USER_ANSWER_ACTION.NotFixed;
            NotClicked = history.UserAction == (short)USER_ANSWER_ACTION.NotClicked;
        }


        public AnswerRated(QuestionKey questionKey, AssignedAnswer assignedAnswer)
        {
            //Console.WriteLine(JsonConvert.SerializeObject(history));
            QuestionKey = questionKey;
            AnswerKey = assignedAnswer.AnswerKey;
            AnswerTitle = assignedAnswer.AnswerTitle!;
            Fixed = true;
            NotFixed = false;
            NotClicked = false;
        }

    }
 }



