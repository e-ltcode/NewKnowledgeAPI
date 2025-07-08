using NewKnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System.Diagnostics.Metrics;


namespace NewKnowledgeAPI.A.Answers.Model
{
    public class AnswerKey : IEquatable<AnswerKey>
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;

        public AnswerKey()
        {
        }

        public AnswerKey(string partitionKey, string id)
        {
            PartitionKey = partitionKey;
            Id = id; 
        }

        public AnswerKey(Answer answer)
        {
            PartitionKey = answer.PartitionKey;
            Id = answer.Id;
        }


        public override bool Equals(object? obj) => this.Equals(obj as AnswerKey);

        public bool Equals(AnswerKey? key)
        {
            if (key is null)
            {
                return false;
            }

            // Optimization for a common success case.
            if (Object.ReferenceEquals(this, key))
            {
                return true;
            }

            // If run-time types are not exactly the same, return false.
            if (this.GetType() != key.GetType())
            {
                return false;
            }

            // Return true if the fields match.
            // Note that the base class is not invoked because it is
            // System.Object, which defines Equals as reference equality.
            return (PartitionKey == key.PartitionKey) && (Id == key.Id);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PartitionKey, Id);
        }
    }

}
