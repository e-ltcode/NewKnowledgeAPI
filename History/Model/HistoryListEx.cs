using NewKnowledgeAPI.Common;
using Newtonsoft.Json;

namespace NewKnowledgeAPI.History.Model
{

    public class HistoryListEx
    {
        public HistoryListEx(List<History>? historyList, string msg)
        {
            this.historyList = historyList;
            this.msg = msg;
        }

        public List<History>? historyList { get; set; }
        public string msg { get; set; }

        internal void Deconstruct(out List<History>? historyList, out string msg)
        {
            historyList = this.historyList;
            msg = this.msg;
        }
    }
}
