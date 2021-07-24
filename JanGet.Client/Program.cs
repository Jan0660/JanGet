using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Console = Log73.Console;
using JanGet.ApiClient;
using JanGet.Client;
using Log73;
using Log73.Extensions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using File = System.IO.File;

MessageTypes.Info.Name = null;
Console.Options.UseBrackets = false;
MessageTypes.Warn = new()
{
    Name = "==> ",
    LogType = LogType.Warn,
    WriteToStdErr = true,
    Style = new()
    {
        Bold = true,
        Color = Color.White
    },
    ContentStyle = new()
    {
        Bold = true,
        Color = Color.White
    }
};
MessageTypes.Error = new()
{
    Name = "==> ERROR:",
    LogType = LogType.Error,
    WriteToStdErr = true,
    Style = new()
    {
        Bold = true,
        Color = Color.Red
    },
    ContentStyle = new()
    {
        Bold = true,
        Color = Color.White
    }
};

async Task<Process> Execute(ProcessStartInfo startInfo)
{
    Console.Warn($"Executing {startInfo.FileName} {startInfo.Arguments}");
    var process = Process.Start(startInfo);
    await process!.WaitForExitAsync();
    return process;
}

var config = File.Exists("/etc/janget.json")
    ? await JsonSerializer.DeserializeAsync<Config>(new FileStream("/etc/janget.json", FileMode.Open, FileAccess.Read))
    : new Config();
var client = new JanGetClient(config.Url, config!.Token);
switch (args[0])
{
    // get package info
    case "-I":
    {
        var pkg = await client.GetPackageAsync(args[1]);
        Console.Object.Yaml(pkg);
        break;
    }
    // install from repos
    case "-S":
    {
        Console.Warn($"Getting {args[1].ToLower()}...");
        var pkg = await client.GetPackageAsync(args[1]);
        if (pkg == null)
        {
            Console.Error("Package not found.");
            return 1;
        }

        // the resource acceptable for the current system
        var acceptable = new List<Resource>();
        foreach (var res in pkg.Resources)
            if (res.Platform == "archlinux-x64")
                acceptable.Add(res);
        if (acceptable.Count == 0)
        {
            Console.Error("No acceptable resources for the package found!");
            return 1;
        }

        Resource resource;
        if (acceptable.Count > 1)
        {
            Console.Warn("Multiple acceptable resources:");
            for (var i = 0; i < acceptable.Count; i++)
            {
                var res = acceptable[i];
                Console.Warn($" {i}:  Platform: {res.Platform}; Type: {res.Type};");
            }
            Console.Write("Choose one: ");
            // todo: out of index and failed to parse msgs
            var num = int.Parse(Console.ReadLine());
            resource = acceptable[num];
        }
        else
            resource = acceptable.First();
        var dir = $"/home/{Environment.UserName}/.cache/janget/" + pkg.Name;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        if (resource.Type == "pkgbuild-git")
        {
            Console.Info($"Installing {pkg.Name}...");
            await Execute(new("git", $"clone {resource.GitUrl} .")
            {
                WorkingDirectory = dir
            });
            await Execute(new("paru", "-Ui")
            {
                WorkingDirectory = dir
            });
        }
        else if (resource.Type == "archpkg")
        {
            Console.Warn("Downloading files...");

            Task DownloadFile(string name)
                => new WebClient().DownloadFileTaskAsync(client.Url + "files/" + name, dir + "/" + name);

            Task.WaitAll(DownloadFile(resource.File), DownloadFile(resource.SigFile));
            Console.Warn("Installing...");
            await Execute(new("paru", $"-U {resource.File}")
            {
                WorkingDirectory = dir
            });
        }

        break;
    }
    // build from current dir
    case "-B":
    {
        var buildFile = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build()
            .Deserialize<BuildFile>(new StreamReader("./janget-build.yaml"));
        Console.Warn($"Building {buildFile.Resources.Count} resources in {buildFile.Name}...");
        foreach (var resource in buildFile.Resources)
        {
            Console.Warn($"Building: platform: {resource.Platform}; type: {resource.Type};");
            // TMEP TEST CODE !!!! ! ! ! ! ! ! !! !!!!?!?!??!!!!!
            // await client.UploadFileAsync("imposter.sus",
            //     "/home/jan/amogus/Screenshot_20210720_213937_com.reddit.frontpage.png");
            switch (resource.Type)
            {
                case "archpkg":
                {
                    Console.Warn("Removing existing builds...");
                    await Process.Start("sh", "-c \"rm *.pkg.tar.zst\"")!.WaitForExitAsync();
                    foreach (var cmd in resource.Build)
                    {
                        Console.Warn($"Executing {cmd}");
                        await Execute(new("sh", $"-c \"{cmd.Replace("\\", "\\\\").Replace("\"", "\\\"")}\""));
                    }

                    // upload the .pkg.tar.zst
                    var pkgFile = Directory.GetFiles("./", "*.pkg.tar.zst").FirstOrDefault();
                    Console.Warn(pkgFile);
                    var fileId = await client.UploadFileAsync(pkgFile, pkgFile);
                    var sigFileId = await client.UploadFileAsync(pkgFile + ".sig", pkgFile + ".sig");
                    await client.UpdateResourceAsync(new()
                    {
                        Name = buildFile.Name,
                        Platform = resource.Platform,
                        Type = resource.Type,
                        GitUrl = resource.GitUrl,
                        File = fileId,
                        SigFile = sigFileId
                    });
                    break;
                }
                case "pkgbuild-git":
                {
                    await client.UpdateResourceAsync(new()
                    {
                        Name = buildFile.Name,
                        Platform = resource.Platform,
                        Type = resource.Type,
                        GitUrl = resource.GitUrl
                    });
                    break;
                }
                default:
                {
                    Console.Error($"Invalid type: {resource.Type}");
                    return 1;
                }
            }
        }

        break;
    }
}

return 0;

public class Config
{
    public string Token { get; set; } = "r";
    public string Url { get; set; } = "https://janget.jan0660.dev/";
}