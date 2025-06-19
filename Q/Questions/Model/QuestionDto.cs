using Knowledge.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using NewKnowledgeAPI.A.Answers;
using NewKnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Net;
using NewKnowledgeAPI.Q.Questions.Model;

namespace NewKnowledgeAPI.Q.Questions.Model
{
    //public class QuestionRowDto : RecordDto
    public class QuestionRowDto
    {
        public string PartitionKey { get; set; }
        public string Id { get; set; }
        public string Title { get; set; }
        public string? CategoryTitle { get; set; }

       
        public string? ParentCategory { get; set; }
       
        public QuestionRowDto()
            //: base()
        {
        }

        public QuestionRowDto(QuestionRow questionRow)
           //: base(questionRow.Created, questionRow.Modified, questionRow.Archived)
        {
            //Console.WriteLine(JsonConvert.SerializeObject(question));
            PartitionKey = questionRow.PartitionKey;
            Id = questionRow.Id;
            Title = questionRow.Title;
            ParentCategory = questionRow.ParentCategory;
        }

        public QuestionRowDto(Question question)
            //: base(question.Created, question.Modified, question.Archived)
        {
            //Console.WriteLine(JsonConvert.SerializeObject(question));
            var questionKey = new QuestionKey(question);
            PartitionKey = question.PartitionKey;
            Id = question.Id;
            Title = question.Title;
            CategoryTitle = question.CategoryTitle;
            ParentCategory = question.ParentCategory;
            //
            // We don't modify question AssignedAnswers through QuestionDto
            //
        }
    }

    public class QuestionDto : RecordDto // QuestionRowDto
    {
        public string PartitionKey { get; set; }
        public string Id { get; set; }
        public string Title { get; set; }
        public string? CategoryTitle { get; set; }
        public string? OldParentCategory { get; set; }
        public string? ParentCategory { get; set; }
        public List<AssignedAnswerDto>? AssignedAnswerDtos { get; set; }
        public int NumOfAssignedAnswers { get; set; }
        public List<RelatedFilterDto>? RelatedFilterDtos { get; set; }
        public int NumOfRelatedFilters { get; set; }
        public int Source { get; set; }
        public int Status { get; set; }

        public QuestionDto()
            : base()

        {
        }

        public QuestionDto(Question question)
        //: base(question)
        : base(question.Created, question.Modified) //, question.Archived)
        {
            var questionKey = new QuestionKey(question);
            ////////////////
            // QuestionDto
            PartitionKey = question.PartitionKey;
            Id = question.Id;
            Title = question.Title;
            CategoryTitle = question.CategoryTitle;
            ParentCategory = question.ParentCategory;
            ///////////////////////////////////////////////
            
            //Console.WriteLine(JsonConvert.SerializeObject(question));
            //var questionKey = new QuestionKey(question);
            
            var assignedAnswers = question.AssignedAnswers ?? [];
            assignedAnswers.Sort(AssignedAnswer.Comparer); // put the most rated AssignedAnswers to the top
            AssignedAnswerDtos = assignedAnswers
                .Select(assignedAnswer => new AssignedAnswerDto(questionKey, assignedAnswer))
                .ToList();
            NumOfAssignedAnswers = question.NumOfAssignedAnswers ?? 0;

            var relatedFilters = question.RelatedFilters ?? [];
            relatedFilters.Sort(RelatedFilter.Comparer); // put the most rated AssignedAnswers to the top
            RelatedFilterDtos = relatedFilters
                //.Select(relatedFilters => new RelatedFilterDto(questionKey, relatedFilters))
                .Select(relatedFilters => new RelatedFilterDto(questionKey, relatedFilters))
                .ToList();
            NumOfRelatedFilters = question.NumOfRelatedFilters ?? 0 ;
            Source = question.Source;
            Status = question.Status;
        }

        public void Deconstruct(out string partitionKey, out string id,
                                out string? oldParentCategory,  out string? parentCategory,
                              out string title, out int source, out int status, out WhoWhenDto? modified)

        {
            partitionKey = PartitionKey;
            id = Id;
            oldParentCategory = OldParentCategory;
            parentCategory = ParentCategory;
            title = Title;
            source = Source;
            status = Status;
            modified = Modified;
        }

    }
}
