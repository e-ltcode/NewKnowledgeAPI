using NewKnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Net;
using NewKnowledgeAPI.A.Answers.Model;
using NewKnowledgeAPI.Q.Questions.Model;

namespace NewKnowledgeAPI.A.Groups.Model
{
    public class GroupDto : RecordDto
    {
        [JsonProperty(PropertyName = "Id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "PartitionKey")]
        public string? PartitionKey { get; set; }

        public string Title { get; set; }
        public int Kind { get; set; }
        public string? ParentGroup { get; set; }
        public int Level { get; set; }
        public List<string>? Variations { get; set; }
        public int? NumOfAnswers { get; set; }
        public bool? HasSubGroups { get; set; }
        
        public List<AnswerDto>? Answers { get; set; }
        public bool? HasMoreAnswers { get; set; }

        public GroupDto()
            : base()
        {
        }
      

        public GroupDto(GroupKey groupKey, AnswersMore answersMore)
            : base() // TODO
            //: base(null, null, null) // TODO prosledi 
        {
            var (partitionKey, id) = groupKey;
            Id = id;
            PartitionKey = partitionKey;
            Title = "deca";
            Kind = 1;
            Level = 1;
            Variations = [];

            Console.WriteLine("odgovora {0}", answersMore.AnswerRows.Count);
            //if (answersMore.answers.Count > 0) {
            //    Answer q = answersMore.answers.First();
            //}
            Answers = Answers2Dto(answersMore.AnswerRows.Select(row => new Answer(row)).ToList());
            HasMoreAnswers = answersMore.hasMoreAnswers;
        }


        public GroupDto(Group group)
            : base(group.Created, group.Modified)
        {
            Id = group.Id;
            PartitionKey = group.PartitionKey!;
            Title = group.Title;
            Kind = group.Kind;
            ParentGroup = group.ParentGroup;
            Level = group.Level;
            Variations = group.Variations;
            NumOfAnswers = group.NumOfAnswers;
            HasSubGroups = group.HasSubGroups;
            if (group.Answers == null)
            {
                Answers = null;
                HasMoreAnswers = false;
            }
            else
            {
                //IList<AnswerDto> answers = new List<AnswerDto>();
                //foreach (var answer in group.answers)
                //    answers.Add(new AnswerDto(answer));
                Answers = Answers2Dto(group.Answers!);
                HasMoreAnswers = group.HasMoreAnswers;
            }
        }

        public List<AnswerDto> Answers2Dto(List<Answer> answers)
        {
            List<AnswerDto> list = [];
            foreach (var answer in answers)
            {
                //Console.WriteLine(JsonConvert.SerializeObject(answer));
                list.Add(new AnswerDto(answer));
            }
            return list;
        }

        public void Deconstruct(out string partitionKey, out string id)
        {
            partitionKey = PartitionKey;
            id = Id;
        }

        public void Deconstruct(out string partitionKey, out string id, out string parentGroup, out string title, out int level, out int kind, out List<string>? variations)
        {
            partitionKey = PartitionKey;
            id = Id;
            parentGroup = ParentGroup;
            title = Title;
            kind = Kind;
            level = Level;
            variations = Variations;
        }

    }
}



