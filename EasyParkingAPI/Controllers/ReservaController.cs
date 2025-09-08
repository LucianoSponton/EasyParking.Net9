using EasyParkingAPI.Data;
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
using Model.Enums;
using ServiceWebApi.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EasyParkingAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class ReservaController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _UserId;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EasyParkingAuthContext _EasyParkingAuthContext;

        public ReservaController(IConfiguration configuration, EasyParkingAuthContext EasyParkingAuthContext,
                                            IHttpContextAccessor httpContextAccessor,
                                            UserManager<ApplicationUser> userManager)
        {
            try
            {
                _configuration = configuration;
                _userManager = userManager; 
                _EasyParkingAuthContext = EasyParkingAuthContext;

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

        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<Reserva>>> GetAllAsync()
        {
            try
            {
                DataContext dataContext = new DataContext();
                var lista = await dataContext.Reservas.AsNoTracking().ToListAsync();
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
        [Route("[action]/{estado}")]
        public async Task<ActionResult<List<ReservaDTO>>> GetMisReservasAsync(EstadoReserva estado)
        {
            try
            {
                using var dataContext = new DataContext();

                IQueryable<Reserva> query = dataContext.Reservas
                    .Where(x => x.UserId == _UserId)
                    .AsNoTracking();

                // Aplicamos filtros según el estado recibido
                switch (estado)
                {
                    case EstadoReserva.NONE:
                        query = query.Where(x => x.Estado == EstadoReserva.ESPERANDO_ARRIBO
                                              || x.Estado == EstadoReserva.ARRIBO_EXITOSO);
                        break;

                    case EstadoReserva.ESPERANDO_ARRIBO:
                        query = query.Where(x => x.Estado == EstadoReserva.ESPERANDO_ARRIBO);
                        break;

                    case EstadoReserva.ARRIBO_EXITOSO:
                        query = query.Where(x => x.Estado == EstadoReserva.ARRIBO_EXITOSO);
                        break;

                    case EstadoReserva.CANCELADO_POR_EL_CLIENTE:
                    case EstadoReserva.CANCELADO_POR_EL_DUEÑO:
                    case EstadoReserva.CANCELADO_POR_TIEMPO_EXPIRADO:
                        query = query.Where(x => x.Estado == EstadoReserva.CANCELADO_POR_EL_CLIENTE
                                              || x.Estado == EstadoReserva.CANCELADO_POR_EL_DUEÑO
                                              || x.Estado == EstadoReserva.CANCELADO_POR_TIEMPO_EXPIRADO);
                        break;

                    case EstadoReserva.SE_HA_MARCHADO:
                        query = query.Where(x => x.Estado == EstadoReserva.SE_HA_MARCHADO);
                        break;

                    case EstadoReserva.TODOS:
                        // no filtramos nada extra
                        break;

                    default:
                        return BadRequest("Estado no válido");
                }

                var reservas = await query.ToListAsync();

                if (!reservas.Any())
                    return NotFound("No hay reservas en este estado");

                List<ReservaDTO> listaDTO = new List<ReservaDTO>();

                foreach (var item in reservas)
                {
                    ReservaDTO reservaDTO = Tools.Tools.PropertyCopier<Reserva, ReservaDTO>.Copy(item, new ReservaDTO());

                    var estacionamiento = await dataContext.Estacionamientos
                        .Include(e => e.Jornadas)
                            .ThenInclude(j => j.Horarios)
                        .Include(e => e.TiposDeVehiculosAdmitidos)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == item.EstacionamientoId);

                    if (estacionamiento != null)
                    {
                        EstacionamientoDTO estacionamientoDTO = new EstacionamientoDTO();
                        reservaDTO.EstacionamientoDTO = Tools.Tools.PropertyCopier<Estacionamiento, EstacionamientoDTO>.Copy(estacionamiento, estacionamientoDTO);
                    }

                    listaDTO.Add(reservaDTO);
                }

                return Ok(listaDTO);
            }
            catch (Exception e)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }

        [HttpGet]
        [Route("[action]/{estado}")]
        public async Task<ActionResult<List<ReservaDTO>>> GetReservasModalidadDueñoAsync(EstadoReserva estado)
        {
            try
            {
                DataContext dataContext = new DataContext();

                // Estacionamientos del dueño

                var estacionamientos = await dataContext.Estacionamientos.Include("Jornadas.Horarios").AsNoTracking()
                                    .Include("TiposDeVehiculosAdmitidos").AsNoTracking().Where(x => !x.Inactivo && !x.PublicacionPausada && x.UserId == _UserId).ToListAsync();

                if (estacionamientos == null)
                {
                    return NotFound();
                }

                // Ids de esos estacionamientos
                var estacionamientoIds = estacionamientos.Select(e => e.Id).ToList();

                // Reservas asociadas a esos estacionamientos
                var reservas = await dataContext.Reservas
                    .Where(r => estacionamientoIds.Contains(r.EstacionamientoId) && r.Estado == estado)
                    .AsNoTracking()
                    .ToListAsync();
          
                if (reservas == null)
                {
                    return NotFound();
                }

                List<ReservaDTO> listaDTO = new List<ReservaDTO>();

                foreach (var item in reservas)
                {
                    ServiceWebApi.DTO.ReservaDTO reservaDTO = new ServiceWebApi.DTO.ReservaDTO();
                    reservaDTO = Tools.Tools.PropertyCopier<Reserva, ServiceWebApi.DTO.ReservaDTO>.Copy(item, reservaDTO);

                    var estacionamiento = estacionamientos.Where(x => x.Id == item.EstacionamientoId).FirstOrDefault();

                    ServiceWebApi.DTO.EstacionamientoDTO estacionamientoDTO = new ServiceWebApi.DTO.EstacionamientoDTO();
                    reservaDTO.EstacionamientoDTO = Tools.Tools.PropertyCopier<Estacionamiento, ServiceWebApi.DTO.EstacionamientoDTO>.Copy(estacionamiento, estacionamientoDTO);
                    //reservaDTO.EstacionamientoDTO.Jornadas = estacionamiento.Jornadas;
                    //reservaDTO.EstacionamientoDTO.TiposDeVehiculosAdmitidos = estacionamiento.TiposDeVehiculosAdmitidos;

                    ApplicationUser appuser = _userManager.FindByIdAsync(reservaDTO.UserId).Result; // Obtengo los datos del usuario logeado
                    reservaDTO.Nombre = appuser.Nombre;
                    reservaDTO.Apellido = appuser.Apellido;
                    reservaDTO.TipoDeVehiculo = dataContext.Vehiculos.Where(x => x.UserId == reservaDTO.UserId).Select(e => e.TipoDeVehiculo).FirstOrDefault();

                    listaDTO.Add(reservaDTO);
                }
 


                return listaDTO;

            }
            catch (Exception e)
            {

                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }

        [HttpGet]
        [Route("[action]/{estado}")]
        public async Task<ActionResult<int>> GetCantidadReservasPorEstadoAsync(EstadoReserva estado)
        {
            try
            {
                DataContext dataContext = new DataContext();

                // Estacionamientos del dueño

                var estacionamientos = await dataContext.Estacionamientos.Where(x => !x.Inactivo && !x.PublicacionPausada && x.UserId == _UserId).ToListAsync();

                if (estacionamientos == null)
                {
                    return NotFound();
                }

                // Ids de esos estacionamientos
                var estacionamientoIds = estacionamientos.Select(e => e.Id).ToList();

                // Reservas asociadas a esos estacionamientos
                var reservas = await dataContext.Reservas
                    .Where(r => estacionamientoIds.Contains(r.EstacionamientoId) && r.Estado == estado)
                    .AsNoTracking()
                    .ToListAsync();

                if (reservas == null)
                {
                    return NotFound();
                }

                return reservas.Count;

            }
            catch (Exception e)
            {

                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }

        [HttpPost]
        [Route("[action]")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, AppUser")]
        public async Task<ActionResult> AddAsync([FromBody] Reserva reserva)
        {
            try
            {
                DataContext dataContext = new DataContext();
                reserva.UserId = _UserId;
               
                var vehiculo = await dataContext.Vehiculos.Where(x => x.Id == reserva.VehiculoId).FirstOrDefaultAsync();

                if(vehiculo == null)
                    return BadRequest("ERROR.. No se encontro su vehículo");

                var datoVehiculoSobreAlojado = await dataContext.DataVehiculoAlojados.Where(x => x.EstacionamientoId == reserva.EstacionamientoId && x.TipoDeVehiculo == vehiculo.TipoDeVehiculo).FirstOrDefaultAsync();

                if (datoVehiculoSobreAlojado == null)
                    return BadRequest($"ERROR.. El tipo de su vehículo ({vehiculo.TipoDeVehiculo}) no puede ser alojado en este lugar, ya que no es admitido");

                if (datoVehiculoSobreAlojado.CapacidadDeAlojamiento > datoVehiculoSobreAlojado.CantidadActualAlojados)
                {
                    datoVehiculoSobreAlojado.CantidadActualAlojados++;
                }
                else
                {
                    return BadRequest("ERROR.. No hay más cupos disponibles para este vehículo");
                }

                dataContext.DataVehiculoAlojados.Update(datoVehiculoSobreAlojado);
                await dataContext.Reservas.AddAsync(reserva);
                await dataContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }


        [HttpDelete("[action]/{estacionamientoId}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, AppUser")]
        public async Task<ActionResult> DeleteAsync(int reservaId)
        {
            try
            {
                DataContext dataContext = new DataContext();
                var reserva = await dataContext.Reservas.FirstOrDefaultAsync(x => x.Id == reservaId && x.UserId == _UserId);
                dataContext.Reservas.Remove(reserva);
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
        public async Task<ActionResult> SetReservaCanceladaPorElClienteAsync([FromBody] int reservaId)
        {
            try
            {
                DataContext dataContext = new DataContext();
                var reserva = dataContext.Reservas.Where(x => x.Id == reservaId).FirstOrDefault();
                reserva.Estado = EstadoReserva.CANCELADO_POR_EL_CLIENTE;

                var vehiculo = await dataContext.Vehiculos.Where(x => x.Id == reserva.VehiculoId).FirstOrDefaultAsync();

                var datoVehiculoSobreAlojado = await dataContext.DataVehiculoAlojados.Where(x => x.EstacionamientoId == reserva.EstacionamientoId && x.TipoDeVehiculo == vehiculo.TipoDeVehiculo).FirstOrDefaultAsync();

                if (datoVehiculoSobreAlojado.CantidadActualAlojados > 0)
                {
                    datoVehiculoSobreAlojado.CantidadActualAlojados--;
                }

                dataContext.DataVehiculoAlojados.Update(datoVehiculoSobreAlojado);

                dataContext.Reservas.Update(reserva);
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
        public async Task<ActionResult> SetReservaCanceladaPorElDueñoAsync([FromBody] int reservaId)
        {
            try
            {
                DataContext dataContext = new DataContext();
                var reserva = dataContext.Reservas.Where(x => x.Id == reservaId).FirstOrDefault();
                reserva.Estado = EstadoReserva.CANCELADO_POR_EL_DUEÑO;

                var vehiculo = await dataContext.Vehiculos.Where(x => x.Id == reserva.VehiculoId).FirstOrDefaultAsync();

                var datoVehiculoSobreAlojado = await dataContext.DataVehiculoAlojados.Where(x => x.EstacionamientoId == reserva.EstacionamientoId && x.TipoDeVehiculo == vehiculo.TipoDeVehiculo).FirstOrDefaultAsync();

                if (datoVehiculoSobreAlojado.CantidadActualAlojados > 0)
                {
                    datoVehiculoSobreAlojado.CantidadActualAlojados--;
                }

                dataContext.DataVehiculoAlojados.Update(datoVehiculoSobreAlojado);

                dataContext.Reservas.Update(reserva);
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
        public async Task<ActionResult> SetReservaCanceladaPorTiempoExpiradoAsync([FromBody] int reservaId)
        {
            try
            {
                DataContext dataContext = new DataContext();
                var reserva = dataContext.Reservas.Where(x => x.Id == reservaId).FirstOrDefault();
                reserva.Estado = EstadoReserva.CANCELADO_POR_TIEMPO_EXPIRADO;

                var vehiculo = await dataContext.Vehiculos.Where(x => x.Id == reserva.VehiculoId).FirstOrDefaultAsync();

                var datoVehiculoSobreAlojado = await dataContext.DataVehiculoAlojados.Where(x => x.EstacionamientoId == reserva.EstacionamientoId && x.TipoDeVehiculo == vehiculo.TipoDeVehiculo).FirstOrDefaultAsync();

                if (datoVehiculoSobreAlojado.CantidadActualAlojados > 0)
                {
                    datoVehiculoSobreAlojado.CantidadActualAlojados--;
                }

                dataContext.DataVehiculoAlojados.Update(datoVehiculoSobreAlojado);

                dataContext.Reservas.Update(reserva);
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
        public async Task<ActionResult> SetReservaArriboExitosoAsync([FromBody] int reservaId)
        {
            try
            {
                DataContext dataContext = new DataContext();
                var reserva = dataContext.Reservas.Where(x => x.Id == reservaId).FirstOrDefault();
                reserva.Estado = EstadoReserva.ARRIBO_EXITOSO;
                reserva.FechaDeArribo = DateTime.Now;

                dataContext.Reservas.Update(reserva);
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
        public async Task<ActionResult> SetReservaSeHaMarchadoAsync([FromBody] int reservaId)
        {
            try
            {
                DataContext dataContext = new DataContext();
                var reserva = dataContext.Reservas.Where(x => x.Id == reservaId).FirstOrDefault();
                reserva.Estado = EstadoReserva.SE_HA_MARCHADO;
                reserva.FechaDeSalida = DateTime.Now;

                var vehiculo = await dataContext.Vehiculos.Where(x => x.Id == reserva.VehiculoId).FirstOrDefaultAsync();

                var datoVehiculoSobreAlojado = await dataContext.DataVehiculoAlojados.Where(x => x.EstacionamientoId == reserva.EstacionamientoId && x.TipoDeVehiculo == vehiculo.TipoDeVehiculo).FirstOrDefaultAsync();

                if (datoVehiculoSobreAlojado.CantidadActualAlojados > 0)
                {
                    datoVehiculoSobreAlojado.CantidadActualAlojados--;
                }

                dataContext.DataVehiculoAlojados.Update(datoVehiculoSobreAlojado);

                dataContext.Reservas.Update(reserva);
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
