using EasyParkingAPI.Data;
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

        private string _From_SmtpServer;
        private int _From_SmtpServerPort;
        private string _From_Name;
        private string _From_EmailAdress;
        private string _From_EmailPassword;
        public ReservaController(IConfiguration configuration, EasyParkingAuthContext EasyParkingAuthContext,
                                            IHttpContextAccessor httpContextAccessor,
                                            UserManager<ApplicationUser> userManager)
        {
            try
            {
                _configuration = configuration;
                _userManager = userManager;
                _EasyParkingAuthContext = EasyParkingAuthContext;

                _From_SmtpServer = _configuration.GetValue<string>("EmailAccount:From_SmtpServer");
                _From_SmtpServerPort = _configuration.GetValue<int>("EmailAccount:From_SmtpServerPort");
                _From_Name = _configuration.GetValue<string>("EmailAccount:From_Name");
                _From_EmailAdress = _configuration.GetValue<string>("EmailAccount:From_EmailAdress");
                _From_EmailPassword = _configuration.GetSection("EmailAccount")["From_EmailPassword"];

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
                        query = query.Where(x => x.Estado == EstadoReserva.CANCELADO_POR_EL_CLIENTE);
                        break;

                    case EstadoReserva.CANCELADO_POR_EL_DUEÑO:
                        query = query.Where(x => x.Estado == EstadoReserva.CANCELADO_POR_EL_DUEÑO);
                        break;

                    case EstadoReserva.CANCELADO_POR_TIEMPO_EXPIRADO:
                        query = query.Where(x => x.Estado == EstadoReserva.CANCELADO_POR_TIEMPO_EXPIRADO);
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

        //[HttpPost]
        //[Route("[action]")]
        //public async Task<ActionResult> AddAsync([FromBody] Reserva reserva)
        //{
        //    try
        //    {
        //        DataContext dataContext = new DataContext();
        //        reserva.UserId = _UserId;

        //        var vehiculo = await dataContext.Vehiculos.Where(x => x.Id == reserva.VehiculoId).FirstOrDefaultAsync();

        //        if (vehiculo == null)
        //            return BadRequest("ERROR.. No se encontro su vehículo");

        //        var datoVehiculoSobreAlojado = await dataContext.DataVehiculoAlojados.Where(x => x.EstacionamientoId == reserva.EstacionamientoId && x.TipoDeVehiculo == vehiculo.TipoDeVehiculo).FirstOrDefaultAsync();

        //        if (datoVehiculoSobreAlojado == null)
        //            return BadRequest($"ERROR.. El tipo de su vehículo ({vehiculo.TipoDeVehiculo}) no puede ser alojado en este lugar, ya que no es admitido");

        //        if (datoVehiculoSobreAlojado.CapacidadDeAlojamiento > datoVehiculoSobreAlojado.CantidadActualAlojados)
        //        {
        //            datoVehiculoSobreAlojado.CantidadActualAlojados++;
        //        }
        //        else
        //        {
        //            return BadRequest("ERROR.. No hay más cupos disponibles para este vehículo");
        //        }

        //        dataContext.DataVehiculoAlojados.Update(datoVehiculoSobreAlojado);
        //        await dataContext.Reservas.AddAsync(reserva);
        //        await dataContext.SaveChangesAsync();
        //        return Ok();
        //    }
        //    catch (Exception e)
        //    {
        //        return BadRequest(Tools.Tools.ExceptionMessage(e));
        //    }
        //}


        [HttpDelete("[action]/{estacionamientoId}")]
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

        //[HttpPost]
        //[Route("[action]")]
        //public async Task<ActionResult> SetReservaCanceladaPorElClienteAsync([FromBody] int reservaId)
        //{
        //    try
        //    {
        //        DataContext dataContext = new DataContext();
        //        var reserva = dataContext.Reservas.Where(x => x.Id == reservaId).FirstOrDefault();
        //        reserva.Estado = EstadoReserva.CANCELADO_POR_EL_CLIENTE;

        //        var vehiculo = await dataContext.Vehiculos.Where(x => x.Id == reserva.VehiculoId).FirstOrDefaultAsync();

        //        var datoVehiculoSobreAlojado = await dataContext.DataVehiculoAlojados.Where(x => x.EstacionamientoId == reserva.EstacionamientoId && x.TipoDeVehiculo == vehiculo.TipoDeVehiculo).FirstOrDefaultAsync();

        //        if (datoVehiculoSobreAlojado.CantidadActualAlojados > 0)
        //        {
        //            datoVehiculoSobreAlojado.CantidadActualAlojados--;
        //        }

        //        dataContext.DataVehiculoAlojados.Update(datoVehiculoSobreAlojado);

        //        dataContext.Reservas.Update(reserva);
        //        await dataContext.SaveChangesAsync();
        //        return Ok();
        //    }
        //    catch (Exception e)
        //    {
        //        return BadRequest(Tools.Tools.ExceptionMessage(e));
        //    }
        //}

        //[HttpPost]
        //[Route("[action]")]
        //public async Task<ActionResult> SetReservaCanceladaPorElDueñoAsync([FromBody] int reservaId)
        //{
        //    try
        //    {
        //        DataContext dataContext = new DataContext();
        //        var reserva = dataContext.Reservas.Where(x => x.Id == reservaId).FirstOrDefault();
        //        reserva.Estado = EstadoReserva.CANCELADO_POR_EL_DUEÑO;

        //        var vehiculo = await dataContext.Vehiculos.Where(x => x.Id == reserva.VehiculoId).FirstOrDefaultAsync();

        //        var datoVehiculoSobreAlojado = await dataContext.DataVehiculoAlojados.Where(x => x.EstacionamientoId == reserva.EstacionamientoId && x.TipoDeVehiculo == vehiculo.TipoDeVehiculo).FirstOrDefaultAsync();

        //        if (datoVehiculoSobreAlojado.CantidadActualAlojados > 0)
        //        {
        //            datoVehiculoSobreAlojado.CantidadActualAlojados--;
        //        }

        //        dataContext.DataVehiculoAlojados.Update(datoVehiculoSobreAlojado);

        //        dataContext.Reservas.Update(reserva);
        //        await dataContext.SaveChangesAsync();
        //        return Ok();
        //    }
        //    catch (Exception e)
        //    {
        //        return BadRequest(Tools.Tools.ExceptionMessage(e));
        //    }
        //}

        [HttpPost]
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

        //[HttpPost]
        //[Route("[action]")]
        //public async Task<ActionResult> SetReservaArriboExitosoAsync([FromBody] int reservaId)
        //{
        //    try
        //    {
        //        DataContext dataContext = new DataContext();
        //        var reserva = dataContext.Reservas.Where(x => x.Id == reservaId).FirstOrDefault();
        //        reserva.Estado = EstadoReserva.ARRIBO_EXITOSO;
        //        reserva.FechaDeArribo = DateTime.Now;

        //        dataContext.Reservas.Update(reserva);
        //        await dataContext.SaveChangesAsync();
        //        return Ok();
        //    }
        //    catch (Exception e)
        //    {
        //        return BadRequest(Tools.Tools.ExceptionMessage(e));
        //    }
        //}

        //[HttpPost]
        //[Route("[action]")]
        //public async Task<ActionResult> SetReservaSeHaMarchadoAsync([FromBody] int reservaId)
        //{
        //    try
        //    {
        //        DataContext dataContext = new DataContext();
        //        var reserva = dataContext.Reservas.Where(x => x.Id == reservaId).FirstOrDefault();
        //        reserva.Estado = EstadoReserva.SE_HA_MARCHADO;
        //        reserva.FechaDeSalida = DateTime.Now;

        //        var vehiculo = await dataContext.Vehiculos.Where(x => x.Id == reserva.VehiculoId).FirstOrDefaultAsync();

        //        var datoVehiculoSobreAlojado = await dataContext.DataVehiculoAlojados.Where(x => x.EstacionamientoId == reserva.EstacionamientoId && x.TipoDeVehiculo == vehiculo.TipoDeVehiculo).FirstOrDefaultAsync();

        //        if (datoVehiculoSobreAlojado.CantidadActualAlojados > 0)
        //        {
        //            datoVehiculoSobreAlojado.CantidadActualAlojados--;
        //        }

        //        dataContext.DataVehiculoAlojados.Update(datoVehiculoSobreAlojado);

        //        dataContext.Reservas.Update(reserva);
        //        await dataContext.SaveChangesAsync();
        //        return Ok();
        //    }
        //    catch (Exception e)
        //    {
        //        return BadRequest(Tools.Tools.ExceptionMessage(e));
        //    }
        //}


        //-----------------------------------------------------

        // ============================================
        // MÉTODO AddAsync MEJORADO CON NOTIFICACIÓN POR EMAIL
        // ============================================

        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult> AddAsync([FromBody] Reserva reserva)
        {
            try
            {
                DataContext dataContext = new DataContext();
                reserva.UserId = _UserId;

                // Contrar unica reserva para determinado vehiculo
                var result = await dataContext.Reservas
                    .AnyAsync(x => x.UserId == reserva.UserId && x.Patente == reserva.Patente && (x.Estado == EstadoReserva.ESPERANDO_ARRIBO || x.Estado == EstadoReserva.ARRIBO_EXITOSO));

                if (result)
                    return BadRequest("ERROR.. Ya tiene una reserva realizada con este vehículo en curso.");

                // Validar vehículo
                var vehiculo = await dataContext.Vehiculos
                    .Where(x => x.Id == reserva.VehiculoId)
                    .FirstOrDefaultAsync();

                if (vehiculo == null)
                    return BadRequest("ERROR.. No se encontró su vehículo");

                // Validar datos de alojamiento
                var datoVehiculoSobreAlojado = await dataContext.DataVehiculoAlojados
                    .Where(x => x.EstacionamientoId == reserva.EstacionamientoId &&
                                x.TipoDeVehiculo == vehiculo.TipoDeVehiculo)
                    .FirstOrDefaultAsync();

                if (datoVehiculoSobreAlojado == null)
                    return BadRequest($"ERROR.. El tipo de su vehículo ({vehiculo.TipoDeVehiculo}) no puede ser alojado en este lugar, ya que no es admitido");

                // Validar capacidad
                if (datoVehiculoSobreAlojado.CapacidadDeAlojamiento > datoVehiculoSobreAlojado.CantidadActualAlojados)
                {
                    datoVehiculoSobreAlojado.CantidadActualAlojados++;
                }
                else
                {
                    return BadRequest("ERROR.. No hay más cupos disponibles para este vehículo");
                }

                // Obtener datos del estacionamiento
                var estacionamiento = await dataContext.Estacionamientos
                    .Where(x => x.Id == reserva.EstacionamientoId)
                    .FirstOrDefaultAsync();

                if (estacionamiento == null)
                    return BadRequest("ERROR.. No se encontró el estacionamiento");

                // Obtener datos del propietario del estacionamiento
                var propietario = await _userManager.FindByIdAsync(estacionamiento.UserId);

                if (propietario == null || string.IsNullOrEmpty(propietario.Email))
                {
                    // Log del error pero continúa con la reserva
                    Console.WriteLine("Advertencia: No se pudo obtener el email del propietario");
                }

                // Obtener datos del cliente (usuario que hace la reserva)
                var cliente = await _userManager.FindByIdAsync(reserva.UserId);

                if (cliente == null)
                    return BadRequest("ERROR.. No se encontraron los datos del cliente");

                // Guardar cambios en la base de datos
                dataContext.DataVehiculoAlojados.Update(datoVehiculoSobreAlojado);
                await dataContext.Reservas.AddAsync(reserva);
                await dataContext.SaveChangesAsync();

                // Preparar y enviar notificación por email al propietario
                if (propietario != null && !string.IsNullOrEmpty(propietario.Email))
                {
                    try
                    {
                        DateTime fechaCreacionMastiempoDeEspera = reserva.FechaDeCreacion;

                        var notificacion = new ReservationNotificationDTO
                        {
                            // Datos del cliente
                            NombreCliente = cliente.Nombre ?? "Cliente",
                            ApellidoCliente = cliente.Apellido ?? "",
                            TipoDeVehiculoCliente = vehiculo.TipoDeVehiculo,
                            PatenteCliente = vehiculo.Patente,

                            // Datos del estacionamiento
                            NombreDelEstacionamiento = estacionamiento.Nombre,
                            DireccionDelEstacionamiento = estacionamiento.Direccion ?? "No especificada",
                            TipoDeLugarDelEstacionamiento = estacionamiento.TipoDeLugar,
                            MontoDeLaReserva = reserva.Monto,

                            // Datos adicionales
                            EmailPropietario = propietario.Email,
                            FechaHoraReserva = DateTime.Now,
                            FechaHoraExpiracion = fechaCreacionMastiempoDeEspera.AddMinutes(estacionamiento.TiempoDeEsperaEnMinutos),
                            NumeroReserva = reserva.Id.ToString()
                        };

                        bool emailEnviado = await SendReservationEmail(notificacion);

                        if (!emailEnviado)
                        {
                            Console.WriteLine($"Advertencia: Reserva creada pero no se pudo enviar email de notificación. ReservaId: {reserva.Id}");
                        }
                        else
                        {
                            Console.WriteLine($"✅ Notificación enviada exitosamente al propietario: {propietario.Email}");
                        }
                    }
                    catch (Exception emailEx)
                    {
                        // Log del error pero no falla la reserva
                        Console.WriteLine($"Error al enviar notificación por email: {emailEx.Message}");
                    }
                }

                return Ok(new
                {
                    message = "Reserva creada exitosamente",
                    reservaId = reserva.Id,
                    emailEnviado = propietario != null && !string.IsNullOrEmpty(propietario.Email)
                });
            }
            catch (Exception e)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }

        // ============================================
        // MÉTODO AUXILIAR PARA ENVIAR EMAIL DE RESERVA
        // (Si no lo tienes ya en el AccountController, agrégalo aquí)
        // ============================================

        private async Task<bool> SendReservationEmail(ReservationNotificationDTO reservation)
        {
            try
            {
                using (var smtpClient = new System.Net.Mail.SmtpClient(_From_SmtpServer))
                {
                    smtpClient.Port = 587;
                    smtpClient.EnableSsl = true;
                    smtpClient.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new System.Net.NetworkCredential(_From_EmailAdress, _From_EmailPassword);
                    smtpClient.Timeout = 20000;

                    var mailMessage = new System.Net.Mail.MailMessage
                    {
                        From = new System.Net.Mail.MailAddress(_From_EmailAdress, _From_Name),
                        Subject = $"🅿️ Nueva Reserva en {reservation.NombreDelEstacionamiento}",
                        Body = GenerateReservationEmailBody(reservation),
                        IsBodyHtml = true,
                        Priority = System.Net.Mail.MailPriority.High
                    };

                    mailMessage.To.Add(reservation.EmailPropietario);

                    await smtpClient.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar email de reserva: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        private string GenerateReservationEmailBody(ReservationNotificationDTO reservation)
        {
            // 👇 AQUÍ SE FORMATEAN LAS FECHAS
            string fechaReserva = reservation.FechaHoraReserva != default(DateTime)
                ? reservation.FechaHoraReserva.ToString("dd/MM/yyyy HH:mm")
                : DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            string fechaExpiracion = reservation.FechaHoraExpiracion != default(DateTime)
                ? reservation.FechaHoraExpiracion.ToString("dd/MM/yyyy HH:mm")
                : DateTime.Now.AddHours(2).ToString("dd/MM/yyyy HH:mm");

            string numeroReserva = !string.IsNullOrEmpty(reservation.NumeroReserva)
                ? reservation.NumeroReserva
                : Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

            // 👇 AQUÍ SE CALCULA LA DURACIÓN
            TimeSpan duracion = reservation.FechaHoraExpiracion != default(DateTime)
                ? reservation.FechaHoraExpiracion - reservation.FechaHoraReserva
                : TimeSpan.FromHours(2);

            string duracionTexto = duracion.TotalHours >= 24
                ? $"{(int)duracion.TotalDays} día(s) {duracion.Hours} hora(s)"
                : duracion.TotalHours >= 1
                    ? $"{(int)duracion.TotalHours} hora(s) {duracion.Minutes} min"
                    : $"{duracion.Minutes} minutos";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            background-color: #f5f5f5;
            margin: 0;
            padding: 0;
        }}
        .email-container {{
            max-width: 650px;
            margin: 20px auto;
            background-color: #ffffff;
            border-radius: 10px;
            overflow: hidden;
            box-shadow: 0 4px 15px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px 20px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 32px;
            font-weight: bold;
        }}
        .header p {{
            margin: 10px 0 0 0;
            font-size: 18px;
            opacity: 0.9;
        }}
        .alert-banner {{
            background-color: #ff9800;
            color: white;
            padding: 15px 20px;
            text-align: center;
            font-weight: bold;
            font-size: 16px;
        }}
        .content {{
            padding: 30px;
        }}
        .reservation-number {{
            background-color: #e3f2fd;
            border-left: 4px solid #2196F3;
            padding: 15px 20px;
            margin-bottom: 25px;
            border-radius: 4px;
        }}
        .reservation-number strong {{
            color: #1976D2;
            font-size: 18px;
        }}
        .section {{
            margin-bottom: 30px;
        }}
        .section-title {{
            font-size: 20px;
            font-weight: bold;
            color: #667eea;
            margin-bottom: 15px;
            padding-bottom: 10px;
            border-bottom: 2px solid #e0e0e0;
            display: flex;
            align-items: center;
        }}
        .section-title::before {{
            content: '';
            display: inline-block;
            width: 6px;
            height: 24px;
            background-color: #667eea;
            margin-right: 10px;
            border-radius: 3px;
        }}
        .info-grid {{
            display: table;
            width: 100%;
            border-collapse: collapse;
        }}
        .info-row {{
            display: table-row;
            border-bottom: 1px solid #f0f0f0;
        }}
        .info-row:last-child {{
            border-bottom: none;
        }}
        .info-label {{
            display: table-cell;
            padding: 12px 15px;
            font-weight: 600;
            color: #555;
            width: 40%;
            background-color: #f9f9f9;
        }}
        .info-value {{
            display: table-cell;
            padding: 12px 15px;
            color: #333;
        }}
        .highlight-box {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 20px;
            border-radius: 8px;
            text-align: center;
            margin: 25px 0;
        }}
        .highlight-box .amount {{
            font-size: 36px;
            font-weight: bold;
            margin: 10px 0;
        }}
        .highlight-box .label {{
            font-size: 16px;
            opacity: 0.9;
        }}
        .vehicle-badge {{
            display: inline-block;
            background-color: #4CAF50;
            color: white;
            padding: 6px 15px;
            border-radius: 20px;
            font-weight: bold;
            font-size: 14px;
        }}
        .patent-badge {{
            display: inline-block;
            background-color: #2196F3;
            color: white;
            padding: 8px 20px;
            border-radius: 5px;
            font-weight: bold;
            font-size: 18px;
            letter-spacing: 2px;
            font-family: 'Courier New', monospace;
        }}
        .important-note {{
            background-color: #fff3cd;
            border-left: 4px solid #ffc107;
            padding: 15px 20px;
            margin: 20px 0;
            border-radius: 4px;
        }}
        .important-note strong {{
            color: #856404;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 25px 30px;
            text-align: center;
            border-top: 1px solid #e0e0e0;
        }}
        .footer p {{
            margin: 5px 0;
            font-size: 13px;
            color: #666;
        }}
        .icon {{
            font-size: 20px;
            margin-right: 8px;
        }}
    </style>
</head>
<body>
    <div class='email-container'>
        <div class='header'>
            <h1>🅿️ EasyParking</h1>
            <p>Notificación de Reserva</p>
        </div>

        <div class='alert-banner'>
            ⚠️ ¡NUEVA RESERVA CONFIRMADA! Un cliente se dirige a tu estacionamiento
        </div>

        <div class='content'>
            <!-- 👇👇👇 AQUÍ ESTÁN LAS FECHAS EN EL HTML 👇👇👇 -->
            <div class='reservation-number'>
                <strong>Número de Reserva: #{numeroReserva}</strong><br>
                <span style='color: #666; font-size: 14px;'>📅 Creada el: {fechaReserva}</span><br>
                <span style='color: #d32f2f; font-size: 14px; font-weight: 600;'>⏰ Expira el: {fechaExpiracion}</span><br>
                <span style='color: #666; font-size: 13px;'>⌛ Duración: {duracionTexto}</span>
            </div>

            <!-- Datos del Cliente -->
            <div class='section'>
                <div class='section-title'>
                    👤 Datos del Cliente
                </div>
                <div class='info-grid'>
                    <div class='info-row'>
                        <div class='info-label'>Nombre Completo:</div>
                        <div class='info-value'><strong>{reservation.NombreCliente} {reservation.ApellidoCliente}</strong></div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Tipo de Vehículo:</div>
                        <div class='info-value'>
                            <span class='vehicle-badge'>{reservation.TipoDeVehiculoCliente}</span>
                        </div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Patente:</div>
                        <div class='info-value'>
                            <span class='patent-badge'>{reservation.PatenteCliente}</span>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Datos del Estacionamiento -->
            <div class='section'>
                <div class='section-title'>
                    🏢 Datos del Estacionamiento
                </div>
                <div class='info-grid'>
                    <div class='info-row'>
                        <div class='info-label'>Nombre:</div>
                        <div class='info-value'><strong>{reservation.NombreDelEstacionamiento}</strong></div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Dirección:</div>
                        <div class='info-value'>{reservation.DireccionDelEstacionamiento}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Tipo de Lugar:</div>
                        <div class='info-value'>{reservation.TipoDeLugarDelEstacionamiento}</div>
                    </div>
                </div>
            </div>

            <!-- Monto de la Reserva -->
            <div class='highlight-box'>
                <div class='label'>💰 Monto de la Reserva</div>
                <div class='amount'>${reservation.MontoDeLaReserva:N2}</div>
            </div>

            <!-- Nota Importante -->
            <div class='important-note'>
                <strong>⚠️ Importante:</strong>
                <ul style='margin: 10px 0 0 0; padding-left: 20px;'>
                    <li>El cliente está en camino a tu estacionamiento</li>
                    <li>Asegúrate de tener el lugar disponible</li>
                    <li>Verifica la patente del vehículo al momento de ingreso</li>
                    <li>Mantén esta información a mano para referencia</li>
                </ul>
            </div>
        </div>

        <!-- Footer -->
        <div class='footer'>
            <p><strong>EasyParking</strong> - Sistema de Gestión de Estacionamientos</p>
            <p>Este es un correo automático, por favor no respondas a este mensaje.</p>
            <p style='margin-top: 15px; color: #999;'>
                Si tienes alguna consulta, contacta con nuestro soporte<br>
                📧 soporte@easyparking.com | 📱 +54 9 379 XXX-XXXX
            </p>
            <p style='margin-top: 10px;'>&copy; 2025 EasyParking. Todos los derechos reservados.</p>
        </div>
    </div>
</body>
</html>";
        }


        //------------------------------------------------------

        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult> SetReservaCanceladaPorElDueñoAsync([FromBody] int reservaId)
        {
            try
            {
                DataContext dataContext = new DataContext();

                // Obtener la reserva
                var reserva = await dataContext.Reservas
                    .Where(x => x.Id == reservaId)
                    .FirstOrDefaultAsync();

                if (reserva == null)
                    return NotFound("Reserva no encontrada");

                // Obtener el vehículo
                var vehiculo = await dataContext.Vehiculos
                    .Where(x => x.Id == reserva.VehiculoId)
                    .FirstOrDefaultAsync();

                if (vehiculo == null)
                    return BadRequest("No se encontró el vehículo asociado");

                // Obtener datos del estacionamiento
                var estacionamiento = await dataContext.Estacionamientos
                    .Where(x => x.Id == reserva.EstacionamientoId)
                    .FirstOrDefaultAsync();

                if (estacionamiento == null)
                    return BadRequest("No se encontró el estacionamiento");

                // Obtener datos del cliente
                var cliente = await _userManager.FindByIdAsync(reserva.UserId);

                if (cliente == null || string.IsNullOrEmpty(cliente.Email))
                {
                    Console.WriteLine("Advertencia: No se pudo obtener el email del cliente");
                }

                // Actualizar estado de la reserva
                reserva.Estado = EstadoReserva.CANCELADO_POR_EL_DUEÑO;

                // Actualizar contador de vehículos alojados
                var datoVehiculoSobreAlojado = await dataContext.DataVehiculoAlojados
                    .Where(x => x.EstacionamientoId == reserva.EstacionamientoId &&
                                x.TipoDeVehiculo == vehiculo.TipoDeVehiculo)
                    .FirstOrDefaultAsync();

                if (datoVehiculoSobreAlojado != null && datoVehiculoSobreAlojado.CantidadActualAlojados > 0)
                {
                    datoVehiculoSobreAlojado.CantidadActualAlojados--;
                    dataContext.DataVehiculoAlojados.Update(datoVehiculoSobreAlojado);
                }

                // Guardar cambios
                dataContext.Reservas.Update(reserva);
                await dataContext.SaveChangesAsync();

                // Enviar notificación al cliente
                if (cliente != null && !string.IsNullOrEmpty(cliente.Email))
                {
                    try
                    {

                        DateTime fechaCreacionMastiempoDeEspera = reserva.FechaDeCreacion;

                        var notificacion = new CancellationNotificationDTO
                        {
                            // Datos del cliente
                            Nombre = cliente.Nombre ?? "Cliente",
                            Apellido = cliente.Apellido ?? "",
                            Email = cliente.Email,

                            // Datos de la reserva
                            NumeroReserva = reserva.Id.ToString(),
                            FechaHoraReserva = reserva.FechaDeCreacion, // Ajusta según tu modelo
                            FechaHoraExpiracion = fechaCreacionMastiempoDeEspera.AddMinutes(estacionamiento.TiempoDeEsperaEnMinutos),
                            MontoReserva = reserva.Monto,

                            // Datos del estacionamiento
                            NombreDelEstacionamiento = estacionamiento.Nombre,
                            DireccionDelEstacionamiento = estacionamiento.Direccion ?? "No especificada",
                            TipoDeLugarDelEstacionamiento = estacionamiento?.TipoDeLugar ?? "No especificado",

                            // Datos del vehículo
                            TipoDeVehiculo = vehiculo.TipoDeVehiculo,
                            Patente = vehiculo.Patente,

                            // Información de cancelación
                            FechaHoraCancelacion = DateTime.Now,
                            MotivoCancelacion = "El propietario del estacionamiento ha cancelado la reserva" // Puedes hacerlo parametrizable
                        };

                        bool emailEnviado = await SendCancellationEmail(notificacion);

                        if (!emailEnviado)
                        {
                            Console.WriteLine($"Advertencia: Reserva cancelada pero no se pudo enviar email al cliente. ReservaId: {reserva.Id}");
                        }
                        else
                        {
                            Console.WriteLine($"✅ Notificación de cancelación enviada al cliente: {cliente.Email}");
                        }
                    }
                    catch (Exception emailEx)
                    {
                        Console.WriteLine($"Error al enviar notificación de cancelación: {emailEx.Message}");
                    }
                }

                return Ok(new
                {
                    message = "Reserva cancelada exitosamente",
                    reservaId = reserva.Id,
                    emailEnviado = cliente != null && !string.IsNullOrEmpty(cliente.Email)
                });
            }
            catch (Exception e)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }

        // ============================================
        // 3. MÉTODO PARA ENVIAR EMAIL DE CANCELACIÓN
        // ============================================

        private async Task<bool> SendCancellationEmail(CancellationNotificationDTO cancellation)
        {
            try
            {
                using (var smtpClient = new System.Net.Mail.SmtpClient(_From_SmtpServer))
                {
                    smtpClient.Port = 587;
                    smtpClient.EnableSsl = true;
                    smtpClient.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new System.Net.NetworkCredential(_From_EmailAdress, _From_EmailPassword);
                    smtpClient.Timeout = 20000;

                    var mailMessage = new System.Net.Mail.MailMessage
                    {
                        From = new System.Net.Mail.MailAddress(_From_EmailAdress, _From_Name),
                        Subject = $"❌ Reserva Cancelada - {cancellation.NombreDelEstacionamiento}",
                        Body = GenerateCancellationEmailBody(cancellation),
                        IsBodyHtml = true,
                        Priority = System.Net.Mail.MailPriority.High
                    };

                    mailMessage.To.Add(cancellation.Email);

                    await smtpClient.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar email de cancelación: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        // ============================================
        // 4. PLANTILLA HTML DEL EMAIL DE CANCELACIÓN
        // ============================================

        private string GenerateCancellationEmailBody(CancellationNotificationDTO cancellation)
        {
            string fechaReserva = cancellation.FechaHoraReserva != default(DateTime)
                ? cancellation.FechaHoraReserva.ToString("dd/MM/yyyy HH:mm")
                : "No disponible";

            string fechaExpiracion = cancellation.FechaHoraExpiracion != default(DateTime)
                ? cancellation.FechaHoraExpiracion.ToString("dd/MM/yyyy HH:mm")
                : "No disponible";

            string fechaCancelacion = cancellation.FechaHoraCancelacion.ToString("dd/MM/yyyy HH:mm");

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            background-color: #f5f5f5;
            margin: 0;
            padding: 0;
        }}
        .email-container {{
            max-width: 650px;
            margin: 20px auto;
            background-color: #ffffff;
            border-radius: 10px;
            overflow: hidden;
            box-shadow: 0 4px 15px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #f44336 0%, #d32f2f 100%);
            color: white;
            padding: 30px 20px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 32px;
            font-weight: bold;
        }}
        .header p {{
            margin: 10px 0 0 0;
            font-size: 18px;
            opacity: 0.9;
        }}
        .alert-banner {{
            background-color: #ff5252;
            color: white;
            padding: 15px 20px;
            text-align: center;
            font-weight: bold;
            font-size: 16px;
        }}
        .content {{
            padding: 30px;
        }}
        .cancellation-box {{
            background: linear-gradient(135deg, #ffebee 0%, #ffcdd2 100%);
            border-left: 4px solid #f44336;
            padding: 18px 20px;
            margin-bottom: 25px;
            border-radius: 4px;
            box-shadow: 0 2px 8px rgba(0,0,0,0.05);
        }}
        .cancellation-box strong {{
            color: #c62828;
            font-size: 18px;
        }}
        .section {{
            margin-bottom: 30px;
        }}
        .section-title {{
            font-size: 20px;
            font-weight: bold;
            color: #f44336;
            margin-bottom: 15px;
            padding-bottom: 10px;
            border-bottom: 2px solid #e0e0e0;
            display: flex;
            align-items: center;
        }}
        .section-title::before {{
            content: '';
            display: inline-block;
            width: 6px;
            height: 24px;
            background-color: #f44336;
            margin-right: 10px;
            border-radius: 3px;
        }}
        .info-grid {{
            display: table;
            width: 100%;
            border-collapse: collapse;
        }}
        .info-row {{
            display: table-row;
            border-bottom: 1px solid #f0f0f0;
        }}
        .info-row:last-child {{
            border-bottom: none;
        }}
        .info-label {{
            display: table-cell;
            padding: 12px 15px;
            font-weight: 600;
            color: #555;
            width: 40%;
            background-color: #f9f9f9;
        }}
        .info-value {{
            display: table-cell;
            padding: 12px 15px;
            color: #333;
        }}
        .refund-box {{
            background: linear-gradient(135deg, #e8f5e9 0%, #c8e6c9 100%);
            color: #1b5e20;
            padding: 20px;
            border-radius: 8px;
            text-align: center;
            margin: 25px 0;
            border: 2px solid #4CAF50;
        }}
        .refund-box .amount {{
            font-size: 36px;
            font-weight: bold;
            margin: 10px 0;
            color: #2e7d32;
        }}
        .refund-box .label {{
            font-size: 16px;
            font-weight: 600;
        }}
        .apology-box {{
            background-color: #fff3e0;
            border-left: 4px solid #ff9800;
            padding: 20px;
            margin: 20px 0;
            border-radius: 4px;
        }}
        .apology-box strong {{
            color: #e65100;
        }}
        .patent-badge {{
            display: inline-block;
            background-color: #2196F3;
            color: white;
            padding: 8px 20px;
            border-radius: 5px;
            font-weight: bold;
            font-size: 18px;
            letter-spacing: 2px;
            font-family: 'Courier New', monospace;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 25px 30px;
            text-align: center;
            border-top: 1px solid #e0e0e0;
        }}
        .footer p {{
            margin: 5px 0;
            font-size: 13px;
            color: #666;
        }}
    </style>
</head>
<body>
    <div class='email-container'>
        <!-- Header -->
        <div class='header'>
            <h1>🅿️ EasyParking</h1>
            <p>Notificación de Cancelación</p>
        </div>

        <!-- Alert Banner -->
        <div class='alert-banner'>
            ❌ RESERVA CANCELADA - El propietario ha cancelado tu reserva
        </div>

        <!-- Content -->
        <div class='content'>
            <!-- Saludo -->
            <h2>Estimado/a {cancellation.Nombre} {cancellation.Apellido},</h2>
            <p>Lamentamos informarte que tu reserva ha sido cancelada por el propietario del estacionamiento.</p>

            <!-- Información de Cancelación -->
            <div class='cancellation-box'>
                <strong>Número de Reserva: #{cancellation.NumeroReserva}</strong><br>
                <span style='color: #666; font-size: 14px;'>❌ Cancelada el: {fechaCancelacion}</span>
            </div>

            <!-- Detalles de la Reserva Cancelada -->
            <div class='section'>
                <div class='section-title'>
                    📋 Detalles de la Reserva Cancelada
                </div>
                <div class='info-grid'>
                    <div class='info-row'>
                        <div class='info-label'>Estacionamiento:</div>
                        <div class='info-value'><strong>{cancellation.NombreDelEstacionamiento}</strong></div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Dirección:</div>
                        <div class='info-value'>{cancellation.DireccionDelEstacionamiento}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Tipo de Lugar:</div>
                        <div class='info-value'>{cancellation.TipoDeLugarDelEstacionamiento}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Fecha Inicio:</div>
                        <div class='info-value'>{fechaReserva}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Fecha Expiración:</div>
                        <div class='info-value'>{fechaExpiracion}</div>
                    </div>
                </div>
            </div>

            <!-- Datos del Vehículo -->
            <div class='section'>
                <div class='section-title'>
                    🚗 Tu Vehículo
                </div>
                <div class='info-grid'>
                    <div class='info-row'>
                        <div class='info-label'>Tipo:</div>
                        <div class='info-value'>{cancellation.TipoDeVehiculo}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Patente:</div>
                        <div class='info-value'>
                            <span class='patent-badge'>{cancellation.Patente}</span>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Reembolso -->
            <div class='refund-box'>
                <div class='label'>💰 Reembolso Total</div>
                <div class='amount'>${cancellation.MontoReserva:N2}</div>
                <p style='margin: 10px 0 0 0; font-size: 14px;'>
                    El monto será devuelto automáticamente a tu método de pago en 5-7 días hábiles
                </p>
            </div>

            <!-- Disculpas -->
            <div class='apology-box'>
                <strong>🙏 Nuestras disculpas</strong>
                <p style='margin: 10px 0 0 0;'>
                    Entendemos que esto puede causarte inconvenientes. Te invitamos a buscar otros estacionamientos 
                    disponibles en nuestra aplicación. Si tienes alguna consulta, no dudes en contactarnos.
                </p>
            </div>

            <!-- Motivo -->
            {(string.IsNullOrEmpty(cancellation.MotivoCancelacion) ? "" : $@"
            <div class='section'>
                <div class='section-title'>
                    📝 Motivo de la Cancelación
                </div>
                <p style='padding: 15px; background-color: #f9f9f9; border-radius: 5px; margin: 0;'>
                    {cancellation.MotivoCancelacion}
                </p>
            </div>
            ")}
        </div>

        <!-- Footer -->
        <div class='footer'>
            <p><strong>EasyParking</strong> - Sistema de Gestión de Estacionamientos</p>
            <p>Si tienes alguna consulta sobre esta cancelación o tu reembolso, no dudes en contactarnos.</p>
            <p style='margin-top: 15px; color: #999;'>
                📧 soporte@easyparking.com | 📱 +54 9 379 XXX-XXXX
            </p>
            <p style='margin-top: 10px;'>&copy; 2025 EasyParking. Todos los derechos reservados.</p>
        </div>
    </div>
</body>
</html>";
        }


        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult> SetReservaCanceladaPorElClienteAsync([FromBody] int reservaId)
        {
            try
            {
                DataContext dataContext = new DataContext();

                // Obtener la reserva
                var reserva = await dataContext.Reservas
                    .Where(x => x.Id == reservaId)
                    .FirstOrDefaultAsync();

                if (reserva == null)
                    return NotFound("Reserva no encontrada");

                // Obtener el vehículo
                var vehiculo = await dataContext.Vehiculos
                    .Where(x => x.Id == reserva.VehiculoId)
                    .FirstOrDefaultAsync();

                if (vehiculo == null)
                    return BadRequest("No se encontró el vehículo asociado");

                // Obtener datos del estacionamiento
                var estacionamiento = await dataContext.Estacionamientos
                    .Where(x => x.Id == reserva.EstacionamientoId)
                    .FirstOrDefaultAsync();

                if (estacionamiento == null)
                    return BadRequest("No se encontró el estacionamiento");

                // Obtener datos del propietario del estacionamiento
                var propietario = await _userManager.FindByIdAsync(estacionamiento.UserId);

                if (propietario == null || string.IsNullOrEmpty(propietario.Email))
                {
                    Console.WriteLine("Advertencia: No se pudo obtener el email del propietario");
                }

                // Obtener datos del cliente que cancela
                var cliente = await _userManager.FindByIdAsync(reserva.UserId);

                if (cliente == null)
                {
                    Console.WriteLine("Advertencia: No se pudo obtener los datos del cliente");
                }

                // Actualizar estado de la reserva
                reserva.Estado = EstadoReserva.CANCELADO_POR_EL_CLIENTE;

                // Actualizar contador de vehículos alojados
                var datoVehiculoSobreAlojado = await dataContext.DataVehiculoAlojados
                    .Where(x => x.EstacionamientoId == reserva.EstacionamientoId &&
                                x.TipoDeVehiculo == vehiculo.TipoDeVehiculo)
                    .FirstOrDefaultAsync();

                if (datoVehiculoSobreAlojado != null && datoVehiculoSobreAlojado.CantidadActualAlojados > 0)
                {
                    datoVehiculoSobreAlojado.CantidadActualAlojados--;
                    dataContext.DataVehiculoAlojados.Update(datoVehiculoSobreAlojado);
                }

                // Guardar cambios
                dataContext.Reservas.Update(reserva);
                await dataContext.SaveChangesAsync();

                // Enviar notificación al propietario
                if (propietario != null && !string.IsNullOrEmpty(propietario.Email))
                {
                    try
                    {
                        var notificacion = new ClientCancellationNotificationDTO
                        {
                            // Datos del propietario
                            NombrePropietario = propietario.Nombre ?? "Propietario",
                            ApellidoPropietario = propietario.Apellido ?? "",
                            EmailPropietario = propietario.Email,

                            // Datos del cliente que canceló
                            NombreCliente = cliente?.Nombre ?? "Cliente",
                            ApellidoCliente = cliente?.Apellido ?? "",
                            TelefonoCliente = cliente?.Telefono ?? "No disponible",

                            // Datos de la reserva
                            NumeroReserva = reserva.Id.ToString(),
                            FechaDeCreacion = reserva.FechaDeCreacion,
                            FechaDeExpiracion = reserva.FechaDeExpiracion,
                            MontoReserva = reserva.Monto,

                            // Datos del estacionamiento
                            NombreDelEstacionamiento = estacionamiento.Nombre,
                            DireccionDelEstacionamiento = estacionamiento.Direccion ?? "No especificada",
                            TipoDeLugar = estacionamiento.TipoDeLugar ?? "No especificado",

                            // Datos del vehículo
                            TipoDeVehiculo = vehiculo.TipoDeVehiculo,
                            Patente = vehiculo.Patente ?? reserva.Patente, // Usa la de reserva si existe

                            // Información de cancelación
                            FechaHoraCancelacion = DateTime.Now
                        };

                        bool emailEnviado = await SendClientCancellationEmail(notificacion);

                        if (!emailEnviado)
                        {
                            Console.WriteLine($"Advertencia: Reserva cancelada pero no se pudo enviar email al propietario. ReservaId: {reserva.Id}");
                        }
                        else
                        {
                            Console.WriteLine($"✅ Notificación de cancelación enviada al propietario: {propietario.Email}");
                        }
                    }
                    catch (Exception emailEx)
                    {
                        Console.WriteLine($"Error al enviar notificación de cancelación al propietario: {emailEx.Message}");
                    }
                }

                return Ok(new
                {
                    message = "Reserva cancelada exitosamente",
                    reservaId = reserva.Id,
                    espacioLiberado = true,
                    emailEnviado = propietario != null && !string.IsNullOrEmpty(propietario.Email)
                });
            }
            catch (Exception e)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }

        // ============================================
        // 3. MÉTODO PARA ENVIAR EMAIL AL PROPIETARIO
        // ============================================

        private async Task<bool> SendClientCancellationEmail(ClientCancellationNotificationDTO notification)
        {
            try
            {
                using (var smtpClient = new System.Net.Mail.SmtpClient(_From_SmtpServer))
                {
                    smtpClient.Port = 587;
                    smtpClient.EnableSsl = true;
                    smtpClient.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new System.Net.NetworkCredential(_From_EmailAdress, _From_EmailPassword);
                    smtpClient.Timeout = 20000;

                    var mailMessage = new System.Net.Mail.MailMessage
                    {
                        From = new System.Net.Mail.MailAddress(_From_EmailAdress, _From_Name),
                        Subject = $"🔔 Cancelación de Reserva - {notification.NombreDelEstacionamiento}",
                        Body = GenerateClientCancellationEmailBody(notification),
                        IsBodyHtml = true,
                        Priority = System.Net.Mail.MailPriority.Normal
                    };

                    mailMessage.To.Add(notification.EmailPropietario);

                    await smtpClient.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar email al propietario: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        // ============================================
        // 4. PLANTILLA HTML DEL EMAIL PARA EL PROPIETARIO
        // ============================================

        private string GenerateClientCancellationEmailBody(ClientCancellationNotificationDTO notification)
        {
            string fechaCreacion = notification.FechaDeCreacion.ToString("dd/MM/yyyy HH:mm");
            string fechaExpiracion = notification.FechaDeExpiracion.ToString("dd/MM/yyyy HH:mm");
            string fechaCancelacion = notification.FechaHoraCancelacion.ToString("dd/MM/yyyy HH:mm");

            // Calcular duración que tenía la reserva
            TimeSpan duracion = notification.FechaDeExpiracion - notification.FechaDeCreacion;
            string duracionTexto = duracion.TotalHours >= 24
                ? $"{(int)duracion.TotalDays} día(s) {duracion.Hours} hora(s)"
                : duracion.TotalHours >= 1
                    ? $"{(int)duracion.TotalHours} hora(s) {duracion.Minutes} min"
                    : $"{duracion.Minutes} minutos";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            background-color: #f5f5f5;
            margin: 0;
            padding: 0;
        }}
        .email-container {{
            max-width: 650px;
            margin: 20px auto;
            background-color: #ffffff;
            border-radius: 10px;
            overflow: hidden;
            box-shadow: 0 4px 15px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #ff9800 0%, #f57c00 100%);
            color: white;
            padding: 30px 20px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 32px;
            font-weight: bold;
        }}
        .header p {{
            margin: 10px 0 0 0;
            font-size: 18px;
            opacity: 0.9;
        }}
        .alert-banner {{
            background-color: #ff9800;
            color: white;
            padding: 15px 20px;
            text-align: center;
            font-weight: bold;
            font-size: 16px;
        }}
        .content {{
            padding: 30px;
        }}
        .cancellation-box {{
            background: linear-gradient(135deg, #fff3e0 0%, #ffe0b2 100%);
            border-left: 4px solid #ff9800;
            padding: 18px 20px;
            margin-bottom: 25px;
            border-radius: 4px;
            box-shadow: 0 2px 8px rgba(0,0,0,0.05);
        }}
        .cancellation-box strong {{
            color: #e65100;
            font-size: 18px;
        }}
        .section {{
            margin-bottom: 30px;
        }}
        .section-title {{
            font-size: 20px;
            font-weight: bold;
            color: #ff9800;
            margin-bottom: 15px;
            padding-bottom: 10px;
            border-bottom: 2px solid #e0e0e0;
            display: flex;
            align-items: center;
        }}
        .section-title::before {{
            content: '';
            display: inline-block;
            width: 6px;
            height: 24px;
            background-color: #ff9800;
            margin-right: 10px;
            border-radius: 3px;
        }}
        .info-grid {{
            display: table;
            width: 100%;
            border-collapse: collapse;
        }}
        .info-row {{
            display: table-row;
            border-bottom: 1px solid #f0f0f0;
        }}
        .info-row:last-child {{
            border-bottom: none;
        }}
        .info-label {{
            display: table-cell;
            padding: 12px 15px;
            font-weight: 600;
            color: #555;
            width: 40%;
            background-color: #f9f9f9;
        }}
        .info-value {{
            display: table-cell;
            padding: 12px 15px;
            color: #333;
        }}
        .available-box {{
            background: linear-gradient(135deg, #e8f5e9 0%, #c8e6c9 100%);
            color: #1b5e20;
            padding: 20px;
            border-radius: 8px;
            text-align: center;
            margin: 25px 0;
            border: 2px solid #4CAF50;
        }}
        .available-box .icon {{
            font-size: 48px;
            margin-bottom: 10px;
        }}
        .available-box .message {{
            font-size: 18px;
            font-weight: bold;
            color: #2e7d32;
        }}
        .patent-badge {{
            display: inline-block;
            background-color: #2196F3;
            color: white;
            padding: 8px 20px;
            border-radius: 5px;
            font-weight: bold;
            font-size: 18px;
            letter-spacing: 2px;
            font-family: 'Courier New', monospace;
        }}
        .info-note {{
            background-color: #e3f2fd;
            border-left: 4px solid #2196F3;
            padding: 15px 20px;
            margin: 20px 0;
            border-radius: 4px;
        }}
        .info-note strong {{
            color: #1565c0;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 25px 30px;
            text-align: center;
            border-top: 1px solid #e0e0e0;
        }}
        .footer p {{
            margin: 5px 0;
            font-size: 13px;
            color: #666;
        }}
    </style>
</head>
<body>
    <div class='email-container'>
        <!-- Header -->
        <div class='header'>
            <h1>🅿️ EasyParking</h1>
            <p>Notificación de Cancelación</p>
        </div>

        <!-- Alert Banner -->
        <div class='alert-banner'>
            🔔 UN CLIENTE HA CANCELADO SU RESERVA - Espacio ahora disponible
        </div>

        <!-- Content -->
        <div class='content'>
            <!-- Saludo -->
            <h2>Hola {notification.NombrePropietario} {notification.ApellidoPropietario},</h2>
            <p>Te informamos que un cliente ha cancelado su reserva en tu estacionamiento <strong>{notification.NombreDelEstacionamiento}</strong>.</p>

            <!-- Información de Cancelación -->
            <div class='cancellation-box'>
                <strong>Reserva #{notification.NumeroReserva} - CANCELADA</strong><br>
                <span style='color: #666; font-size: 14px;'>🕐 Cancelada el: {fechaCancelacion}</span><br>
                <span style='color: #666; font-size: 13px;'>⌛ Duración original: {duracionTexto}</span>
            </div>

            <!-- Espacio Disponible -->
            <div class='available-box'>
                <div class='icon'>✅</div>
                <div class='message'>El espacio está nuevamente disponible</div>
                <p style='margin: 10px 0 0 0; font-size: 14px;'>
                    Otros clientes pueden reservar este lugar ahora
                </p>
            </div>

            <!-- Datos del Cliente -->
            <div class='section'>
                <div class='section-title'>
                    👤 Datos del Cliente que Canceló
                </div>
                <div class='info-grid'>
                    <div class='info-row'>
                        <div class='info-label'>Nombre:</div>
                        <div class='info-value'><strong>{notification.NombreCliente} {notification.ApellidoCliente}</strong></div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Teléfono:</div>
                        <div class='info-value'>{notification.TelefonoCliente}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Vehículo:</div>
                        <div class='info-value'>{notification.TipoDeVehiculo}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Patente:</div>
                        <div class='info-value'>
                            <span class='patent-badge'>{notification.Patente}</span>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Detalles de la Reserva Cancelada -->
            <div class='section'>
                <div class='section-title'>
                    📋 Detalles de la Reserva Cancelada
                </div>
                <div class='info-grid'>
                    <div class='info-row'>
                        <div class='info-label'>Tu Estacionamiento:</div>
                        <div class='info-value'><strong>{notification.NombreDelEstacionamiento}</strong></div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Dirección:</div>
                        <div class='info-value'>{notification.DireccionDelEstacionamiento}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Tipo de Lugar:</div>
                        <div class='info-value'>{notification.TipoDeLugar}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Fecha Creación:</div>
                        <div class='info-value'>{fechaCreacion}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Fecha Expiración:</div>
                        <div class='info-value'>{fechaExpiracion}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Monto:</div>
                        <div class='info-value'><strong>${notification.MontoReserva:N2}</strong></div>
                    </div>
                </div>
            </div>

            <!-- Información Adicional -->
            <div class='info-note'>
                <strong>ℹ️ Información importante:</strong>
                <ul style='margin: 10px 0 0 0; padding-left: 20px;'>
                    <li>El cliente recibirá su reembolso automáticamente</li>
                    <li>El espacio ya está disponible para nuevas reservas</li>
                    <li>Tu capacidad de alojamiento se ha actualizado</li>
                    <li>No necesitas realizar ninguna acción adicional</li>
                </ul>
            </div>
        </div>

        <!-- Footer -->
        <div class='footer'>
            <p><strong>EasyParking</strong> - Sistema de Gestión de Estacionamientos</p>
            <p>Este es un correo automático, por favor no respondas a este mensaje.</p>
            <p style='margin-top: 15px; color: #999;'>
                Si tienes alguna consulta, contacta con nuestro soporte<br>
                📧 soporte@easyparking.com | 📱 +54 9 379 XXX-XXXX
            </p>
            <p style='margin-top: 10px;'>&copy; 2025 EasyParking. Todos los derechos reservados.</p>
        </div>
    </div>
</body>
</html>";
        }


        //---------------------------------------------------------------------

        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult> SetReservaSeHaMarchadoAsync([FromBody] int reservaId)
        {
            try
            {
                DataContext dataContext = new DataContext();

                // Obtener la reserva
                var reserva = await dataContext.Reservas
                    .Where(x => x.Id == reservaId)
                    .FirstOrDefaultAsync();

                if (reserva == null)
                    return NotFound("Reserva no encontrada");

                // Obtener el vehículo
                var vehiculo = await dataContext.Vehiculos
                    .Where(x => x.Id == reserva.VehiculoId)
                    .FirstOrDefaultAsync();

                if (vehiculo == null)
                    return BadRequest("No se encontró el vehículo asociado");

                // Obtener datos del estacionamiento
                var estacionamiento = await dataContext.Estacionamientos
                    .Where(x => x.Id == reserva.EstacionamientoId)
                    .FirstOrDefaultAsync();

                if (estacionamiento == null)
                    return BadRequest("No se encontró el estacionamiento");

                // Obtener datos del cliente
                var cliente = await _userManager.FindByIdAsync(reserva.UserId);

                if (cliente == null || string.IsNullOrEmpty(cliente.Email))
                {
                    Console.WriteLine("Advertencia: No se pudo obtener el email del cliente");
                }

                // Obtener datos del propietario
                var propietario = await _userManager.FindByIdAsync(estacionamiento.UserId);

                // Actualizar estado de la reserva
                reserva.Estado = EstadoReserva.SE_HA_MARCHADO;
                reserva.FechaDeSalida = DateTime.Now;

                // Actualizar contador de vehículos alojados
                var datoVehiculoSobreAlojado = await dataContext.DataVehiculoAlojados
                    .Where(x => x.EstacionamientoId == reserva.EstacionamientoId &&
                                x.TipoDeVehiculo == vehiculo.TipoDeVehiculo)
                    .FirstOrDefaultAsync();

                if (datoVehiculoSobreAlojado != null && datoVehiculoSobreAlojado.CantidadActualAlojados > 0)
                {
                    datoVehiculoSobreAlojado.CantidadActualAlojados--;
                    dataContext.DataVehiculoAlojados.Update(datoVehiculoSobreAlojado);
                }

                // Guardar cambios
                dataContext.Reservas.Update(reserva);
                await dataContext.SaveChangesAsync();

                // Enviar notificación de agradecimiento al cliente
                if (cliente != null && !string.IsNullOrEmpty(cliente.Email))
                {
                    try
                    {
                        var notificacion = new CheckoutNotificationDTO
                        {
                            // Datos del cliente
                            NombreCliente = cliente.Nombre ?? "Cliente",
                            ApellidoCliente = cliente.Apellido ?? "",
                            EmailCliente = cliente.Email,

                            // Datos de la reserva
                            NumeroReserva = reserva.Id.ToString(),
                            FechaDeCreacion = reserva.FechaDeCreacion,
                            FechaDeArribo = reserva.FechaDeArribo,
                            FechaDeSalida = reserva.FechaDeSalida ?? DateTime.Now,
                            MontoTotal = reserva.Monto,

                            // Datos del estacionamiento
                            NombreDelEstacionamiento = estacionamiento.Nombre,
                            DireccionDelEstacionamiento = estacionamiento.Direccion ?? "No especificada",
                            TipoDeLugar = estacionamiento.TipoDeLugar ?? "No especificado",

                            // Datos del vehículo
                            TipoDeVehiculo = vehiculo.TipoDeVehiculo,
                            Patente = vehiculo.Patente ?? reserva.Patente,

                            // Datos del propietario
                            NombrePropietario = propietario?.Nombre ?? "Propietario",
                            TelefonoPropietario = propietario?.Telefono ?? "No disponible"
                        };

                        bool emailEnviado = await SendCheckoutConfirmationEmail(notificacion);

                        if (!emailEnviado)
                        {
                            Console.WriteLine($"Advertencia: Salida registrada pero no se pudo enviar email al cliente. ReservaId: {reserva.Id}");
                        }
                        else
                        {
                            Console.WriteLine($"✅ Email de agradecimiento enviado al cliente: {cliente.Email}");
                        }
                    }
                    catch (Exception emailEx)
                    {
                        Console.WriteLine($"Error al enviar email de confirmación de salida: {emailEx.Message}");
                    }
                }

                return Ok(new
                {
                    message = "Salida registrada exitosamente",
                    reservaId = reserva.Id,
                    fechaSalida = reserva.FechaDeSalida,
                    emailEnviado = cliente != null && !string.IsNullOrEmpty(cliente.Email)
                });
            }
            catch (Exception e)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }

        // ============================================
        // 3. MÉTODO PARA ENVIAR EMAIL DE CONFIRMACIÓN DE SALIDA
        // ============================================

        private async Task<bool> SendCheckoutConfirmationEmail(CheckoutNotificationDTO notification)
        {
            try
            {
                using (var smtpClient = new System.Net.Mail.SmtpClient(_From_SmtpServer))
                {
                    smtpClient.Port = 587;
                    smtpClient.EnableSsl = true;
                    smtpClient.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new System.Net.NetworkCredential(_From_EmailAdress, _From_EmailPassword);
                    smtpClient.Timeout = 20000;

                    var mailMessage = new System.Net.Mail.MailMessage
                    {
                        From = new System.Net.Mail.MailAddress(_From_EmailAdress, _From_Name),
                        Subject = $"✅ Gracias por usar EasyParking - {notification.NombreDelEstacionamiento}",
                        Body = GenerateCheckoutConfirmationEmailBody(notification),
                        IsBodyHtml = true,
                        Priority = System.Net.Mail.MailPriority.Normal
                    };

                    mailMessage.To.Add(notification.EmailCliente);

                    await smtpClient.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar email de confirmación de salida: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        // ============================================
        // 4. PLANTILLA HTML DEL EMAIL DE AGRADECIMIENTO
        // ============================================

        private string GenerateCheckoutConfirmationEmailBody(CheckoutNotificationDTO notification)
        {
            string fechaArribo = notification.FechaDeArribo.HasValue
                ? notification.FechaDeArribo.Value.ToString("dd/MM/yyyy HH:mm")
                : notification.FechaDeCreacion.ToString("dd/MM/yyyy HH:mm");

            string fechaSalida = notification.FechaDeSalida.ToString("dd/MM/yyyy HH:mm");

            // Calcular tiempo de estadía
            DateTime arriboReal = notification.FechaDeArribo ?? notification.FechaDeCreacion;
            TimeSpan tiempoEstadia = notification.FechaDeSalida - arriboReal;

            string tiempoEstadiaTexto = tiempoEstadia.TotalHours >= 24
                ? $"{(int)tiempoEstadia.TotalDays} día(s) {tiempoEstadia.Hours} hora(s)"
                : tiempoEstadia.TotalHours >= 1
                    ? $"{(int)tiempoEstadia.TotalHours} hora(s) {tiempoEstadia.Minutes} min"
                    : $"{tiempoEstadia.Minutes} minutos";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            background-color: #f5f5f5;
            margin: 0;
            padding: 0;
        }}
        .email-container {{
            max-width: 650px;
            margin: 20px auto;
            background-color: #ffffff;
            border-radius: 10px;
            overflow: hidden;
            box-shadow: 0 4px 15px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #4CAF50 0%, #2e7d32 100%);
            color: white;
            padding: 30px 20px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 32px;
            font-weight: bold;
        }}
        .header p {{
            margin: 10px 0 0 0;
            font-size: 18px;
            opacity: 0.9;
        }}
        .thank-you-banner {{
            background: linear-gradient(135deg, #66BB6A 0%, #4CAF50 100%);
            color: white;
            padding: 20px;
            text-align: center;
            font-weight: bold;
            font-size: 20px;
        }}
        .content {{
            padding: 30px;
        }}
        .success-box {{
            background: linear-gradient(135deg, #e8f5e9 0%, #c8e6c9 100%);
            border-left: 4px solid #4CAF50;
            padding: 20px;
            margin-bottom: 25px;
            border-radius: 4px;
            text-align: center;
            box-shadow: 0 2px 8px rgba(0,0,0,0.05);
        }}
        .success-box .icon {{
            font-size: 64px;
            margin-bottom: 10px;
        }}
        .success-box strong {{
            color: #2e7d32;
            font-size: 20px;
        }}
        .section {{
            margin-bottom: 30px;
        }}
        .section-title {{
            font-size: 20px;
            font-weight: bold;
            color: #4CAF50;
            margin-bottom: 15px;
            padding-bottom: 10px;
            border-bottom: 2px solid #e0e0e0;
            display: flex;
            align-items: center;
        }}
        .section-title::before {{
            content: '';
            display: inline-block;
            width: 6px;
            height: 24px;
            background-color: #4CAF50;
            margin-right: 10px;
            border-radius: 3px;
        }}
        .info-grid {{
            display: table;
            width: 100%;
            border-collapse: collapse;
        }}
        .info-row {{
            display: table-row;
            border-bottom: 1px solid #f0f0f0;
        }}
        .info-row:last-child {{
            border-bottom: none;
        }}
        .info-label {{
            display: table-cell;
            padding: 12px 15px;
            font-weight: 600;
            color: #555;
            width: 40%;
            background-color: #f9f9f9;
        }}
        .info-value {{
            display: table-cell;
            padding: 12px 15px;
            color: #333;
        }}
        .summary-box {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 25px;
            border-radius: 8px;
            margin: 25px 0;
        }}
        .summary-box h3 {{
            margin: 0 0 15px 0;
            font-size: 20px;
        }}
        .summary-item {{
            display: flex;
            justify-content: space-between;
            padding: 10px 0;
            border-bottom: 1px solid rgba(255,255,255,0.2);
        }}
        .summary-item:last-child {{
            border-bottom: none;
            font-size: 24px;
            font-weight: bold;
            margin-top: 10px;
        }}
        .patent-badge {{
            display: inline-block;
            background-color: #2196F3;
            color: white;
            padding: 8px 20px;
            border-radius: 5px;
            font-weight: bold;
            font-size: 18px;
            letter-spacing: 2px;
            font-family: 'Courier New', monospace;
        }}
        .feedback-box {{
            background-color: #fff9c4;
            border: 2px dashed #FBC02D;
            padding: 20px;
            margin: 25px 0;
            border-radius: 8px;
            text-align: center;
        }}
        .feedback-box h3 {{
            color: #F57F17;
            margin: 0 0 10px 0;
        }}
        .star-rating {{
            font-size: 32px;
            margin: 15px 0;
        }}
        .info-note {{
            background-color: #e3f2fd;
            border-left: 4px solid #2196F3;
            padding: 15px 20px;
            margin: 20px 0;
            border-radius: 4px;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 25px 30px;
            text-align: center;
            border-top: 1px solid #e0e0e0;
        }}
        .footer p {{
            margin: 5px 0;
            font-size: 13px;
            color: #666;
        }}
    </style>
</head>
<body>
    <div class='email-container'>
        <!-- Header -->
        <div class='header'>
            <h1>🅿️ EasyParking</h1>
            <p>Confirmación de Salida</p>
        </div>

        <!-- Thank You Banner -->
        <div class='thank-you-banner'>
            ✨ ¡GRACIAS POR ELEGIRNOS! Esperamos verte pronto
        </div>

        <!-- Content -->
        <div class='content'>
            <!-- Success Message -->
            <div class='success-box'>
                <div class='icon'>✅</div>
                <strong>Tu salida ha sido registrada exitosamente</strong>
                <p style='margin: 10px 0 0 0; color: #2e7d32;'>
                    Gracias por confiar en nosotros, {notification.NombreCliente}!
                </p>
            </div>

            <!-- Saludo personalizado -->
            <h2>¡Hasta pronto, {notification.NombreCliente} {notification.ApellidoCliente}!</h2>
            <p>Esperamos que hayas tenido una excelente experiencia en <strong>{notification.NombreDelEstacionamiento}</strong>. 
            Fue un placer tenerte como cliente.</p>

            <!-- Resumen de la Visita -->
            <div class='summary-box'>
                <h3>📊 Resumen de tu Visita</h3>
                <div class='summary-item'>
                    <span>Reserva:</span>
                    <span>#{notification.NumeroReserva}</span>
                </div>
                <div class='summary-item'>
                    <span>Entrada:</span>
                    <span>{fechaArribo}</span>
                </div>
                <div class='summary-item'>
                    <span>Salida:</span>
                    <span>{fechaSalida}</span>
                </div>
                <div class='summary-item'>
                    <span>Tiempo total:</span>
                    <span>{tiempoEstadiaTexto}</span>
                </div>
                <div class='summary-item'>
                    <span>💰 Total:</span>
                    <span>${notification.MontoTotal:N2}</span>
                </div>
            </div>

            <!-- Datos del Estacionamiento -->
            <div class='section'>
                <div class='section-title'>
                    🏢 Estacionamiento Utilizado
                </div>
                <div class='info-grid'>
                    <div class='info-row'>
                        <div class='info-label'>Nombre:</div>
                        <div class='info-value'><strong>{notification.NombreDelEstacionamiento}</strong></div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Dirección:</div>
                        <div class='info-value'>{notification.DireccionDelEstacionamiento}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Tipo de Lugar:</div>
                        <div class='info-value'>{notification.TipoDeLugar}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Contacto:</div>
                        <div class='info-value'>{notification.NombrePropietario} - {notification.TelefonoPropietario}</div>
                    </div>
                </div>
            </div>

            <!-- Datos del Vehículo -->
            <div class='section'>
                <div class='section-title'>
                    🚗 Tu Vehículo
                </div>
                <div class='info-grid'>
                    <div class='info-row'>
                        <div class='info-label'>Tipo:</div>
                        <div class='info-value'>{notification.TipoDeVehiculo}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Patente:</div>
                        <div class='info-value'>
                            <span class='patent-badge'>{notification.Patente}</span>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Feedback Box -->
            <div class='feedback-box'>
                <h3>⭐ ¿Cómo fue tu experiencia?</h3>
                <p>Tu opinión es muy importante para nosotros</p>
                <div class='star-rating'>⭐⭐⭐⭐⭐</div>
                <p style='margin: 15px 0 5px 0; font-size: 14px; color: #666;'>
                    Pronto podrás calificar tu experiencia desde la app
                </p>
            </div>

            <!-- Información Adicional -->
            <div class='info-note'>
                <strong>ℹ️ Información útil:</strong>
                <ul style='margin: 10px 0 0 0; padding-left: 20px;'>
                    <li>Guarda este email como comprobante de tu visita</li>
                    <li>Puedes consultar tu historial en la aplicación</li>
                    <li>Si olvidaste algo, contacta al propietario: {notification.TelefonoPropietario}</li>
                    <li>Próximamente recibirás puntos de fidelidad por esta reserva</li>
                </ul>
            </div>

            <!-- Mensaje final -->
            <div style='text-align: center; margin: 30px 0; padding: 20px; background-color: #f9f9f9; border-radius: 8px;'>
                <h3 style='color: #4CAF50; margin: 0 0 10px 0;'>🎉 ¡Vuelve pronto!</h3>
                <p style='margin: 0; color: #666;'>
                    Esperamos verte nuevamente en <strong>{notification.NombreDelEstacionamiento}</strong><br>
                    o en cualquiera de nuestros estacionamientos asociados.
                </p>
            </div>
        </div>

        <!-- Footer -->
        <div class='footer'>
            <p><strong>EasyParking</strong> - Tu mejor opción para estacionar</p>
            <p>¿Tienes alguna consulta o sugerencia? Estamos aquí para ayudarte.</p>
            <p style='margin-top: 15px; color: #999;'>
                📧 soporte@easyparking.com | 📱 +54 9 379 XXX-XXXX
            </p>
            <p style='margin-top: 15px;'>
                Síguenos en redes sociales: 
                <span style='font-size: 18px;'>📱 💻 🌐</span>
            </p>
            <p style='margin-top: 10px;'>&copy; 2025 EasyParking. Todos los derechos reservados.</p>
        </div>
    </div>
</body>
</html>";
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult> SetReservaArriboExitosoAsync([FromBody] int reservaId)
        {
            try
            {
                DataContext dataContext = new DataContext();

                // Obtener la reserva
                var reserva = await dataContext.Reservas
                    .Where(x => x.Id == reservaId)
                    .FirstOrDefaultAsync();

                if (reserva == null)
                    return NotFound("Reserva no encontrada");

                // Obtener el vehículo
                var vehiculo = await dataContext.Vehiculos
                    .Where(x => x.Id == reserva.VehiculoId)
                    .FirstOrDefaultAsync();

                if (vehiculo == null)
                    return BadRequest("No se encontró el vehículo asociado");

                // Obtener datos del estacionamiento
                var estacionamiento = await dataContext.Estacionamientos
                    .Where(x => x.Id == reserva.EstacionamientoId)
                    .FirstOrDefaultAsync();

                if (estacionamiento == null)
                    return BadRequest("No se encontró el estacionamiento");

                // Obtener datos del cliente
                var cliente = await _userManager.FindByIdAsync(reserva.UserId);

                if (cliente == null || string.IsNullOrEmpty(cliente.Email))
                {
                    Console.WriteLine("Advertencia: No se pudo obtener el email del cliente");
                }

                // Obtener datos del propietario
                var propietario = await _userManager.FindByIdAsync(estacionamiento.UserId);

                // Actualizar estado de la reserva
                reserva.Estado = EstadoReserva.ARRIBO_EXITOSO;
                reserva.FechaDeArribo = DateTime.Now;

                // Guardar cambios
                dataContext.Reservas.Update(reserva);
                await dataContext.SaveChangesAsync();

                // Enviar email de bienvenida al cliente
                if (cliente != null && !string.IsNullOrEmpty(cliente.Email))
                {
                    try
                    {
                        var notificacion = new ArrivalNotificationDTO
                        {
                            // Datos del cliente
                            NombreCliente = cliente.Nombre ?? "Cliente",
                            ApellidoCliente = cliente.Apellido ?? "",
                            EmailCliente = cliente.Email,

                            // Datos de la reserva
                            NumeroReserva = reserva.Id.ToString(),
                            FechaDeCreacion = reserva.FechaDeCreacion,
                            FechaDeArribo = reserva.FechaDeArribo ?? DateTime.Now,
                            FechaDeExpiracion = reserva.FechaDeExpiracion,
                            CodigoDeValidacion = reserva.CodigoDeValidacion ?? "N/A",
                            MontoReserva = reserva.Monto,

                            // Datos del estacionamiento
                            NombreDelEstacionamiento = estacionamiento.Nombre,
                            DireccionDelEstacionamiento = estacionamiento.Direccion ?? "No especificada",
                            TipoDeLugar = estacionamiento.TipoDeLugar ?? "No especificado",

                            // Datos del vehículo
                            TipoDeVehiculo = vehiculo.TipoDeVehiculo,
                            Patente = vehiculo.Patente ?? reserva.Patente,

                            // Datos del propietario
                            NombrePropietario = propietario?.Nombre ?? "Propietario",
                            TelefonoPropietario = propietario?.Telefono ?? "No disponible"
                        };

                        bool emailEnviado = await SendArrivalConfirmationEmail(notificacion);

                        if (!emailEnviado)
                        {
                            Console.WriteLine($"Advertencia: Llegada registrada pero no se pudo enviar email al cliente. ReservaId: {reserva.Id}");
                        }
                        else
                        {
                            Console.WriteLine($"✅ Email de bienvenida enviado al cliente: {cliente.Email}");
                        }
                    }
                    catch (Exception emailEx)
                    {
                        Console.WriteLine($"Error al enviar email de confirmación de llegada: {emailEx.Message}");
                    }
                }

                return Ok(new
                {
                    message = "Llegada registrada exitosamente",
                    reservaId = reserva.Id,
                    fechaArribo = reserva.FechaDeArribo,
                    emailEnviado = cliente != null && !string.IsNullOrEmpty(cliente.Email)
                });
            }
            catch (Exception e)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }

        // ============================================
        // 3. MÉTODO PARA ENVIAR EMAIL DE CONFIRMACIÓN DE LLEGADA
        // ============================================

        private async Task<bool> SendArrivalConfirmationEmail(ArrivalNotificationDTO notification)
        {
            try
            {
                using (var smtpClient = new System.Net.Mail.SmtpClient(_From_SmtpServer))
                {
                    smtpClient.Port = 587;
                    smtpClient.EnableSsl = true;
                    smtpClient.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new System.Net.NetworkCredential(_From_EmailAdress, _From_EmailPassword);
                    smtpClient.Timeout = 20000;

                    var mailMessage = new System.Net.Mail.MailMessage
                    {
                        From = new System.Net.Mail.MailAddress(_From_EmailAdress, _From_Name),
                        Subject = $"🎉 ¡Bienvenido a {notification.NombreDelEstacionamiento}!",
                        Body = GenerateArrivalConfirmationEmailBody(notification),
                        IsBodyHtml = true,
                        Priority = System.Net.Mail.MailPriority.Normal
                    };

                    mailMessage.To.Add(notification.EmailCliente);

                    await smtpClient.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar email de confirmación de llegada: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        // ============================================
        // 4. PLANTILLA HTML DEL EMAIL DE BIENVENIDA
        // ============================================

        private string GenerateArrivalConfirmationEmailBody(ArrivalNotificationDTO notification)
        {
            string fechaArribo = notification.FechaDeArribo.ToString("dd/MM/yyyy HH:mm");
            string fechaExpiracion = notification.FechaDeExpiracion.ToString("dd/MM/yyyy HH:mm");

            // Calcular tiempo disponible
            TimeSpan tiempoDisponible = notification.FechaDeExpiracion - notification.FechaDeArribo;
            string tiempoDisponibleTexto = tiempoDisponible.TotalHours >= 24
                ? $"{(int)tiempoDisponible.TotalDays} día(s) {tiempoDisponible.Hours} hora(s)"
                : tiempoDisponible.TotalHours >= 1
                    ? $"{(int)tiempoDisponible.TotalHours} hora(s) {tiempoDisponible.Minutes} min"
                    : $"{tiempoDisponible.Minutes} minutos";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            background-color: #f5f5f5;
            margin: 0;
            padding: 0;
        }}
        .email-container {{
            max-width: 650px;
            margin: 20px auto;
            background-color: #ffffff;
            border-radius: 10px;
            overflow: hidden;
            box-shadow: 0 4px 15px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px 20px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 32px;
            font-weight: bold;
        }}
        .header p {{
            margin: 10px 0 0 0;
            font-size: 18px;
            opacity: 0.9;
        }}
        .welcome-banner {{
            background: linear-gradient(135deg, #4CAF50 0%, #2e7d32 100%);
            color: white;
            padding: 20px;
            text-align: center;
            font-weight: bold;
            font-size: 22px;
        }}
        .content {{
            padding: 30px;
        }}
        .welcome-box {{
            background: linear-gradient(135deg, #e8f5e9 0%, #c8e6c9 100%);
            border-left: 4px solid #4CAF50;
            padding: 20px;
            margin-bottom: 25px;
            border-radius: 4px;
            text-align: center;
            box-shadow: 0 2px 8px rgba(0,0,0,0.05);
        }}
        .welcome-box .icon {{
            font-size: 64px;
            margin-bottom: 10px;
        }}
        .welcome-box strong {{
            color: #2e7d32;
            font-size: 20px;
            display: block;
            margin-top: 10px;
        }}
        .section {{
            margin-bottom: 30px;
        }}
        .section-title {{
            font-size: 20px;
            font-weight: bold;
            color: #667eea;
            margin-bottom: 15px;
            padding-bottom: 10px;
            border-bottom: 2px solid #e0e0e0;
            display: flex;
            align-items: center;
        }}
        .section-title::before {{
            content: '';
            display: inline-block;
            width: 6px;
            height: 24px;
            background-color: #667eea;
            margin-right: 10px;
            border-radius: 3px;
        }}
        .info-grid {{
            display: table;
            width: 100%;
            border-collapse: collapse;
        }}
        .info-row {{
            display: table-row;
            border-bottom: 1px solid #f0f0f0;
        }}
        .info-row:last-child {{
            border-bottom: none;
        }}
        .info-label {{
            display: table-cell;
            padding: 12px 15px;
            font-weight: 600;
            color: #555;
            width: 40%;
            background-color: #f9f9f9;
        }}
        .info-value {{
            display: table-cell;
            padding: 12px 15px;
            color: #333;
        }}
        .time-box {{
            background: linear-gradient(135deg, #fff3e0 0%, #ffe0b2 100%);
            padding: 20px;
            border-radius: 8px;
            margin: 25px 0;
            border: 2px solid #ff9800;
        }}
        .time-box h3 {{
            margin: 0 0 15px 0;
            color: #e65100;
            font-size: 20px;
            text-align: center;
        }}
        .time-item {{
            display: flex;
            justify-content: space-between;
            padding: 10px 0;
            border-bottom: 1px solid rgba(0,0,0,0.1);
        }}
        .time-item:last-child {{
            border-bottom: none;
            font-size: 18px;
            font-weight: bold;
            margin-top: 10px;
        }}
        .validation-code {{
            background: linear-gradient(135deg, #e3f2fd 0%, #bbdefb 100%);
            padding: 20px;
            border-radius: 8px;
            text-align: center;
            margin: 25px 0;
            border: 2px solid #2196F3;
        }}
        .validation-code h3 {{
            margin: 0 0 15px 0;
            color: #1565c0;
        }}
        .code {{
            font-size: 36px;
            font-weight: bold;
            color: #0d47a1;
            letter-spacing: 8px;
            font-family: 'Courier New', monospace;
            background-color: white;
            padding: 15px 30px;
            border-radius: 8px;
            display: inline-block;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }}
        .patent-badge {{
            display: inline-block;
            background-color: #2196F3;
            color: white;
            padding: 8px 20px;
            border-radius: 5px;
            font-weight: bold;
            font-size: 18px;
            letter-spacing: 2px;
            font-family: 'Courier New', monospace;
        }}
        .tips-box {{
            background-color: #fff9c4;
            border-left: 4px solid #fbc02d;
            padding: 20px;
            margin: 20px 0;
            border-radius: 4px;
        }}
        .tips-box h3 {{
            margin: 0 0 10px 0;
            color: #f57f17;
        }}
        .tips-box ul {{
            margin: 10px 0 0 0;
            padding-left: 20px;
        }}
        .tips-box li {{
            margin: 8px 0;
        }}
        .contact-box {{
            background-color: #f3e5f5;
            padding: 20px;
            border-radius: 8px;
            margin: 25px 0;
            text-align: center;
        }}
        .contact-box h3 {{
            margin: 0 0 10px 0;
            color: #6a1b9a;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 25px 30px;
            text-align: center;
            border-top: 1px solid #e0e0e0;
        }}
        .footer p {{
            margin: 5px 0;
            font-size: 13px;
            color: #666;
        }}
    </style>
</head>
<body>
    <div class='email-container'>
        <!-- Header -->
        <div class='header'>
            <h1>🅿️ EasyParking</h1>
            <p>Confirmación de Llegada</p>
        </div>

        <!-- Welcome Banner -->
        <div class='welcome-banner'>
            🎉 ¡BIENVENIDO! Disfruta tu estadía
        </div>

        <!-- Content -->
        <div class='content'>
            <!-- Welcome Message -->
            <div class='welcome-box'>
                <div class='icon'>🚗✨</div>
                <p style='margin: 0; font-size: 18px; color: #2e7d32;'>
                    ¡Hola, {notification.NombreCliente}!
                </p>
                <strong>Tu llegada ha sido confirmada exitosamente</strong>
                <p style='margin: 10px 0 0 0; color: #2e7d32;'>
                    Gracias por elegirnos. ¡Que tengas un excelente día!
                </p>
            </div>

            <!-- Código de Validación -->
            {(string.IsNullOrEmpty(notification.CodigoDeValidacion) || notification.CodigoDeValidacion == "N/A" ? "" : $@"
            <div class='validation-code'>
                <h3>🔐 Código de Validación</h3>
                <p style='margin: 0 0 15px 0; font-size: 14px; color: #666;'>
                    Guarda este código, puede ser requerido para tu salida
                </p>
                <div class='code'>{notification.CodigoDeValidacion}</div>
            </div>
            ")}

            <!-- Tiempo Disponible -->
            <div class='time-box'>
                <h3>⏰ Información de Tiempo</h3>
                <div class='time-item'>
                    <span>✅ Llegada:</span>
                    <span>{fechaArribo}</span>
                </div>
                <div class='time-item'>
                    <span>⚠️ Vence:</span>
                    <span>{fechaExpiracion}</span>
                </div>
                <div class='time-item'>
                    <span>⌛ Tiempo disponible:</span>
                    <span>{tiempoDisponibleTexto}</span>
                </div>
            </div>

            <!-- Detalles del Estacionamiento -->
            <div class='section'>
                <div class='section-title'>
                    🏢 Tu Estacionamiento
                </div>
                <div class='info-grid'>
                    <div class='info-row'>
                        <div class='info-label'>Nombre:</div>
                        <div class='info-value'><strong>{notification.NombreDelEstacionamiento}</strong></div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Dirección:</div>
                        <div class='info-value'>{notification.DireccionDelEstacionamiento}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Tipo de Lugar:</div>
                        <div class='info-value'>{notification.TipoDeLugar}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Reserva:</div>
                        <div class='info-value'>#{notification.NumeroReserva}</div>
                    </div>
                </div>
            </div>

            <!-- Datos del Vehículo -->
            <div class='section'>
                <div class='section-title'>
                    🚗 Tu Vehículo Estacionado
                </div>
                <div class='info-grid'>
                    <div class='info-row'>
                        <div class='info-label'>Tipo:</div>
                        <div class='info-value'>{notification.TipoDeVehiculo}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Patente:</div>
                        <div class='info-value'>
                            <span class='patent-badge'>{notification.Patente}</span>
                        </div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Monto:</div>
                        <div class='info-value'><strong>${notification.MontoReserva:N2}</strong></div>
                    </div>
                </div>
            </div>

            <!-- Tips Importantes -->
            <div class='tips-box'>
                <h3>💡 Consejos Importantes</h3>
                <ul>
                    <li><strong>Guarda este email</strong> como comprobante de tu estadía</li>
                    <li>Asegúrate de retirar tu vehículo antes de <strong>{fechaExpiracion}</strong></li>
                    <li>Verifica que tu vehículo esté cerrado y con seguro activado</li>
                    <li>Si necesitas extender tu tiempo, contacta al propietario</li>
                    <li>Al salir, espera la confirmación del propietario</li>
                </ul>
            </div>

            <!-- Contacto -->
            <div class='contact-box'>
                <h3>📞 ¿Necesitas ayuda?</h3>
                <p style='margin: 10px 0;'>
                    <strong>Propietario:</strong> {notification.NombrePropietario}<br>
                    <strong>Teléfono:</strong> {notification.TelefonoPropietario}
                </p>
                <p style='margin: 15px 0 0 0; font-size: 14px; color: #666;'>
                    No dudes en contactar si necesitas algo durante tu estadía
                </p>
            </div>

            <!-- Mensaje final -->
            <div style='text-align: center; margin: 30px 0; padding: 20px; background: linear-gradient(135deg, #e8f5e9 0%, #c8e6c9 100%); border-radius: 8px;'>
                <h3 style='color: #2e7d32; margin: 0 0 10px 0;'>✨ Disfruta tu tiempo</h3>
                <p style='margin: 0; color: #1b5e20;'>
                    Tu vehículo está seguro. Realiza tus actividades con tranquilidad.<br>
                    ¡Gracias por confiar en <strong>EasyParking</strong>!
                </p>
            </div>
        </div>

        <!-- Footer -->
        <div class='footer'>
            <p><strong>EasyParking</strong> - Estaciona con confianza</p>
            <p>Este es un correo automático, por favor no respondas a este mensaje.</p>
            <p style='margin-top: 15px; color: #999;'>
                ¿Dudas o consultas?<br>
                📧 soporte@easyparking.com | 📱 +54 9 379 XXX-XXXX
            </p>
            <p style='margin-top: 10px;'>&copy; 2025 EasyParking. Todos los derechos reservados.</p>
        </div>
    </div>
</body>
</html>";
        }

    }


}
