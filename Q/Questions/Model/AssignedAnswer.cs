using NewKnowledgeAPI.A.Answers.Model;
using NewKnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;

namespace NewKnowledgeAPI.Q.Questions.Model
{
    public class AssignedAnswer: IDisposable
    {
        public AnswerKey AnswerKey { get; set; }
        public WhoWhen Created { get; set; }
        public WhoWhen? Modified { get; set; }

        [JsonProperty(PropertyName = "AnswerTitle", NullValueHandling = NullValueHandling.Ignore)]
        public string? AnswerTitle;

        [JsonProperty(PropertyName = "AnswerLink", NullValueHandling = NullValueHandling.Ignore)]
        public string? AnswerLink;

        public uint Fixed { get; set; } // num of clicks to Fixed
        public uint NotFixed { get; set; } // num of clicks to NotFixed
        public uint NotClicked { get; set; } // num of not clicked

        public AssignedAnswer()
        {
        }

        public AssignedAnswer(AnswerKey answerKey)
        {
            AnswerKey = answerKey;
            AnswerTitle = null;
            AnswerLink = null;
            Created = new WhoWhen("Admin");
            Fixed = 0;
            NotFixed = 0;
            NotClicked = 0;
        }

        public AssignedAnswer(AssignedAnswerDto dto)
        {
            var (_, answerKey, _, _, created, modified) = dto; //, Fixed, NotFixed, NotClicked) = dto; 
            AnswerKey = answerKey;
            AnswerTitle = null;
            AnswerLink = null;
            Created = new WhoWhen(created);
            Modified = modified != null ? new WhoWhen(modified) : null;
            Fixed = 0;
            NotFixed = 0;
            NotClicked = 0;
        }

        //public override string ToString() => 
        //    $"{PartitionKey}/{Id}, {Title} {ParentGroup} ";


        internal void Deconstruct(out AnswerKey answerKey, out string? answerTitle, out string? answerLink,
            out WhoWhen created, out WhoWhen? modified,
            out uint Fixed, out uint NotFixed, out uint NotClicked)
        {
            answerKey = AnswerKey;
            answerTitle = AnswerTitle;
            answerLink = AnswerLink;
            created = Created;
            modified = Modified ?? null;
            Fixed = this.Fixed;
            NotFixed = this.NotFixed;
            NotClicked = this.NotClicked;
        }

        public static int Comparer(AssignedAnswer x, AssignedAnswer y)
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
                    int retval = y.Fixed.CompareTo(x.Fixed);  // DESC

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
                        return x.NotFixed.CompareTo(y.NotFixed);
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
