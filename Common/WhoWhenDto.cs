
namespace NewKnowledgeAPI.Common
{

    public class WhoWhenDto
    {
        public WhoWhenDto() 
        {
        } 

        public WhoWhenDto(string NickName)
        {
            Time = DateTime.Now;
            this.NickName = NickName;
        }

        public WhoWhenDto(WhoWhen? whoWhen)
        {
            if (whoWhen == null)
            {
                Time = DateTime.Now;
                NickName = "NN";
            }
            else
            {
                Time = whoWhen.Time;
                NickName = whoWhen.NickName;
            }
        }

        public DateTime Time { get; set; }
        public string NickName { get; set; }
    }
}

