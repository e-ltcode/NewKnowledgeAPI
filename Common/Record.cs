namespace NewKnowledgeAPI.Common
{

    
    public class Record 
    {
        public WhoWhen? Created { get; set; }
        public WhoWhen? Modified { get; set; }
        public WhoWhen? Archived { get; set; }

        public Record(WhoWhen? created, WhoWhen? modified, WhoWhen? archived)
        {
            Created = created;
            Modified = modified;
            Archived = archived;
        }

        public Record(WhoWhenDto created, WhoWhenDto modified, WhoWhenDto archived)
        {
            Created = created != null ? new WhoWhen(created) : null;
            Modified = modified != null ? new WhoWhen(modified) : null;
            Archived = archived != null ? new WhoWhen(archived) : null;
        }

        public Record()
        {
        }
    }
}
