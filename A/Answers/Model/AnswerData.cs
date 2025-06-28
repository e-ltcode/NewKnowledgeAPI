using System.Diagnostics.Metrics;


namespace NewKnowledgeAPI.A.Answers.Model
{
    public class AnswerData
    {
        public string? ParentGroup { get; set; }
        public string? Id { get; set; }
        //public string? PartitionKey { get; set; }

        public string Title { get; set; }
        public string? Link { get; set; }
        public IList<string>? AssignedAnswers { get; set; }
        public int? Source { get; set; }
        public int? Status { get; set; }

        public AnswerData() { 
        }

        public AnswerData(string ParentGroup, string Title)
        {
            this.ParentGroup = ParentGroup;
            this.Title = Title; 
        }
    }

}
