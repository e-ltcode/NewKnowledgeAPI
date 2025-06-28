using NewKnowledgeAPI.Common;
using Newtonsoft.Json;

namespace NewKnowledgeAPI.Hist.Model
{

    public class HistoryEx
    {
        public HistoryEx(History? history, string msg)
        {
            this.history = history;
            this.msg = msg;
        }

        public History? history { get; set; }
        public string msg { get; set; }

        internal void Deconstruct(out History? history, out string msg)
        {
            history = this.history;
            msg = this.msg;
        }
    }
}
