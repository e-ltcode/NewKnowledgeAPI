using Newtonsoft.Json;
using System.Diagnostics.Metrics;


namespace NewKnowledgeAPI.HistoryFilter.Model
{
    public class HistoryFilterKey
    {
        public string PartitionKey { get; set; }
        public long Id { get; set; }

        public HistoryFilterKey()
        {
        }
    }

}
