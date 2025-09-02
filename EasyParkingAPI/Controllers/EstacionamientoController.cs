using EasyParkingAPI.Data;
using EasyParkingAPI.DTO;
using EasyParkingAPI.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Model;
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
                .Include("TiposDeVehiculosAdmitidos").AsNoTracking().Where(x => x.Inactivo == false && x.PublicacionPausada == false).AsNoTracking().ToListAsync();

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
        public async Task<ActionResult<List<Estacionamiento>>> GetMisEstacionamientosAsync()
        {
            try
            {
                DataContext dataContext = new DataContext();
                var lista = await dataContext.Estacionamientos.Include("Jornadas.Horarios")
                .Include("TiposDeVehiculosAdmitidos").Where(x => x.UserId == _UserId && x.Inactivo == false).AsNoTracking().ToListAsync(); // Retorna los estacionamientos de la persona logeada
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



        [HttpGet("[action]/{estacionamientoId}")]
        public async Task<ActionResult<Estacionamiento>> GetAsync(int estacionamientoId)
        {
            try
            {
                DataContext dataContext = new DataContext();
                var estacionamiento = await dataContext.Estacionamientos.Include("Jornadas.Horarios")
                  .Include("TiposDeVehiculosAdmitidos").FirstOrDefaultAsync(x => x.Id == estacionamientoId);

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
        public async Task<ActionResult<List<Estacionamiento>>> GetOrderByReservasAsync()
        {
            try
            {
                DataContext dataContext = new DataContext();
                var estacionamiento = await dataContext.Estacionamientos.Include("Jornadas.Horarios")
                                            .Include("TiposDeVehiculosAdmitidos").OrderBy(x => x.MontoReserva).ToListAsync();

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
                .Include("TiposDeVehiculosAdmitidos").Where(x => x.Direccion.Contains(text) || x.Nombre.Contains(text)).ToListAsync();

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
        public async Task<ActionResult<List<Estacionamiento>>> GetByTiposDeVehiculosAdmitidosAsync(string vehiculo) // consulta por nombre o direccion del estacionamiento
        {
            try
            {
                DataContext dataContext = new DataContext();
                var estacionamientos = await dataContext.Estacionamientos.Include("Jornadas.Horarios")
                .Include("TiposDeVehiculosAdmitidos").ToListAsync();


                if (estacionamientos == null)
                {
                    return NotFound();
                }

                List<Estacionamiento> lista = new List<Estacionamiento>();

                foreach (var item in estacionamientos)
                {
                    foreach (var i in item.TiposDeVehiculosAdmitidos)
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
                .Include("TiposDeVehiculosAdmitidos").Where(x => x.TipoDeLugar == tipoDeLugar).ToListAsync();


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

        //[HttpGet]
        //[Route("[action]")]
        //public async Task<ActionResult<List<Estacionamiento>>> GetAbiertosAsync()
        //{
        //    try
        //    {
        //        DataContext dataContext = new DataContext();
        //        var estacionamientos = await dataContext.Estacionamientos.Include("Jornadas.Horarios")
        //        .Include("TiposDeVehiculosAdmitidos").Where(x => x.Jornadas.Contains(text) || x.Nombre.Contains(text)).ToListAsync();

        //        if (estacionamientos == null)
        //        {
        //            return NotFound();
        //        }
        //        return estacionamientos;

        //    }
        //    catch (Exception e)
        //    {

        //        return BadRequest(Tools.Tools.ExceptionMessage(e));
        //    }
        //}


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
                DataContext dataContext = new DataContext();

                var query = dataContext.Estacionamientos
                    .Include(e => e.Jornadas)
                        .ThenInclude(j => j.Horarios)
                    .Include(e => e.TiposDeVehiculosAdmitidos)
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
                    query = query.Where(e =>
                        e.TiposDeVehiculosAdmitidos.Any(v =>
                            filtros.TipoDeVehiculos.Contains(v.TipoDeVehiculo)));
                }

                List<ServiceWebApi.DTO.EstacionamientoDTO> listaDTO = new List<ServiceWebApi.DTO.EstacionamientoDTO>();


                foreach (var item in query)
                {
                    ServiceWebApi.DTO.EstacionamientoDTO estacionamientoDTO = new ServiceWebApi.DTO.EstacionamientoDTO();
                    estacionamientoDTO = Tools.Tools.PropertyCopier<Estacionamiento, ServiceWebApi.DTO.EstacionamientoDTO>.Copy(item, estacionamientoDTO);
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
        public async Task<ActionResult<List<EstacionamientoDTO>>> BusquedaAsync([FromBody] string textoBusqueda)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textoBusqueda))
                    return BadRequest("El texto de búsqueda no puede estar vacío.");

                DataContext dataContext = new DataContext();

                string texto = textoBusqueda.Trim().ToLower();

                var query = dataContext.Estacionamientos
                    .Include(e => e.Jornadas)
                        .ThenInclude(j => j.Horarios)
                    .Include(e => e.TiposDeVehiculosAdmitidos)
                    .AsNoTracking()
                    .Where(e => !e.Inactivo && !e.PublicacionPausada)
                    .Where(e =>
                        e.Ciudad.ToLower().Contains(texto) ||
                        e.Nombre.ToLower().Contains(texto) ||
                        e.Direccion.ToLower().Contains(texto) ||
                        e.TipoDeLugar.ToLower().Contains(texto) ||
                        e.TiposDeVehiculosAdmitidos.Any(v => v.TipoDeVehiculo.ToLower().Contains(texto))
                    )
                    .AsQueryable();

                var estacionamientos = await query.ToListAsync();

                List<EstacionamientoDTO> listaDTO = new List<EstacionamientoDTO>();

                foreach (var item in estacionamientos)
                {
                    EstacionamientoDTO estacionamientoDTO = new EstacionamientoDTO();
                    estacionamientoDTO = Tools.Tools.PropertyCopier<Estacionamiento, EstacionamientoDTO>.Copy(item, estacionamientoDTO);
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, AppUser")]
        public async Task<ActionResult> AddAsync([FromBody] Estacionamiento estacionamiento)
        {
            try
            {
                DataContext dataContext = new DataContext();
                estacionamiento.UserId = _UserId;
                estacionamiento.FechaCreacion = DateTime.Now;

                await dataContext.Estacionamientos.AddAsync(estacionamiento);
                await dataContext.SaveChangesAsync();
                return Ok();

                //var ExisteUnoConMismoNombre = dataContext.Estacionamientos.Any(x => x.Nombre == estacionamiento.Nombre && x.UserId == _UserId); // De mis estacionamientos
                //var ExisteUnoConMismaDireccion = dataContext.Estacionamientos.Any(x => x.Direccion == estacionamiento.Direccion && x.Ciudad == estacionamiento.Ciudad && x.UserId == _UserId);  // De mis estacionamientos
                //var ExisteUnoConMismaDireccion_QueNoEsMio = dataContext.Estacionamientos.Any(x => x.Direccion == estacionamiento.Direccion && x.Ciudad == estacionamiento.Ciudad);  // De todos los existentes estacionamientos

                //if (ExisteUnoConMismoNombre == false)
                //{
                //    if (ExisteUnoConMismaDireccion == false)
                //    {
                //        if (ExisteUnoConMismaDireccion_QueNoEsMio == false)
                //        {

                //            await dataContext.Estacionamientos.AddAsync(estacionamiento);
                //            await dataContext.SaveChangesAsync();
                //            return Ok();
                //        }
                //        else
                //        {
                //            return BadRequest("Alguien mas ya registro un estacionamiento con la misma dirección y en la misma ciudad. Revise su información");
                //        }
                //    }
                //    else
                //    {
                //        return BadRequest("Usted ya tiene un estacionamiento con la misma dirección y en la misma ciudad.");
                //    }
                //}
                //else
                //{
                //    return BadRequest("Usted ya tiene un estacionamiento con el mismo nombre, debe elegir otro diferente.");
                //}


            }
            catch (Exception e)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }

        [HttpPost]
        [Route("[action]")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, AppUser")]
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, AppUser")]
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, AppUser")]
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, AppUser")]
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, AppUser")]
        public async Task<ActionResult> UpdateAsync([FromBody] Estacionamiento estacionamiento)
        {
            try
            {
                DataContext dataContext = new DataContext();
                var lista_TipoVehiculosAlojados = dataContext.DataVehiculoAlojados.Where(x => x.EstacionamientoId == estacionamiento.Id);

                foreach (var item in lista_TipoVehiculosAlojados)
                {
                    if (!item.Id.Equals(estacionamiento.TiposDeVehiculosAdmitidos))
                    {
                        dataContext.DataVehiculoAlojados.Remove(item);
                    }
                }
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, AppUser")]
        public async Task<ActionResult> UploadsAsync([FromBody] IFormFile file)
        {
            try
            {
                List<FileUploadResult> fileUploadResults = new List<FileUploadResult>();

                string fileName = Path.GetFileName(file.FileName);
                var path = Path.Combine(_configuration.GetValue<string>("EasyParkingAPI:Images:Estacionamientos_Folder"), fileName);

                var stream = new FileStream(path, FileMode.Create);
                await file.CopyToAsync(stream);
                stream.Close();
                fileUploadResults.Add(new FileUploadResult() { Length = file.Length, Name = file.FileName });

                return Ok(fileUploadResults);
            }
            catch (Exception e)
            {

                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }


        [HttpPost]
        [Route("[action]")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, AppUser")]
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, AppUser")]
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

