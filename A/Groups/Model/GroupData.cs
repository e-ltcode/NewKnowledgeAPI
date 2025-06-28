using NewKnowledgeAPI.A.Answers.Model;
using System.Diagnostics.Metrics;
using System.Net;


namespace NewKnowledgeAPI.A.Groups.Model
{
    public class GroupData
    {
        public string? ParentGroup { get; set; }
        public string Id { get; set; }
        public string? PartitionKey { get; set; }
        public string Title { get; set; }
        public int Kind { get; set; }
        public int? Level { get; set; }
        public List<string>? Variations { get; set; }
        public List<GroupData>? Groups { get; set; }
        public List<AnswerData>? Answers { get; set; }

        public void Deconstruct(
            out string? partitionKey,
            out string id,
            out string title,
            out string? parentGroup,
            out int kind,
            out int? level,
            out List<string>? variations,
            out List<GroupData>? groups,
            out List<AnswerData>? answers)
            {
                partitionKey = PartitionKey;
                id = Id;
                title = Title;
                parentGroup = ParentGroup;
                kind = Kind;
                level = Level;
                variations = Variations;
                groups = Groups;
                answers = Answers;
            }
        }
    

    public class GroupsData
    {
        public List<GroupData> Groups { get; set; }
    }
}
