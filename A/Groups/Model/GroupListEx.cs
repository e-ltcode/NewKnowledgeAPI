namespace NewKnowledgeAPI.A.Groups.Model
{
 
    public class GroupListEx
    {
        public GroupListEx(List<Group>? groupList, string msg)
        {
            GroupList = groupList;
            Msg = msg;
        }

        public void Deconstruct(out List<Group>? groupList, out string msg)
        {
            groupList = GroupList;
            msg = Msg;
        }


        public List<Group>? GroupList { get; set; }
        public string Msg { get; set; }    
    }
}
