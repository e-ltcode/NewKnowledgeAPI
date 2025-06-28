using Microsoft.AspNetCore.Mvc;
using Knowledge.Services;
using Microsoft.AspNetCore.Authorization;
using NewKnowledgeAPI.A.Groups.Model;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace NewKnowledgeAPI.A.Groups
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    //[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]

    public class GroupController : ControllerBase
    {
        private readonly IConfiguration Configuration;
        DbService dbService { get; set; }

        public GroupController(IConfiguration configuration)
        {
            dbService = new DbService(configuration);
            dbService.Initialize.Wait();
            Configuration = configuration;
        }

        // GET api/<FamilyController>
        [HttpGet]
        [ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any)] //, VaryByQueryKeys = new[] { "impactlevel", "pii" })]
        public async Task<IActionResult> GetAllGroups()
        {
            try
            {
                Console.WriteLine("GetAllGroups");
                //using (var db = new Db(this.Configuration))
                //{
                //    await db.Initialize;
                //var group = new Group(_Db);
                //List<Group> subGroups = await group.GetAllGroups();
                var groupService = new GroupService(dbService);
                List<Group> subGroups = await groupService.GetAllGroups();
                if (subGroups != null)
                {
                    List<GroupDto> list = [];
                    foreach (Group group in subGroups)
                    {
                        list.Add(new GroupDto(group));
                    }
                    return Ok(list);
                }
                //}
                return NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{partitionKey}/{id}")]
        [ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "partitionKey", "id" })]
        public async Task<IActionResult> GetSubGroups(string partitionKey, string id)
        {
            try
            {
                Console.WriteLine("GetSubGroups {0}/{1}", partitionKey, id); 
                //using (var db = new Db(this.Configuration))
                //{
                //    await db.Initialize;
                //var group = new Group(_Db);
                //List<Group> subGroups = await group.GetSubGroups(partitionKey, parentGroup);
                var groupService = new GroupService(dbService);
                List<Group> subGroups = await groupService.GetSubGroups(partitionKey, id);
                if (subGroups != null)
                {
                    List<GroupDto> list = [];
                    foreach (Group group in subGroups)
                    {
                        list.Add(new GroupDto(group));
                    }
                    return Ok(list);
                }
                //}
                return NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet("{partitionKey}/{id}/{pageSize}/{includeAnswerId}")]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "partitionKey", "id", "pageSize", "includeAnswerId" })]
        public async Task<IActionResult> GetGroup(string partitionKey, string id, int pageSize, string includeAnswerId)
        {
            try
            {
                GroupKey groupKey = new (partitionKey, id);
                Console.WriteLine("GetGroup: {0}, {1}, {2}, {3} \n", partitionKey, id, pageSize, includeAnswerId);

                // TODO 1. ovo 2. what does  /partitionKey mean?
                //using(var db = new Db(this.Configuration))
                //{
                //    await db.Initialize;
                // TODO Answer.Db = db;
                //var group = new Group(_Db);
                // var container = await Db.GetContainer(this.containerId);
                //Group group = await group.GetGroup(
                //    partitionKey, id, true, pageSize, includeAnswerId=="null" ? null : includeAnswerId);
                var groupService = new GroupService(dbService);
                GroupEx groupEx = await groupService.GetGroup(
                       groupKey, true, pageSize, includeAnswerId == "null" ? null : includeAnswerId);
                if (groupEx.group != null)
                {
                    return Ok(new GroupDtoEx(groupEx));
                }
                return NotFound(new GroupDtoEx(groupEx));
            }
            catch (Exception ex)
            {
                return BadRequest(new GroupDtoEx(ex.Message));
            }
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Post([FromBody] GroupDto groupDto)
        {
            try
            {
                Console.WriteLine("===>>> CreateGroup: {0} \n", groupDto.Title);
                var groupService = new GroupService(dbService);
                if (groupDto.PartitionKey == "null")
                {
                    groupDto.PartitionKey = groupDto.Id;
                }
                GroupEx groupEx = await groupService.CreateGroup(groupDto);
                return Ok(new GroupDtoEx(groupEx));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        [Authorize]
        public async Task<IActionResult> Put([FromBody] GroupDto groupDto)
        {
            try
            {
                Console.WriteLine("===>>> UpdateGroup: {0} \n", groupDto.Title);
                var groupService = new GroupService(dbService);
                GroupEx groupEx = await groupService.UpdateGroup(groupDto);
                if (groupEx != null)
                {
                    return Ok(new GroupDtoEx(groupEx));
                }
                return NotFound(new GroupDtoEx(groupEx));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //[HttpDelete("{partitionKey}, {id}")]
        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> Delete([FromBody] GroupDto groupDto) //string PartitionKey, string id)
        {
            try
            {
                Console.WriteLine("===>>> DeleteGroup: {0}/{1} \n", groupDto.PartitionKey, groupDto.Id);
                var groupService = new GroupService(dbService);
                GroupEx groupEx = await groupService.DeleteGroup(groupDto);

                if (groupEx.group != null)
                {
                    return Ok(new GroupDtoEx(groupEx));
                }
                return NotFound(groupEx);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
