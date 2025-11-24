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
            catch (Exception)
            {
                throw;
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
                        .Include(e => e.Plazas)
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

                //var estacionamientos = await dataContext.Estacionamientos.Include("Jornadas.Horarios").AsNoTracking()
                //                    .Include("TiposDeVehiculosAdmitidos").AsNoTracking().Where(x => !x.Inactivo && !x.PublicacionPausada && x.UserId == _UserId).ToListAsync();

                //if (estacionamientos == null)
                //{
                //    return NotFound();
                //}

                var estacionamientos = await dataContext.Estacionamientos
                    .Include(e => e.Jornadas)
                        .ThenInclude(j => j.Horarios)
                    .Include(e => e.Tarifas)
                    .Include(e => e.Plazas)
                    .AsNoTracking()
                    .Where(x => !x.Inactivo && !x.PublicacionPausada && x.UserId == _UserId)
                    .ToListAsync();

                if (estacionamientos == null || !estacionamientos.Any())
                {
                    return NoContent();
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
                   
                    var fileName = appuser.Id + ".jpg";
                    reservaDTO.Link_FotoCliente = $"http://40.118.242.96:12595/images/usuarios/{fileName}";
                  
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

                //var datoVehiculoSobreAlojado = await dataContext.DataVehiculoAlojados.Where(x => x.EstacionamientoId == reserva.EstacionamientoId && x.TipoDeVehiculo == vehiculo.TipoDeVehiculo).FirstOrDefaultAsync();

                //if (datoVehiculoSobreAlojado.CantidadActualAlojados > 0)
                //{
                //    datoVehiculoSobreAlojado.CantidadActualAlojados--;
                //}

                //dataContext.DataVehiculoAlojados.Update(datoVehiculoSobreAlojado);

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
        //public async Task<ActionResult> AddAsync([FromBody] Reserva reserva)
        //{
        //    try
        //    {
        //        DataContext dataContext = new DataContext();
        //        reserva.UserId = _UserId;

        //        // **VALIDACIONES BÁSICAS DE FECHAS**
        //        if (reserva.FechaInicio == default || reserva.FechaFin == default)
        //            return BadRequest("ERROR.. Debe especificar la fecha de inicio y fin de la reserva");

        //        if (reserva.FechaInicio >= reserva.FechaFin)
        //            return BadRequest("ERROR.. La fecha de inicio debe ser anterior a la fecha de fin");

        //        if (reserva.FechaInicio < DateTime.Now)
        //            return BadRequest("ERROR.. No puede hacer reservas para fechas pasadas");

        //        // **VALIDAR QUE NO TENGA RESERVAS ACTIVAS CON ESTE VEHÍCULO**
        //        var tieneReservaActiva = await dataContext.Reservas
        //            .AnyAsync(x => x.UserId == reserva.UserId &&
        //                          x.VehiculoId == reserva.VehiculoId &&
        //                          (x.Estado == EstadoReserva.ESPERANDO_ARRIBO ||
        //                           x.Estado == EstadoReserva.ARRIBO_EXITOSO));

        //        if (tieneReservaActiva)
        //            return BadRequest("ERROR.. Ya tiene una reserva activa con este vehículo");

        //        // **VALIDAR VEHÍCULO**
        //        var vehiculo = await dataContext.Vehiculos
        //            .Where(x => x.Id == reserva.VehiculoId && x.UserId == _UserId)
        //            .FirstOrDefaultAsync();

        //        if (vehiculo == null)
        //            return BadRequest("ERROR.. No se encontró su vehículo");

        //        // Asignar el tipo de vehículo a la reserva
        //        reserva.TipoDeVehiculo = vehiculo.TipoDeVehiculo;
        //        reserva.Patente = vehiculo.Patente;

        //        // **OBTENER ESTACIONAMIENTO**
        //        var estacionamiento = await dataContext.Estacionamientos
        //            .Include(e => e.Jornadas)
        //                .ThenInclude(j => j.Horarios)
        //            .Where(x => x.Id == reserva.EstacionamientoId)
        //            .FirstOrDefaultAsync();

        //        if (estacionamiento == null)
        //            return BadRequest("ERROR.. No se encontró el estacionamiento");

        //        if (estacionamiento.PublicacionPausada)
        //            return BadRequest("ERROR.. Este estacionamiento no está disponible actualmente");

        //        // **VALIDAR QUE EL TIPO DE VEHÍCULO SEA ADMITIDO**
        //        var datoVehiculoAdmitido = await dataContext.DataVehiculoAlojados
        //            .Where(x => x.EstacionamientoId == reserva.EstacionamientoId &&
        //                        x.TipoDeVehiculo == vehiculo.TipoDeVehiculo)
        //            .FirstOrDefaultAsync();

        //        if (datoVehiculoAdmitido == null)
        //            return BadRequest($"ERROR.. El tipo de su vehículo ({vehiculo.TipoDeVehiculo}) no es admitido en este estacionamiento");

        //        // **VALIDAR JORNADAS (que el estacionamiento esté abierto)**
        //        if (!ValidarJornadasDisponibles(estacionamiento.Jornadas, reserva.FechaInicio, reserva.FechaFin))
        //            return BadRequest("ERROR.. El estacionamiento no está disponible en el horario solicitado. Revise los días y horarios de operación.");

        //        // **BUSCAR PLAZA DISPONIBLE SIN SOLAPAMIENTOS**
        //        var plazaDisponible = await BuscarPlazaDisponibleSinSolapamiento(
        //            dataContext,
        //            reserva.EstacionamientoId,
        //            vehiculo.TipoDeVehiculo,
        //            reserva.FechaInicio,
        //            reserva.FechaFin
        //        );

        //        if (plazaDisponible == null)
        //            return BadRequest($"ERROR.. No hay plazas disponibles para {vehiculo.TipoDeVehiculo} en las fechas seleccionadas. Todas las plazas están ocupadas en ese período.");

        //        // **ASIGNAR LA PLAZA A LA RESERVA**
        //        reserva.PlazaId = plazaDisponible.Id;

        //        // **CALCULAR MONTO TOTAL**
        //        decimal montoTotal = CalcularMontoReserva(
        //            datoVehiculoAdmitido,
        //            reserva.FechaInicio,
        //            reserva.FechaFin,
        //            reserva.Monto // Monto de reserva inicial
        //        );

        //        reserva.Monto = montoTotal;

        //        // **OBTENER DATOS PARA NOTIFICACIONES**
        //        var propietario = await _userManager.FindByIdAsync(estacionamiento.UserId);
        //        var cliente = await _userManager.FindByIdAsync(reserva.UserId);

        //        if (cliente == null)
        //            return BadRequest("ERROR.. No se encontraron los datos del cliente");

        //        // **GUARDAR LA RESERVA**
        //        await dataContext.Reservas.AddAsync(reserva);
        //        await dataContext.SaveChangesAsync();

        //        // **ENVIAR NOTIFICACIÓN POR EMAIL**
        //        if (propietario != null && !string.IsNullOrEmpty(propietario.Email))
        //        {
        //            try
        //            {
        //                var notificacion = new ReservationNotificationDTO
        //                {
        //                    // Datos del cliente
        //                    NombreCliente = cliente.Nombre ?? "Cliente",
        //                    ApellidoCliente = cliente.Apellido ?? "",
        //                    TipoDeVehiculoCliente = vehiculo.TipoDeVehiculo,
        //                    PatenteCliente = vehiculo.Patente,

        //                    // Datos del estacionamiento
        //                    NombreDelEstacionamiento = estacionamiento.Nombre,
        //                    DireccionDelEstacionamiento = estacionamiento.Direccion ?? "No especificada",
        //                    TipoDeLugarDelEstacionamiento = estacionamiento.TipoDeLugar,
        //                    MontoDeLaReserva = montoTotal,

        //                    // Datos adicionales
        //                    EmailPropietario = propietario.Email,
        //                    FechaHoraReserva = reserva.FechaDeCreacion,
        //                    FechaHoraExpiracion = reserva.FechaDeExpiracion,
        //                    FechaInicio = reserva.FechaInicio,
        //                    FechaFin = reserva.FechaFin,
        //                    NumeroReserva = reserva.Id.ToString(),
        //                    NumeroPlaza = plazaDisponible.Id.ToString(),
        //                    CodigoDeValidacion = reserva.CodigoDeValidacion
        //                };

        //                bool emailEnviado = await SendReservationEmail(notificacion);

        //                if (emailEnviado)
        //                {
        //                    Console.WriteLine($"✅ Notificación enviada exitosamente al propietario: {propietario.Email}");
        //                }
        //            }
        //            catch (Exception emailEx)
        //            {
        //                Console.WriteLine($"Error al enviar notificación por email: {emailEx.Message}");
        //            }
        //        }

        //        return Ok(new
        //        {
        //            success = true,
        //            message = "Reserva creada exitosamente",
        //            reserva = new
        //            {
        //                id = reserva.Id,
        //                plazaId = plazaDisponible.Id,
        //                codigoValidacion = reserva.CodigoDeValidacion,
        //                fechaInicio = reserva.FechaInicio,
        //                fechaFin = reserva.FechaFin,
        //                monto = montoTotal,
        //                estado = reserva.Estado.ToString(),
        //                fechaExpiracion = reserva.FechaDeExpiracion
        //            },
        //            emailEnviado = propietario != null && !string.IsNullOrEmpty(propietario.Email)
        //        });
        //    }
        //    catch (Exception e)
        //    {
        //        return BadRequest($"ERROR.. {Tools.Tools.ExceptionMessage(e)}");
        //    }
        //}

        /// <summary>
        /// Busca una plaza disponible verificando que NO haya solapamiento de reservas
        /// ESTE ES EL MÉTODO CLAVE PARA EVITAR CONFLICTOS
        /// </summary>
        private async Task<Plaza> BuscarPlazaDisponibleSinSolapamiento(
            DataContext dataContext,
            int estacionamientoId,
            TipoDeVehiculo tipoDeVehiculo,
            DateTime fechaInicio,
            DateTime fechaFin)
        {
            // 1. Obtener todas las plazas del estacionamiento para este tipo de vehículo
            var plazasDelTipo = await dataContext.Plazas
                .Where(p => p.EstacionamientoId == estacionamientoId &&
                            p.TipoDeVehiculo == tipoDeVehiculo)
                .ToListAsync();

            if (!plazasDelTipo.Any())
                return null; // No hay plazas de este tipo

            // 2. Obtener TODAS las reservas ACTIVAS que puedan solaparse con el período solicitado
            // Una reserva se solapa si: su inicio es antes del fin solicitado Y su fin es después del inicio solicitado
            var reservasEnPeriodo = await dataContext.Reservas
                .Where(r => r.EstacionamientoId == estacionamientoId &&
                            r.TipoDeVehiculo == tipoDeVehiculo &&
                            // Solo considerar reservas activas/confirmadas
                            (r.Estado == EstadoReserva.ESPERANDO_ARRIBO ||
                             r.Estado == EstadoReserva.ARRIBO_EXITOSO) &&
                            // Condición de solapamiento: las reservas se solapan si:
                            // La reserva existente comienza ANTES de que termine la nueva solicitud
                            r.FechaInicio < fechaFin &&
                            // Y la reserva existente termina DESPUÉS de que comience la nueva solicitud
                            r.FechaFin > fechaInicio)
                .ToListAsync();

            // 3. Buscar la primera plaza que NO tenga reservas que se solapen
            foreach (var plaza in plazasDelTipo)
            {
                // Verificar si esta plaza específica tiene alguna reserva que se solape
                bool plazaOcupada = reservasEnPeriodo.Any(r => r.PlazaId == plaza.Id);

                if (!plazaOcupada)
                {
                    // ¡Plaza disponible encontrada!
                    return plaza;
                }
            }

            // No se encontró ninguna plaza disponible
            return null;
        }

        /// <summary>
        /// Valida que el estacionamiento esté abierto (jornadas) en las fechas solicitadas
        /// </summary>
        private bool ValidarJornadasDisponibles(
            List<Jornada> jornadas,
            DateTime fechaInicio,
            DateTime fechaFin)
        {
            if (jornadas == null || !jornadas.Any())
                return false; // Si no hay jornadas configuradas, no está disponible

            // Validar cada día entre fechaInicio y fechaFin
            DateTime fechaActual = fechaInicio.Date;

            while (fechaActual <= fechaFin.Date)
            {
                // Convertir DayOfWeek a tu enum Dia
                Dia diaEnum = ConvertirDayOfWeekADia(fechaActual.DayOfWeek);

                var jornadaDelDia = jornadas.FirstOrDefault(j => j.DiaDeLaSemana == diaEnum);

                if (jornadaDelDia == null || jornadaDelDia.Horarios == null || !jornadaDelDia.Horarios.Any())
                    return false; // Este día no está configurado o no tiene horarios

                // Si es el primer día, validar hora de inicio
                if (fechaActual.Date == fechaInicio.Date)
                {
                    bool horaInicioValida = jornadaDelDia.Horarios.Any(h =>
                    {
                        var inicioRango = new TimeSpan(h.DesdeHora, h.DesdeMinuto, 0);
                        var finRango = new TimeSpan(h.HastaHora, h.HastaMinuto, 0);
                        return fechaInicio.TimeOfDay >= inicioRango && fechaInicio.TimeOfDay <= finRango;
                    });

                    if (!horaInicioValida)
                        return false;
                }

                // Si es el último día, validar hora de fin
                if (fechaActual.Date == fechaFin.Date)
                {
                    bool horaFinValida = jornadaDelDia.Horarios.Any(h =>
                    {
                        var inicioRango = new TimeSpan(h.DesdeHora, h.DesdeMinuto, 0);
                        var finRango = new TimeSpan(h.HastaHora, h.HastaMinuto, 0);
                        return fechaFin.TimeOfDay >= inicioRango && fechaFin.TimeOfDay <= finRango;
                    });

                    if (!horaFinValida)
                        return false;
                }

                // Para días intermedios, solo verificar que exista la jornada
                // (ya validamos arriba que jornadaDelDia existe y tiene horarios)

                fechaActual = fechaActual.AddDays(1);
            }

            return true;
        }

        /// <summary>
        /// Convierte DayOfWeek del sistema a tu enum Dia personalizado
        /// </summary>
        private Dia ConvertirDayOfWeekADia(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => Dia.LUNES,
                DayOfWeek.Tuesday => Dia.MARTES,
                DayOfWeek.Wednesday => Dia.MIERCOLES,
                DayOfWeek.Thursday => Dia.JUEVES,
                DayOfWeek.Friday => Dia.VIERNES,
                DayOfWeek.Saturday => Dia.SABADO,
                DayOfWeek.Sunday => Dia.DOMINGO,
                _ => Dia.LUNES
            };
        }

        /// <summary>
        /// Calcula el monto total según la duración y las tarifas configuradas
        /// </summary>
        private decimal CalcularMontoReserva(
            Tarifa tarifa,
            DateTime fechaInicio,
            DateTime fechaFin,
            decimal montoReservaBase)
        {
            TimeSpan duracion = fechaFin - fechaInicio;

            // Seleccionar la tarifa más apropiada según la duración
            if (duracion.TotalDays >= 30)
            {
                // Tarifa mensual
                int meses = (int)Math.Ceiling(duracion.TotalDays / 30);
                return (tarifa.Tarifa_Mes * meses) + montoReservaBase;
            }
            else if (duracion.TotalDays >= 7)
            {
                // Tarifa semanal
                int semanas = (int)Math.Ceiling(duracion.TotalDays / 7);
                return (tarifa.Tarifa_Semana * semanas) + montoReservaBase;
            }
            else if (duracion.TotalDays >= 1)
            {
                // Tarifa diaria
                int dias = (int)Math.Ceiling(duracion.TotalDays);
                return (tarifa.Tarifa_Dia * dias) + montoReservaBase;
            }
            else
            {
                // Tarifa por hora
                int horas = (int)Math.Ceiling(duracion.TotalHours);
                if (horas < 1) horas = 1; // Mínimo 1 hora
                return (tarifa.Tarifa_Hora * horas) + montoReservaBase;
            }
        }



        //--------------------------------------------

        //[HttpPost]
        //[Route("[action]")]
        //public async Task<ActionResult> AddMultipleAsync([FromBody] List<Reserva> reservas)
        //{
        //    try
        //    {
        //        DataContext dataContext = new DataContext();

        //        if (reservas == null || !reservas.Any())
        //            return BadRequest("ERROR.. Debe enviar al menos una reserva");

        //        if (reservas.Count > 100)
        //            return BadRequest("ERROR.. No puede crear más de 100 reservas a la vez");

        //        var reservasCreadas = new List<object>();
        //        var reservasRechazadas = new List<object>();
        //        int totalProcesadas = 0;

        //        Console.WriteLine($"📦 Procesando {reservas.Count} reservas...");
        //        Console.WriteLine("==========================================");

        //        foreach (var reserva in reservas)
        //        {
        //            totalProcesadas++;
        //            Console.WriteLine($"\n[{totalProcesadas}/{reservas.Count}] Procesando reserva para {reserva.FechaInicio:dd/MM/yyyy HH:mm}...");

        //            try
        //            {
        //                reserva.UserId = _UserId;

        //                // **VALIDACIONES BÁSICAS**
        //                if (reserva.FechaInicio >= reserva.FechaFin)
        //                {
        //                    reservasRechazadas.Add(new
        //                    {
        //                        fecha = reserva.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
        //                        motivo = "Fecha de inicio debe ser anterior a fecha fin"
        //                    });
        //                    Console.WriteLine($"  ❌ Rechazada: Fecha inválida");
        //                    continue;
        //                }

        //                if (reserva.FechaInicio < DateTime.Now)
        //                {
        //                    reservasRechazadas.Add(new
        //                    {
        //                        fecha = reserva.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
        //                        motivo = "No puede hacer reservas para fechas pasadas"
        //                    });
        //                    Console.WriteLine($"  ❌ Rechazada: Fecha pasada");
        //                    continue;
        //                }

        //                // **VALIDAR VEHÍCULO**
        //                var vehiculo = await dataContext.Vehiculos
        //                    .Where(x => x.Id == reserva.VehiculoId && x.UserId == _UserId)
        //                    .FirstOrDefaultAsync();

        //                if (vehiculo == null)
        //                {
        //                    reservasRechazadas.Add(new
        //                    {
        //                        fecha = reserva.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
        //                        motivo = "No se encontró el vehículo"
        //                    });
        //                    Console.WriteLine($"  ❌ Rechazada: Vehículo no encontrado");
        //                    continue;
        //                }

        //                reserva.TipoDeVehiculo = vehiculo.TipoDeVehiculo;
        //                reserva.Patente = vehiculo.Patente;

        //                // **OBTENER ESTACIONAMIENTO**
        //                var estacionamiento = await dataContext.Estacionamientos
        //                    .Include(e => e.Jornadas)
        //                        .ThenInclude(j => j.Horarios)
        //                    .Where(x => x.Id == reserva.EstacionamientoId)
        //                    .FirstOrDefaultAsync();

        //                if (estacionamiento == null)
        //                {
        //                    reservasRechazadas.Add(new
        //                    {
        //                        fecha = reserva.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
        //                        motivo = "No se encontró el estacionamiento"
        //                    });
        //                    Console.WriteLine($"  ❌ Rechazada: Estacionamiento no encontrado");
        //                    continue;
        //                }

        //                if (estacionamiento.PublicacionPausada)
        //                {
        //                    reservasRechazadas.Add(new
        //                    {
        //                        fecha = reserva.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
        //                        motivo = "Estacionamiento no disponible (pausado)"
        //                    });
        //                    Console.WriteLine($"  ❌ Rechazada: Estacionamiento pausado");
        //                    continue;
        //                }

        //                // **VALIDAR TIPO DE VEHÍCULO ADMITIDO**
        //                var plazas = await dataContext.Plazas
        //                    .Where(x => x.EstacionamientoId == reserva.EstacionamientoId &&
        //                                x.TipoDeVehiculo == vehiculo.TipoDeVehiculo)
        //                    .FirstOrDefaultAsync();

        //                if (plazas == null)
        //                {
        //                    reservasRechazadas.Add(new
        //                    {
        //                        fecha = reserva.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
        //                        motivo = $"Tipo de vehículo ({vehiculo.TipoDeVehiculo}) no admitido en este estacionamiento"
        //                    });
        //                    Console.WriteLine($"  ❌ Rechazada: Tipo de vehículo no admitido");
        //                    continue;
        //                }

        //                // **VALIDAR JORNADAS**
        //                if (!ValidarJornadasDisponibles(estacionamiento.Jornadas, reserva.FechaInicio, reserva.FechaFin))
        //                {
        //                    var diaSemana = reserva.FechaInicio.ToString("dddd", new System.Globalization.CultureInfo("es-ES"));
        //                    reservasRechazadas.Add(new
        //                    {
        //                        fecha = reserva.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
        //                        motivo = $"Estacionamiento cerrado el {diaSemana} en horario {reserva.FechaInicio:HH:mm}-{reserva.FechaFin:HH:mm}"
        //                    });
        //                    Console.WriteLine($"  ❌ Rechazada: Fuera de jornada");
        //                    continue;
        //                }

        //                // ==========================================
        //                // 🔒 PASO 1: VERIFICAR BLOQUEOS PRIMERO
        //                // ==========================================
        //                Console.WriteLine($"  🔍 Verificando bloqueos...");

        //                // **1.1: BLOQUEO DE ESTACIONAMIENTO COMPLETO**
        //                var bloqueoEstacionamientoCompleto = await dataContext.BloqueoPlazas
        //                    .Where(b => b.EstacionamientoId == reserva.EstacionamientoId &&
        //                                b.Activo &&
        //                                b.PlazaId == null &&
        //                                b.TipoDeVehiculo == null &&
        //                                b.FechaInicio < reserva.FechaFin &&
        //                                b.FechaFin > reserva.FechaInicio)
        //                    .FirstOrDefaultAsync();

        //                if (bloqueoEstacionamientoCompleto != null)
        //                {
        //                    reservasRechazadas.Add(new
        //                    {
        //                        fecha = reserva.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
        //                        motivo = $"🔒 Estacionamiento bloqueado: {bloqueoEstacionamientoCompleto.Motivo} (del {bloqueoEstacionamientoCompleto.FechaInicio:dd/MM/yyyy} al {bloqueoEstacionamientoCompleto.FechaFin:dd/MM/yyyy})"
        //                    });
        //                    Console.WriteLine($"  🔒 Rechazada: Estacionamiento completo bloqueado - {bloqueoEstacionamientoCompleto.Motivo}");
        //                    continue;
        //                }

        //                // **1.2: BLOQUEO POR TIPO DE VEHÍCULO**
        //                var bloqueoTipoVehiculo = await dataContext.BloqueoPlazas
        //                    .Where(b => b.EstacionamientoId == reserva.EstacionamientoId &&
        //                                b.Activo &&
        //                                b.PlazaId == null &&
        //                                b.TipoDeVehiculo == vehiculo.TipoDeVehiculo &&
        //                                b.FechaInicio < reserva.FechaFin &&
        //                                b.FechaFin > reserva.FechaInicio)
        //                    .FirstOrDefaultAsync();

        //                if (bloqueoTipoVehiculo != null)
        //                {
        //                    reservasRechazadas.Add(new
        //                    {
        //                        fecha = reserva.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
        //                        motivo = $"🔒 Plazas de {vehiculo.TipoDeVehiculo} bloqueadas: {bloqueoTipoVehiculo.Motivo} (del {bloqueoTipoVehiculo.FechaInicio:dd/MM/yyyy} al {bloqueoTipoVehiculo.FechaFin:dd/MM/yyyy})"
        //                    });
        //                    Console.WriteLine($"  🔒 Rechazada: Tipo {vehiculo.TipoDeVehiculo} bloqueado - {bloqueoTipoVehiculo.Motivo}");
        //                    continue;
        //                }

        //                Console.WriteLine($"  ✅ Sin bloqueos generales");

        //                // ==========================================
        //                // 🔎 PASO 2: BUSCAR PLAZA DISPONIBLE
        //                // ==========================================
        //                Console.WriteLine($"  🔍 Buscando plaza disponible para {vehiculo.TipoDeVehiculo}...");

        //                // Obtener todas las plazas del tipo
        //                var plazasDelTipo = await dataContext.Plazas
        //                    .Where(p => p.EstacionamientoId == reserva.EstacionamientoId &&
        //                                p.TipoDeVehiculo == vehiculo.TipoDeVehiculo)
        //                    .ToListAsync();

        //                if (!plazasDelTipo.Any())
        //                {
        //                    reservasRechazadas.Add(new
        //                    {
        //                        fecha = reserva.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
        //                        motivo = $"No existen plazas para {vehiculo.TipoDeVehiculo} en este estacionamiento"
        //                    });
        //                    Console.WriteLine($"  ❌ Rechazada: No hay plazas del tipo {vehiculo.TipoDeVehiculo}");
        //                    continue;
        //                }

        //                // Obtener reservas activas
        //                var reservasEnPeriodo = await dataContext.Reservas
        //                    .Where(r => r.EstacionamientoId == reserva.EstacionamientoId &&
        //                                r.TipoDeVehiculo == vehiculo.TipoDeVehiculo &&
        //                                (r.Estado == EstadoReserva.ESPERANDO_ARRIBO ||
        //                                 r.Estado == EstadoReserva.ARRIBO_EXITOSO) &&
        //                                r.FechaInicio < reserva.FechaFin &&
        //                                r.FechaFin > reserva.FechaInicio)
        //                    .ToListAsync();

        //                // Obtener bloqueos de plazas específicas
        //                var bloqueosPlazasEspecificas = await dataContext.BloqueoPlazas
        //                    .Where(b => b.EstacionamientoId == reserva.EstacionamientoId &&
        //                                b.Activo &&
        //                                b.PlazaId != null &&
        //                                (b.TipoDeVehiculo == null || b.TipoDeVehiculo == vehiculo.TipoDeVehiculo) &&
        //                                b.FechaInicio < reserva.FechaFin &&
        //                                b.FechaFin > reserva.FechaInicio)
        //                    .ToListAsync();

        //                Console.WriteLine($"    📊 Plazas totales: {plazasDelTipo.Count} | Reservas: {reservasEnPeriodo.Count} | Bloqueos: {bloqueosPlazasEspecificas.Count}");

        //                // Buscar plaza disponible
        //                Plaza plazaDisponible = null;
        //                int plazasOcupadas = 0;
        //                int plazasBloqueadas = 0;

        //                foreach (var plaza in plazasDelTipo)
        //                {
        //                    // Verificar reservas
        //                    bool tieneReserva = reservasEnPeriodo.Any(r => r.PlazaId == plaza.Id);
        //                    if (tieneReserva)
        //                    {
        //                        plazasOcupadas++;
        //                        continue;
        //                    }

        //                    // Verificar bloqueos
        //                    var bloqueoPlaza = bloqueosPlazasEspecificas.FirstOrDefault(b => b.PlazaId == plaza.Id);
        //                    if (bloqueoPlaza != null)
        //                    {
        //                        plazasBloqueadas++;
        //                        continue;
        //                    }

        //                    // Plaza disponible
        //                    plazaDisponible = plaza;
        //                    break;
        //                }

        //                // Si no hay plaza disponible
        //                if (plazaDisponible == null)
        //                {
        //                    string motivoDetallado;

        //                    if (plazasBloqueadas > 0 && plazasOcupadas > 0)
        //                    {
        //                        motivoDetallado = $"Sin plazas disponibles: {plazasOcupadas} ocupadas por reservas y {plazasBloqueadas} bloqueadas por el propietario";
        //                    }
        //                    else if (plazasBloqueadas > 0)
        //                    {
        //                        motivoDetallado = $"🔒 Las {plazasBloqueadas} plaza(s) disponible(s) están bloqueadas por el propietario";
        //                    }
        //                    else if (plazasOcupadas > 0)
        //                    {
        //                        motivoDetallado = $"Las {plazasOcupadas} plaza(s) están ocupadas por otras reservas";
        //                    }
        //                    else
        //                    {
        //                        motivoDetallado = "No hay plazas disponibles en este horario";
        //                    }

        //                    reservasRechazadas.Add(new
        //                    {
        //                        fecha = reserva.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
        //                        motivo = motivoDetallado
        //                    });

        //                    Console.WriteLine($"  ❌ Rechazada: {motivoDetallado}");
        //                    continue;
        //                }

        //                // ==========================================
        //                // ✅ PLAZA DISPONIBLE - CREAR RESERVA
        //                // ==========================================
        //                reserva.PlazaId = plazaDisponible.Id;

        //                var tarifa = await dataContext.Tarifas
        //                        .Where(x => x.EstacionamientoId == reserva.EstacionamientoId && x.TipoDeVehiculo == reserva.TipoDeVehiculo)
        //                        .FirstOrDefaultAsync();
        //                // Calcular monto
        //                decimal montoTotal = CalcularMontoReserva(
        //                    tarifa,
        //                    reserva.FechaInicio,
        //                    reserva.FechaFin,
        //                    reserva.Monto
        //                );

        //                reserva.Monto = montoTotal;

        //                // Guardar reserva
        //                await dataContext.Reservas.AddAsync(reserva);
        //                await dataContext.SaveChangesAsync();

        //                reservasCreadas.Add(new
        //                {
        //                    id = reserva.Id,
        //                    plazaId = plazaDisponible.Id,
        //                    codigoValidacion = reserva.CodigoDeValidacion,
        //                    fechaInicio = reserva.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
        //                    fechaFin = reserva.FechaFin.ToString("dd/MM/yyyy HH:mm"),
        //                    monto = montoTotal
        //                });

        //                Console.WriteLine($"  ✅ CREADA: ID {reserva.Id} | Plaza {plazaDisponible.Id} | Monto ${montoTotal:F2}");
        //            }
        //            catch (Exception exReserva)
        //            {
        //                reservasRechazadas.Add(new
        //                {
        //                    fecha = reserva.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
        //                    motivo = $"Error al procesar: {exReserva.Message}"
        //                });
        //                Console.WriteLine($"  💥 Error: {exReserva.Message}");
        //            }
        //        }

        //        Console.WriteLine("\n==========================================");
        //        Console.WriteLine($"📊 RESUMEN FINAL:");
        //        Console.WriteLine($"   ✅ Reservas creadas: {reservasCreadas.Count}");
        //        Console.WriteLine($"   ❌ Reservas rechazadas: {reservasRechazadas.Count}");
        //        Console.WriteLine($"   📦 Total procesadas: {totalProcesadas}");
        //        Console.WriteLine("==========================================");

        //        // **VALIDAR SI SE CREÓ AL MENOS UNA RESERVA**
        //        if (!reservasCreadas.Any())
        //        {
        //            // Agrupar motivos de rechazo
        //            var motivosAgrupados = reservasRechazadas
        //                .GroupBy(r => ((dynamic)r).motivo)
        //                .Select(g => new
        //                {
        //                    motivo = g.Key,
        //                    cantidad = g.Count(),
        //                    fechas = g.Select(x => ((dynamic)x).fecha).ToList()
        //                })
        //                .OrderByDescending(x => x.cantidad)
        //                .ToList();

        //            var mensajesError = new List<string>();

        //            foreach (var grupo in motivosAgrupados.Take(3))
        //            {
        //                if (grupo.cantidad == 1)
        //                {
        //                    mensajesError.Add($"• {grupo.fechas.First()}: {grupo.motivo}");
        //                }
        //                else
        //                {
        //                    mensajesError.Add($"• {grupo.motivo} ({grupo.cantidad} reservas)");
        //                    mensajesError.Add($"  Fechas: {string.Join(", ", grupo.fechas.Take(3))}");
        //                    if (grupo.fechas.Count > 3)
        //                    {
        //                        mensajesError.Add($"  ... y {grupo.fechas.Count - 3} más");
        //                    }
        //                }
        //            }

        //            string mensajeCompleto = $"❌ No se pudo crear ninguna reserva de las {reservas.Count} solicitadas.\n\n" +
        //                                   $"📋 Principales motivos de rechazo:\n\n{string.Join("\n", mensajesError)}";

        //            if (motivosAgrupados.Count > 3)
        //            {
        //                mensajesError.Add($"\n... y otros {motivosAgrupados.Count - 3} motivos más.");
        //            }

        //            Console.WriteLine($"⚠️ NINGUNA RESERVA CREADA");

        //            return BadRequest(new
        //            {
        //                success = false,
        //                message = "No se pudo crear ninguna reserva",
        //                error = mensajeCompleto,
        //                totalSolicitadas = reservas.Count,
        //                reservasCreadas = 0,
        //                reservasRechazadas = reservasRechazadas.Count,
        //                motivosAgrupados = motivosAgrupados,
        //                detalleReservasRechazadas = reservasRechazadas
        //            });
        //        }

        //        // **ENVIAR NOTIFICACIÓN AL PROPIETARIO (OPCIONAL)**
        //        if (reservasCreadas.Any())
        //        {
        //            try
        //            {
        //                var estacionamiento = await dataContext.Estacionamientos
        //                    .FirstOrDefaultAsync(e => e.Id == reservas.First().EstacionamientoId);

        //                var propietario = await _userManager.FindByIdAsync(estacionamiento?.UserId);
        //                var cliente = await _userManager.FindByIdAsync(_UserId);

        //                if (propietario != null && !string.IsNullOrEmpty(propietario.Email) && cliente != null)
        //                {
        //                    Console.WriteLine($"📧 Notificando al propietario: {reservasCreadas.Count} reservas creadas");
        //                    // Aquí puedes enviar un email consolidado
        //                }
        //            }
        //            catch (Exception emailEx)
        //            {
        //                Console.WriteLine($"⚠️ Error al enviar notificación: {emailEx.Message}");
        //            }
        //        }

        //        // **GENERAR MENSAJE DE RESULTADO**
        //        string mensajeResultado;
        //        bool todasCreadas = !reservasRechazadas.Any();

        //        if (todasCreadas)
        //        {
        //            mensajeResultado = $"✅ ¡Éxito total! Las {reservasCreadas.Count} reservas fueron creadas exitosamente";
        //        }
        //        else if (reservasRechazadas.Count > reservasCreadas.Count)
        //        {
        //            mensajeResultado = $"⚠️ Proceso completado con observaciones: Se crearon {reservasCreadas.Count} reservas, pero {reservasRechazadas.Count} fueron rechazadas";
        //        }
        //        else
        //        {
        //            mensajeResultado = $"✅ Proceso exitoso: Se crearon {reservasCreadas.Count} reservas. {reservasRechazadas.Count} no pudieron procesarse";
        //        }

        //        return Ok(new
        //        {
        //            success = true,
        //            message = mensajeResultado,
        //            totalSolicitadas = reservas.Count,
        //            reservasCreadas = reservasCreadas.Count,
        //            reservasRechazadas = reservasRechazadas.Count,
        //            todasCreadas = todasCreadas,
        //            detalleReservasCreadas = reservasCreadas,
        //            detalleReservasRechazadas = reservasRechazadas,
        //            resumenMotivosRechazo = reservasRechazadas
        //                .GroupBy(r => ((dynamic)r).motivo)
        //                .Select(g => new { motivo = g.Key, cantidad = g.Count() })
        //                .OrderByDescending(x => x.cantidad)
        //                .ToList()
        //        });
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine($"💥 ERROR CRÍTICO en AddMultipleAsync: {e.Message}");
        //        Console.WriteLine($"   Stack Trace: {e.StackTrace}");
        //        return BadRequest($"ERROR.. {Tools.Tools.ExceptionMessage(e)}");
        //    }
        //}

       
        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult> AddMultipleAsync([FromBody] List<Reserva> reservas)
        {
            try
            {
                DataContext dataContext = new DataContext();

                if (reservas == null || !reservas.Any())
                    return BadRequest("ERROR.. Debe enviar al menos una reserva");

                if (reservas.Count > 100)
                    return BadRequest("ERROR.. No puede crear más de 100 reservas a la vez");

                var reservasCreadas = new List<object>();
                var reservasRechazadas = new List<object>();
                int totalProcesadas = 0;

                Console.WriteLine($"📦 Procesando {reservas.Count} reservas en modo transaccional...");
                Console.WriteLine("==========================================");

                // ==========================================
                // 🔍 PASO 1: VALIDAR TODAS LAS RESERVAS PRIMERO (SIN GUARDAR NADA)
                // ==========================================
                var reservasValidadas = new List<(Reserva reserva, Plaza plaza, decimal monto, string error)>();
                bool hayErrorCritico = false;
                string errorCriticoMensaje = "";

                foreach (var reserva in reservas)
                {
                    totalProcesadas++;
                    Console.WriteLine($"\n[{totalProcesadas}/{reservas.Count}] Validando reserva para {reserva.FechaInicio:dd/MM/yyyy HH:mm}...");

                    try
                    {
                        reserva.UserId = _UserId;

                        // **VALIDACIONES BÁSICAS**
                        if (reserva.FechaInicio >= reserva.FechaFin)
                        {
                            hayErrorCritico = true;
                            errorCriticoMensaje = $"Fecha inválida el {reserva.FechaInicio:dd/MM/yyyy}: la hora de inicio debe ser anterior a la hora de fin";
                            reservasRechazadas.Add(new
                            {
                                fecha = reserva.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
                                motivo = "Fecha de inicio debe ser anterior a fecha fin"
                            });
                            Console.WriteLine($"  ❌ ERROR: Fecha inválida");
                            break;
                        }

                        if (reserva.FechaInicio < DateTime.Now)
                        {
                            hayErrorCritico = true;
                            errorCriticoMensaje = $"Fecha inválida el {reserva.FechaInicio:dd/MM/yyyy}: no puede hacer reservas para fechas pasadas";
                            reservasRechazadas.Add(new
                            {
                                fecha = reserva.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
                                motivo = "No puede hacer reservas para fechas pasadas"
                            });
                            Console.WriteLine($"  ❌ ERROR: Fecha pasada");
                            break;
                        }

                        // **VALIDAR VEHÍCULO** (solo una vez, es el mismo para todas)
                        var vehiculo = await dataContext.Vehiculos
                            .Where(x => x.Id == reserva.VehiculoId && x.UserId == _UserId)
                            .FirstOrDefaultAsync();

                        if (vehiculo == null)
                        {
                            hayErrorCritico = true;
                            errorCriticoMensaje = "No se encontró el vehículo especificado";
                            reservasRechazadas.Add(new
                            {
                                fecha = reserva.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
                                motivo = "No se encontró el vehículo"
                            });
                            Console.WriteLine($"  ❌ ERROR: Vehículo no encontrado");
                            break;
                        }

                        reserva.TipoDeVehiculo = vehiculo.TipoDeVehiculo;
                        reserva.Patente = vehiculo.Patente;

                        // **OBTENER ESTACIONAMIENTO** (solo una vez, es el mismo para todas)
                        var estacionamiento = await dataContext.Estacionamientos
                            .Include(e => e.Jornadas)
                                .ThenInclude(j => j.Horarios)
                            .Where(x => x.Id == reserva.EstacionamientoId)
                            .FirstOrDefaultAsync();

                        if (estacionamiento == null)
                        {
                            hayErrorCritico = true;
                            errorCriticoMensaje = "No se encontró el estacionamiento";
                            reservasRechazadas.Add(new
                            {
                                fecha = reserva.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
                                motivo = "No se encontró el estacionamiento"
                            });
                            Console.WriteLine($"  ❌ ERROR: Estacionamiento no encontrado");
                            break;
                        }

                        if (estacionamiento.PublicacionPausada)
                        {
                            hayErrorCritico = true;
                            errorCriticoMensaje = "El estacionamiento no está disponible (pausado)";
                            reservasRechazadas.Add(new
                            {
                                fecha = reserva.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
                                motivo = "Estacionamiento no disponible (pausado)"
                            });
                            Console.WriteLine($"  ❌ ERROR: Estacionamiento pausado");
                            break;
                        }

                        // **VALIDAR QUE EXISTAN PLAZAS DEL TIPO**
                        var plazasDelTipo = await dataContext.Plazas
                            .Where(p => p.EstacionamientoId == reserva.EstacionamientoId &&
                                        p.TipoDeVehiculo == vehiculo.TipoDeVehiculo)
                            .ToListAsync();

                        if (!plazasDelTipo.Any())
                        {
                            hayErrorCritico = true;
                            errorCriticoMensaje = $"Este estacionamiento no tiene plazas para vehículos tipo {vehiculo.TipoDeVehiculo}";
                            reservasRechazadas.Add(new
                            {
                                fecha = reserva.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
                                motivo = $"No existen plazas para {vehiculo.TipoDeVehiculo}"
                            });
                            Console.WriteLine($"  ❌ ERROR: No hay plazas del tipo {vehiculo.TipoDeVehiculo}");
                            break;
                        }

                        // **VALIDAR JORNADAS**
                        if (!ValidarJornadasDisponibles(estacionamiento.Jornadas, reserva.FechaInicio, reserva.FechaFin))
                        {
                            var diaSemana = reserva.FechaInicio.ToString("dddd", new System.Globalization.CultureInfo("es-ES"));
                            hayErrorCritico = true;
                            errorCriticoMensaje = $"El estacionamiento está cerrado el {diaSemana} ({reserva.FechaInicio:dd/MM/yyyy}) en el horario {reserva.FechaInicio:HH:mm}-{reserva.FechaFin:HH:mm}";
                            reservasRechazadas.Add(new
                            {
                                fecha = reserva.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
                                motivo = $"Estacionamiento cerrado el {diaSemana} en horario {reserva.FechaInicio:HH:mm}-{reserva.FechaFin:HH:mm}"
                            });
                            Console.WriteLine($"  ❌ ERROR: Fuera de jornada");
                            break;
                        }

                        // **VERIFICAR BLOQUEO DE ESTACIONAMIENTO COMPLETO**
                        Console.WriteLine($"  🔍 Verificando bloqueos...");

                        var bloqueoEstacionamientoCompleto = await dataContext.BloqueoPlazas
                            .Where(b => b.EstacionamientoId == reserva.EstacionamientoId &&
                                        b.Activo &&
                                        b.PlazaId == null &&
                                        b.TipoDeVehiculo == null &&
                                        b.FechaInicio < reserva.FechaFin &&
                                        b.FechaFin > reserva.FechaInicio)
                            .FirstOrDefaultAsync();

                        if (bloqueoEstacionamientoCompleto != null)
                        {
                            hayErrorCritico = true;
                            errorCriticoMensaje = $"El estacionamiento está bloqueado el {reserva.FechaInicio:dd/MM/yyyy}. Motivo: {bloqueoEstacionamientoCompleto.Motivo} (del {bloqueoEstacionamientoCompleto.FechaInicio:dd/MM/yyyy} al {bloqueoEstacionamientoCompleto.FechaFin:dd/MM/yyyy})";
                            reservasRechazadas.Add(new
                            {
                                fecha = reserva.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
                                motivo = $"🔒 Estacionamiento bloqueado: {bloqueoEstacionamientoCompleto.Motivo}"
                            });
                            Console.WriteLine($"  🔒 ERROR: Estacionamiento bloqueado - {bloqueoEstacionamientoCompleto.Motivo}");
                            break;
                        }

                        // **VERIFICAR BLOQUEO POR TIPO DE VEHÍCULO**
                        var bloqueoTipoVehiculo = await dataContext.BloqueoPlazas
                            .Where(b => b.EstacionamientoId == reserva.EstacionamientoId &&
                                        b.Activo &&
                                        b.PlazaId == null &&
                                        b.TipoDeVehiculo == vehiculo.TipoDeVehiculo &&
                                        b.FechaInicio < reserva.FechaFin &&
                                        b.FechaFin > reserva.FechaInicio)
                            .FirstOrDefaultAsync();

                        if (bloqueoTipoVehiculo != null)
                        {
                            hayErrorCritico = true;
                            errorCriticoMensaje = $"Las plazas para {vehiculo.TipoDeVehiculo} están bloqueadas el {reserva.FechaInicio:dd/MM/yyyy}. Motivo: {bloqueoTipoVehiculo.Motivo} (del {bloqueoTipoVehiculo.FechaInicio:dd/MM/yyyy} al {bloqueoTipoVehiculo.FechaFin:dd/MM/yyyy})";
                            reservasRechazadas.Add(new
                            {
                                fecha = reserva.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
                                motivo = $"🔒 Plazas de {vehiculo.TipoDeVehiculo} bloqueadas: {bloqueoTipoVehiculo.Motivo}"
                            });
                            Console.WriteLine($"  🔒 ERROR: Tipo {vehiculo.TipoDeVehiculo} bloqueado - {bloqueoTipoVehiculo.Motivo}");
                            break;
                        }

                        Console.WriteLine($"  ✅ Sin bloqueos generales");

                        // **BUSCAR PLAZA DISPONIBLE PARA ESTA FECHA ESPECÍFICA**
                        Console.WriteLine($"  🔍 Verificando disponibilidad de plazas...");

                        // Obtener TODAS las reservas que podrían afectar (incluyendo las que estamos por crear)
                        var reservasExistentes = await dataContext.Reservas
                            .Where(r => r.EstacionamientoId == reserva.EstacionamientoId &&
                                        r.TipoDeVehiculo == vehiculo.TipoDeVehiculo &&
                                        (r.Estado == EstadoReserva.ESPERANDO_ARRIBO ||
                                         r.Estado == EstadoReserva.ARRIBO_EXITOSO) &&
                                        r.FechaInicio < reserva.FechaFin &&
                                        r.FechaFin > reserva.FechaInicio)
                            .ToListAsync();

                        // Agregar las reservas ya validadas de este lote (simulación de reservas futuras)
                        var reservasSimuladas = reservasValidadas
                            .Where(rv => rv.plaza != null)
                            .Select(rv => new
                            {
                                PlazaId = rv.plaza.Id,
                                FechaInicio = rv.reserva.FechaInicio,
                                FechaFin = rv.reserva.FechaFin
                            })
                            .ToList();

                        // Obtener bloqueos de plazas específicas
                        var bloqueosPlazasEspecificas = await dataContext.BloqueoPlazas
                            .Where(b => b.EstacionamientoId == reserva.EstacionamientoId &&
                                        b.Activo &&
                                        b.PlazaId != null &&
                                        (b.TipoDeVehiculo == null || b.TipoDeVehiculo == vehiculo.TipoDeVehiculo) &&
                                        b.FechaInicio < reserva.FechaFin &&
                                        b.FechaFin > reserva.FechaInicio)
                            .ToListAsync();

                        Console.WriteLine($"    📊 Plazas: {plazasDelTipo.Count} | Reservas existentes: {reservasExistentes.Count} | Reservas simuladas: {reservasSimuladas.Count} | Bloqueos: {bloqueosPlazasEspecificas.Count}");

                        // Buscar plaza disponible
                        Plaza plazaDisponible = null;
                        int plazasOcupadas = 0;
                        int plazasBloqueadas = 0;

                        foreach (var plaza in plazasDelTipo)
                        {
                            // Verificar reservas existentes en BD
                            bool tieneReservaExistente = reservasExistentes.Any(r => r.PlazaId == plaza.Id);

                            // 🔑 CRÍTICO: Verificar reservas simuladas de este mismo lote
                            bool tieneReservaSimulada = reservasSimuladas.Any(rs =>
                                rs.PlazaId == plaza.Id &&
                                rs.FechaInicio < reserva.FechaFin &&
                                rs.FechaFin > reserva.FechaInicio
                            );

                            if (tieneReservaExistente || tieneReservaSimulada)
                            {
                                plazasOcupadas++;
                                if (tieneReservaSimulada)
                                {
                                    Console.WriteLine($"      ⏭️ Plaza {plaza.Id} - Ya asignada a otra reserva de este lote");
                                }
                                continue;
                            }

                            // Verificar bloqueos
                            var bloqueoPlaza = bloqueosPlazasEspecificas.FirstOrDefault(b => b.PlazaId == plaza.Id);
                            if (bloqueoPlaza != null)
                            {
                                plazasBloqueadas++;
                                Console.WriteLine($"      🔒 Plaza {plaza.Id} - Bloqueada: {bloqueoPlaza.Motivo}");
                                continue;
                            }

                            // ✅ Plaza disponible encontrada
                            plazaDisponible = plaza;
                            Console.WriteLine($"      ✅ Plaza {plaza.Id} - Disponible para esta fecha");
                            break;
                        }

                        // Si NO hay plaza disponible, RECHAZAR TODO
                        if (plazaDisponible == null)
                        {
                            string motivoDetallado;

                            if (plazasBloqueadas > 0 && plazasOcupadas > 0)
                            {
                                motivoDetallado = $"No hay plazas disponibles el {reserva.FechaInicio:dd/MM/yyyy}: {plazasOcupadas} ocupadas y {plazasBloqueadas} bloqueadas";
                            }
                            else if (plazasBloqueadas > 0)
                            {
                                motivoDetallado = $"No hay plazas disponibles el {reserva.FechaInicio:dd/MM/yyyy}: las {plazasBloqueadas} plaza(s) están bloqueadas por el propietario";
                            }
                            else if (plazasOcupadas > 0)
                            {
                                motivoDetallado = $"No hay plazas disponibles el {reserva.FechaInicio:dd/MM/yyyy}: las {plazasOcupadas} plaza(s) están ocupadas por otras reservas";
                            }
                            else
                            {
                                motivoDetallado = $"No hay plazas disponibles el {reserva.FechaInicio:dd/MM/yyyy}";
                            }

                            hayErrorCritico = true;
                            errorCriticoMensaje = motivoDetallado;

                            reservasRechazadas.Add(new
                            {
                                fecha = reserva.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
                                motivo = motivoDetallado
                            });

                            Console.WriteLine($"  ❌ ERROR CRÍTICO: {motivoDetallado}");
                            Console.WriteLine($"  🚫 TODAS LAS RESERVAS SERÁN RECHAZADAS");
                            break; // SALIR DEL LOOP
                        }

                        // Calcular monto
                        var tarifa = await dataContext.Tarifas
                            .Where(x => x.EstacionamientoId == reserva.EstacionamientoId &&
                                        x.TipoDeVehiculo == reserva.TipoDeVehiculo)
                            .FirstOrDefaultAsync();

                        decimal montoTotal = CalcularMontoReserva(
                            tarifa,
                            reserva.FechaInicio,
                            reserva.FechaFin,
                            reserva.Monto
                        );

                        // Guardar en lista de validadas (NO en BD todavía)
                        reservasValidadas.Add((reserva, plazaDisponible, montoTotal, null));
                        Console.WriteLine($"  ✅ Validación exitosa - Plaza {plazaDisponible.Id} asignada temporalmente");

                    }
                    catch (Exception exReserva)
                    {
                        hayErrorCritico = true;
                        errorCriticoMensaje = $"Error al procesar reserva del {reserva.FechaInicio:dd/MM/yyyy}: {exReserva.Message}";
                        reservasRechazadas.Add(new
                        {
                            fecha = reserva.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
                            motivo = $"Error al procesar: {exReserva.Message}"
                        });
                        Console.WriteLine($"  💥 ERROR: {exReserva.Message}");
                        break;
                    }
                }

                // ==========================================
                // 🔍 DECISIÓN: ¿GUARDAR O RECHAZAR TODO?
                // ==========================================
                Console.WriteLine("\n==========================================");

                if (hayErrorCritico)
                {
                    Console.WriteLine($"🚫 TRANSACCIÓN RECHAZADA - SE ENCONTRÓ UN CONFLICTO");
                    Console.WriteLine($"   ❌ Ninguna reserva fue creada");
                    Console.WriteLine($"   📋 Motivo: {errorCriticoMensaje}");
                    Console.WriteLine($"   📊 Reservas validadas exitosamente antes del error: {reservasValidadas.Count}");
                    Console.WriteLine($"   📊 Total de reservas solicitadas: {reservas.Count}");
                    Console.WriteLine("==========================================");

                    return BadRequest(new
                    {
                        success = false,
                        message = "❌ No se pudo completar la reserva",
                        error = $"Se rechazó el conjunto completo de reservas porque:\n\n{errorCriticoMensaje}\n\n" +
                               $"ℹ️ Para reservar múltiples días de forma consecutiva, TODOS los días deben estar disponibles. " +
                               $"Si un solo día tiene conflicto, no se puede procesar ninguna de las reservas del período.\n\n" +
                               $"📊 Se validaron exitosamente {reservasValidadas.Count} de {reservas.Count} reservas antes de encontrar el conflicto.",
                        totalSolicitadas = reservas.Count,
                        reservasCreadas = 0,
                        reservasRechazadas = reservas.Count,
                        fechaConflictiva = reservasRechazadas.Any() ? ((dynamic)reservasRechazadas.Last()).fecha : null,
                        motivoConflicto = errorCriticoMensaje,
                        detalleReservasRechazadas = reservasRechazadas
                    });
                }

                // ==========================================
                // ✅ PASO 2: TODAS LAS VALIDACIONES PASARON - GUARDAR EN BD
                // ==========================================
                Console.WriteLine($"✅ TODAS LAS VALIDACIONES PASARON");
                Console.WriteLine($"💾 Guardando {reservasValidadas.Count} reservas en la base de datos...");
                Console.WriteLine("==========================================");

                foreach (var (reserva, plaza, monto, _) in reservasValidadas)
                {
                    reserva.PlazaId = plaza.Id;
                    reserva.Monto = monto;

                    await dataContext.Reservas.AddAsync(reserva);
                    await dataContext.SaveChangesAsync();

                    reservasCreadas.Add(new
                    {
                        id = reserva.Id,
                        plazaId = plaza.Id,
                        codigoValidacion = reserva.CodigoDeValidacion,
                        fechaInicio = reserva.FechaInicio.ToString("dd/MM/yyyy HH:mm"),
                        fechaFin = reserva.FechaFin.ToString("dd/MM/yyyy HH:mm"),
                        monto = monto
                    });

                    Console.WriteLine($"  ✅ Guardada: ID {reserva.Id} | Plaza {plaza.Id} | {reserva.FechaInicio:dd/MM/yyyy} | ${monto:F2}");
                }

                Console.WriteLine("\n==========================================");
                Console.WriteLine($"✅ TRANSACCIÓN COMPLETADA EXITOSAMENTE");
                Console.WriteLine($"   ✅ Reservas creadas: {reservasCreadas.Count}");
                Console.WriteLine($"   📦 Total procesadas: {reservas.Count}");
                Console.WriteLine("==========================================");

                // **VALIDAR SI SE CREÓ AL MENOS UNA RESERVA**
                if (!reservasCreadas.Any())
                {
                    // Agrupar motivos de rechazo
                    var motivosAgrupados = reservasRechazadas
                        .GroupBy(r => ((dynamic)r).motivo)
                        .Select(g => new
                        {
                            motivo = g.Key,
                            cantidad = g.Count(),
                            fechas = g.Select(x => ((dynamic)x).fecha).ToList()
                        })
                        .OrderByDescending(x => x.cantidad)
                        .ToList();

                    var mensajesError = new List<string>();

                    foreach (var grupo in motivosAgrupados.Take(3))
                    {
                        if (grupo.cantidad == 1)
                        {
                            mensajesError.Add($"• {grupo.fechas.First()}: {grupo.motivo}");
                        }
                        else
                        {
                            mensajesError.Add($"• {grupo.motivo} ({grupo.cantidad} reservas)");
                            mensajesError.Add($"  Fechas: {string.Join(", ", grupo.fechas.Take(3))}");
                            if (grupo.fechas.Count > 3)
                            {
                                mensajesError.Add($"  ... y {grupo.fechas.Count - 3} más");
                            }
                        }
                    }

                    string mensajeCompleto = $"❌ No se pudo crear ninguna reserva de las {reservas.Count} solicitadas.\n\n" +
                                           $"📋 Principales motivos de rechazo:\n\n{string.Join("\n", mensajesError)}";

                    if (motivosAgrupados.Count > 3)
                    {
                        mensajesError.Add($"\n... y otros {motivosAgrupados.Count - 3} motivos más.");
                    }

                    Console.WriteLine($"⚠️ NINGUNA RESERVA CREADA");

                    return BadRequest(new
                    {
                        success = false,
                        message = "No se pudo crear ninguna reserva",
                        error = mensajeCompleto,
                        totalSolicitadas = reservas.Count,
                        reservasCreadas = 0,
                        reservasRechazadas = reservasRechazadas.Count,
                        motivosAgrupados = motivosAgrupados,
                        detalleReservasRechazadas = reservasRechazadas
                    });
                }

                // **ENVIAR NOTIFICACIÓN AL PROPIETARIO (OPCIONAL)**
                if (reservasCreadas.Any())
                {
                    try
                    {
                        var estacionamiento = await dataContext.Estacionamientos
                            .FirstOrDefaultAsync(e => e.Id == reservas.First().EstacionamientoId);

                        var propietario = await _userManager.FindByIdAsync(estacionamiento?.UserId);
                        var cliente = await _userManager.FindByIdAsync(_UserId);

                        if (propietario != null && !string.IsNullOrEmpty(propietario.Email) && cliente != null)
                        {
                            Console.WriteLine($"📧 Notificando al propietario: {reservasCreadas.Count} reservas creadas");
                            // Aquí puedes enviar un email consolidado
                        }
                    }
                    catch (Exception emailEx)
                    {
                        Console.WriteLine($"⚠️ Error al enviar notificación: {emailEx.Message}");
                    }
                }

                // **GENERAR MENSAJE DE RESULTADO**
                string mensajeResultado;
                bool todasCreadas = !reservasRechazadas.Any();

                if (todasCreadas)
                {
                    mensajeResultado = $"✅ ¡Éxito total! Las {reservasCreadas.Count} reservas fueron creadas exitosamente";
                }
                else if (reservasRechazadas.Count > reservasCreadas.Count)
                {
                    mensajeResultado = $"⚠️ Proceso completado con observaciones: Se crearon {reservasCreadas.Count} reservas, pero {reservasRechazadas.Count} fueron rechazadas";
                }
                else
                {
                    mensajeResultado = $"✅ Proceso exitoso: Se crearon {reservasCreadas.Count} reservas. {reservasRechazadas.Count} no pudieron procesarse";
                }

                return Ok(new
                {
                    success = true,
                    message = mensajeResultado,
                    totalSolicitadas = reservas.Count,
                    reservasCreadas = reservasCreadas.Count,
                    reservasRechazadas = reservasRechazadas.Count,
                    todasCreadas = todasCreadas,
                    detalleReservasCreadas = reservasCreadas,
                    detalleReservasRechazadas = reservasRechazadas,
                    resumenMotivosRechazo = reservasRechazadas
                        .GroupBy(r => ((dynamic)r).motivo)
                        .Select(g => new { motivo = g.Key, cantidad = g.Count() })
                        .OrderByDescending(x => x.cantidad)
                        .ToList()
                });
            }
            catch (Exception e)
            {
                Console.WriteLine($"💥 ERROR CRÍTICO en AddMultipleAsync: {e.Message}");
                Console.WriteLine($"   Stack Trace: {e.StackTrace}");
                return BadRequest($"ERROR.. {Tools.Tools.ExceptionMessage(e)}");
            }
        }
        //--------------------------------------------
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
                //var datoVehiculoSobreAlojado = await dataContext.DataVehiculoAlojados
                //    .Where(x => x.EstacionamientoId == reserva.EstacionamientoId &&
                //                x.TipoDeVehiculo == vehiculo.TipoDeVehiculo)
                //    .FirstOrDefaultAsync();

                //if (datoVehiculoSobreAlojado != null && datoVehiculoSobreAlojado.CantidadActualAlojados > 0)
                //{
                //    datoVehiculoSobreAlojado.CantidadActualAlojados--;
                //    dataContext.DataVehiculoAlojados.Update(datoVehiculoSobreAlojado);
                //}

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
                //var datoVehiculoSobreAlojado = await dataContext.DataVehiculoAlojados
                //    .Where(x => x.EstacionamientoId == reserva.EstacionamientoId &&
                //                x.TipoDeVehiculo == vehiculo.TipoDeVehiculo)
                //    .FirstOrDefaultAsync();

                //if (datoVehiculoSobreAlojado != null && datoVehiculoSobreAlojado.CantidadActualAlojados > 0)
                //{
                //    datoVehiculoSobreAlojado.CantidadActualAlojados--;
                //    dataContext.DataVehiculoAlojados.Update(datoVehiculoSobreAlojado);
                //}

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
                //var datoVehiculoSobreAlojado = await dataContext.DataVehiculoAlojados
                //    .Where(x => x.EstacionamientoId == reserva.EstacionamientoId &&
                //                x.TipoDeVehiculo == vehiculo.TipoDeVehiculo)
                //    .FirstOrDefaultAsync();

                //if (datoVehiculoSobreAlojado != null && datoVehiculoSobreAlojado.CantidadActualAlojados > 0)
                //{
                //    datoVehiculoSobreAlojado.CantidadActualAlojados--;
                //    dataContext.DataVehiculoAlojados.Update(datoVehiculoSobreAlojado);
                //}

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

        //-------------------------------------------


        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult> AddAsync([FromBody] Reserva reserva)
        {
            try
            {
                DataContext dataContext = new DataContext();
                reserva.UserId = _UserId;

                // **VALIDACIONES BÁSICAS DE FECHAS**
                if (reserva.FechaInicio == default || reserva.FechaFin == default)
                    return BadRequest("ERROR.. Debe especificar la fecha de inicio y fin de la reserva");

                if (reserva.FechaInicio >= reserva.FechaFin)
                    return BadRequest("ERROR.. La fecha de inicio debe ser anterior a la fecha de fin");

                if (reserva.FechaInicio < DateTime.Now)
                    return BadRequest("ERROR.. No puede hacer reservas para fechas pasadas");

                // **VALIDAR QUE NO TENGA RESERVAS ACTIVAS CON ESTE VEHÍCULO**
                var tieneReservaActiva = await dataContext.Reservas
                    .AnyAsync(x => x.UserId == reserva.UserId &&
                                  x.VehiculoId == reserva.VehiculoId &&
                                  (x.Estado == EstadoReserva.ESPERANDO_ARRIBO ||
                                   x.Estado == EstadoReserva.ARRIBO_EXITOSO));

                if (tieneReservaActiva)
                    return BadRequest("ERROR.. Ya tiene una reserva activa con este vehículo");

                // **VALIDAR VEHÍCULO**
                var vehiculo = await dataContext.Vehiculos
                    .Where(x => x.Id == reserva.VehiculoId && x.UserId == _UserId)
                    .FirstOrDefaultAsync();

                if (vehiculo == null)
                    return BadRequest("ERROR.. No se encontró su vehículo");

                // Asignar el tipo de vehículo a la reserva
                reserva.TipoDeVehiculo = vehiculo.TipoDeVehiculo;
                reserva.Patente = vehiculo.Patente;

                // **OBTENER ESTACIONAMIENTO**
                var estacionamiento = await dataContext.Estacionamientos
                    .Include(e => e.Jornadas)
                        .ThenInclude(j => j.Horarios)
                    .Where(x => x.Id == reserva.EstacionamientoId)
                    .FirstOrDefaultAsync();

                if (estacionamiento == null)
                    return BadRequest("ERROR.. No se encontró el estacionamiento");

                if (estacionamiento.PublicacionPausada)
                    return BadRequest("ERROR.. Este estacionamiento no está disponible actualmente");

                // **VALIDAR QUE EL TIPO DE VEHÍCULO SEA ADMITIDO**
                var plazas = await dataContext.Plazas
                    .Where(x => x.EstacionamientoId == reserva.EstacionamientoId &&
                                x.TipoDeVehiculo == vehiculo.TipoDeVehiculo)
                    .FirstOrDefaultAsync();

                if (plazas == null)
                    return BadRequest($"ERROR.. El tipo de su vehículo ({vehiculo.TipoDeVehiculo}) no es admitido en este estacionamiento");

                // **VALIDAR JORNADAS (que el estacionamiento esté abierto)**
                if (!ValidarJornadasDisponibles(estacionamiento.Jornadas, reserva.FechaInicio, reserva.FechaFin))
                    return BadRequest("ERROR.. El estacionamiento no está disponible en el horario solicitado. Revise los días y horarios de operación.");

                // ==========================================
                // 🔒 PASO 1: VERIFICAR BLOQUEOS PRIMERO
                // ==========================================
                Console.WriteLine($"🔍 Verificando bloqueos para estacionamiento {reserva.EstacionamientoId}...");

                // **1.1: VERIFICAR BLOQUEO DE ESTACIONAMIENTO COMPLETO**
                var bloqueoEstacionamientoCompleto = await dataContext.BloqueoPlazas
                    .Where(b => b.EstacionamientoId == reserva.EstacionamientoId &&
                                b.Activo &&
                                b.PlazaId == null && // Bloqueo de TODO el estacionamiento
                                b.TipoDeVehiculo == null && // Todos los tipos de vehículo
                                b.FechaInicio < reserva.FechaFin &&
                                b.FechaFin > reserva.FechaInicio)
                    .FirstOrDefaultAsync();

                if (bloqueoEstacionamientoCompleto != null)
                {
                    Console.WriteLine($"🔒 RECHAZADA: Estacionamiento completo bloqueado - {bloqueoEstacionamientoCompleto.Motivo}");

                    string mensaje = $"ERROR.. El estacionamiento está completamente bloqueado en las fechas solicitadas.\n\n" +
                                   $"📅 Período bloqueado: {bloqueoEstacionamientoCompleto.FechaInicio:dd/MM/yyyy HH:mm} - {bloqueoEstacionamientoCompleto.FechaFin:dd/MM/yyyy HH:mm}\n" +
                                   $"🔒 Motivo: {bloqueoEstacionamientoCompleto.Motivo}";

                    if (!string.IsNullOrEmpty(bloqueoEstacionamientoCompleto.Observaciones))
                    {
                        mensaje += $"\n📝 Observaciones: {bloqueoEstacionamientoCompleto.Observaciones}";
                    }

                    return BadRequest(mensaje);
                }

                // **1.2: VERIFICAR BLOQUEO POR TIPO DE VEHÍCULO**
                var bloqueoTipoVehiculo = await dataContext.BloqueoPlazas
                    .Where(b => b.EstacionamientoId == reserva.EstacionamientoId &&
                                b.Activo &&
                                b.PlazaId == null && // No es una plaza específica
                                b.TipoDeVehiculo == vehiculo.TipoDeVehiculo && // Tipo específico bloqueado
                                b.FechaInicio < reserva.FechaFin &&
                                b.FechaFin > reserva.FechaInicio)
                    .FirstOrDefaultAsync();

                if (bloqueoTipoVehiculo != null)
                {
                    Console.WriteLine($"🔒 RECHAZADA: Tipo de vehículo bloqueado - {vehiculo.TipoDeVehiculo} - {bloqueoTipoVehiculo.Motivo}");

                    string mensaje = $"ERROR.. Las plazas para vehículos tipo {vehiculo.TipoDeVehiculo} están bloqueadas en las fechas solicitadas.\n\n" +
                                   $"📅 Período bloqueado: {bloqueoTipoVehiculo.FechaInicio:dd/MM/yyyy HH:mm} - {bloqueoTipoVehiculo.FechaFin:dd/MM/yyyy HH:mm}\n" +
                                   $"🔒 Motivo: {bloqueoTipoVehiculo.Motivo}";

                    if (!string.IsNullOrEmpty(bloqueoTipoVehiculo.Observaciones))
                    {
                        mensaje += $"\n📝 Observaciones: {bloqueoTipoVehiculo.Observaciones}";
                    }

                    return BadRequest(mensaje);
                }

                Console.WriteLine("✅ No hay bloqueos generales que impidan la reserva");

                // ==========================================
                // 🔎 PASO 2: BUSCAR PLAZA DISPONIBLE
                // ==========================================
                Console.WriteLine($"🔍 Buscando plaza disponible para {vehiculo.TipoDeVehiculo}...");

                // Obtener todas las plazas del tipo solicitado
                var plazasDelTipo = await dataContext.Plazas
                    .Where(p => p.EstacionamientoId == reserva.EstacionamientoId &&
                                p.TipoDeVehiculo == vehiculo.TipoDeVehiculo)
                    .ToListAsync();

                if (!plazasDelTipo.Any())
                {
                    Console.WriteLine($"❌ RECHAZADA: No existen plazas para {vehiculo.TipoDeVehiculo}");
                    return BadRequest($"ERROR.. Este estacionamiento no tiene plazas configuradas para vehículos tipo {vehiculo.TipoDeVehiculo}");
                }

                Console.WriteLine($"📊 Total de plazas de {vehiculo.TipoDeVehiculo}: {plazasDelTipo.Count}");

                // Obtener reservas activas que se solapan
                var reservasEnPeriodo = await dataContext.Reservas
                    .Where(r => r.EstacionamientoId == reserva.EstacionamientoId &&
                                r.TipoDeVehiculo == vehiculo.TipoDeVehiculo &&
                                (r.Estado == EstadoReserva.ESPERANDO_ARRIBO ||
                                 r.Estado == EstadoReserva.ARRIBO_EXITOSO) &&
                                r.FechaInicio < reserva.FechaFin &&
                                r.FechaFin > reserva.FechaInicio)
                    .ToListAsync();

                Console.WriteLine($"📊 Reservas activas en el período: {reservasEnPeriodo.Count}");

                // Obtener bloqueos de plazas específicas que se solapan
                var bloqueosPlazasEspecificas = await dataContext.BloqueoPlazas
                    .Where(b => b.EstacionamientoId == reserva.EstacionamientoId &&
                                b.Activo &&
                                b.PlazaId != null && // Solo bloqueos de plazas específicas
                                (b.TipoDeVehiculo == null || b.TipoDeVehiculo == vehiculo.TipoDeVehiculo) &&
                                b.FechaInicio < reserva.FechaFin &&
                                b.FechaFin > reserva.FechaInicio)
                    .ToListAsync();

                Console.WriteLine($"📊 Bloqueos de plazas específicas: {bloqueosPlazasEspecificas.Count}");

                // Buscar la primera plaza disponible
                Plaza plazaDisponible = null;
                int plazasOcupadas = 0;
                int plazasBloqueadas = 0;

                foreach (var plaza in plazasDelTipo)
                {
                    // Verificar si la plaza tiene reservas que se solapen
                    bool tieneReserva = reservasEnPeriodo.Any(r => r.PlazaId == plaza.Id);

                    if (tieneReserva)
                    {
                        plazasOcupadas++;
                        Console.WriteLine($"  ⏭️ Plaza {plaza.Id} - Ocupada por reserva activa");
                        continue;
                    }

                    // Verificar si la plaza específica está bloqueada
                    var bloqueoPlaza = bloqueosPlazasEspecificas.FirstOrDefault(b => b.PlazaId == plaza.Id);

                    if (bloqueoPlaza != null)
                    {
                        plazasBloqueadas++;
                        Console.WriteLine($"  🔒 Plaza {plaza.Id} - Bloqueada: {bloqueoPlaza.Motivo} ({bloqueoPlaza.FechaInicio:dd/MM/yyyy} - {bloqueoPlaza.FechaFin:dd/MM/yyyy})");
                        continue;
                    }

                    // ¡Plaza disponible encontrada!
                    plazaDisponible = plaza;
                    Console.WriteLine($"  ✅ Plaza {plaza.Id} - DISPONIBLE");
                    break;
                }

                // Si no se encontró plaza disponible, generar mensaje detallado
                if (plazaDisponible == null)
                {
                    Console.WriteLine($"❌ RECHAZADA: No hay plazas disponibles");
                    Console.WriteLine($"   - Total plazas: {plazasDelTipo.Count}");
                    Console.WriteLine($"   - Plazas ocupadas: {plazasOcupadas}");
                    Console.WriteLine($"   - Plazas bloqueadas: {plazasBloqueadas}");

                    string mensaje = $"ERROR.. No hay plazas disponibles para {vehiculo.TipoDeVehiculo} en las fechas seleccionadas.\n\n";

                    if (plazasBloqueadas > 0 && plazasOcupadas > 0)
                    {
                        mensaje += $"📊 Estado de las plazas:\n" +
                                  $"• Total de plazas: {plazasDelTipo.Count}\n" +
                                  $"• Plazas ocupadas por otras reservas: {plazasOcupadas}\n" +
                                  $"• Plazas bloqueadas por el propietario: {plazasBloqueadas}\n\n" +
                                  $"💡 Todas las plazas están ocupadas o bloqueadas en el horario solicitado.";
                    }
                    else if (plazasBloqueadas > 0)
                    {
                        mensaje += $"🔒 Las {plazasBloqueadas} plaza(s) disponible(s) están bloqueadas por el propietario del estacionamiento.\n\n";

                        // Mostrar motivos de los bloqueos
                        var motivosUnicos = bloqueosPlazasEspecificas
                            .Select(b => b.Motivo)
                            .Distinct()
                            .ToList();

                        if (motivosUnicos.Any())
                        {
                            mensaje += $"Motivo(s) del bloqueo:\n";
                            foreach (var motivo in motivosUnicos)
                            {
                                mensaje += $"• {motivo}\n";
                            }
                        }
                    }
                    else if (plazasOcupadas > 0)
                    {
                        mensaje += $"📅 Las {plazasOcupadas} plaza(s) disponible(s) están ocupadas por otras reservas en el horario solicitado.\n\n" +
                                  $"💡 Intente con otro horario o fecha.";
                    }
                    else
                    {
                        mensaje += $"📊 Las {plazasDelTipo.Count} plaza(s) no están disponibles en el horario solicitado.";
                    }

                    return BadRequest(mensaje);
                }

                // ==========================================
                // ✅ PLAZA DISPONIBLE ENCONTRADA
                // ==========================================
                Console.WriteLine($"✅ Plaza disponible encontrada: {plazaDisponible.Id}");

                // **ASIGNAR LA PLAZA A LA RESERVA**
                reserva.PlazaId = plazaDisponible.Id;

                var tarifa = await dataContext.Tarifas
                        .Where(x => x.EstacionamientoId == reserva.EstacionamientoId && x.TipoDeVehiculo == reserva.TipoDeVehiculo)
                        .FirstOrDefaultAsync();

                // Calcular monto
                decimal montoTotal = CalcularMontoReserva(
                    tarifa,
                    reserva.FechaInicio,
                    reserva.FechaFin,
                    reserva.Monto
                );

                reserva.Monto = montoTotal;

                // **OBTENER DATOS PARA NOTIFICACIONES**
                var propietario = await _userManager.FindByIdAsync(estacionamiento.UserId);
                var cliente = await _userManager.FindByIdAsync(reserva.UserId);

                if (cliente == null)
                    return BadRequest("ERROR.. No se encontraron los datos del cliente");

                // **GUARDAR LA RESERVA**
                await dataContext.Reservas.AddAsync(reserva);
                await dataContext.SaveChangesAsync();

                Console.WriteLine($"✅ RESERVA CREADA EXITOSAMENTE:");
                Console.WriteLine($"   - ID Reserva: {reserva.Id}");
                Console.WriteLine($"   - Plaza: {plazaDisponible.Id}");
                Console.WriteLine($"   - Vehículo: {vehiculo.TipoDeVehiculo} - {vehiculo.Patente}");
                Console.WriteLine($"   - Período: {reserva.FechaInicio:dd/MM/yyyy HH:mm} - {reserva.FechaFin:dd/MM/yyyy HH:mm}");
                Console.WriteLine($"   - Monto: ${montoTotal:F2}");

                // **ENVIAR NOTIFICACIÓN POR EMAIL**
                if (propietario != null && !string.IsNullOrEmpty(propietario.Email))
                {
                    try
                    {
                        var notificacion = new ReservationNotificationDTO
                        {
                            NombreCliente = cliente.Nombre ?? "Cliente",
                            ApellidoCliente = cliente.Apellido ?? "",
                            TipoDeVehiculoCliente = vehiculo.TipoDeVehiculo,
                            PatenteCliente = vehiculo.Patente,
                            NombreDelEstacionamiento = estacionamiento.Nombre,
                            DireccionDelEstacionamiento = estacionamiento.Direccion ?? "No especificada",
                            TipoDeLugarDelEstacionamiento = estacionamiento.TipoDeLugar,
                            MontoDeLaReserva = montoTotal,
                            EmailPropietario = propietario.Email,
                            FechaHoraReserva = reserva.FechaDeCreacion,
                            FechaHoraExpiracion = reserva.FechaDeExpiracion,
                            FechaInicio = reserva.FechaInicio,
                            FechaFin = reserva.FechaFin,
                            NumeroReserva = reserva.Id.ToString(),
                            NumeroPlaza = plazaDisponible.Id.ToString(),
                            CodigoDeValidacion = reserva.CodigoDeValidacion
                        };

                        bool emailEnviado = await SendReservationEmail(notificacion);
                        if (emailEnviado)
                        {
                            Console.WriteLine($"📧 Notificación enviada al propietario: {propietario.Email}");
                        }
                    }
                    catch (Exception emailEx)
                    {
                        Console.WriteLine($"⚠️ Error al enviar email: {emailEx.Message}");
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "Reserva creada exitosamente",
                    reserva = new
                    {
                        id = reserva.Id,
                        plazaId = plazaDisponible.Id,
                        codigoValidacion = reserva.CodigoDeValidacion,
                        fechaInicio = reserva.FechaInicio,
                        fechaFin = reserva.FechaFin,
                        monto = montoTotal,
                        estado = reserva.Estado.ToString(),
                        fechaExpiracion = reserva.FechaDeExpiracion
                    },
                    emailEnviado = propietario != null && !string.IsNullOrEmpty(propietario.Email)
                });
            }
            catch (Exception e)
            {
                Console.WriteLine($"💥 ERROR CRÍTICO en AddAsync: {e.Message}");
                Console.WriteLine($"   Stack Trace: {e.StackTrace}");
                return BadRequest($"ERROR.. {Tools.Tools.ExceptionMessage(e)}");
            }
        }

        /// <summary>
        /// Verifica si el estacionamiento COMPLETO está bloqueado (PlazaId = NULL)
        /// </summary>
        private async Task<BloqueoPlaza> VerificarBloqueoEstacionamiento(
            DataContext dataContext,
            int estacionamientoId,
            DateTime fechaInicio,
            DateTime fechaFin)
        {
            var bloqueo = await dataContext.BloqueoPlazas
                .Where(b => b.EstacionamientoId == estacionamientoId &&
                            b.Activo &&
                            b.PlazaId == null && // Bloqueo de todo el estacionamiento
                            b.TipoDeVehiculo == null && // Todos los tipos de vehículo
                            b.FechaInicio < fechaFin &&
                            b.FechaFin > fechaInicio)
                .FirstOrDefaultAsync();

            if (bloqueo != null)
            {
                Console.WriteLine($"⚠️ Estacionamiento bloqueado - Motivo: {bloqueo.Motivo} - Desde: {bloqueo.FechaInicio:dd/MM/yyyy} Hasta: {bloqueo.FechaFin:dd/MM/yyyy}");
            }

            return bloqueo;
        }

        /// <summary>
        /// Verifica si hay bloqueo para un tipo específico de vehículo
        /// </summary>
        private async Task<BloqueoPlaza> VerificarBloqueoTipoVehiculo(
            DataContext dataContext,
            int estacionamientoId,
            TipoDeVehiculo tipoDeVehiculo,
            DateTime fechaInicio,
            DateTime fechaFin)
        {
            var bloqueo = await dataContext.BloqueoPlazas
                .Where(b => b.EstacionamientoId == estacionamientoId &&
                            b.Activo &&
                            b.PlazaId == null && // No es una plaza específica
                            b.TipoDeVehiculo == tipoDeVehiculo && // Tipo específico bloqueado
                            b.FechaInicio < fechaFin &&
                            b.FechaFin > fechaInicio)
                .FirstOrDefaultAsync();

            if (bloqueo != null)
            {
                Console.WriteLine($"⚠️ Tipo de vehículo bloqueado: {tipoDeVehiculo} - Motivo: {bloqueo.Motivo}");
            }

            return bloqueo;
        }

        /// <summary>
        /// Busca una plaza disponible considerando reservas Y bloqueos
        /// </summary>
        private async Task<Plaza> BuscarPlazaDisponibleSinSolapamientoYBloqueos(
            DataContext dataContext,
            int estacionamientoId,
            TipoDeVehiculo tipoDeVehiculo,
            DateTime fechaInicio,
            DateTime fechaFin)
        {
            // 1. Obtener todas las plazas del tipo solicitado
            var plazasDelTipo = await dataContext.Plazas
                .Where(p => p.EstacionamientoId == estacionamientoId &&
                            p.TipoDeVehiculo == tipoDeVehiculo)
                .ToListAsync();

            if (!plazasDelTipo.Any())
            {
                Console.WriteLine($"❌ No hay plazas del tipo {tipoDeVehiculo} en este estacionamiento");
                return null;
            }

            // 2. Obtener reservas que se solapan
            var reservasEnPeriodo = await dataContext.Reservas
                .Where(r => r.EstacionamientoId == estacionamientoId &&
                            r.TipoDeVehiculo == tipoDeVehiculo &&
                            (r.Estado == EstadoReserva.ESPERANDO_ARRIBO ||
                             r.Estado == EstadoReserva.ARRIBO_EXITOSO) &&
                            r.FechaInicio < fechaFin &&
                            r.FechaFin > fechaInicio)
                .ToListAsync();

            // 3. 🔒 NUEVO: Obtener bloqueos de plazas específicas que se solapan
            var bloqueosEnPeriodo = await dataContext.BloqueoPlazas
                .Where(b => b.EstacionamientoId == estacionamientoId &&
                            b.Activo &&
                            b.PlazaId != null && // Solo bloqueos de plazas específicas
                            (b.TipoDeVehiculo == null || b.TipoDeVehiculo == tipoDeVehiculo) &&
                            b.FechaInicio < fechaFin &&
                            b.FechaFin > fechaInicio)
                .ToListAsync();

            Console.WriteLine($"📊 Buscando plaza: {plazasDelTipo.Count} plazas disponibles | {reservasEnPeriodo.Count} reservas activas | {bloqueosEnPeriodo.Count} bloqueos activos");

            // 4. Buscar la primera plaza que NO esté ocupada NI bloqueada
            foreach (var plaza in plazasDelTipo)
            {
                // Verificar si la plaza tiene reservas que se solapen
                bool tieneReserva = reservasEnPeriodo.Any(r => r.PlazaId == plaza.Id);

                if (tieneReserva)
                {
                    Console.WriteLine($"  ⏭️ Plaza {plaza.Id} - Ocupada por reserva");
                    continue;
                }

                // 🔒 NUEVO: Verificar si la plaza está bloqueada
                bool estaBloqueada = bloqueosEnPeriodo.Any(b => b.PlazaId == plaza.Id);

                if (estaBloqueada)
                {
                    var bloqueo = bloqueosEnPeriodo.First(b => b.PlazaId == plaza.Id);
                    Console.WriteLine($"  🔒 Plaza {plaza.Id} - Bloqueada: {bloqueo.Motivo} ({bloqueo.FechaInicio:dd/MM/yyyy} - {bloqueo.FechaFin:dd/MM/yyyy})");
                    continue;
                }

                // Plaza disponible
                Console.WriteLine($"  ✅ Plaza {plaza.Id} - Disponible");
                return plaza;
            }

            Console.WriteLine($"❌ No se encontró ninguna plaza disponible para {tipoDeVehiculo}");
            return null;
        }
    }


}
