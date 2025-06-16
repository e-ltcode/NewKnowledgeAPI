using Knowledge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewKnowledgeAPI.Common;
using NewKnowledgeAPI.Q.Categories.Model;
using NewKnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System.Collections.Generic;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace NewKnowledgeAPI.Q.Categories
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    //[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
    public class CategoryController : ControllerBase
    {
        private readonly IConfiguration Configuration;
        DbService dbService { get; set; }

        public CategoryController(IConfiguration configuration)
        {
            dbService = new DbService(configuration);
            dbService.Initialize.Wait();
            Configuration = configuration;
        }

       
        [HttpGet("{partitionKey}/{id}/{pageSize}/{includeQuestionId}")]
        //[ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "partitionKey", "id", "pageSize", "includeQuestionId" })]
        public async Task<IActionResult> GetCategory(string partitionKey, string id, int pageSize, string? includeQuestionId)
        {
            try
            {
                CategoryKey categoryKey = new (partitionKey, id);
                Console.WriteLine("GetCategory: {0}, {1}, {2}, {3} \n", partitionKey, id, pageSize, includeQuestionId);

                //using(var db = new Db(this.Configuration))
                //{
                //    await db.Initialize;
                // TODO Question.Db = db;
                //var category = new Category(_Db);
                // var container = await Db.GetContainer(this.containerId);
                //Category cat = await category.GetCategory(
                //    partitionKey, id, true, pageSize, includeQuestionId=="null" ? null : includeQuestionId);
                var categoryService = new CategoryService(dbService);
                CategoryEx categoryEx = await categoryService.GetCategoryHidrated(
                       categoryKey, 
                       pageSize, 
                       includeQuestionId //== "null" ? null : includeQuestionId
                );
                if (categoryEx.category != null)
                {
                    return Ok(new CategoryDtoEx(categoryEx));
                }
                return NotFound(new CategoryDtoEx(categoryEx));
            }
            catch (Exception ex)
            {
                return BadRequest(new CategoryDtoEx(ex.Message));
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
                CategoryEx categoryEx = await categoryService.GetCategoryHidrated(categoryKey, 0, null);
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

        /*
        [HttpGet("{partitionKey}/{id}")]
        //[ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "partitionKey", "id" })]
        public async Task<IActionResult> GetCategoryWithSubCategories(string partitionKey, string id)
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
                CategoryEx categoryEx = await categoryService.GetCategoryWithSubCategories(categoryKey);
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
        */

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Post([FromBody] CategoryDto categoryDto)
        {
            try
            {
                Console.WriteLine("===>>> CreateCategory: {0} \n", categoryDto.Title);
                var categoryService = new CategoryService(dbService);
                if (categoryDto.PartitionKey == "null")
                {
                    categoryDto.PartitionKey = categoryDto.Id;
                }
                CategoryEx categoryEx = await categoryService.CreateCategory(categoryDto);
                return Ok(new CategoryDtoEx(categoryEx));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        [Authorize]
        public async Task<IActionResult> Put([FromBody] CategoryDto categoryDto)
        {
            try
            {
                Console.WriteLine("===>>> UpdateCategory: {0} \n", categoryDto.Title);
                var categoryService = new CategoryService(dbService);
                CategoryEx categoryEx = await categoryService.UpdateCategory(categoryDto);
                var (category, msg) = categoryEx;
                if (category != null)
                {
                    if (category.ParentCategory != categoryDto.ParentCategory)
                    {
                        // TODO we need to update category questions, too 
                        // category changed 
                        /*
                        await categoryService.UpdateHasSubCategories(
                            new CategoryKey(categoryDto.PartitionKey, categoryDto.ParentCategory!),
                            new WhoWhen(categoryDto.Modified!));
                        await categoryService.UpdateNumOfQuestions(
                            new CategoryKey(category.PartitionKey, category.ParentCategory!),
                            category.Modified!,
                            1);
                        */
                    }
                    return Ok(new CategoryDtoEx(categoryEx));
                }
                return NotFound(new CategoryDtoEx("Jok Found"));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //[HttpDelete("{partitionKey}, {id}")]
        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> Delete([FromBody] CategoryDto categoryDto) //string PartitionKey, string id)
        {
            try
            {
                Console.WriteLine("===>>> DeleteCategory: {0}/{1} \n", categoryDto.PartitionKey, categoryDto.Id);
                var categoryService = new CategoryService(dbService);
                CategoryEx categoryEx = await categoryService.DeleteCategory(categoryDto);
                if (categoryEx.category != null)
                {
                    return Ok(new CategoryDtoEx(categoryEx));
                }
                return NotFound(categoryEx);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
