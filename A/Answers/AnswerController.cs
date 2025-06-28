using Knowledge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using NewKnowledgeAPI.A.Answers.Model;
using NewKnowledgeAPI.A.Groups;
using NewKnowledgeAPI.A.Groups.Model;
using NewKnowledgeAPI.Q.Questions;
using NewKnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System.Configuration;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace NewKnowledgeAPI.A.Answers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]

    public class AnswerController : ControllerBase
    {
        private readonly IConfiguration Configuration;
        DbService dbService { get; set; }

        public AnswerController(IConfiguration configuration)
        {
            dbService = new DbService(configuration);
            dbService.Initialize.Wait();
            Configuration = configuration;
        }


        [HttpGet("{partitionKey}/{parentGroup}/{startCursor}/{pageSize}/{includeAnswerId}")]
        [ResponseCache(Duration = 10, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "partitionKey", "parentGroup", "startCursor" })]
        public async Task<IActionResult> LoadGroupAnswers(string partitionKey, string parentGroup, int startCursor, int pageSize, string? includeAnswerId)
        {
            string message = string.Empty;
            try
            {
                var groupService = new GroupService(dbService);
                GroupKey groupKey = new GroupKey(partitionKey, parentGroup);
                GroupEx groupEx = await groupService.GetGroup(groupKey);
                var (group, msg) = groupEx;
                if (group != null)
                {
                    var answerService = new AnswerService(dbService);
                    AnswersMore answersMore = await answerService.GetAnswers(parentGroup, startCursor, pageSize, includeAnswerId);
                    Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>> Count {0}", answersMore.AnswerRows.Count);
                    var groupDto = new GroupDto(groupKey, answersMore);
                    groupDto.Title = group.Title;
                    return Ok(new GroupDtoEx(groupDto, msg));
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return Ok(new GroupDtoEx(message));

        }

        [HttpGet("{partitionKey}/{id}")]
        public async Task<IActionResult> GetAnswer(string partitionKey, string id)
        {
            try
            {
                var groupService = new GroupService(dbService);
                var answerService = new AnswerService(dbService);
                AnswerEx answerEx = await answerService.GetAnswer(partitionKey, id);
                var (answer, msg) = answerEx;
                if (answer == null)
                    return NotFound(new AnswerDtoEx(answerEx));
                GroupKey groupKey = new(partitionKey, answer.ParentGroup!);
                // get group Title
                GroupEx groupEx = await groupService.GetGroup(groupKey);
                var (group, message) = groupEx;
                answer.GroupTitle = group != null 
                    ? group.Title
                    : "NotFound Group";
                return Ok(new AnswerDtoEx(answerEx));
            }
            catch (Exception ex)
            {
                return BadRequest(new AnswerDtoEx(ex.Message));
            }
        }

        [HttpGet("{filter}/{count}/{nesto}")]
        [ResponseCache(Duration = 10, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "filter", "count", "nesto" })]
        public async Task<IActionResult> SearchAnswerRows(string filter, int count, string nesto)
        {
            Console.WriteLine("GetShortAnswers", filter, count, nesto);
            try
            {
                var words = filter //.ToLower()
                                .Replace("?", "")
                                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .Where(w => w.Length > 2)
                                .ToList();
                var answerService = new AnswerService(dbService);
                List<AnswerRowDto> answers = await answerService.SearchAnswerRows(words, count);
                return Ok(answers);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

         
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Post([FromBody] AnswerDto answerDto)
        {
            try
            {
                Console.WriteLine("*********=====>>>>>> answerDto");
                Console.WriteLine(JsonConvert.SerializeObject(answerDto));
                var groupService = new GroupService(dbService);
                var answerService = new AnswerService(dbService);

                AnswerEx answerEx = await answerService.CreateAnswer(answerDto);
                Console.WriteLine("*********=====>>>>>> answerEx");
                Console.WriteLine(JsonConvert.SerializeObject(answerEx));
                var (answer, msg) = answerEx;
                if (answer != null)
                {
                    //Group group = new Group(answerEx.answer);
                    answerDto.Modified = answerDto.Created; // to be used for group
                    await groupService.UpdateNumOfAnswers(answerDto, 1);
                }
                // Console.WriteLine("^^^^^^^^^^^ answerEx" + JsonConvert.SerializeObject(answerEx));
                return Ok(new AnswerDtoEx(answerEx));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        [Authorize]
        public async Task<IActionResult> Put([FromBody] AnswerDto answerDto)
        {
            try
            {
                Console.WriteLine("===>>> UpdateAnswer: {0} \n", answerDto.Title);
                var answerService = new AnswerService(dbService);

                AnswerEx answerEx = await answerService.UpdateAnswer(answerDto);
                if (answerEx!.answer != null)
                    return Ok(new AnswerDtoEx(answerEx));
                return NotFound(new AnswerDtoEx(answerEx));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> Delete([FromBody] AnswerDto answerDto) //string PartitionKey, string id)
        {
            try
            {
                Console.WriteLine("===>>> DeleteAnswer: {0}/{1} \n", answerDto.PartitionKey, answerDto.Id);
                var groupService = new GroupService(dbService);
                var answerService = new AnswerService(dbService);
                AnswerEx answerEx = await answerService.DeleteAnswer(answerDto);
                if (answerEx!.answer != null)
                {
                    answerDto.Modified = answerDto.Modified;
                    await groupService.UpdateNumOfAnswers(answerDto, -1);
                    return Ok(new AnswerDtoEx(answerEx));
                }
                return NotFound(answerEx);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
