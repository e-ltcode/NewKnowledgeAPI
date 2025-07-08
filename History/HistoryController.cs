using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System.Configuration;
using Newtonsoft.Json;
using Knowledge.Services;
using Microsoft.AspNetCore.Authorization;
using NewKnowledgeAPI.History.Model;
using NewKnowledgeAPI.Q.Categories.Model;
using NewKnowledgeAPI.Q.Questions.Model;
using NewKnowledgeAPI.Q.Questions;
using System.Collections.Generic;
using NewKnowledgeAPI.A.Answers;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace NewKnowledgeAPI.History
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]

    public class HistoryController : ControllerBase
    {
        private readonly IConfiguration Configuration;
        DbService dbService { get; set; }

        public HistoryController(IConfiguration configuration)
        {
            dbService = new DbService(configuration);
            dbService.Initialize.Wait();
            Configuration = configuration;
        }


        [HttpGet("{partitionKey}/{questionId}")]
        public async Task<IActionResult> GetAnswersRated(string partitionKey, string questionId)
        {
            string message = string.Empty;
            try
            {
                var questionKey = new QuestionKey(partitionKey, questionId);
                var historyService = new HistoryService(dbService);
                var questionService = new QuestionService(dbService);
                QuestionEx questionEx = await questionService.GetQuestion(questionKey);
                var (q, msg) = questionEx;
                if (q != null)
                {
                    var answerService = new AnswerService(dbService);
                    var question = await questionService.SetAnswerTitles(q, answerService);
                    List<AnswerRatedDto> list = await historyService.GetAnswersRated(question);
                    return Ok(new AnswerRatedDtoListEx(list, string.Empty));
                }
                return NotFound(new AnswerRatedDtoListEx(null, msg));



                //HistoryListEx historyListEx = await historyService.GetHistories(questionId);
                //var (historyList, msg) = historyListEx;
                //if (historyList != null)
                //{
                //    List<HistoryDto> historyDtoList = new List<HistoryDto>();
                //    foreach (History history in historyList)
                //    {
                //        historyDtoList.Add(new HistoryDto(history));
                //    }
                //    return Ok(new HistoryDtoListEx(historyDtoList, msg));
                //}
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return Ok(new HistoryDtoEx(message));

        }

        [HttpGet("{questionId}")]
        public async Task<IActionResult> GetHistories(string historyId)
        {
            string message = string.Empty;
            try
            {
                var historyService = new HistoryService(dbService);
                HistoryListEx historyListEx = await historyService.GetHistories(historyId);
                var (historyList, msg) = historyListEx;
                if (historyList != null)
                {
                    List<HistoryDto> historyDtoList = new List<HistoryDto>();
                    foreach(History history in historyList)
                    {
                        historyDtoList.Add(new HistoryDto(history));
                    }
                    return Ok(new HistoryDtoListEx(historyDtoList, msg));
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return Ok(new HistoryDtoEx(message));

        }

        //[HttpGet("{partitionKey}/{id}")]
        //public async Task<IActionResult> GetHistory(string partitionKey, string id)
        //{
        //    try
        //    {
        //        var categoryService = new HistoryService(dbService);
        //        var historyService = new HistoryService(dbService);
        //        HistoryEx historyEx = await historyService.GetHistory(partitionKey, id);
        //        var (history, msg) = historyEx;
        //        if (history == null)
        //            return NotFound(new HistoryDtoEx(historyEx));
        //        HistoryKey categoryKey = new(partitionKey, history.ParentHistory!);
        //        // get category Title
        //        HistoryEx categoryEx = await categoryService.GetHistory(categoryKey);
        //        var (category, message) = categoryEx;
        //        history.HistoryTitle = category != null 
        //            ? category.Title
        //            : "NotFound History";
        //        return Ok(new HistoryDtoEx(historyEx));
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new HistoryDtoEx(ex.Message));
        //    }
        //}


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Post([FromBody] HistoryDto historyDto)
        {
            try
            {
                Console.WriteLine("*********=====>>>>>> historyDto");
                Console.WriteLine(JsonConvert.SerializeObject(historyDto));
                var questionService = new QuestionService(dbService);
                var historyService = new HistoryService(dbService);
                var history = new History(historyDto);
                QuestionEx questionEx = await historyService.CreateHistory(history, questionService);
                Console.WriteLine("*********=====>>>>>> historyEx");
                Console.WriteLine(JsonConvert.SerializeObject(questionEx));
                //var history = historyEx.history;
                // Console.WriteLine("^^^^^^^^^^^ historyEx" + JsonConvert.SerializeObject(historyEx));
                //return Ok(new HistoryDtoEx(historyEx));
                return Ok(new QuestionDtoEx(questionEx));
            }
            catch (Exception ex)
            {
                return BadRequest(new QuestionDtoEx(new QuestionEx(null, ex.Message)));
            }
        }
    }
}
