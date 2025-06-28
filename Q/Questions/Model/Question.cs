using Microsoft.Azure.Cosmos.Linq;
using NewKnowledgeAPI.A.Answers.Model;
using NewKnowledgeAPI.Common;
using Newtonsoft.Json;

namespace NewKnowledgeAPI.Q.Questions.Model
{
    public class QuestionRow : Record
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "partitionKey")]
        public string PartitionKey { get; set; }

        public string? ParentCategory { get; set; }
        public int? NumOfAssignedAnswers { get; set; }

        public string Title { get; set; }

        [JsonProperty(PropertyName = "Included", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Included {  get; set; }

        public QuestionRow()
            : base(new WhoWhen("Admin"), null)
        {
            Title = string.Empty;
        }

        public QuestionRow(QuestionData data)
           : base(new WhoWhen("Admin"), null)
        {
            string s = DateTime.Now.Ticks.ToString();
            Id = data.Id ?? s.Substring(s.Length - 10);// Guid.NewGuid().ToString();
            PartitionKey = data.ParentCategory!;
            ParentCategory = data.ParentCategory;
            Title = data.Title;
        }

        public QuestionRow(QuestionDto dto)
            : base(dto.Created, dto.Modified)
        {
            //string s = DateTime.Now.Ticks.ToString();
            //Id = s.Substring(s.Length - 10);// Guid.NewGuid().ToString();
            if (dto.Id.Equals("generateId")) {
                string s = DateTime.Now.Ticks.ToString();
                dto.Id = s.Substring(s.Length - 10);// Guid.NewGuid().ToString();
            }

            Id = dto.Id;
            PartitionKey = dto.PartitionKey!;
            Title = dto.Title;
            ParentCategory = dto.ParentCategory;
        }

        public QuestionRow(QuestionRow row)
            : base(row.Created, row.Modified)
        {
            Id = row.Id;
            PartitionKey = row.PartitionKey!;
            Title = row.Title;
            ParentCategory = row.ParentCategory;
        }

        public void Deconstruct(out string partitionKey, out string id, out string title, out string? parentCategory)
        {
            partitionKey = PartitionKey;
            id = Id;
            title = Title;
            parentCategory = ParentCategory;
        }

    }

    public class Question : QuestionRow, IDisposable
    {
        public string Type { get; set; }

        [JsonProperty(PropertyName = "CategoryTitle", NullValueHandling = NullValueHandling.Ignore)]
        public string? CategoryTitle { get; set; }

        public List<AssignedAnswer>? AssignedAnswers { get; set; }

        public List<RelatedFilter>? RelatedFilters { get; set; }
        public int? NumOfRelatedFilters { get; set; }

        public int Source { get; set; }
        public int Status { get; set; }

        public Question()
            : base()   
        {
            Type = "question";
            CategoryTitle = null;
            Source = 0;
            Status = 0;
        }


        public Question(QuestionData questionData)
            : base(questionData)
        {
            Type = "question";
            ParentCategory = questionData.ParentCategory;
            CategoryTitle = null;

            // Assigned Answers
            AssignedAnswers = [];
            if (questionData.AssignedAnswers != null)
            {
                foreach (var ans in questionData.AssignedAnswers)
                {
                    AssignedAnswers.Add(new AssignedAnswer(ans.AnswerKey));
                }
            }
            NumOfAssignedAnswers = AssignedAnswers.Count;

            // Related Filters
            RelatedFilters = [];
            if (questionData.RelatedFilters != null)
            {
                foreach (var relatedFilterData in questionData.RelatedFilters)
                {
                    var relatedFilter = new RelatedFilter(relatedFilterData.Filter, new WhoWhen("Admin"));
                    RelatedFilters.Add(relatedFilter);
                }
            }
            NumOfRelatedFilters = RelatedFilters.Count;

            Source = 0;
            Status = 0;
        }

        public Question(QuestionDto questionDto)
        : base(questionDto)
        {
            Type = "question";
            CategoryTitle = null;
            //AssignedAnswers = questionDto.AssignedAnswers!;
            //NumOfAssignedAnswers = questionDto.NumOfAssignedAnswers;
            Source = questionDto.Source;
            Status = questionDto.Status;    
        }

        public Question(QuestionRow questionRow)
        : base(questionRow)
        {
            Type = "question";
            CategoryTitle = null;
            Source = 0;
            Status = 0;
        }

        //public override string ToString() => 
        //    $"{PartitionKey}/{Id}, {Title} {ParentCategory} ";

        public void Deconstruct(out string partitionKey, out string id, out string title, out string? parentCategory,
                                out string type, out int source, out int status, 
                                out List<AssignedAnswer>? assignedAnswers, // out int? numOfAssignedAnswers) //,
                                out List<RelatedFilter>? relatedFilters) //, out int? numOfRelatedFilters)
        {
            partitionKey = PartitionKey;
            id = Id;
            title = Title;
            parentCategory = ParentCategory;
            type = Type;
            source = Source;
            status = Status;
            assignedAnswers = AssignedAnswers;
            //numOfAssignedAnswers = NumOfAssignedAnswers;
            //numOfRelatedFilters = NumOfRelatedFilters;
            relatedFilters = RelatedFilters;
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
