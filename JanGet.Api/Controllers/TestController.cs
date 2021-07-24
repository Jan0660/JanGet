using System;
using System.Threading.Tasks;
using JanGet.ApiClient;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace JanGet.Api.Controllers
{
    public class TestController : Controller
    {
        [HttpGet("/package/{name}")]
        [ProducesResponseType(typeof(Package), 200)]
        public async Task<IActionResult> GetPackage(string name)
            => Json(await Mongo.PackageCollection.Find(pkg => pkg.Name == name).FirstOrDefaultAsync());
    }
}