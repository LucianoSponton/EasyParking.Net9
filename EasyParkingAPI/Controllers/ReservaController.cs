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

        [HttpPost]
        [Route("[action]")]
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
    }


}
