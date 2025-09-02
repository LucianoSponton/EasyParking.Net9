using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;


namespace ServiceWebApi
{
    public class WebApiPost<T1, T2> // ESTE SE USA CUANDO ENVIAMOS Y ESPERAMOS UNA RESPUESTA
    {
        private WebApiAccess _webApiAccess;

        public WebApiPost(WebApiAccess webApiAccess)
        {
            _webApiAccess = webApiAccess;
        }

        public async Task<T2> PostAsync(string uri, T1 objeto)
        {
            try
            {
                int intento = 0;
                bool retry = true;
                string json = JsonConvert.SerializeObject(objeto, Formatting.None, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                do
                {
                    try
                    {
                        intento += 1;
                        HttpResponseMessage response = _webApiAccess.HttpClient.PostAsync(uri, content).Result;
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            retry = false;

                            string result = await response.Content.ReadAsStringAsync();
                            T2 retorno = JsonConvert.DeserializeObject<T2>(result);

                            //string result = await response.Content.ReadAsStringAsync();
                            //JObject jsonresult = JObject.Parse(result);
                            ////T2 retorno = JsonSerializer.DeserializeObject<T2>(result);
                            //T2 retorno = jsonresult.ToObject<T2>();

                            return retorno;
                        }
                        else
                        {
                            if (intento >= _webApiAccess.Retry)
                            {
                                string errorcontent = response.Content.ReadAsStringAsync().Result;
                                throw new Exception($"ERROR ... {response.StatusCode.ToString()} - {errorcontent}");
                            }
                        }


                    }
                    catch (Exception ex)
                    {
                        if (retry == false | intento >= _webApiAccess.Retry)
                        {
                            throw new Exception($"ERROR ... {ex.Message}");
                        }
                    }
                }
                while (true);
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

    } 

    public class WebApiPost<T1> // ESTE SE USA CUANDO SOLO ENVIAMOS
    {
        private WebApiAccess _webApiAccess;

        public WebApiPost(WebApiAccess webApiAccess)
        {
            _webApiAccess = webApiAccess;
        }

        public async Task PostAsync(string uri, T1 objeto)
        {
            try
            {
                string json = JsonConvert.SerializeObject(objeto, Formatting.None, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                int intento = 0;
                do
                {
                    intento += 1;
                    HttpResponseMessage response = await _webApiAccess.HttpClient.PostAsync(uri, content);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return;
                    }
                    else
                    {
                        if (intento >= _webApiAccess.Retry)
                        {
                            string errorcontent = response.Content.ReadAsStringAsync().Result;
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


    }

}
