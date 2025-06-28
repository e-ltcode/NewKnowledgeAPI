using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using NewKnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Net;

namespace NewKnowledgeAPI.A.Answers.Model
{
    //public class AnswerRowDto : RecordDto
    public class AnswerRowDto
    {
        public string PartitionKey { get; set; }
        public string Id { get; set; }
        public string Title { get; set; }
        public string? GroupTitle { get; set; }
        public string? ParentGroup { get; set; }


        public AnswerRowDto()
        //: base()
        {
        }

        public AnswerRowDto(AnswerRow answerRow)
        //: base(answerRow.Created, answerRow.Modified, answerRow.Archived)
        {
            //Console.WriteLine(JsonConvert.SerializeObject(answer));
            PartitionKey = answerRow.PartitionKey;
            Id = answerRow.Id;
            Title = answerRow.Title;
            ParentGroup = answerRow.ParentGroup;
        }

        public AnswerRowDto(Answer answer)
        //: base(answer.Created, answer.Modified, answer.Archived)
        {
            //Console.WriteLine(JsonConvert.SerializeObject(answer));
            var answerKey = new AnswerKey(answer);
            PartitionKey = answer.PartitionKey;
            Id = answer.Id;
            Title = answer.Title;
            GroupTitle = answer.GroupTitle;
            ParentGroup = answer.ParentGroup;
            //
            // We don't modify answer AssignedAnswers through AnswerDto
            //
        }
    }

    public class AnswerDto : RecordDto // AnswerRowDto
    {
        /// <summary>
        ///     AnswerRowDto
        /// </summary>
        public string PartitionKey { get; set; }
        public string Id { get; set; }
        public string Title { get; set; }
        public string? Link { get; set; }
        public string? GroupTitle { get; set; }
        public string? ParentGroup { get; set; }
        public int Source { get; set; }
        public int Status { get; set; }


        public AnswerDto()
            : base()
        {
        }

        public AnswerDto(Answer answer)
        //: base(answer)
        : base(answer.Created, answer.Modified) //, answer.Archived)
        {
            var answerKey = new AnswerKey(answer);
            ////////////////
            // AnswerDto
            PartitionKey = answer.PartitionKey;
            Id = answer.Id;
            Title = answer.Title;
            Link = answer.Link;
            GroupTitle = answer.GroupTitle;
            ParentGroup = answer.ParentGroup;

            //Console.WriteLine(JsonConvert.SerializeObject(answer));
            Source = answer.Source;
            Status = answer.Status;
        }
    }
}



