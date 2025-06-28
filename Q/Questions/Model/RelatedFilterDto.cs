using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using NewKnowledgeAPI.A.Answers.Model;
using NewKnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Net;

namespace NewKnowledgeAPI.Q.Questions.Model
{
    public class RelatedFilterDto
    {
        public QuestionKey? QuestionKey { get; set; }
        public string Filter { get; set; }
        public WhoWhenDto Created { get; set; }
        public WhoWhenDto LastUsed { get; set; }  // we consider creation when Used is null 
        public uint NumOfUsages { get; set; } // filter used in Chatbot AutoSuggestQuestions

        public RelatedFilterDto()
        {
        }

        public RelatedFilterDto(QuestionKey questionKey, RelatedFilter relatedFilter)
        {
            QuestionKey = questionKey;
            var (_, filter, created, lastUsed, numOfUsages) = relatedFilter;

            Filter = filter;
            Created = new WhoWhenDto(created);
            LastUsed = new WhoWhenDto(lastUsed);
            NumOfUsages = numOfUsages;
        }

        public RelatedFilterDto(RelatedFilter relatedFilter)
        {
            var (questionKey, filter, created, lastUsed, numOfUsages) = relatedFilter;
            QuestionKey = null; // questionKey
            Filter = filter;
            numOfUsages = NumOfUsages;
            Created = new WhoWhenDto(created);
            LastUsed = new WhoWhenDto(lastUsed);
        }

        internal void Deconstruct(out QuestionKey? questionKey, out string filter,
           out WhoWhenDto created, out WhoWhenDto used, out uint numOfUsages)
        {
            questionKey = QuestionKey;
            filter = Filter;
            created = Created;
            used = LastUsed;
            numOfUsages = NumOfUsages;
        }
    }
}
