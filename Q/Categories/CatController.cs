using Microsoft.AspNetCore.Mvc;

using Knowledge.Services;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using Newtonsoft.Json;
using NewKnowledgeAPI.Q.Categories.Model;



// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace NewKnowledgeAPI.Q.Categories
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class CatController : ControllerBase
    {
        private readonly IConfiguration Configuration;
        DbService dbService { get; set; }

        public CatController(IConfiguration configuration)
        {
            dbService = new DbService(configuration);
            dbService.Initialize.Wait();
            Configuration = configuration;
        }

    
        [HttpGet("{partitionKey}/{id}")]
        [ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "partitionKey", "id" })]
        public async Task<IActionResult> GetCatsUpTheTree(string partitionKey, string id)
        {
            try
            {
                Console.WriteLine("GetCatsUpTheTree {0}/{1}", partitionKey, id);
                var categoryService = new CategoryService(dbService);
                var categoryKey = new CategoryKey(partitionKey, id);
                CategoryEx categoryEx = await categoryService.GetCatsUpTheTree(categoryKey);
                Console.WriteLine(JsonConvert.SerializeObject(categoryEx));
                var categoryDtoEx = new CategoryDtoEx(categoryEx);
                return Ok(categoryDtoEx);
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                Console.WriteLine(msg);
                return BadRequest(new CategoryDtoListEx(new CategoryListEx(null, msg)));
            }
        }


        [HttpGet("{partitionKey}/{id}/{hidrate}")]
        //[ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "partitionKey", "id" })]
        public async Task<IActionResult> GetCategoryHidrated(string partitionKey, string id, bool hidrate)
        {
            // hidrate collections except questions
            try
            {
                CategoryKey categoryKey = new(partitionKey, id);
                // TODO what does  /partitionKey mean?
                //using (var db = new Db(this.Configuration))
                //{
                    //await db.Initialize;
                    //var category = new Category(db);
                var categoryService = new CategoryService(dbService);
                CategoryEx categoryEx = await categoryService.GetCategory(categoryKey, hidrate, 0, null);
                if (categoryEx.category != null)
                {
                    return Ok(new CategoryDtoEx(categoryEx));
                }
                //}
                return NotFound(new CategoryDtoEx(categoryEx));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
