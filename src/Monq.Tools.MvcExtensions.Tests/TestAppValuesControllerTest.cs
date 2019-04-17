using Monq.Tools.MvcExtensions.TestApp.ViewModels;
using Microsoft.AspNetCore.TestHost;
using System.Text;
using Xunit;
using Monq.Tools.MvcExtensions.TestApp;
using Microsoft.AspNetCore.Hosting;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Monq.Tools.MvcExtensions.Tests
{
    public class TestAppValuesControllerTest
    {
        readonly TestServer _server;
        readonly HttpClient _client;
        const string route = "/api/values";
        const string mediaType = "application/json";

        public TestAppValuesControllerTest()
        {
            _server = new TestServer(new WebHostBuilder().UseStartup<Startup>());
            _client = _server.CreateClient();
        }

        [Fact(DisplayName = "Модель данных правильно завалидированна.")]
        public async Task ShouldProperlyValidateModel()
        {
            var model = new ValueViewModel()
            {
                Id = 10,
                Capacity = -1
            };
            var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, mediaType);

            HttpResponseMessage response = await _client.PostAsync(route, content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            string responseText = await response.Content.ReadAsStringAsync();
            Assert.Contains("Неверная модель данных в теле запроса.", responseText);
            Assert.Contains("bodyFields", responseText);
        }

        [Fact(DisplayName = "Определена null модель.")]
        public async Task ShouldProperlyDetectNullModel()
        {
            var content = new StringContent("", Encoding.UTF8, mediaType);
            HttpResponseMessage response = await _client.PostAsync(route, content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            string responseText = await response.Content.ReadAsStringAsync();
            Assert.Contains("Неверная модель данных в теле запроса.", responseText);
        }

        [Fact(DisplayName = "Определена неправильно переданная модель данных.")]
        public async Task ShouldProperlyHandleUnmappedModel()
        {
            var wrongModel = new string[] { "value1", "value2" };
            var content = new StringContent(JsonConvert.SerializeObject(wrongModel), Encoding.UTF8, mediaType);

            HttpResponseMessage response = await _client.PostAsync(route, content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            string responseText = await response.Content.ReadAsStringAsync();
            Assert.Contains("Неверная модель данных в теле запроса.", responseText);
        }

        [Fact(DisplayName = "Определена неправильно переданная Patch модель данных.")]
        public async Task ShouldProperlyHandleUnmappedPatchModel()
        {
            var wrongModel = new string[] { "value1", "value2" };
            var id = 10;
            var content = new StringContent(JsonConvert.SerializeObject(wrongModel), Encoding.UTF8, mediaType);

            HttpResponseMessage response = await PatchAsync(_client, $"{route}/{id}", content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            string responseText = await response.Content.ReadAsStringAsync();
            Assert.Contains("Неверная модель данных в теле запроса.", responseText);
        }

        [Fact(DisplayName = "Определена null Patch модель данных.")]
        public async Task ShouldProperlyDetectNullPatchViewModel()
        {
            var content = new StringContent("", Encoding.UTF8, mediaType);
            var id = 10;
            HttpResponseMessage response = await PatchAsync(_client, $"{route}/{id}", content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            string responseText = await response.Content.ReadAsStringAsync();
            Assert.Contains("Неверная модель данных в теле запроса.", responseText);
        }

        [Fact(DisplayName = "Правильно завалидирована пустая Patch модель данных.")]
        public async Task ShouldProperlyValidateEmptyPatchModel()
        {
            var model = new ValuePatchViewModel()
            {
                Id = null,
                Capacity = null,
                Name = null
            };
            var id = 10;
            var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, mediaType);

            HttpResponseMessage response = await PatchAsync(_client, $"{route}/{id}", content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            string responseText = await response.Content.ReadAsStringAsync();
            Assert.Contains("Все поля в модели данных пустые.", responseText);
        }

        [Fact(DisplayName = "Правильно завалидирована Patch модель данных.")]
        public async Task ShouldProperlyValidatePatchModel()
        {
            var model = new ValuePatchViewModel()
            {
                Id = 10,
                Capacity = null,
                Name = ""
            };
            var id = 10;
            var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, mediaType);

            HttpResponseMessage response = await PatchAsync(_client, $"{route}/{id}", content);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(id.ToString(), responseText);
        }

        [Fact(DisplayName = "Правильно завалидирована простая модель данных FromBody.")]
        public async Task ShouldProperlyValidateSimpleFromBodyModel()
        {
            long model = 125;
            var content = new StringContent(model.ToString(), Encoding.UTF8, mediaType);

            HttpResponseMessage response = await _client.PostAsync($"{route}/body", content);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(model.ToString(), responseText);
        }

        async Task<HttpResponseMessage> PatchAsync(HttpClient client, string requestUri, HttpContent content)
        {
            var method = new HttpMethod("PATCH");
            var request = new HttpRequestMessage(method, requestUri)
            {
                Content = content
            };
            try
            {
                return await client.SendAsync(request);
            }
            catch
            {
                throw;
            }
        }

        [Fact(DisplayName = "Правильно рекурсивно завалидирована модель данных FromBody.")]
        public async Task ShouldProperlyValidateModelRecursive()
        {
            var model = new RecursiveViewModel
            {
                Id = 1,
                Name = "TestName",
                SubCollection = new List<SubViewModel>
                {
                    new SubViewModel
                    {
                        Id = 2,
                        Name = "SubItem"
                    }
                },
                SubObject = new SubObjectViewModel
                {
                    Id = 3,
                    Capacity = 150
                }
            };
            var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, mediaType);
            HttpResponseMessage response = await _client.PostAsync($"{route}/recursive", content);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            string responseText = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RecursiveViewModel>(responseText);
            Assert.NotNull(result);
        }

        [Fact(DisplayName = "Правильно рекурсивно завалидирована ошибочная модель данных FromBody.")]
        public async Task ShouldProperlyValidateWrongModelRecursive()
        {
            var model = new RecursiveViewModel
            {
                Id = 15,
                Name = "",
                SubCollection = new List<SubViewModel>
                {
                    new SubViewModel
                    {
                        Id = -2,
                        Name = "Name"
                    }
                },
                SubObject = new SubObjectViewModel
                {
                    Id = 3,
                    Capacity = -15
                }
            };
            var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, mediaType);
            HttpResponseMessage response = await _client.PostAsync($"{route}/recursive", content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            string responseText = await response.Content.ReadAsStringAsync();
            Assert.Contains("Name", responseText);
            Assert.Contains("SubCollection[0].Id", responseText);
            Assert.Contains("SubObject.Capacity", responseText);
        }
    }
}
