using Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ServiceWebApi
{
    public static class AccountServiceWebApi
    {
        private static string _uri = "http://40.118.242.96:12595";
        //string token = Login("EasyParkingAdmin", "easyparking123"); // login de admin

        public static HttpClient httpClient { get; set; } = new HttpClient();

        public static void CreateUser(UserInfo user)
        {
            try
            {

                Console.WriteLine("Autenticando ...");
                //HttpClient httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(_uri);
                Random rand = new Random(DateTime.Now.Second);
                Console.WriteLine("Comienza Add()");

                string dtojson = JsonConvert.SerializeObject(user); // aca va el DTO de userinfo 
                HttpContent content2 = new StringContent(dtojson, Encoding.UTF8, "application/json");
                HttpResponseMessage response2 = httpClient.PostAsync("/api/Account/CreateUser", content2).Result;
                if (response2.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Console.WriteLine("Create ok");
                }
                else
                {
                    string msjError = response2.ReasonPhrase + " " + response2.Content.ReadAsStringAsync().Result;
                    Console.WriteLine(msjError);
                    throw new Exception(msjError + " " + "new Exception");
                }

                //httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
        }
        public static string Login(string username, string password)
        {
            try
            {
                Console.WriteLine("Autenticando ...");
                httpClient.BaseAddress = new Uri(_uri);
                httpClient.DefaultRequestHeaders.Clear();

                var userDTO = new { username = username, password = password };
                Random rand = new Random(DateTime.Now.Second);
                Console.WriteLine("Comienza Login()");
                string dtojson = JsonConvert.SerializeObject(userDTO); // aca va el DTO de userinfo 
                HttpContent content2 = new StringContent(dtojson, Encoding.UTF8, "application/json");
                HttpResponseMessage response2 = httpClient.PostAsync("/api/Account/Login", content2).Result;
                if (response2.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string jsontoken = response2.Content.ReadAsStringAsync().Result;
                    JObject jsonresult = JObject.Parse(jsontoken);
                    string token = jsonresult.SelectToken("token").ToObject<string>();
                    DateTime expiration = jsonresult.SelectToken("expiration").ToObject<DateTime>();
                    Console.WriteLine("Token: " + response2.StatusCode);
                    Console.WriteLine("Token: " + token);
                    httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                    return token;

                }
                else
                {
                    string msjError = response2.ReasonPhrase + " " + response2.Content.ReadAsStringAsync().Result;
                    Console.WriteLine(msjError);
                    throw new Exception(msjError + " " + "new Exception");
                }

            }
            catch (InvalidOperationException inv)
            {
                throw inv;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }

        }
        public static void Update(UserInfo user, string UserName, string Password)
        {
            try
            {
                //string token = Login(UserName, Password);

                string dtojson = JsonConvert.SerializeObject(user); // aca va el DTO de userinfo 
                HttpContent content2 = new StringContent(dtojson, Encoding.UTF8, "application/json");

               // httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

                HttpResponseMessage response2 = httpClient.PostAsync("/api/Account/UpdateUser", content2).Result;
                if (response2.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Console.WriteLine("Update ok");
                }
                else
                {
                    string msjError = response2.ReasonPhrase + " " + response2.Content.ReadAsStringAsync().Result;
                    Console.WriteLine(msjError);
                    throw new Exception(msjError);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }

        }
        public static void UserLock(string username)
        {
            try
            {

                Console.WriteLine("Comienza UserLock()");


                HttpResponseMessage response2 = httpClient.GetAsync($"/api/Account/UserLock/{username}").Result;
                if (response2.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Console.WriteLine("UserLock ok");
                }
                else
                {
                    string msjError = response2.ReasonPhrase + " " + response2.Content.ReadAsStringAsync().Result;
                    Console.WriteLine(msjError);
                    throw new Exception(msjError);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }

        }
        public static void UserUnLock(string username)
        {
            try
            {
                //string token = Login("EasyParkingAdmin", "easyparking123"); // login de admin

                Console.WriteLine("Comienza UserUnLock()");

               // httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

                HttpResponseMessage response2 = httpClient.GetAsync($"/api/Account/UserUnLock/{username}").Result;
                if (response2.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Console.WriteLine("UserUnLock ok");

                }
                else
                {
                    string msjError = response2.ReasonPhrase + " " + response2.Content.ReadAsStringAsync().Result;
                    Console.WriteLine(msjError);
                    throw new Exception(msjError);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }

        }
        public static void UserLockItSelf(string username, string password)
        {
            try
            {
                Console.WriteLine("Comienza UserLockItSelf()");


                HttpResponseMessage response2 = httpClient.GetAsync($"/api/Account/UserLockItSelf/{username}").Result;
                if (response2.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Console.WriteLine("UserLockItSelf ok");

                }
                else
                {
                    string msjError = response2.ReasonPhrase + " " + response2.Content.ReadAsStringAsync().Result;
                    Console.WriteLine(msjError);
                    throw new Exception(msjError);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }

        }


        public static async Task<UserInfo> GetUserInfo(string username, string password)
        {
            try
            {   //easyparking
                //easyparking#1284
                Console.WriteLine("Comienza GetUser()");

                HttpResponseMessage response2 = httpClient.GetAsync($"/api/Account/GetUserInfo/{username}").Result;
                if (response2.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Console.WriteLine("GetUser ok");
                    string result = await response2.Content.ReadAsStringAsync();
                    var userinfo = JsonConvert.DeserializeObject<UserInfo>(result);
                    return userinfo;
                }
                else
                {
                    Console.WriteLine(response2.ReasonPhrase);
                    return null;

                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
    }
}
