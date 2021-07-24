using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JanGet.ApiClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using File = JanGet.ApiClient.File;

namespace JanGet.Api.Controllers
{
    [Authorize(Policy = "JanGet")]
    public class AuthedController : Controller
    {
        [HttpGet("/createSample")]
        public IActionResult CreateSample()
        {
            Mongo.PackageCollection.InsertOne(new Package()
            {
                Name = "jand",
                Resources = new()
                {
                    new()
                    {
                        Platform = "archlinux-x64",
                        Type = "pkgbuild-git",
                        GitUrl = "https://aur.archlinux.org/jand-git.git"
                    }
                }
            });
            return Ok();
        }

        [HttpPost("/createPackage")]
        public async Task<IActionResult> CreatePackage([FromBody] Package pkg)
        {
            pkg.Name = pkg.Name.ToLower();
            await Mongo.UpdatePackageAsync(pkg);
            return Ok();
        }

        [HttpGet("/newPackage/{name}")]
        public async Task<Package> NewPackage(string name)
        {
            var pkg = new Package(name);
            await Mongo.PackageCollection.InsertOneAsync(pkg);
            return pkg;
        }

        [HttpPost("/updateResource")]
        public async Task<IActionResult> UpdateResource([FromBody] UpdateResourceRequest request)
        {
            var pkg = await Mongo.PackageCollection.Find(pkg => pkg.Name == request.Name).FirstOrDefaultAsync() ??
                      await NewPackage(request.Name);
            var matchIndex = -1;
            for (var i = 0; i < pkg.Resources.Count; i++)
            {
                var res = pkg.Resources[i];
                if (res.Platform == request.Platform && res.Type == request.Type)
                {
                    // match found
                    matchIndex = i;
                }
            }

            // add new resource
            Resource old = null;
            if (matchIndex == -1)
                pkg.Resources.Add(request);
            else
            {
                old = pkg.Resources[matchIndex];
                pkg.Resources[matchIndex] = request;
            }

            if (request.Type == "archpkg")
            {
                if (old != null)
                {
                    // delete old files from fs and db
                    async Task RemoveFile(string name)
                    {
                        var file = await Mongo.FileCollection.Find(file => file.Name == name).FirstAsync();
                        await Mongo.FileCollection.DeleteOneAsync(file => file.Name == name);
                        System.IO.File.Delete("./files/" + file.Name);
                    }

                    Task.WaitAll(RemoveFile(old.File), RemoveFile(old.SigFile));
                }

                var newFile = await Mongo.FileCollection.Find(file => file.Id == request.File).FirstAsync();
                System.IO.File.Move($"./files/{request.File}", $"./files/{newFile.Name}");
                System.IO.File.Move($"./files/{request.SigFile}", $"./files/{newFile.Name}.sig");
                var res = matchIndex == -1 ? request : pkg.Resources[matchIndex];
                res.File = newFile.Name;
                res.SigFile = newFile.Name + ".sig";
                await Process.Start(new ProcessStartInfo("repo-add",
                    $"./{Program.Config.ArchDatabaseName}.db.tar.gz ./{newFile.Name}")
                {
                    WorkingDirectory = "./files"
                })!.WaitForExitAsync();
            }

            await Mongo.UpdatePackageAsync(pkg);

            return Ok();
        }

        [HttpPost("/uploadFile/{name}")]
        public async Task<IActionResult> UploadFile(string name)
        {
            var obj = new File()
            {
                Name = name,
                Size = ulong.Parse(Request.Headers["Content-Length"].First())
            };
            await Mongo.FileCollection.InsertOneAsync(obj);
            var stream = new FileStream($"./files/{obj.Id}", FileMode.CreateNew);
            await Request.BodyReader.CopyToAsync(stream);
            await stream.FlushAsync();
            return Ok(obj.Id);
        }
    }
}