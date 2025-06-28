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
using NewKnowledgeAPI.Q.Categories;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace NewKnowledgeAPI.Q.Questions
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]

    public class QuestionFilterController : ControllerBase
    {
        private readonly IConfiguration Configuration;
        DbService dbService { get; set; }

        public QuestionFilterController(IConfiguration configuration)
        {
            dbService = new DbService(configuration);
            dbService.Initialize.Wait();
            Configuration = configuration;
        }


        [HttpPost("Assign")]
        [Authorize]
        public async Task<IActionResult> AssignFilter([FromBody] RelatedFilterDto relatedFiltersDto)
        {
            try
            {
                Console.WriteLine("*********=====>>>>>>>> ASSIGN >>>>>>>>>>>> relatedFiltersDto");
                Console.WriteLine(JsonConvert.SerializeObject(relatedFiltersDto));

                var questionService = new QuestionService(dbService);

                QuestionEx questionEx = await questionService.AssignFilter(relatedFiltersDto);
                var (question, msg) = questionEx;
                Console.WriteLine("*********=====>>>>>> questionEx");
                Console.WriteLine(JsonConvert.SerializeObject(questionEx));

                if (question != null)
                {
                //    var categoryService = new CategoryService(dbService);
                //    var filterService = new FilterService(dbService);
                //    Question q = await questionService.SetFilterTitles(question, categoryService, filterService);
                    return Ok(new QuestionDtoEx(new QuestionEx(question, "")));
                }
                return NotFound(new QuestionDtoEx(questionEx));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        //[HttpPost("UnAssign")]
        //[Authorize]
        //public async Task<IActionResult> UnAssignFilter([FromBody] RelatedFilterDto relatedFiltersDto)
        //{
        //    try
        //    {
        //        Console.WriteLine("*********=====>>>>> UNASSIGN >>>>>>>>>>>>>>> relatedFiltersDto");
        //        Console.WriteLine(JsonConvert.SerializeObject(relatedFiltersDto));

        //        var questionService = new QuestionService(dbService);
        //        QuestionEx questionEx = await questionService.UnAssignFilter(relatedFiltersDto);
        //        Console.WriteLine("*********=====>>>>>> questionEx");
        //        Console.WriteLine(JsonConvert.SerializeObject(questionEx));
        //        var (question, msg) = questionEx;
        //        if (question != null)
        //        {
        //        //    var categoryService = new CategoryService(dbService);
        //        //    var filterService = new FilterService(dbService);
        //        //    var q = await questionService.SetFilterTitles(question, categoryService, filterService);
        //            return Ok(new QuestionDtoEx(new QuestionEx(q, "")));
        //        }
        //        return NotFound(new QuestionDtoEx(questionEx));

        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //}
    }
}
