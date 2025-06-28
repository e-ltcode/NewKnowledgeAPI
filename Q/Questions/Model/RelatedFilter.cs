using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Azure.Cosmos;
using NewKnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;

namespace NewKnowledgeAPI.Q.Questions.Model
{
    public class RelatedFilter: IDisposable
    {
        [JsonProperty(PropertyName = "QuestionKey", NullValueHandling = NullValueHandling.Ignore)]
        public QuestionKey? QuestionKey { get; set; } // not stored to db

        public string Filter { get; set; }
        public WhoWhen Created { get; set; }
        public WhoWhen LastUsed { get; set; }

        public uint NumOfUsages { get; set; } // filter used in Chatbot AutoSuggestQuestions

        public RelatedFilter()
        {
        }

        public RelatedFilter(string filter, WhoWhen created)
        {
            QuestionKey = null;
            Filter = filter;
            Created = created;
            LastUsed = created;
            NumOfUsages = 1;
        }

        //public RelatedQuestion(QuestionKey answerKey)
        //{
        //    Created = new WhoWhen("Admin");
        //}

        public RelatedFilter(RelatedFilterDto dto)
        {
            var (questionKey, filter, created, lastUsed, numOfUsage) = dto;
            Filter = filter;
            QuestionKey = null; // questionKey;
            Created = new WhoWhen(created);
            LastUsed = new WhoWhen(lastUsed != null ? lastUsed : created);
            NumOfUsages++;
        }

        //public override string ToString() => 
        //    $"{PartitionKey}/{Id}, {Title} {ParentGroup} ";


        internal void Deconstruct(out QuestionKey? questionKey, out string filter,
            out WhoWhen created, out WhoWhen lastUsed,  out uint numOfUsages)
        {
            questionKey = QuestionKey;
            filter = Filter;
            created = Created;
            lastUsed = LastUsed;
            numOfUsages = NumOfUsages;
        }

        public bool IsSimmilar(string filter)
        {   
            // improve algo
            return Filter.Equals(filter);
        }

        public static int Comparer(RelatedFilter x, RelatedFilter y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    // If x is null and y is null, they're
                    // equal.
                    return 0;
                }
                else
                {
                    // If x is null and y is not null, y
                    // is greater.
                    return -1;
                }
            }
            else
            {
                // If x is not null...
                //
                if (y == null)
                // ...and y is null, x is greater.
                {
                    return 1;
                }
                else
                {
                    // ...and y is not null, compare the
                    // lengths of the two strings.
                    //
                    int retval = y.NumOfUsages.CompareTo(x.NumOfUsages);  // DESC

                    if (retval != 0)
                    {
                        // If the strings are not of equal length,
                        // the longer string is greater.
                        //
                        return retval;
                    }
                    else
                    {
                        // If the strings are of equal length,
                        // sort them with ordinary string comparison.
                        //
                        //return x.NotNumOfUsages.CompareTo(y.NotNumOfUsages);
                        return 0;
                    }
                }
            }
        }

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
