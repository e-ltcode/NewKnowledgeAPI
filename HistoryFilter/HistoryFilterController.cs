using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System.Configuration;
using Newtonsoft.Json;
using Knowledge.Services;
using Microsoft.AspNetCore.Authorization;
using NewKnowledgeAPI.History.Model;
using NewKnowledgeAPI.Q.Categories.Model;
using NewKnowledgeAPI.A.Answers.Model;
using NewKnowledgeAPI.Q.Questions.Model;
using NewKnowledgeAPI.Q.Questions;
using System.Collections.Generic;
using NewKnowledgeAPI.A.Answers;
using NewKnowledgeAPI.HistoryFilter.Model;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace NewKnowledgeAPI.HistoryFilter
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]

    public class HistoryFilterController : ControllerBase
    {
        private readonly IConfiguration Configuration;
        DbService dbService { get; set; }

        public HistoryFilterController(IConfiguration configuration)
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
                var historyService = new HistoryFilterService(dbService);
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
        public async Task<IActionResult> Post([FromBody] HistoryFilterDto dto)
        {
            try
            {
                Console.WriteLine("*********=====>>>>>> historyFilterDto");
                Console.WriteLine(JsonConvert.SerializeObject(dto));
                var questionService = new QuestionService(dbService);
                var historyFilterService = new HistoryFilterService(dbService);
                var historyFilter = new HistoryFilter(dto);
                QuestionEx questionEx = await historyFilterService.CreateHistoryFilter(historyFilter, questionService);
                return Ok(new QuestionDtoEx(questionEx));
            }
            catch (Exception ex)
            {
                return BadRequest(new QuestionDtoEx(new QuestionEx(null, ex.Message)));
            }
        }
    }
}
