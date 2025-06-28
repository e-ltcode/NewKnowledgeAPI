using Microsoft.AspNetCore.Mvc;

using Knowledge.Services;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using Newtonsoft.Json;
using NewKnowledgeAPI.A.Groups.Model;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace NewKnowledgeAPI.A.Groups
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class ShortGroupController : ControllerBase
    {
        private readonly IConfiguration Configuration;
        DbService dbService { get; set; }

        public ShortGroupController(IConfiguration configuration)
        {
            dbService = new DbService(configuration);
            dbService.Initialize.Wait();
            Configuration = configuration;
        }

    
        [HttpGet("{partitionKey}/{id}")]
        [ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "partitionKey", "id" })]
        public async Task<IActionResult> GetGroupsUpTheTree(string partitionKey, string Id)
        {
            try
            {
                Console.WriteLine("GetGroupsUpTheTree {0}/{1}", partitionKey, Id);
                var groupService = new GroupService(dbService);
                GroupKey groupKey = new(partitionKey, Id);
                GroupListEx groupListEx = await groupService.GetGroupsUpTheTree(groupKey);
                Console.WriteLine(JsonConvert.SerializeObject(groupListEx));
                GroupDtoListEx groupDtoListEx = new(groupListEx);
                return Ok(groupDtoListEx);
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                Console.WriteLine(msg);
                return BadRequest(new GroupDtoListEx( new GroupListEx(null, msg) ));
            }
        }


        [HttpGet("{partitionKey}/{id}/{hidrate}")]
        [ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "partitionKey", "id" })]

        public async Task<IActionResult> GetGroupHidrated(string partitionKey, string id, bool hidrate)
        {
            // hidrate collections except answers
            try
            {
                GroupKey groupKey = new(partitionKey, id);
                // TODO what does  /partitionKey mean?
                //using (var db = new Db(this.Configuration))
                //{
                    //await db.Initialize;
                    //var group = new Group(db);
                var groupService = new GroupService(dbService);
                GroupEx groupEx = await groupService.GetGroup(groupKey, hidrate, 0, null);
                if (groupEx.group != null)
                {
                    return Ok(new GroupDtoEx(groupEx));
                }
                //}
                return NotFound(new GroupDtoEx(groupEx));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
