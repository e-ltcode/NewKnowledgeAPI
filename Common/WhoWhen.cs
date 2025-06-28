
namespace NewKnowledgeAPI.Common
{
   
    public class WhoWhen
    {
        public WhoWhen()
        {
        }

        public WhoWhen(string NickName)
        {
            Time = DateTime.Now;
            this.NickName = NickName;
        }

        public WhoWhen(WhoWhenDto whoWhenDto)
        {
            Time = whoWhenDto.Time;
            NickName = whoWhenDto.NickName;
        }

      
        public DateTime Time { get; set; }
        public string NickName { get; set; }
    }
 
}

