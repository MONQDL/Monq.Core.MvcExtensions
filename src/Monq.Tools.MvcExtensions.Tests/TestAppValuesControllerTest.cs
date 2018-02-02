﻿using Monq.Tools.MvcExtensions.TestApp.ViewModels;
using Microsoft.AspNetCore.TestHost;
using System.Text;
using Xunit;
using Monq.Tools.MvcExtensions.TestApp;
using Microsoft.AspNetCore.Hosting;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net;
using System.Threading.Tasks;

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
        public async void ShouldProperlyValidateModel()
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
        public async void SHouldProperlyDetectNullModel()
        {
            var content = new StringContent("", Encoding.UTF8, mediaType);
            HttpResponseMessage response = await _client.PostAsync(route, content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            string responseText = await response.Content.ReadAsStringAsync();
            Assert.Contains("Неверная модель данных в теле запроса.", responseText);
        }

        [Fact(DisplayName = "Определена неправильно переданная модель данных.")]
        public async void ShouldProperlyHandleUnmappedModel()
        {
            var wrongModel = new string[] { "value1", "value2" };
            var content = new StringContent(JsonConvert.SerializeObject(wrongModel), Encoding.UTF8, mediaType);

            HttpResponseMessage response = await _client.PostAsync(route, content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            string responseText = await response.Content.ReadAsStringAsync();
            Assert.Contains("Неверная модель данных в теле запроса.", responseText);
        }

        [Fact(DisplayName = "Определена неправильно переданная Patch модель данных.")]
        public async void ShouldProperlyHandleUnmappedPatchModel()
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
        public async void SHouldProperlyDetectNullPatchViewModel()
        {
            var content = new StringContent("", Encoding.UTF8, mediaType);
            var id = 10;
            HttpResponseMessage response = await PatchAsync(_client, $"{route}/{id}", content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            string responseText = await response.Content.ReadAsStringAsync();
            Assert.Contains("Неверная модель данных в теле запроса.", responseText);
        }

        [Fact(DisplayName = "Правильно завалидирована пустая Patch модель данных.")]
        public async void ShouldProperlyValidateEmptyPatchModel()
        {
            var model = new ValuePatchViewModel()
            {
                Id = null,
                Capacity = null,
                Name = ""
            };
            var id = 10;
            var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, mediaType);

            HttpResponseMessage response = await PatchAsync(_client, $"{route}/{id}", content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            string responseText = await response.Content.ReadAsStringAsync();
            Assert.Contains("Все поля в модели данных пустые.", responseText);
        }

        [Fact(DisplayName = "Правильно завалидирована Patch модель данных.")]
        public async void ShouldProperlyValidatePatchModel()
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

        async Task<HttpResponseMessage> PatchAsync(HttpClient client, string requestUri, HttpContent content)
        {
            var method = new HttpMethod("PATCH");
            var request = new HttpRequestMessage(method, requestUri)
            {
                Content = content
            };
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                response = await client.SendAsync(request);
                return response;
            }
            catch
            {
                throw;
            }
        }
    }
}
