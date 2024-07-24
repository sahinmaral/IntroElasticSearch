

using Elasticsearch.Net;

using IntroElasticSearch.Context;
using IntroElasticSearch.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Newtonsoft.Json.Linq;

using System.Diagnostics;
using System.Text.Json;

namespace IntroElasticSearch.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public HomeController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("syncElastic")]
        public async Task<IActionResult> SyncToElastic()
        {
            var settings = new ConnectionConfiguration(new Uri("http://localhost:9200"));
            var client = new ElasticLowLevelClient(settings);

            var travels = await _dbContext.Travels.ToListAsync();

            var tasks = new List<Task>();

            travels.ForEach((travel) =>
            {
                tasks.Add(client.IndexAsync<StringResponse>(
                    "travels",
                    travel.Id.ToString(),
                    PostData.Serializable(
                        new
                        {
                            travel.Id,
                            travel.Title,
                            travel.Description
                        }
                        )));
            });

            await Task.WhenAll(tasks);

            return Ok();
        }

        [HttpGet("getAll")]
        public async Task<IActionResult> GetAll()
        {
            var travels = await _dbContext.Travels.ToListAsync();

            return Ok(travels.Take(10));
        }

        [HttpGet("getAll/description/entityFramework/{description}")]
        public async Task<IActionResult> GetAllByDescriptionLikeEntityFramework(string description)
        {
            var travels = await _dbContext.Travels.Where(x => x.Description.Contains(description)).ToListAsync();

            return Ok(travels.Take(10));
        }

        [HttpGet("getAll/description/elasticSearch/{description}")]
        public async Task<IActionResult> GetAllByDescriptionLikeElasticSearch(string description)
        {
            var settings = new ConnectionConfiguration(new Uri("http://localhost:9200"));
            var client = new ElasticLowLevelClient(settings);
            var response = await client.SearchAsync<StringResponse>("travels", PostData.Serializable(new
            {
                query = new
                {
                    wildcard = new
                    {
                        Description = new {value = $"*{description}*"}
                    }
                }
            }));

            var results = JObject.Parse(response.Body);
            var hits = results["hits"]["hits"].ToObject<List<JObject>>();
            List<Travel> travels = new();

            hits.ForEach((hit) =>
            {
                travels.Add(hit["_source"].ToObject<Travel>());
            });

            return Ok(travels.Take(10));
        }

        [HttpPost("seedData")]
        public async Task<IActionResult> SeedData(CancellationToken cancellationToken)
        {
            var travels = new List<Travel>();

            var random = new Random();
            for (int i = 0; i < 10000; i++)
            {
                var newCharArr = Enumerable.Repeat("abcdefgğhıijklmnoöprsştuüvyz", 5).Select(s => s[random.Next(s.Length)]).ToArray();
                var title = new string(newCharArr);

                var words = new List<string>();

                for (int j = 0; j < 500; j++)
                {
                    words.Add(new string(newCharArr));
                }

                var description = string.Join(" ", words);

                var travel = new Travel
                {
                    Title = title,
                    Description = description
                };

                travels.Add(travel);
            }

            await _dbContext.Set<Travel>().AddRangeAsync(travels);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok();
        }
    }
}
