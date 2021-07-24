using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace JanGet.ApiClient
{
    public class JanGetClient
    {
        public string Url { get; set; }
        private string _token { get; }
        public HttpClient Http { get; set; }

        public JanGetClient(string url, string token = null)
        {
            (Url, Http, _token) = (url, new HttpClient(), token);
            Http.DefaultRequestHeaders.UserAgent.Add(new("SussyBaka", "0.1"));
            Http.DefaultRequestHeaders.Add("JanGet-Token", token);
        }

        public async Task<Package> GetPackageAsync(string name) =>
            JsonSerializer.Deserialize<Package>(await Http.GetStringAsync(Url + "package/" + name.ToLower()),
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

        public Task UpdateResourceAsync(UpdateResourceRequest request)
            => Http.PostAsJsonAsync(Url + "updateResource", request);

        public async Task<string> UploadFileAsync(string name, string path)
        {
            var req = await Http.PostAsync(Url + "uploadFile/" + name,
                new StreamContent(new FileStream(path, FileMode.Open, FileAccess.Read)));
            return await req.Content.ReadAsStringAsync();
        }
    }

    public class UpdateResourceRequest : Resource
    {
        public string Name { get; set; }
    }
}