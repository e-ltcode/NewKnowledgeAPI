using NewKnowledgeAPI.Common;
using NewKnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;

namespace NewKnowledgeAPI.A.Answers.Model
{
    public class AnswerRow : Record
    {

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "partitionKey")]
        public string PartitionKey { get; set; }

        public string Title { get; set; }

        public string? ParentGroup { get; set; }

        public AnswerRow()
            : base(new WhoWhen("Admin"), null, null)
        {
        }

        public AnswerRow(AnswerData answerData)
                : base(new WhoWhen("Admin"), null, null)
        {
            string s = DateTime.Now.Ticks.ToString();
            Id = answerData.Id ?? s.Substring(s.Length - 10);// Guid.NewGuid().ToString();
            PartitionKey = answerData.ParentGroup!;
            ParentGroup = answerData.ParentGroup;
            Title = answerData.Title;
            
        }

        public AnswerRow(AnswerDto answerDto)
        : base(answerDto.Created, answerDto.Modified, null)
        {
            // string s = DateTime.Now.Ticks.ToString();
            // Id = s.Substring(s.Length - 10);// Guid.NewGuid().ToString();
            Id = answerDto.Id;
            PartitionKey = answerDto.PartitionKey!;
            ParentGroup = answerDto.ParentGroup;
            Title = answerDto.Title;
        }

        public AnswerRow(AnswerRow row)
            : base(row.Created, row.Modified, row.Archived)
        {
            Id = row.Id;
            PartitionKey = row.PartitionKey!;
            Title = row.Title;
            ParentGroup = row.ParentGroup;
        }

        public void Deconstruct(out string partitionKey, out string id, 
            out string title, out string? parentGroup)
        {
            partitionKey = PartitionKey;
            id = Id;
            title = Title;
            
            parentGroup = ParentGroup;
        }
    }

    public class Answer : AnswerRow, IDisposable
    {
        public string Type { get; set; }
        public string? GroupTitle { get; set; }
        public int Source { get; set; }
        public int Status { get; set; }
        public string? Link { get; set; }


        public Answer()
            :  base()
        {
            Type = "answer";
        }

        public Answer(AnswerRow answerRow)
            : base(answerRow)
        {
            Type = "answer";
        }
        public Answer(AnswerData answerData)
            : base(answerData)
        {
            Type = "answer";
            GroupTitle = null;
            Source = 0;
            Status = 0;
            Link = answerData.Link;
        }

        public Answer(AnswerDto answerDto)
        : base(answerDto)
        {
            Type = "answer";
            GroupTitle = null;
            Source = answerDto.Source;
            Status = answerDto.Status;
            Link = answerDto.Link;
        }

        //public override string ToString() => 
        //    $"{PartitionKey}/{Id}, {Title} {ParentGroup} ";

        public void Deconstruct(out string partitionKey, out string id, out string title, out string? link,
            out string? parentGroup, out string type, out int source, out int status)
        {
            partitionKey = PartitionKey;
            id = Id;
            title = Title;
            link = Link;
            parentGroup = ParentGroup;
            type = Type;
            source = Source;
            status = Status;
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
