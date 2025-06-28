
using NewKnowledgeAPI.Common;
using Newtonsoft.Json;
using NewKnowledgeAPI.A.Answers.Model;
using NewKnowledgeAPI.Q.Categories.Model;

namespace NewKnowledgeAPI.A.Groups.Model
{
    public class Group : Record, IDisposable
    {
        public string Type { get; set; }
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "partitionKey")]
        public string PartitionKey { get; set; }
        public string Title { get; set; }
        public int Kind { get; set; }
        public string? ParentGroup { get; set; }
        public int Level { get; set; }
        public List<string>? Variations { get; set; }
        public int NumOfAnswers { get; set; }
        public bool HasSubGroups { get; set; }

        [JsonProperty(PropertyName = "Answers", NullValueHandling = NullValueHandling.Ignore)]
        public List<Answer>? Answers { get; set; }

        [JsonProperty(PropertyName = "HasMoreAnswers", NullValueHandling = NullValueHandling.Ignore)]
        public bool? HasMoreAnswers { get; set; }

        public Group()
            : base()
        {
        }

        public Group(Answer answer)
          : base()
        {
            Id = answer.ParentGroup!;
            PartitionKey = answer.PartitionKey;
        }


        public Group(GroupData groupData)
            : base(new WhoWhen("Admin"), null, null)
        {
            Type = "group";
            Id = groupData.Id;
            PartitionKey = groupData.PartitionKey!;
            Title = groupData.Title;
            Kind = groupData.Kind;
            ParentGroup = groupData.ParentGroup;
            Level = (int)groupData.Level!;
            Variations = groupData.Variations ?? null;
            NumOfAnswers = groupData.Answers == null ? 0 : groupData.Answers.Count;
            HasSubGroups = groupData.Groups != null && groupData.Groups.Count > 0;
            Answers = null;
        }

        public Group(GroupDto groupDto)
            :base(groupDto.Created, groupDto.Modified, null)
        {
            Type = "group";
            Id = groupDto.Id;
            PartitionKey = groupDto.PartitionKey ?? groupDto.Id;
            Title = groupDto.Title;
            Kind = groupDto.Kind;
            ParentGroup = groupDto.ParentGroup;
            Level = groupDto.Level;
            Variations = groupDto.Variations ?? null;
            Answers = null;
            NumOfAnswers = 0;
            HasSubGroups = false;
        }

        //public override string ToString() =>
        //    $"{PartitionKey}/{Id} : {Title}";


        public void Deconstruct(
            out string partitionKey,
            out string id, 
            out string parentGroup, 
            out string title, 
            out int level, 
            out int kind, 
            out List<string>? variations,
            out List<Answer>? answers)
        {
            partitionKey = PartitionKey;
            id = Id;
            parentGroup = ParentGroup;
            title = Title;
            kind = Kind;
            level = Level;
            variations = Variations;
            answers = Answers;
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



