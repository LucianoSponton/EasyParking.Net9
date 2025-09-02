using Model;
using Model.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ServiceWebApi
{
    public class WebApiAccess
    {
        public HttpClient HttpClient;
        public bool HayToken;
        public string Token;
        public string WebApiUri;
        public DateTime Expiration;
        public int Retry;
        public int TimeOut;
        public string ErrorMessage;

        public WebApiAccess(HttpClient httpClient, string webApiUri, string token, DateTime expiration, int retry, int timeout)
        {
            HttpClient = httpClient;
            Token = token;
            WebApiUri = webApiUri;
            Expiration = expiration;
            Retry = retry;
            TimeOut = timeout;
        }


        public static async Task<WebApiAccess> GetAccessAsync(string webApiUri, string userName, string password, int retry = 3, int timeout = 10)
        {
            try
            {
                HttpClient httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(webApiUri);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.Timeout = TimeSpan.FromSeconds(timeout);
                var userDTO = new
                {
                    username = userName,
                    password = password,
                };

                string userjson = JsonConvert.SerializeObject(userDTO);
                HttpContent content = new StringContent(userjson, Encoding.UTF8, "application/json");

                string token = null;
                string jsontoken = null;
                DateTime expiration;
                int intento = 0;
                do
                {
                    intento += 1;
                    Console.WriteLine($"Iniciando Autenticacion ({intento}) ...");
                    HttpResponseMessage response = await httpClient.PostAsync("api/account/login", content);
                    Console.WriteLine($"Autenticacion ({intento}): " + response.StatusCode);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {

                        jsontoken = response.Content.ReadAsStringAsync().Result;
                        JObject jsonresult = JObject.Parse(jsontoken);

                        token = jsonresult.SelectToken("token").ToObject<string>();
                        expiration = jsonresult.SelectToken("expiration").ToObject<DateTime>();

                        httpClient.DefaultRequestHeaders.Accept.Clear();
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

                        return new WebApiAccess(httpClient, webApiUri, token, expiration, retry, timeout);
                    }
                    else
                    {
                        string errorcontent = response.Content.ReadAsStringAsync().Result;

                        if (errorcontent.Contains("Intento de login NO VALIDO ..."))
                        {
                            throw new Exception($"ERROR ... {response.StatusCode.ToString()} - {errorcontent}");
                        }
                        else if (intento >= retry)
                        {
                            throw new Exception($"ERROR ... {response.StatusCode.ToString()} - {errorcontent}");
                        }
                    }

                } while (true);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public static async Task<bool> CreateUserAsync(string webApiUri , UserInfo userInfo )
        {
            try
            {
                HttpClient httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(webApiUri);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


                string userjson = JsonConvert.SerializeObject(userInfo);
                HttpContent content = new StringContent(userjson, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await httpClient.PostAsync("api/account/CreateUser", content);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
                else
                {
                    string errorcontent = response.Content.ReadAsStringAsync().Result;
                    throw new Exception($"ERROR ... {response.StatusCode.ToString()} - {errorcontent}");

                }

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


    }
}
