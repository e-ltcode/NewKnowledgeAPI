using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using NewKnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Net;

namespace NewKnowledgeAPI.A.Groups.Model
{
       public class GroupDtoEx
    {
        //public GroupDtoEx(GroupDto? groupDto, string msg)
        //{
        //    this.groupDto = groupDto;
        //    this.msg = msg;
        //}
        public GroupDtoEx(GroupEx groupEx)
        {
            groupDto = groupEx.group != null ? new GroupDto(groupEx.group!) : null;
            msg = groupEx.msg!;
        }


        public GroupDtoEx(GroupDto groupDto, string msg)
        {
            this.groupDto = groupDto;
            this.msg = msg;
        }

        public GroupDtoEx(string msg)
        {
            groupDto = null;
            this.msg = msg;
        }



        public GroupDto? groupDto { get; set; }
        public string msg { get; set; }
    }

}



