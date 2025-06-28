using Newtonsoft.Json;
using System.Diagnostics.Metrics;


namespace NewKnowledgeAPI.Hist.Model
{
    public class HistoryKey
    {
        public string PartitionKey { get; set; }
        public long Id { get; set; }

        public HistoryKey()
        {
        }
    }

}
