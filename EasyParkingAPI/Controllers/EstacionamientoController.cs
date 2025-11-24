using EasyParkingAPI.Data;
using EasyParkingAPI.DTO;
using EasyParkingAPI.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Model;
using Model.Enums;
using ServiceWebApi.DTO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EasyParkingAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class EstacionamientoController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _UserId;
        private readonly UserManager<ApplicationUser> _userManager;

        public EstacionamientoController(IConfiguration configuration,
                                            IHttpContextAccessor httpContextAccessor,
                                            UserManager<ApplicationUser> userManager)
        {
            try
            {
                _configuration = configuration;
                _userManager = userManager;
                HttpContext http = httpContextAccessor.HttpContext;
                var user = http.User;

                ApplicationUser appuser = _userManager.FindByNameAsync(user.Identity.Name).Result; // Obtengo los datos del usuario logeado

                _UserId = appuser.Id; // Obtengo el ID del usuario logeado

                if (String.IsNullOrEmpty(_UserId) | String.IsNullOrWhiteSpace(_UserId))
                {
                    throw new Exception("ERROR ... Usuario sin permisos necesarios.");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /////////////////////////// GET ///////////////////////////

        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<Estacionamiento>>> GetAllAsync()
        {
            try
            {
                DataContext dataContext = new DataContext();
                var lista = await dataContext.Estacionamientos.AsNoTracking().ToListAsync();
                if (lista == null)
                {
                    return NotFound();
                }
                return lista;

            }
            catch (Exception e)
            {

                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<EstacionamientoDTO>>> GetAllIncludeAsync()
        {
            try
            {
                DataContext dataContext = new DataContext();
                var estacionamientos = await dataContext.Estacionamientos.Include("Jornadas.Horarios").AsNoTracking()
                .Include("Plazas").Include("Tarifas").AsNoTracking().Where(x => x.Inactivo == false && x.PublicacionPausada == false).AsNoTracking().ToListAsync();

                ///var vehiculo = await dataContext.Vehiculos.Where(x => x.UserId == _UserId && x.Patente == "AA658V").AsNoTracking().FirstOrDefaultAsync();

                // var estacionamientos = await dataContext.Estacionamientos.ToListAsync();

                if (estacionamientos == null)
                {
                    return NotFound();
                }

                List<ServiceWebApi.DTO.EstacionamientoDTO> listaDTO = new List<ServiceWebApi.DTO.EstacionamientoDTO>();


                foreach (var item in estacionamientos)
                {
                    ServiceWebApi.DTO.EstacionamientoDTO estacionamientoDTO = new ServiceWebApi.DTO.EstacionamientoDTO();
                    estacionamientoDTO = Tools.Tools.PropertyCopier<Estacionamiento, ServiceWebApi.DTO.EstacionamientoDTO>.Copy(item, estacionamientoDTO);

                    // Calculate the average Puntaje, handling empty sequences
                    var reseñas = await dataContext.Reseñas
                        .Where(r => r.EstacionamientoId == item.Id)
                        .ToListAsync();
                    var averagePuntaje = reseñas.Any() ? reseñas.Average(r => r.Puntaje ?? 0) : 0;

                    estacionamientoDTO.Puntaje = averagePuntaje;

                    listaDTO.Add(estacionamientoDTO);
                    listaDTO.Add(estacionamientoDTO);


                }

                return listaDTO;

            }
            catch (Exception e)
            {

                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }
       
        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<EstacionamientoDTO>>> GetMisEstacionamientosAsync()
        {
            try
            {
                DataContext dataContext = new DataContext();
                var lista = await dataContext.Estacionamientos
                    .Include(e => e.Jornadas)
                        .ThenInclude(j => j.Horarios)
                    .Include(e => e.Tarifas)
                    .Include(e => e.Plazas)
                    .AsNoTracking()
                    .Where(x => x.UserId == _UserId && x.Inactivo == false)
                    .ToListAsync();

                if (lista == null || !lista.Any())
                {
                    return NoContent();
                }

                var listaDTO = new List<ServiceWebApi.DTO.EstacionamientoDTO>();
                foreach (var item in lista)
                {
                    // Filtrar las plazas activas después de traer los datos
                    item.Plazas = item.Plazas?.Where(p => p.Activo == true).ToList();

                    var estacionamientoDTO = new ServiceWebApi.DTO.EstacionamientoDTO();
                    estacionamientoDTO = Tools.Tools.PropertyCopier<Estacionamiento, ServiceWebApi.DTO.EstacionamientoDTO>.Copy(item, estacionamientoDTO);

                    // Calculate the average Puntaje, handling empty sequences
                    var reseñas = await dataContext.Reseñas
                        .Where(r => r.EstacionamientoId == item.Id)
                        .ToListAsync();
                    var averagePuntaje = reseñas.Any() ? reseñas.Average(r => r.Puntaje ?? 0) : 0;
                    estacionamientoDTO.Puntaje = averagePuntaje;

                    listaDTO.Add(estacionamientoDTO);
                }
                return listaDTO;
            }
            catch (Exception e)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }


        [HttpGet("[action]/{estacionamientoId}")]
        public async Task<ActionResult<Estacionamiento>> GetAsync(int estacionamientoId)
        {
            try
            {
                DataContext dataContext = new DataContext();
                var estacionamiento = await dataContext.Estacionamientos.Include("Jornadas.Horarios")
                  .Include("Plazas").Include("Tarifas").AsNoTracking().FirstOrDefaultAsync(x => x.Id == estacionamientoId);

                if (estacionamiento == null)
                {
                    return NotFound();
                }
                return estacionamiento;

            }
            catch (Exception e)
            {

                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }

        [HttpGet("[action]/{estacionamientoId}")]
        public async Task<ActionResult<string>> GetDueñoUserIdAsync(int estacionamientoId)
        {
            try
            {
                DataContext dataContext = new DataContext();
                var estacionamiento = await dataContext.Estacionamientos.Where(x => x.Id == estacionamientoId).FirstOrDefaultAsync();

                if (estacionamiento == null)
                {
                    return NotFound();
                }

                return estacionamiento.UserId;

            }
            catch (Exception e)
            {

                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }

        [HttpGet("[action]/{estacionamientoId}")]
        public async Task<ActionResult<List<Estacionamiento>>> GetOrderByReservasAsync()
        {
            try
            {
                DataContext dataContext = new DataContext();
                var estacionamiento = await dataContext.Estacionamientos.Include("Jornadas.Horarios")
                                            .Include("Plazas").Include("Tarifas").AsNoTracking().OrderBy(x => x.MontoReserva).ToListAsync();

                if (estacionamiento == null)
                {
                    return NotFound();
                }
                return estacionamiento;

            }
            catch (Exception e)
            {

                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }


        [HttpGet]
        [Route("[action]/{text}")]
        public async Task<ActionResult<List<Tag>>> GetTagsAsync(string text) // consulta por nombre o direccion del estacionamiento
        {
            try
            {
                DataContext dataContext = new DataContext();

                List<Tag> query = await (from est in dataContext.Estacionamientos
                                         where est.Direccion.Contains(text) || est.Nombre.Contains(text)
                                         select new Tag
                                         {
                                             Nombre = est.Nombre,
                                             Direccion = est.Direccion,
                                             EstacionamientoId = est.Id
                                         }).ToListAsync();


                if (query == null)
                {
                    return NotFound();
                }
                return query;

            }
            catch (Exception e)
            {

                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }

        [HttpGet]
        [Route("[action]/{text}")]
        public async Task<ActionResult<List<Estacionamiento>>> GetConsultaSimpleAsync(string text) // consulta por nombre o direccion del estacionamiento
        {
            try
            {
                DataContext dataContext = new DataContext();
                var estacionamientos = await dataContext.Estacionamientos.Include("Jornadas.Horarios")
                .Include("Plazas").Include("Tarifas").AsNoTracking().Where(x => x.Direccion.Contains(text) || x.Nombre.Contains(text)).ToListAsync();

                if (estacionamientos == null)
                {
                    return NotFound();
                }
                return estacionamientos;

            }
            catch (Exception e)
            {

                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }

        [HttpGet]
        [Route("[action]/{vehiculo}")]
        public async Task<ActionResult<List<Estacionamiento>>> GetByTiposDeVehiculosAdmitidosAsync(TipoDeVehiculo vehiculo) // consulta por nombre o direccion del estacionamiento
        {
            try
            {
                DataContext dataContext = new DataContext();
                var estacionamientos = await dataContext.Estacionamientos.Include("Jornadas.Horarios")
                .Include("Plazas").Include("Tarifas").AsNoTracking().ToListAsync();


                if (estacionamientos == null)
                {
                    return NotFound();
                }

                List<Estacionamiento> lista = new List<Estacionamiento>();

                foreach (var item in estacionamientos)
                {
                    foreach (var i in item.Plazas)
                    {
                        if (i.TipoDeVehiculo == vehiculo)
                        {
                            lista.Add(item);
                        }
                    }
                }

                if (lista == null)
                {
                    return NotFound();
                }


                return lista;

            }
            catch (Exception e)
            {

                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }

        [HttpGet]
        [Route("[action]/{tipoDeLugar}")]
        public async Task<ActionResult<List<Estacionamiento>>> GetByTiposDeLugarAsync(string tipoDeLugar) // consulta por nombre o direccion del estacionamiento
        {
            try
            {
                DataContext dataContext = new DataContext();
                var estacionamientos = await dataContext.Estacionamientos.Include("Jornadas.Horarios")
                .Include("Plazas").Include("Tarifas").AsNoTracking().Where(x => x.TipoDeLugar == tipoDeLugar).ToListAsync();


                if (estacionamientos == null)
                {
                    return NotFound();
                }


                return estacionamientos;

            }
            catch (Exception e)
            {

                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }

        [HttpGet("[action]/{textoBusqueda}")]
        public async Task<ActionResult<List<EstacionamientoDTO>>> BusquedaAsync(string textoBusqueda)
        {
            try
            {
                if (string.IsNullOrEmpty(textoBusqueda))
                    return BadRequest("El texto de búsqueda no puede estar vacío.");

                DataContext dataContext = new DataContext();
                string texto = textoBusqueda.Trim().ToLower();

                // Obtener todos los valores del enum que coincidan
                var tiposVehiculoCoincidentes = Enum.GetValues(typeof(TipoDeVehiculo))
                    .Cast<TipoDeVehiculo>()
                    .Where(t => t.ToString().ToLower().Contains(texto))
                    .ToList();

                var query = dataContext.Estacionamientos
                    .Include(e => e.Jornadas)
                        .ThenInclude(j => j.Horarios)
                    .Include(e => e.Plazas)
                    .AsNoTracking()
                    .Where(e => !e.Inactivo && !e.PublicacionPausada)
                    .Where(e =>
                        e.Ciudad.ToLower().Contains(texto) ||
                        e.Nombre.ToLower().Contains(texto) ||
                        e.Direccion.ToLower().Contains(texto) ||
                        e.TipoDeLugar.ToLower().Contains(texto) ||
                        e.Plazas.Any(v => tiposVehiculoCoincidentes.Contains(v.TipoDeVehiculo))
                    );

                var estacionamientos = await query.ToListAsync();

                List<EstacionamientoDTO> listaDTO = new List<EstacionamientoDTO>();

                foreach (var item in estacionamientos)
                {
                    EstacionamientoDTO estacionamientoDTO = new EstacionamientoDTO();
                    estacionamientoDTO = Tools.Tools.PropertyCopier<Estacionamiento, EstacionamientoDTO>.Copy(item, estacionamientoDTO);

                    var reseñas = await dataContext.Reseñas
                        .Where(r => r.EstacionamientoId == item.Id)
                        .ToListAsync();

                    var averagePuntaje = reseñas.Any() ? reseñas.Average(r => r.Puntaje ?? 0) : 0;
                    estacionamientoDTO.Puntaje = averagePuntaje;

                    listaDTO.Add(estacionamientoDTO);
                }

                return Ok(listaDTO);
            }
            catch (Exception e)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }

        [HttpGet]
        [Route("[action]/{estacionamientoId}")]
        public async Task<ActionResult<List<Jornada>>> GetJornadas(int estacionamientoId)
        {
            try
            {
                DataContext dataContext = new DataContext();
                var lista = await dataContext.Jornadas.Include("Horarios").Where(x => x.EstacionamientoId == estacionamientoId).AsNoTracking().ToListAsync();

                if (lista == null)
                {
                    return NotFound();
                }
                return lista;

            }
            catch (Exception e)
            {

                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }

        /////////////////////////// POST ///////////////////////////

        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult<List<EstacionamientoDTO>>> BusquedaAsync([FromBody] ParametroBusquedaDTO filtros)
        {
            try
            {
                using var dataContext = new DataContext();

                if (!dataContext.Estacionamientos.Any())
                    return null;

                var query = dataContext.Estacionamientos
                    .Include(e => e.Jornadas)
                        .ThenInclude(j => j.Horarios)
                    .Include(e => e.Plazas)
                    .Include(e => e.Tarifas)
                    .AsNoTracking()
                    .Where(e => !e.Inactivo && !e.PublicacionPausada)
                    .AsQueryable();

                // Filtro por tipo de lugar
                if (filtros.TipoDeLugars != null && filtros.TipoDeLugars.Any())
                {
                    query = query.Where(e => filtros.TipoDeLugars.Contains(e.TipoDeLugar));
                }

                // Filtro por tipo de vehículo admitido
                if (filtros.TipoDeVehiculos != null && filtros.TipoDeVehiculos.Any())
                {
                    // Normalizamos a minúsculas
                    var tiposNormalizados = filtros.TipoDeVehiculos.Select(t => t.ToLower()).ToList();

                    // Mapeamos los strings a enums
                    var tiposDeVehiculoEnum = new List<TipoDeVehiculo>();

                    if (tiposNormalizados.Contains("auto"))
                        tiposDeVehiculoEnum.Add(TipoDeVehiculo.AUTO);

                    if (tiposNormalizados.Contains("camioneta"))
                        tiposDeVehiculoEnum.Add(TipoDeVehiculo.CAMIONETA);

                    if (tiposNormalizados.Contains("moto"))
                        tiposDeVehiculoEnum.Add(TipoDeVehiculo.MOTO);

                    // Filtramos estacionamientos que tengan AL MENOS UNO de los tipos solicitados
                    if (tiposDeVehiculoEnum.Any())
                    {
                        query = query.Where(e => e.Plazas.Any(p => tiposDeVehiculoEnum.Contains(p.TipoDeVehiculo)));
                    }
                }

                // Filtro por ciudad
                if (!string.IsNullOrWhiteSpace(filtros.Ciudad))
                {
                    query = query.Where(e => e.Ciudad.Contains(filtros.Ciudad));
                }

                // Filtro por monto mínimo
                if (filtros.MontoReservaMinimo.HasValue)
                {
                    query = query.Where(e => e.MontoReserva >= filtros.MontoReservaMinimo.Value);
                }

                // Filtro por monto máximo
                if (filtros.MontoReservaMaximo.HasValue)
                {
                    query = query.Where(e => e.MontoReserva <= filtros.MontoReservaMaximo.Value);
                }

                var listaDTO = new List<ServiceWebApi.DTO.EstacionamientoDTO>();

                foreach (var item in await query.ToListAsync())
                {
                    var estacionamientoDTO = new ServiceWebApi.DTO.EstacionamientoDTO();
                    estacionamientoDTO = Tools.Tools.PropertyCopier<Estacionamiento, ServiceWebApi.DTO.EstacionamientoDTO>.Copy(item, estacionamientoDTO);

                    // Calculate the average Puntaje, handling empty sequences
                    var reseñas = await dataContext.Reseñas
                        .Where(r => r.EstacionamientoId == item.Id)
                        .ToListAsync();
                    var averagePuntaje = reseñas.Any() ? reseñas.Average(r => r.Puntaje ?? 0) : 0;

                    estacionamientoDTO.Puntaje = averagePuntaje;

                    listaDTO.Add(estacionamientoDTO);
                }

                return Ok(listaDTO);
            }
            catch (Exception e)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }


        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult> AddAsync([FromBody] EstacionamientoDTO estacionamientoDTO)
        {
            try
            {
                DataContext dataContext = new DataContext();

                // Copiamos los datos básicos
                Estacionamiento estacionamiento = new Estacionamiento();
                estacionamiento = Tools.Tools.PropertyCopier<EstacionamientoDTO, Estacionamiento>.Copy(estacionamientoDTO, estacionamiento);
                estacionamiento.UserId = _UserId;
                estacionamiento.FechaCreacion = DateTime.Now;

                // Guardamos primero el estacionamiento para obtener el Id
                await dataContext.Estacionamientos.AddAsync(estacionamiento);
                await dataContext.SaveChangesAsync();

                string fileName = null;

                // Guardar imagen si viene
                if (estacionamientoDTO.ImageBytes != null && estacionamientoDTO.ImageBytes.Length > 0)
                {
                    // Carpeta desde configuración
                    var folder = _configuration.GetValue<string>("EasyParkingAPI:Images:Estacionamientos_Folder");

                    if (string.IsNullOrEmpty(folder))
                        return BadRequest("Ruta de almacenamiento no configurada");

                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    // Nombre del archivo = Id del estacionamiento + extensión
                    fileName = estacionamiento.Id + ".jpg";
                    var path = Path.Combine(folder, fileName);

                    await System.IO.File.WriteAllBytesAsync(path, estacionamientoDTO.ImageBytes);
                }

                // Si se guardó imagen, actualizar la URL pública
                if (!string.IsNullOrEmpty(fileName))
                {
                    estacionamiento.URL = $"http://40.118.242.96:12595/images/estacionamientos/{fileName}";
                    dataContext.Estacionamientos.Update(estacionamiento);
                    await dataContext.SaveChangesAsync();
                }

                return Ok(new { estacionamiento.Id, estacionamiento.URL });
            }
            catch (Exception e)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }


        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult> SetActivoAsync([FromBody] int estacionamientoId)
        {
            try
            {
                DataContext dataContext = new DataContext();
                var estacionamiento = dataContext.Estacionamientos.Where(x => x.Id == estacionamientoId).FirstOrDefault();
                estacionamiento.Inactivo = false;
                dataContext.Estacionamientos.Update(estacionamiento);
                await dataContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }


        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult> SetInactivoAsync([FromBody] int estacionamientoId)
        {
            try
            {
                DataContext dataContext = new DataContext();
                var estacionamiento = dataContext.Estacionamientos.Where(x => x.Id == estacionamientoId).FirstOrDefault();
                estacionamiento.Inactivo = true;
                dataContext.Estacionamientos.Update(estacionamiento);
                await dataContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }


        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult> SetPublicacionPausadaAsync([FromBody] int estacionamientoId)
        {
            try
            {
                DataContext dataContext = new DataContext();
                var estacionamiento = dataContext.Estacionamientos.Where(x => x.Id == estacionamientoId).FirstOrDefault();
                estacionamiento.PublicacionPausada = true;
                dataContext.Estacionamientos.Update(estacionamiento);
                await dataContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }


        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult> SetReanudarPublicacionAsync([FromBody] int estacionamientoId)
        {
            try
            {
                DataContext dataContext = new DataContext();
                var estacionamiento = dataContext.Estacionamientos.Where(x => x.Id == estacionamientoId).FirstOrDefault();
                estacionamiento.PublicacionPausada = false;
                dataContext.Estacionamientos.Update(estacionamiento);
                await dataContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }



        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult> UpdateAsync([FromBody] EstacionamientoDTO estacionamientoDTO)
        {
            try
            {
                DataContext dataContext = new DataContext();

                // Obtenemos el estacionamiento existente
                var estacionamiento = await dataContext.Estacionamientos
                    .FirstOrDefaultAsync(x => x.Id == estacionamientoDTO.Id);

                if (estacionamiento == null)
                    return NotFound("Estacionamiento no encontrado");

                // Copiamos propiedades editables
                estacionamiento = Tools.Tools.PropertyCopier<EstacionamientoDTO, Estacionamiento>
                    .Copy(estacionamientoDTO, estacionamiento);

               

                // Manejo de Tipos de Vehículos
                var lista_Jornadas = dataContext.Jornadas
                    .Where(x => x.EstacionamientoId == estacionamiento.Id);

                foreach (var item in lista_Jornadas)
                {
                    dataContext.Jornadas.Remove(item);
                }

                var tarifas = dataContext.Tarifas
                        .Where(x => x.EstacionamientoId == estacionamiento.Id);

                foreach (var item in tarifas)
                {
                    dataContext.Tarifas.Remove(item);
                }



                // Guardar/Actualizar imagen
                string fileName = null;
                if (estacionamientoDTO.ImageBytes != null && estacionamientoDTO.ImageBytes.Length > 0)
                {
                    var folder = _configuration.GetValue<string>("EasyParkingAPI:Images:Estacionamientos_Folder");

                    if (string.IsNullOrEmpty(folder))
                        return BadRequest("Ruta de almacenamiento no configurada");

                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    // Reemplazar imagen con el mismo nombre
                    fileName = estacionamiento.Id + ".jpg";
                    var path = Path.Combine(folder, fileName);

                    await System.IO.File.WriteAllBytesAsync(path, estacionamientoDTO.ImageBytes);

                    // Actualizamos URL pública
                    estacionamiento.URL = $"http://40.118.242.96:12595/images/estacionamientos/{fileName}";
                }

                dataContext.Estacionamientos.Update(estacionamiento);
                await dataContext.SaveChangesAsync();

                return Ok(new { estacionamiento.Id, estacionamiento.URL });
            }
            catch (Exception e)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }


        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult> UploadsAsync([FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No se recibió ningún archivo");

                // Carpeta desde config
                var folder = _configuration.GetValue<string>("EasyParkingAPI:Images:Estacionamientos_Folder");

                if (string.IsNullOrEmpty(folder))
                    return BadRequest("Ruta de almacenamiento no configurada");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                // Usar el nombre tal cual viene en el parámetro (ya debería ser único)
                var fileName = Path.GetFileName(file.FileName); // aseguramos que no vengan rutas extrañas
                var path = Path.Combine(folder, fileName);

                using var stream = new FileStream(path, FileMode.Create);
                await file.CopyToAsync(stream);

                var result = new FileUploadResult()
                {
                    Length = file.Length,
                    Name = fileName
                };

                return Ok(result);
            }
            catch (Exception e)
            {
                return StatusCode(500, $"Error interno: {e.Message}");
            }
        }

        [HttpGet("GetImagen/{fileName}")]
        public IActionResult GetImagen(string fileName)
        {
            var folder = _configuration.GetValue<string>("EasyParkingAPI:Images:Estacionamientos_Folder");
            var path = Path.Combine(folder, fileName);

            if (!System.IO.File.Exists(path))
                return NotFound();

            var contentType = "image/" + Path.GetExtension(fileName).TrimStart('.');
            var fileBytes = System.IO.File.ReadAllBytes(path);
            return File(fileBytes, contentType);
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult<List<Estacionamiento>>> GetConsultaGenericaAsync([FromBody] ServiceWebApi.DTO.ParametroBusquedaDTO parametroBusquedaDTO)
        {
            return NotFound();

            //try
            //{
            //    DataContext dataContext = new DataContext(); //

            //    //var qweryResult = qweryActivoInactivo(true).Intersect(qweryUsuario(_UserId)); // Hasta aca, solo se prepara el qwery
            //    //var qweryResult = qweryActivoInactivo(true).Intersect(qweryUsuario(_UserId)); // Hasta aca, solo se prepara el qwery

            //    IQueryable<Estacionamiento> qweryResult;
            //    IQueryable<Estacionamiento> qweryResult_TipoDeVehiculo;
            //    IQueryable<Estacionamiento> qweryResult_TipoDeLugar;

            //    qweryResult = qweryActivoInactivo(dataContext, parametroBusquedaDTO.Inactivo);


            //    if (!string.IsNullOrEmpty(parametroBusquedaDTO.Ciudad))
            //    {
            //        qweryResult = qweryResult.Intersect(qweryCiudad(dataContext, parametroBusquedaDTO.Ciudad));
            //    }

            //    if (!parametroBusquedaDTO.TipoDeVehiculos.Contains("Todos los vehiculos") && parametroBusquedaDTO.TipoDeVehiculos.Count != 0)
            //    {
            //        List<IQueryable<Estacionamiento>> Lista_qweryResult_TipoDeVehiculo = new List<IQueryable<Estacionamiento>>();

            //        foreach (var vehiculo in parametroBusquedaDTO.TipoDeVehiculos)
            //        {
            //            Lista_qweryResult_TipoDeVehiculo.Add(qweryTipoDeVehiculo(dataContext, vehiculo));
            //        }

            //        if (Lista_qweryResult_TipoDeVehiculo != null && Lista_qweryResult_TipoDeVehiculo.Count != 0)
            //        {
            //            qweryResult_TipoDeVehiculo = Lista_qweryResult_TipoDeVehiculo[0];

            //            if (Lista_qweryResult_TipoDeVehiculo.Count > 1)
            //            {
            //                for (int i = 1; i <= Lista_qweryResult_TipoDeVehiculo.Count - 1; i++)
            //                {
            //                    qweryResult_TipoDeVehiculo = qweryResult_TipoDeVehiculo.Union(Lista_qweryResult_TipoDeVehiculo[i]);
            //                }
            //            }

            //            qweryResult = qweryResult.Intersect(qweryResult_TipoDeVehiculo);

            //        }
            //    }

            //    if (!parametroBusquedaDTO.TipoDeLugars.Contains("Todos los lugares") && parametroBusquedaDTO.TipoDeLugars.Count != 0)
            //    {
            //        List<IQueryable<Estacionamiento>> Lista_qweryResult_TipoDeLugar = new List<IQueryable<Estacionamiento>>();

            //        foreach (var lugar in parametroBusquedaDTO.TipoDeLugars)
            //        {
            //            Lista_qweryResult_TipoDeLugar.Add(qweryTipoDeLugar(dataContext, lugar));
            //        }

            //        if (Lista_qweryResult_TipoDeLugar != null && Lista_qweryResult_TipoDeLugar.Count != 0)
            //        {
            //            qweryResult_TipoDeLugar = Lista_qweryResult_TipoDeLugar[0];

            //            if (Lista_qweryResult_TipoDeLugar.Count > 1)
            //            {
            //                for (int i = 1; i <= Lista_qweryResult_TipoDeLugar.Count - 1; i++)
            //                {
            //                    qweryResult_TipoDeLugar = qweryResult_TipoDeLugar.Union(Lista_qweryResult_TipoDeLugar[i]);
            //                }
            //            }

            //            qweryResult = qweryResult.Intersect(qweryResult_TipoDeLugar);

            //        }
            //    }


            //    if (qweryResult == null)
            //    {
            //        return NotFound();
            //    }

            //    var x = await qweryResult.Include("Jornadas.Horarios").Include("TiposDeVehiculosAdmitidos").AsNoTracking().ToListAsync(); // Aca se hace la busqueda efectivamente
            //    return x;

            //}
            //catch (Exception e)
            //{

            //    return BadRequest(Tools.Tools.ExceptionMessage(e));
            //}


            //IQueryable<Estacionamiento> qweryActivoInactivo(DataContext dataContext, bool Inactivo)
            //{
            //    return dataContext.Estacionamientos.Where(x => x.Inactivo == Inactivo);
            //}


            //IQueryable<Estacionamiento> qweryTipoDeLugar(DataContext dataContext, string TipoDeLugar)
            //{
            //    return dataContext.Estacionamientos.Where(x => x.TipoDeLugar.ToLower().Trim() == TipoDeLugar.ToLower().Trim());
            //}

            //IQueryable<Estacionamiento> qweryMontoReservaMayorA(DataContext dataContext, decimal MontoReserva)
            //{
            //    return dataContext.Estacionamientos.Where(x => x.MontoReserva >= MontoReserva);
            //}

            //IQueryable<Estacionamiento> qweryMontoReservaMenorA(DataContext dataContext, decimal MontoReserva)
            //{
            //    return dataContext.Estacionamientos.Where(x => x.MontoReserva <= MontoReserva);
            //}

            //IQueryable<Estacionamiento> qweryCiudad(DataContext dataContext, string Ciudad)
            //{
            //    return dataContext.Estacionamientos.Where(x => x.Ciudad == Ciudad);
            //}

            //IQueryable<Estacionamiento> qweryTipoDeVehiculo(DataContext dataContext, string TipoDeVehiculo)
            //{
            //    var query =
            //               from est in dataContext.Estacionamientos
            //               join datav in dataContext.DataVehiculoAlojados on est equals datav.Estacionamiento
            //               where datav.TipoDeVehiculo == TipoDeVehiculo
            //               select est;

            //    return query;

            //    IQueryable<Estacionamiento> qweryUsuario(DataContext dataContext, string UserId)
            //    {
            //        if (UserId == null)
            //        {
            //            return dataContext.Estacionamientos;
            //        }
            //        else
            //        {
            //            return dataContext.Estacionamientos.Where(x => x.UserId == UserId);
            //        }
            //    }
            //}

        }

        /////////////////////////// DELETE ///////////////////////////

        [HttpDelete("[action]/{estacionamientoId}")]
        public async Task<ActionResult> DeleteAsync(int estacionamientoId)
        {
            try
            {
                DataContext dataContext = new DataContext();
                var estacionamiento = await dataContext.Estacionamientos.FirstOrDefaultAsync(x => x.Id == estacionamientoId);
                dataContext.Estacionamientos.Remove(estacionamiento);
                await dataContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }
    }
}

