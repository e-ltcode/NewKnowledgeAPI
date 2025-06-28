using Microsoft.AspNetCore.Mvc;
using Knowledge.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace NewKnowledgeAPI.Admin
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    //[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
    public class AdminController : ControllerBase
    {
        private readonly IConfiguration Configuration;
        DbService dbService { get; set; }

        public AdminController(IConfiguration configuration)
        {
            dbService = new DbService(configuration);
            dbService.Initialize.Wait();
            Configuration = configuration;
        }

    
        [HttpGet]
        public async Task<IActionResult> CreateDatabaseIfNotExistsAsync()
        {
            try
            {
                bool created = await dbService.CreateDatabaseIfNotExistsAsync();
                return Ok(created ? "Created database" : "Database already exists");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}
