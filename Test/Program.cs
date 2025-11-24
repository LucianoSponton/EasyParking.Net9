using Model;
using Model.Enums;
using Newtonsoft.Json;
using ServiceWebApi;
using ServiceWebApi.DTO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        // **** Con esto se usa la api de arriba o la local *** ///
       // static string Uri = "http://localhost:5000";
        static string Uri = "http://40.118.242.96:12595";
        static HttpClient httpClient { get; set; } = new HttpClient();

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            try
            {
                AddBloqueoPlaza().Wait();           
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.ReadKey();

        }

        static async Task AddBloqueoPlaza()
        {
            try
            {
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 60);
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "analia@hotmail.com", "analia123", 3, 60);
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "admin", "admin", 3, 180);
                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "EasyParkingAdmin", "luciano123", 3, 180);

                BloqueoPlazaServiceWebApi bloqueoPlazaServiceWebApi = new BloqueoPlazaServiceWebApi(webapiaccess);

                var bloqueoFeriado = new BloqueoPlaza
                {
                    EstacionamientoId = 80,
                    UserId = "b88889a4-2012-4c6b-979b-70d98c1867ba",
                    PlazaId = 48, // NULL = todas las plazas del estacionamiento
                    TipoDeVehiculo = TipoDeVehiculo.MOTO, // NULL = todos los tipos de vehículos
                    FechaInicio = new DateTime(2025, 12, 27, 0, 0, 0), // 25 de diciembre 00:00
                    FechaFin = new DateTime(2025, 12, 28, 0, 0, 0), // 26 de diciembre 00:00
                    Motivo = "Feriado - por que quiero",
                    Observaciones = "El estacionamiento permanecerá cerrado durante este dia",
                    Activo = true,
                    FechaDeCreacion = DateTime.Now
                };

                await bloqueoPlazaServiceWebApi.Add(bloqueoFeriado);

                Console.WriteLine("Add BloqueoPlaza Ok");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw ex;
            }

        }

        public static string GetItem(string lat, string lon)
        {
            var url = $"https://apis.datos.gob.ar/georef/api/ubicacion?lat={lat}&lon={lon}";
            Console.WriteLine(url);
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = "application/json";
            request.Accept = "application/json";

            try
            {
                using (WebResponse response = request.GetResponse())
                {
                    Console.WriteLine(response);
                    using (Stream strReader = response.GetResponseStream())
                    {
                        Console.WriteLine(strReader);
                        if (strReader == null)
                            return null;
                        using (StreamReader objReader = new StreamReader(strReader))
                        {
                            string responseBody = objReader.ReadToEnd();
                            Console.WriteLine(responseBody);

                            var json = JsonConvert.DeserializeObject<Datos>(responseBody);

                            Console.WriteLine(json.ubicacion.municipio.nombre);
                            return json.ubicacion.municipio.nombre;
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                throw ex;
            }
        }

        public class Datos
        {
            public Parametros parametros { get; set; }
            public Ubicacion ubicacion { get; set; }

        }

        public class Parametros
        {
            public double lat { get; set; }
            public double lon { get; set; }
        }

        public class Ubicacion
        {
            public Departamento departamento { get; set; }
            public double lat { get; set; }
            public double lon { get; set; }

            public Municipio municipio { get; set; }

            public Provincia provincia { get; set; }
        }

        public class Departamento
        {
            public string id { get; set; }
            public string nombre { get; set; }
        }
        public class Municipio
        {
            public string id { get; set; }
            public string nombre { get; set; }
        }

        public class Provincia
        {
            public string id { get; set; }
            public string nombre { get; set; }
        }

        static async Task GetReseñas()
        {
            try
            {
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 60);
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "analia@hotmail.com", "analia123", 3, 60);
                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "admin", "admin", 3, 180);
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "EasyParkingAdmin", "easyparking123", 3, 180);

                ReseñaServiceWebApi reseñaServiceWebApi = new ReseñaServiceWebApi(webapiaccess);

                var z = await reseñaServiceWebApi.GetByEstacionamiento(26);

                Console.WriteLine("Add ReservaFals Ok");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw ex;
            }

        }

        static async Task AddReservaFalsa()
        {
            try
            {
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 60);
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "analia@hotmail.com", "analia123", 3, 60);
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "admin", "admin", 3, 180);
                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "EasyParkingAdmin", "easyparking123", 3, 180);

                ReseñaServiceWebApi reseñaServiceWebApi = new ReseñaServiceWebApi(webapiaccess);

                await reseñaServiceWebApi.Add(new Reseña());

                Console.WriteLine("Add ReservaFals Ok");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw ex;
            }

        }

        static async Task CrearUsuaio()
        {
            try
            {

                var userInfo = new UserInfo
                {
                    UserName = "admin5",
                    Email = "admin5",
                    Apellido = "admin5",
                    Nombre = "admin5",
                    Password = "12345678",
                    FechaDeNacimiento = new DateTime(2000, 1, 5),
                    NumeroDeDocumento = "123456789",
                    TipoDeDocumento = TipoDeDocumento.DNI,
                    Telefono = "3666985487",
                    Sexo = TipoDeSexo.HOMBRE,
                };


                await WebApiAccess.CreateUserAsync(Uri, userInfo);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        //static async Task ResetPassword(string username)
        //{
        //    try
        //    {
        //        //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 60);
        //        var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "lucho2798@gmail.com", "luciano123", 3, 180);

        //        AccountServiceWebApi accountServiceWebApi = new AccountServiceWebApi(webapiaccess);
        //        await accountServiceWebApi.ResetPassword(username);

        //        Console.WriteLine("Add ResetPassword2 Ok");

        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);

        //        throw ex;
        //    }

        //}


        static async Task ChangePassword(string email)
        {
            try
            {
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 60);
                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "lucho2798@gmail.com", "g9d7depp", 3, 180);

                AccountServiceWebApi accountServiceWebApi = new AccountServiceWebApi(webapiaccess);
                await accountServiceWebApi.ChangeUserPassword("g9d7depp", "hola1234", "hola1234");

                Console.WriteLine("ResetPassword Ok");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw ex;
            }

        }

        static async Task ResetPassword(string email)
        {
            try
            {
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 60);
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "lucho2798@gmail.com", "luciano123", 3, 180);

                AccountServiceWebApi accountServiceWebApi = new AccountServiceWebApi(Uri);
                await accountServiceWebApi.ResetPassword(email);

                Console.WriteLine("ResetPassword Ok");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw ex;
            }

        }

        static async Task Login(string Uri, string user, string pass)
        {
            try
            {
                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, user, pass, 3, 60);

                Console.WriteLine($"Token {webapiaccess.Token}");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw ex;
            }

        }



        static async Task PruebaLogin()
        {
            try
            {

                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 60);
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "EasyParkingAdmin", "easyparking123", 3, 180);
                AccountServiceWebApi accountServiceWebApi = new AccountServiceWebApi(webapiaccess);
                var x = await accountServiceWebApi.GetUserInfo("debranahir@gmail.com");
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        static async Task<bool> IsFavoriteAsync(int estacionamientoId)
        {
            try
            {
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 60);
                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "admin", "admin", 3, 180);

                FavoritoServiceWebApi favoritoServiceWebApi = new FavoritoServiceWebApi(webapiaccess);
                return await favoritoServiceWebApi.IsFavorite(estacionamientoId);

                Console.WriteLine("Add favorito Ok");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw ex;
            }

        }

        static async Task<List<Estacionamiento>> GetAll()
        {
            try
            {
                Console.WriteLine("GetAccess");
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "cristiano@hotmail.com", "cristiano123", 3, 3 * 60);
                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 60);
                EstacionamientoServiceWebApi estacionamientoServiceWebApi = new EstacionamientoServiceWebApi(webapiaccess);
                Console.WriteLine("Consultando");
                var x = await estacionamientoServiceWebApi.GetAll();
                return x;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        static async Task<List<EstacionamientoDTO>> GetAllInclude()
        {
            try
            {
                Console.WriteLine("GetAccess");
                // var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 180);
                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "admin", "admin", 3, 180);
                EstacionamientoServiceWebApi estacionamientoServiceWebApi = new EstacionamientoServiceWebApi(webapiaccess);
                Console.WriteLine("Consultando");
                var x = await estacionamientoServiceWebApi.GetAllInclude();
                return x;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        static async Task<List<Estacionamiento>> GetConsultaSimple(string text)
        {
            try
            {
                Console.WriteLine("GetAccess");
                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 60);
                EstacionamientoServiceWebApi estacionamientoServiceWebApi = new EstacionamientoServiceWebApi(webapiaccess);
                Console.WriteLine("Consultando");
                var estacionamiento = await estacionamientoServiceWebApi.GetConsultaSimple(text);
                Console.WriteLine("GetConsultaSimple Ok");
                return estacionamiento;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        static async Task<List<EstacionamientoDTO>> Busqueda()
        {
            try
            {
                Console.WriteLine("GetAccess");
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 60);
                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "admin", "admin", 3, 180);
                EstacionamientoServiceWebApi estacionamientoServiceWebApi = new EstacionamientoServiceWebApi(webapiaccess);
                Console.WriteLine("Consultando");

                var filtros = new ParametroBusquedaDTO
                {
                    TipoDeLugars = new List<string> { "Casa", "Galpón abierto" },
                    TipoDeVehiculos = new List<string> { "Auto", "Moto" }
                };

                var estacionamiento = await estacionamientoServiceWebApi.Busqueda(filtros);
                Console.WriteLine("Busqueda Ok");
                return estacionamiento;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        static async Task<List<EstacionamientoDTO>> Busqueda(string text)
        {
            try
            {
                Console.WriteLine("GetAccess");
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 60);
                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "admin", "admin", 3, 180);
                EstacionamientoServiceWebApi estacionamientoServiceWebApi = new EstacionamientoServiceWebApi(webapiaccess);
                Console.WriteLine("Consultando");

                var estacionamiento = await estacionamientoServiceWebApi.Busqueda(text);
                Console.WriteLine("Busqueda Ok");
                return estacionamiento;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        static async Task<List<Estacionamiento>> GetConsultaGenerica(ServiceWebApi.DTO.ParametroBusquedaDTO parametroBusquedaDTO)
        {
            try
            {
                Console.WriteLine("GetAccess");
                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 60);
                EstacionamientoServiceWebApi estacionamientoServiceWebApi = new EstacionamientoServiceWebApi(webapiaccess);
                Console.WriteLine("Consultando");
                var estacionamiento = await estacionamientoServiceWebApi.GetConsultaGenerica(parametroBusquedaDTO);
                Console.WriteLine("GetConsultaGenerica Ok");
                return estacionamiento;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        static async Task<Estacionamiento> Get(int estacionamientoId)
        {
            try
            {
                Console.WriteLine("GetAccess");
                var webapiaccess = await WebApiAccess.GetAccessAsync("http://40.118.242.96:12595", "debranahir@gmail.com", "debra1234", 3, 60);
                EstacionamientoServiceWebApi estacionamientoServiceWebApi = new EstacionamientoServiceWebApi(webapiaccess);
                Console.WriteLine("Consultando");
                var estacionamiento = await estacionamientoServiceWebApi.Get(estacionamientoId);
                Console.WriteLine("Get Ok");
                return estacionamiento;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        static async Task SetInactivo(int estacionamientoId)
        {
            try
            {
                Console.WriteLine("GetAccess");
                var webapiaccess = await WebApiAccess.GetAccessAsync("http://40.118.242.96:12595", "debranahir@gmail.com", "debra1234", 3, 60);
                EstacionamientoServiceWebApi estacionamientoServiceWebApi = new EstacionamientoServiceWebApi(webapiaccess);
                Console.WriteLine("Seteando");
                await estacionamientoServiceWebApi.SetInactivo(estacionamientoId);
                Console.WriteLine("SetInactivo ok");
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public static async Task SetPublicacionPausada(int estacionamientoId)
        {
            try
            {
                Console.WriteLine("GetAccess");
                var webapiaccess = await WebApiAccess.GetAccessAsync("http://40.118.242.96:12595", "debranahir@gmail.com", "debra1234", 3, 60);
                EstacionamientoServiceWebApi estacionamientoServiceWebApi = new EstacionamientoServiceWebApi(webapiaccess);

                Console.WriteLine("Seteando");
                await estacionamientoServiceWebApi.SetPublicacionPausada(estacionamientoId);
                Console.WriteLine("SetPublicacionPausada ok");
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        static async Task Delete(int estacionamientoId)
        {
            try
            {
                Console.WriteLine("GetAccess");
                var webapiaccess = await WebApiAccess.GetAccessAsync("http://40.118.242.96:12595", "debranahir@gmail.com", "debra1234", 3, 60);
                EstacionamientoServiceWebApi estacionamientoServiceWebApi = new EstacionamientoServiceWebApi(webapiaccess);
                Console.WriteLine("Eliminando");
                await estacionamientoServiceWebApi.Delete(estacionamientoId);
                Console.WriteLine("Delete ok");
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        static async Task Update(Estacionamiento estacionamiento)
        {
            try
            {
                Console.WriteLine("GetAccess");
                var webapiaccess = await WebApiAccess.GetAccessAsync("http://40.118.242.96:12595", "debranahir@gmail.com", "debra1234", 3, 60);
                EstacionamientoServiceWebApi estacionamientoServiceWebApi = new EstacionamientoServiceWebApi(webapiaccess);
                Console.WriteLine("Actualizando");
                //estacionamiento.Imagen = Tools.GetBytesFromUrl("https://www.neahoy.com/wp-content/uploads/2022/02/San-Valentin-cuanto-puede-costar-una-salida-en-pareja-en-la-capital-correntina-2.jpg");
                estacionamiento.PublicacionPausada = true;
                estacionamiento.Inactivo = false;

                await estacionamientoServiceWebApi.Update(estacionamiento);
                Console.WriteLine("Update ok");
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        static async Task SendImage(byte[] image)
        {
            try
            {
                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 60);
                EstacionamientoServiceWebApi estacionamientoServiceWebApi = new EstacionamientoServiceWebApi(webapiaccess);

                List<FileInfo> files = new List<FileInfo>();
                FileInfo fileInfo = new FileInfo(@"C:\Users\LUCIANO\Downloads\usuario.png");
                files.Add(fileInfo);

                MultipartFormDataContent multipartFormDataContent = new MultipartFormDataContent();

                foreach (FileInfo file in files)
                {
                    FileStream stream = file.OpenRead();
                    StreamContent streamContent = new StreamContent(stream);
                    multipartFormDataContent.Add(streamContent, "file", file.FullName);

                }
                await estacionamientoServiceWebApi.SedImage(image);

                Console.WriteLine("Se mando");


                //using (var httpClient = new HttpClient())
                //{
                //    httpClient.BaseAddress = new Uri(webApiUri);
                //    httpClient.DefaultRequestHeaders.Accept.Clear();
                //    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //    httpClient.Timeout = TimeSpan.FromSeconds(timeout);
                //    var userDTO = new
                //    {
                //        username = userName,
                //        password = password,
                //    };

                //    string userjson = JsonConvert.SerializeObject(userDTO);
                //    HttpContent content = new StringContent(userjson, Encoding.UTF8, "application/json");
                //    using (var content =
                //        new MultipartFormDataContent("Upload----" + DateTime.Now.ToString(CultureInfo.InvariantCulture)))
                //    {
                //        content.Add(new StreamContent(new MemoryStream(image)), "bilddatei", "upload.jpg");

                //        using (
                //           var message =
                //               await httpClient.PostAsync($"api/Estacionamiento/Uploads", content))
                //        {
                //            var input = await message.Content.ReadAsStringAsync();

                //            //return !string.IsNullOrWhiteSpace(input) ? Regex.Match(input, @"http://\w*\.directupload\.net/images/\d*/\w*\.[a-z]{3}").Value : null;
                //        }
                //    }
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw ex;
            }

        }


        static async Task<List<Favorito>> GetAllFavorito()
        {
            try
            {
                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 60);
                FavoritoServiceWebApi favoritoServiceWebApi = new FavoritoServiceWebApi(webapiaccess);
                var x = await favoritoServiceWebApi.GetAll();

                Console.WriteLine("GetAll favorito Ok");

                return x;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw ex;
            }

        }

        static async Task<List<Jornada>> GetJornadas(int estacionamientoId)
        {
            try
            {
                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 60);
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "analia@hotmail.com", "analia123", 3, 60);
                EstacionamientoServiceWebApi estacionamientoServiceWebApi = new EstacionamientoServiceWebApi(webapiaccess);
                var x = await estacionamientoServiceWebApi.GetJornadas(estacionamientoId);

                Console.WriteLine("GetJornadas Ok");

                return x;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw ex;
            }

        }

        static async Task<List<ServiceWebApi.DTO.EstacionamientoDTO>> GetMisFavorito()
        {
            try
            {
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 60);
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "analia@hotmail.com", "analia123", 3, 60);
                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "EasyParkingAdmin", "easyparking123", 3, 180);

                FavoritoServiceWebApi favoritoServiceWebApi = new FavoritoServiceWebApi(webapiaccess);
                var x = await favoritoServiceWebApi.GetMisFavoritos();

                Console.WriteLine("GetMisFavoritos Ok");

                return x;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw ex;
            }

        }

        static async Task AddFavorito(int estacionamientoId)
        {
            try
            {
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 60);
                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "analia@hotmail.com", "analia123", 3, 60);

                FavoritoServiceWebApi favoritoServiceWebApi = new FavoritoServiceWebApi(webapiaccess);
                await favoritoServiceWebApi.Add(estacionamientoId);

                Console.WriteLine("Add favorito Ok");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw ex;
            }

        }

        static async Task DeleteFavorito(int estacionamientoId)
        {
            try
            {
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 60);
                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "analia@hotmail.com", "analia123", 3, 180);

                FavoritoServiceWebApi favoritoServiceWebApi = new FavoritoServiceWebApi(webapiaccess);
                await favoritoServiceWebApi.Delete(estacionamientoId);

                Console.WriteLine("Add favorito Ok");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw ex;
            }

        }

        static async Task<List<Vehiculo>> GetAllVehiculos()
        {
            try
            {
                Console.WriteLine("GetAccess");
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "cristiano@hotmail.com", "cristiano123", 3, 3 * 60);
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 60);

                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "admin", "admin", 3, 180);
                VehiculoServiceWebApi vehiculoServiceWebApi = new VehiculoServiceWebApi(webapiaccess);
                Console.WriteLine("Consultando");
                var x = await vehiculoServiceWebApi.GetAll();
                return x;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        // AA658V
        static async Task<List<Vehiculo>> GetAllMisVehiculos()
        {
            try
            {
                Console.WriteLine("GetAccess");
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "cristiano@hotmail.com", "cristiano123", 3, 3 * 60);
                //     var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 180);
                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "admin", "admin", 3, 180);

                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "analia@hotmail.com", "analia123", 3, 60);
                VehiculoServiceWebApi vehiculoServiceWebApi = new VehiculoServiceWebApi(webapiaccess);
                Console.WriteLine("Consultando");
                var x = await vehiculoServiceWebApi.GetMisVehiculos();
                return x;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        static async Task<Vehiculo> GetVehiculoByPatente(string patente)
        {
            try
            {
                Console.WriteLine("GetAccess");

                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "admin", "admin", 3, 180);
                // var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 180);
                VehiculoServiceWebApi vehiculoServiceWebApi = new VehiculoServiceWebApi(webapiaccess);
                Console.WriteLine("Consultando");
                var x = await vehiculoServiceWebApi.GetVehiculoByPatente(patente);
                return x;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        static async Task<Vehiculo> GetFirstVehiculo()
        {
            try
            {
                Console.WriteLine("GetAccess");
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "cristiano@hotmail.com", "cristiano123", 3, 3 * 60);
                // var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 180);
                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "admin", "admin", 3, 180);

                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "analia@hotmail.com", "analia123", 3, 60);
                VehiculoServiceWebApi vehiculoServiceWebApi = new VehiculoServiceWebApi(webapiaccess);

                Console.WriteLine("Consultando");
                var x = await vehiculoServiceWebApi.GetFirst();
                return x;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        static async Task AddVehiculo()
        {
            try
            {
                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 60);
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "analia@hotmail.com", "analia123", 3, 60);

                VehiculoServiceWebApi vehiculoServiceWebApi = new VehiculoServiceWebApi(webapiaccess);

                Vehiculo vehiculo = new Vehiculo();
                vehiculo.Patente = "FR98YH";
                vehiculo.TipoDeVehiculo = TipoDeVehiculo.AUTO;

                await vehiculoServiceWebApi.Add(vehiculo);

                Console.WriteLine("Add Vehiculo Ok");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw ex;
            }

        }

        static async Task AddReserva()
        {
            //try
            //{
            //    // var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 60);
            //    //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "analia@hotmail.com", "analia123", 3, 60);
            //    var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "admin", "admin", 3, 180);

            //    ReservaServiceWebApi reservaServiceWebApi = new ReservaServiceWebApi(webapiaccess);

            //    Model.Reserva reserva = new Model.Reserva();
            //    reserva.CodigoDeValidacion = "DFERTYGH";
            //    reserva.EstacionamientoId = 30;
            //    reserva.Monto = 321;
            //    reserva.Patente = "AS345SQ";
            //    reserva.VehiculoId = 28;
            //    reserva.Estado = Model.Enums.EstadoReserva.ESPERANDO_ARRIBO;

            //    await reservaServiceWebApi.Add(reserva);

            //    Console.WriteLine("Add Reserva Ok");

            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.Message);

            //    throw ex;
            //}

        }

        static async Task<List<ReservaDTO>> GetMisReservas()
        {
            try
            {
                Console.WriteLine("GetAccess");
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "cristiano@hotmail.com", "cristiano123", 3, 3 * 60);
                /// var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 60);
                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "admin", "admin", 3, 180);
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "analia@hotmail.com", "analia123", 3, 60);
                ReservaServiceWebApi reservaServiceWebApi = new ReservaServiceWebApi(webapiaccess);
                Console.WriteLine("Consultando");
                var x = await reservaServiceWebApi.GetMisReservas(EstadoReserva.NONE);
                return x;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        static async Task<List<ReservaDTO>> GetReservasModalidadDueño(EstadoReserva estadoReserva)
        {
            try
            {
                Console.WriteLine("GetAccess");
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "cristiano@hotmail.com", "cristiano123", 3, 3 * 60);
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "debranahir@gmail.com", "debra1234", 3, 180);
                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "admin", "admin", 3, 180);
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "analia@hotmail.com", "analia123", 3, 60);
                ReservaServiceWebApi reservaServiceWebApi = new ReservaServiceWebApi(webapiaccess);
                Console.WriteLine("Consultando");
                var x = await reservaServiceWebApi.GetReservasModalidadDueño(estadoReserva);
                return x;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        static async Task GetAllEstacionamientos()
        {
            try
            {
                Console.WriteLine("GetAccess");

                var webapiaccess = WebApiAccess.GetAccessAsync("http://40.118.242.96:12595", "EasyParkingAdmin", "easyparking123").Result;
                EstacionamientoServiceWebApi estacionamientoServiceWebApi = new EstacionamientoServiceWebApi(webapiaccess);

                Console.WriteLine("Consultando Estacionamiento");

                var list = await estacionamientoServiceWebApi.GetAll();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        static async Task UnLockUser(string username)
        {
            try
            {
                //var webapiaccess = await WebApiAccess.GetAccessAsync("http://40.118.242.96:12595", "debranahir@gmail.com", "debra1234", 3, 60);
                var webapiaccess = await WebApiAccess.GetAccessAsync("http://40.118.242.96:12595", "analia@gmail.com", "analia123", 3, 60);

                AccountServiceWebApi accountServiceWebApi = new AccountServiceWebApi(webapiaccess);


                await accountServiceWebApi.UserUnLock(username);

                Console.WriteLine("User UnLock Ok");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw ex;
            }
        }

    }
}
