using Newtonsoft.Json;

namespace NewKnowledgeAPI.A.Groups.Model
{
    public class GroupKey
    {
        public string PartitionKey { get; set; }
        public string Id { get; set; }

        public GroupKey()
        {
        }

        public GroupKey(string partitionKey, string id)
        {
            PartitionKey = partitionKey;
            Id = id;
        }

        public GroupKey(Group group)
        {
            PartitionKey = group.PartitionKey;
            Id = group.Id;
        }


        public GroupKey(GroupDto groupDto)
        {
            PartitionKey = groupDto.PartitionKey;
            Id = groupDto.Id;
        }

        public void Deconstruct(out string partitionKey, out string id)
        {
            partitionKey = PartitionKey;
            id = Id;
        }
    }

}
