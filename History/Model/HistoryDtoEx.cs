using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using NewKnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Net;

namespace NewKnowledgeAPI.Hist.Model
{
     public class HistoryDtoEx
    {
        //public HistoryDtoEx(HistoryDto? historyDto, string msg)
        //{
        //    this.historyDto = historyDto;
        //    this.msg = msg;
        //}
        public HistoryDtoEx(HistoryEx historyEx)
        {
            historyDto = historyEx.history != null ? new HistoryDto(historyEx.history!) : null;
            msg = historyEx.msg!;
        }

        public HistoryDtoEx(string msg)
        {
            historyDto = null;
            this.msg = msg;
        }



        public HistoryDto? historyDto { get; set; }
        public string msg { get; set; }
    }

}



