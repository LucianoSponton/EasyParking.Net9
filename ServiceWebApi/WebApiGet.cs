using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ServiceWebApi
{
    public class WebApiGet<T> // ESTE SE USA CUANDO ENVIAMOS Y ESPERAMOS UNA RESPUESTA
    {
        private WebApiAccess _webApiAccess;

        public WebApiGet(WebApiAccess webApiAccess)
        {
            _webApiAccess = webApiAccess;
        }

        public async Task<T> GetAsync(string uri) 
        {
            string errorcontent;
            int intento = 0;
            bool retry = true;
            do
            {
                try
                {
                    intento += 1;
                    HttpResponseMessage response = await _webApiAccess.HttpClient.GetAsync(uri);
                    Console.WriteLine($"Get ({intento}): " + response.StatusCode);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        retry = false;
                        string result = await response.Content.ReadAsStringAsync();
                        T retorno = JsonConvert.DeserializeObject<T>(result);

                        return retorno;
                    }
                    else
                    {
                        if (intento >= _webApiAccess.Retry)
                        {
                            errorcontent = response.Content.ReadAsStringAsync().Result;
                            throw new Exception($"ERROR ... {response.StatusCode} - {errorcontent}");

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
    }

    public class WebApiGet // ESTE CUANDO SOLO ENVIAMOS
    {
        private WebApiAccess _webApiAccess;

        public WebApiGet(WebApiAccess webApiAccess)
        {
            _webApiAccess = webApiAccess;
        }

        public async Task GetAsync(string uri)
        {
            try
            {
                int intento = 0;
                do
                {
                    intento += 1;
                    HttpResponseMessage response = await _webApiAccess.HttpClient.GetAsync(uri);
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
