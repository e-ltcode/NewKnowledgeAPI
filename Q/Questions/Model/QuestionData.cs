using System.Diagnostics.Metrics;


namespace NewKnowledgeAPI.Q.Questions.Model
{
    public class QuestionData
    {
        public string? ParentCategory { get; set; }
        public string? Id { get; set; }
        //public string? PartitionKey { get; set; }

        public string Title { get; set; }
        public List<AssignedAnswerData>? AssignedAnswers { get; set; }
        public List<RelatedFilterData>? RelatedFilters { get; set; }
        public int? Source { get; set; }
        public int? Status { get; set; }

        public QuestionData() { 
        }

        public QuestionData(string ParentCategory, string Title)
        {
            this.ParentCategory = ParentCategory;
            this.Title = Title; 
        }
    }

}
