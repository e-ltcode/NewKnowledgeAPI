namespace NewKnowledgeAPI.A.Groups.Model
{
 
    public class GroupEx
    {
        public GroupEx(Group? group, string msg)
        {
            this.group = group;
            this.msg = msg;
        }

        public void Deconstruct(out Group? group, out string msg)
        {
            group = this.group;
            msg = this.msg;
        }

        public Group? group { get; set; }
        public string msg { get; set; }    
    }
}
