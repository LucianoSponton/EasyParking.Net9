using EasyParkingAPI.Data;
using EasyParkingAPI.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using ServiceWebApi.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class ReservaExpirationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservaExpirationService> _logger;

    private readonly string _From_SmtpServer;
    private readonly int _From_SmtpServerPort;
    private readonly string _From_Name;
    private readonly string _From_EmailAdress;
    private readonly string _From_EmailPassword;

    public ReservaExpirationService(IServiceScopeFactory scopeFactory, ILogger<ReservaExpirationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        _From_SmtpServer = "smtp.gmail.com";
        _From_SmtpServerPort = 587;
        _From_Name = "EasyParking";
        _From_EmailAdress = "easyparking.ep@gmail.com";
        _From_EmailPassword = "qhqzywvsiypjghwz";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("✅ ReservaExpirationService iniciado. Verificando reservas cada 5 minutos...");

        // Esperar 120 segundos antes de la primera ejecución para dar tiempo al startup
        await Task.Delay(TimeSpan.FromSeconds(120), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcesarReservasExpiradas();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error crítico procesando expiración de reservas.");
            }

            // Esperar 5 minutos antes de la próxima verificación
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }

        _logger.LogInformation("⚠️ ReservaExpirationService detenido.");
    }

    /// <summary>
    /// Procesa todas las reservas que han excedido su tiempo de espera
    /// </summary>
    private async Task ProcesarReservasExpiradas()
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
            var _userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var ahora = DateTime.Now;

            try
            {
                var reservas = await dataContext.Reservas
                    .Where(x => x.Estado == EstadoReserva.ESPERANDO_ARRIBO)
                    .ToListAsync();

                Console.WriteLine($"📋 Procesando {reservas.Count} reservas en estado ESPERANDO_ARRIBO");

                int procesadasExitosamente = 0;
                int conErrores = 0;

                foreach (var r in reservas)
                {
                    try
                    {
                        if (ahora <= r.FechaDeExpiracion)
                        {
                            // Esta reserva aún no ha expirado, continuar con la siguiente
                            continue;
                        }

                        Console.WriteLine($"🔄 Procesando reserva ID: {r.Id}");

                        // Obtener el vehículo
                        var vehiculo = await dataContext.Vehiculos
                            .Where(x => x.Id == r.VehiculoId)
                            .FirstOrDefaultAsync();

                        if (vehiculo == null)
                        {
                            Console.WriteLine($"⚠️ Error en reserva {r.Id}: No se encontró el vehículo asociado");
                            conErrores++;
                            continue; // Continuar con la siguiente reserva
                        }

                        // Obtener datos del estacionamiento
                        var estacionamiento = await dataContext.Estacionamientos
                            .Where(x => x.Id == r.EstacionamientoId)
                            .FirstOrDefaultAsync();

                        if (estacionamiento == null)
                        {
                            Console.WriteLine($"⚠️ Error en reserva {r.Id}: No se encontró el estacionamiento");
                            conErrores++;
                            continue; // Continuar con la siguiente reserva
                        }

                        // Obtener datos del cliente
                        var cliente = await _userManager.FindByIdAsync(r.UserId);

                        if (cliente == null || string.IsNullOrEmpty(cliente.Email))
                        {
                            Console.WriteLine($"⚠️ Advertencia en reserva {r.Id}: No se pudo obtener el email del cliente");
                        }

                        // Obtener datos del dueño
                        var dueño = await _userManager.FindByIdAsync(estacionamiento.UserId);

                        if (dueño == null || string.IsNullOrEmpty(dueño.Email))
                        {
                            Console.WriteLine($"⚠️ Advertencia en reserva {r.Id}: No se pudo obtener el email del dueño");
                        }

                        // Actualizar estado de la reserva
                        r.Estado = EstadoReserva.CANCELADO_POR_EL_DUEÑO;

                        // Guardar cambios
                        dataContext.Reservas.Update(r);
                        await dataContext.SaveChangesAsync();

                        Console.WriteLine($"✅ Reserva {r.Id} actualizada a estado CANCELADO");

                        // Enviar notificación al cliente
                        if (cliente != null && !string.IsNullOrEmpty(cliente.Email))
                        {
                            try
                            {
                                var notificacionCliente = new CancellationNotificationDTO
                                {
                                    // Datos del cliente
                                    Nombre = cliente.Nombre ?? "Cliente",
                                    Apellido = cliente.Apellido ?? "",
                                    Email = cliente.Email,

                                    // Datos de la reserva
                                    NumeroReserva = r.Id.ToString(),
                                    FechaHoraReserva = r.FechaDeCreacion,
                                    FechaHoraExpiracion = r.FechaDeExpiracion,
                                    MontoReserva = r.Monto,

                                    // Datos del estacionamiento
                                    NombreDelEstacionamiento = estacionamiento.Nombre,
                                    DireccionDelEstacionamiento = estacionamiento.Direccion ?? "No especificada",
                                    TipoDeLugarDelEstacionamiento = estacionamiento?.TipoDeLugar ?? "No especificado",

                                    // Datos del vehículo
                                    TipoDeVehiculo = vehiculo.TipoDeVehiculo,
                                    Patente = vehiculo.Patente,

                                    // Información de cancelación
                                    FechaHoraCancelacion = DateTime.Now,
                                    MotivoCancelacion = "La reserva ha sido cancelada porque el tiempo de espera de arribo al lugar ha expirado, has demorado mucho en llegar"
                                };

                                bool emailEnviado = await SendCancellationEmail(notificacionCliente);

                                if (!emailEnviado)
                                {
                                    Console.WriteLine($"⚠️ Reserva {r.Id} cancelada pero no se pudo enviar email al cliente");
                                }
                                else
                                {
                                    Console.WriteLine($"✅ Notificación de cancelación enviada al cliente: {cliente.Email}");
                                }
                            }
                            catch (Exception emailEx)
                            {
                                Console.WriteLine($"❌ Error al enviar notificación al cliente (Reserva {r.Id}): {emailEx.Message}");
                                // No interrumpimos el proceso, continuamos
                            }
                        }

                        // Enviar notificación al dueño
                        if (dueño != null && !string.IsNullOrEmpty(dueño.Email))
                        {
                            try
                            {
                                var notificacionDueño = new CancellationNotificationDTO
                                {
                                    // Datos del Dueño
                                    Nombre = dueño.Nombre ?? "Dueño",
                                    Apellido = dueño.Apellido ?? "",
                                    Email = dueño.Email,

                                    // Datos de la reserva
                                    NumeroReserva = r.Id.ToString(),
                                    FechaHoraReserva = r.FechaDeCreacion,
                                    FechaHoraExpiracion = r.FechaDeExpiracion,
                                    MontoReserva = r.Monto,

                                    // Datos del estacionamiento
                                    NombreDelEstacionamiento = estacionamiento.Nombre,
                                    DireccionDelEstacionamiento = estacionamiento.Direccion ?? "No especificada",
                                    TipoDeLugarDelEstacionamiento = estacionamiento?.TipoDeLugar ?? "No especificado",

                                    // Datos del vehículo
                                    TipoDeVehiculo = vehiculo.TipoDeVehiculo,
                                    Patente = vehiculo.Patente,

                                    // Información de cancelación
                                    FechaHoraCancelacion = DateTime.Now,
                                    MotivoCancelacion = "La reserva ha sido cancelada porque el tiempo de espera de arribo al lugar ha expirado, el cliente ha demorado mucho en llegar"
                                };

                                bool emailEnviado = await SendCancellationEmail(notificacionDueño);

                                if (!emailEnviado)
                                {
                                    Console.WriteLine($"⚠️ Reserva {r.Id} cancelada pero no se pudo enviar email al dueño");
                                }
                                else
                                {
                                    Console.WriteLine($"✅ Notificación de cancelación enviada al dueño: {dueño.Email}");
                                }
                            }
                            catch (Exception emailEx)
                            {
                                Console.WriteLine($"❌ Error al enviar notificación al dueño (Reserva {r.Id}): {emailEx.Message}");
                                // No interrumpimos el proceso, continuamos
                            }
                        }

                        procesadasExitosamente++;
                        Console.WriteLine($"✅ Reserva {r.Id} procesada exitosamente");
                    }
                    catch (Exception ex)
                    {
                        // Capturamos cualquier error en el procesamiento de esta reserva específica
                        conErrores++;
                        Console.WriteLine($"❌ Error al procesar reserva {r.Id}: {ex.Message}");
                        Console.WriteLine($"   Stack Trace: {ex.StackTrace}");

                        // Continuamos con la siguiente reserva sin interrumpir el foreach
                        continue;
                    }
                }

                // Resumen final del procesamiento
                Console.WriteLine($"\n📊 Resumen del procesamiento:");
                Console.WriteLine($"   Total de reservas analizadas: {reservas.Count}");
                Console.WriteLine($"   ✅ Procesadas exitosamente: {procesadasExitosamente}");
                Console.WriteLine($"   ❌ Con errores: {conErrores}");
            }
            catch (Exception e)
            {
                // Este catch solo captura errores al obtener la lista inicial de reservas
                Console.WriteLine($"❌ Error crítico al obtener las reservas: {e.Message}");
                Console.WriteLine($"   Stack Trace: {e.StackTrace}");
            }
        }
    }

    /// <summary>
    /// Envía el correo de notificación de cancelación por expiración
    /// </summary>
    private async Task<bool> SendCancellationEmail(CancellationNotificationDTO notificacion)
    {
        try
        {
            using (var smtpClient = new System.Net.Mail.SmtpClient(_From_SmtpServer))
            {
                smtpClient.Port = _From_SmtpServerPort;
                smtpClient.EnableSsl = true;
                smtpClient.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new System.Net.NetworkCredential(_From_EmailAdress, _From_EmailPassword);
                smtpClient.Timeout = 20000;

                var mailMessage = new System.Net.Mail.MailMessage
                {
                    From = new System.Net.Mail.MailAddress(_From_EmailAdress, _From_Name),
                    Subject = $"⏰ Reserva #{notificacion.NumeroReserva} Cancelada por Tiempo Excedido - {notificacion.NombreDelEstacionamiento}",
                    Body = GenerarHtmlCorreoCancelacionPorExpiracion(notificacion),
                    IsBodyHtml = true,
                    Priority = System.Net.Mail.MailPriority.High
                };

                mailMessage.To.Add(notificacion.Email);

                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error al enviar email de cancelación por expiración.");
            return false;
        }
    }

    /// <summary>
    /// Genera el HTML del correo de cancelación por expiración
    /// </summary>
    private string GenerarHtmlCorreoCancelacionPorExpiracion(CancellationNotificationDTO cancellation)
    {
        string fechaReserva = cancellation.FechaHoraReserva != default(DateTime)
            ? cancellation.FechaHoraReserva.ToString("dd/MM/yyyy HH:mm")
            : "No disponible";

        string fechaExpiracion = cancellation.FechaHoraExpiracion != default(DateTime)
            ? cancellation.FechaHoraExpiracion.ToString("dd/MM/yyyy HH:mm")
            : "No disponible";

        string fechaCancelacion = cancellation.FechaHoraCancelacion.ToString("dd/MM/yyyy HH:mm");

        // Calcular minutos de exceso
        TimeSpan tiempoExcedido = cancellation.FechaHoraCancelacion - cancellation.FechaHoraExpiracion;
        int minutosExcedidos = (int)tiempoExcedido.TotalMinutes;

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
            background: linear-gradient(135deg, #ff6f00 0%, #e65100 100%);
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
            background-color: #ff6f00;
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
            border-left: 4px solid #ff6f00;
            padding: 18px 20px;
            margin-bottom: 25px;
            border-radius: 4px;
            box-shadow: 0 2px 8px rgba(0,0,0,0.05);
        }}
        .cancellation-box strong {{
            color: #e65100;
            font-size: 18px;
        }}
        .time-exceeded-box {{
            background: linear-gradient(135deg, #ffebee 0%, #ffcdd2 100%);
            border: 2px solid #f44336;
            padding: 20px;
            margin: 20px 0;
            border-radius: 8px;
            text-align: center;
        }}
        .time-exceeded-box .icon {{
            font-size: 48px;
            margin-bottom: 10px;
        }}
        .time-exceeded-box .time {{
            font-size: 32px;
            font-weight: bold;
            color: #c62828;
            margin: 10px 0;
        }}
        .section {{
            margin-bottom: 30px;
        }}
        .section-title {{
            font-size: 20px;
            font-weight: bold;
            color: #ff6f00;
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
            background-color: #ff6f00;
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
        .warning-box {{
            background-color: #fff3e0;
            border-left: 4px solid #ff9800;
            padding: 20px;
            margin: 20px 0;
            border-radius: 4px;
        }}
        .warning-box strong {{
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
            <p>Notificación de Cancelación Automática</p>
        </div>

        <!-- Alert Banner -->
        <div class='alert-banner'>
            ⏰ RESERVA CANCELADA - Tiempo de espera excedido
        </div>

        <!-- Content -->
        <div class='content'>
            <!-- Saludo -->
            <h2>Estimado/a {cancellation.Nombre} {cancellation.Apellido},</h2>
            <p>Lamentamos informarte que tu reserva ha sido <strong>cancelada automáticamente</strong> porque el tiempo de espera ha sido excedido.</p>

            <!-- Tiempo Excedido -->
            <div class='time-exceeded-box'>
                <div class='icon'>⏰</div>
                <div style='font-size: 18px; font-weight: 600; color: #555;'>Tiempo Excedido</div>
                <div class='time'>{minutosExcedidos} minutos</div>
                <p style='margin: 10px 0 0 0; font-size: 14px; color: #666;'>
                    La reserva debía ser confirmada antes de las <strong>{fechaExpiracion}</strong>
                </p>
            </div>

            <!-- Información de Cancelación -->
            <div class='cancellation-box'>
                <strong>Número de Reserva: #{cancellation.NumeroReserva}</strong><br>
                <span style='color: #666; font-size: 14px;'>❌ Cancelada automáticamente el: {fechaCancelacion}</span>
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
                        <div class='info-label'>Fecha de Reserva:</div>
                        <div class='info-value'>{fechaReserva}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Fecha Límite:</div>
                        <div class='info-value'><strong style='color: #f44336;'>{fechaExpiracion}</strong></div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Estado:</div>
                        <div class='info-value'><span style='color: #f44336; font-weight: bold;'>CANCELADA POR EXPIRACIÓN</span></div>
                    </div>
                </div>
            </div>

            <!-- Datos del Vehículo -->
            <div class='section'>
                <div class='section-title'>
                    🚗 Datos del Vehículo
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
                <div class='label'>💰 Reembolso Automático</div>
                <div class='amount'>${cancellation.MontoReserva:N2}</div>
                <p style='margin: 10px 0 0 0; font-size: 14px;'>
                    El monto será devuelto automáticamente a tu método de pago en 5-7 días hábiles
                </p>
            </div>

            <!-- Explicación -->
            <div class='warning-box'>
                <strong>ℹ️ ¿Por qué se canceló mi reserva?</strong>
                <p style='margin: 10px 0 0 0;'>
                    {cancellation.MotivoCancelacion}
                </p>
                <p style='margin: 10px 0 0 0;'>
                    Este proceso es automático y tiene como objetivo mantener la disponibilidad de espacios 
                    para otros usuarios cuando una reserva no se concreta en el tiempo establecido.
                </p>
            </div>

            <!-- Invitación -->
            <div style='background-color: #e3f2fd; padding: 20px; border-radius: 8px; border-left: 4px solid #2196F3; margin-top: 20px;'>
                <strong style='color: #1565c0;'>🔍 Busca otro estacionamiento</strong>
                <p style='margin: 10px 0 0 0;'>
                    Te invitamos a buscar otros estacionamientos disponibles en nuestra aplicación. 
                    Hay muchas opciones cerca de ti esperando por tu vehículo.
                </p>
            </div>
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
}