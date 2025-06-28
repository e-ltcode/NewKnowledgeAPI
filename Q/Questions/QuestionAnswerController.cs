using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System.Configuration;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos.Serialization.HybridRow.Schemas;
using Knowledge.Services;
using Microsoft.AspNetCore.Authorization;
using NewKnowledgeAPI.Q.Categories.Model;
using NewKnowledgeAPI.Q.Questions.Model;
using NewKnowledgeAPI.A.Answers.Model;
using NewKnowledgeAPI.A.Answers;
using NewKnowledgeAPI.Q.Categories;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace NewKnowledgeAPI.Q.Questions
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]

    public class QuestionAnswerController : ControllerBase
    {
        private readonly IConfiguration Configuration;
        DbService dbService { get; set; }

        public QuestionAnswerController(IConfiguration configuration)
        {
            dbService = new DbService(configuration);
            dbService.Initialize.Wait();
            Configuration = configuration;
        }


        [HttpPost("Assign")]
        [Authorize]
        public async Task<IActionResult> AssignAnswer([FromBody] AssignedAnswerDto assignedAnswerDto)
        {
            try
            {
                Console.WriteLine("*********=====>>>>>>>> ASSIGN >>>>>>>>>>>> assignedAnswerDto");
                Console.WriteLine(JsonConvert.SerializeObject(assignedAnswerDto));

                var questionService = new QuestionService(dbService);

                QuestionEx questionEx = await questionService.AssignAnswer(assignedAnswerDto);
                var (question, msg) = questionEx;
                Console.WriteLine("*********=====>>>>>> questionEx");
                Console.WriteLine(JsonConvert.SerializeObject(questionEx));

                if (question != null)
                {
                    var categoryService = new CategoryService(dbService);
                    var answerService = new AnswerService(dbService);
                    Question q = await questionService.SetAnswerTitles(question, categoryService, answerService);
                    return Ok(new QuestionDtoEx(new QuestionEx(q, "")));
                }

                return NotFound(new QuestionDtoEx(questionEx));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost("UnAssign")]
        [Authorize]
        public async Task<IActionResult> UnAssignAnswer([FromBody] AssignedAnswerDto assignedAnswerDto)
        {
            try
            {
                Console.WriteLine("*********=====>>>>> UNASSIGN >>>>>>>>>>>>>>> assignedAnswerDto");
                Console.WriteLine(JsonConvert.SerializeObject(assignedAnswerDto));

                var questionService = new QuestionService(dbService);
                QuestionEx questionEx = await questionService.UnAssignAnswer(assignedAnswerDto);
                Console.WriteLine("*********=====>>>>>> questionEx");
                Console.WriteLine(JsonConvert.SerializeObject(questionEx));
                var (question, msg) = questionEx;
                if (question != null)
                {
                    var categoryService = new CategoryService(dbService);
                    var answerService = new AnswerService(dbService);
                    var q = await questionService.SetAnswerTitles(question, categoryService, answerService);
                    return Ok(new QuestionDtoEx(new QuestionEx(q, "")));

                }
                return NotFound(new QuestionDtoEx(questionEx));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
