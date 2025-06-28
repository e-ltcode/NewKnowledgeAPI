namespace NewKnowledgeAPI.A.Groups.Model
{
 
    public class GroupDtoListEx
    {
        public GroupDtoListEx(GroupListEx groupListEx)
        {
            groupDtoList = [];
            var (groupList, msg) = groupListEx;
            List<GroupDto> list = [];
            if (groupList != null)
            {
                foreach (var group in groupList)
                {
                    GroupDto groupDto = new GroupDto(group);
                    groupDtoList.Add(groupDto);
                }
            }
            this.msg = msg;
        }

        public List<GroupDto> groupDtoList { get; set; }
        public string msg { get; set; }    
    }
}
