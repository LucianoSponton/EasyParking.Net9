using Model;
using Model.Enums;
using ServiceWebApi;
using ServiceWebApi.DTO;
using static Test2.Program.EstacionamientoFactory;

namespace Test2
{
    class Program
    {
         static string Uri = "http://localhost:5000";
        //static string Uri = "http://40.118.242.96:12595";
        static void Main(string[] args)
        {
            //Console.WriteLine("=== SISTEMA DE RESERVA DE ESTACIONAMIENTOS - MOCK ===\n");

            //// Crear el mock completo
            //var mockData = CrearMockData();

            //// Mostrar los estacionamientos creados
            //MostrarEstacionamientos(mockData.Estacionamientos, mockData.Plazas);

            //// Mostrar las reservas exitosas
            //MostrarReservasExitosas(mockData.ReservasExitosas, mockData.Plazas, mockData.Estacionamientos);

            //// Simular reservas rechazadas
            //SimularReservasRechazadas(mockData);

            //Console.WriteLine("\n=== FIN DE LA SIMULACIÓN ===");
            //Console.ReadKey();



            //Add().Wait();
            //AddReserva().Wait();
            GetMisEstacionamiento().Wait();

        }

        static async Task GetMisEstacionamiento()
        {
            try
            {
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "lucho2798@gmail.com", "f552u4r4", 3, 180);
                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "EasyParkingAdmin", "luciano123", 3, 180);

                EstacionamientoServiceWebApi estacionamientoServiceWebApi = new EstacionamientoServiceWebApi(webapiaccess);
                var estacionamientos = await estacionamientoServiceWebApi.GetMisEstacionamientos();




                Console.WriteLine("Add Reserva Ok");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static Model.Reserva GetReservaEjemplo()
        {
            return new Model.Reserva
            {
                Id = 0,
                EstacionamientoId = 86,
                UserId = null,
                PlazaId = 0,
                VehiculoId = 1074,
                Monto = 320.00m,
                Patente = "S658KU",
                CodigoDeValidacion = "12UO3LCK",
                Estado = (Model.Enums.EstadoReserva)1, // o Model.Enums.EstadoReserva.ESPERANDO_ARRIBO
                FechaDeCreacion = DateTime.Parse("2025-10-31T13:43:31.0572634-03:00"),
                FechaDeArribo = null,
                FechaDeSalida = null,
                FechaDeExpiracion = DateTime.Parse("2025-10-31T14:13:31.061686-03:00"),
                FechaInicio = DateTime.Parse("2025-11-05T08:00:00"),
                FechaFin = DateTime.Parse("2025-11-05T12:00:00"),
                TipoDeVehiculo = TipoDeVehiculo.AUTO
            };
        }

        static async Task GetMisBloqueos(int id)
        {
            try
            {
                //var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "lucho2798@gmail.com", "f552u4r4", 3, 180);
                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "EasyParkingAdmin", "luciano123", 3, 180);

                BloqueoPlazaServiceWebApi bloqueoService = new BloqueoPlazaServiceWebApi(webapiaccess);
                var bloqueos = await bloqueoService.GetMisBloqueos();

                Console.WriteLine("=== BLOQUEOS DE PLAZAS ===\n");

                foreach (var b in bloqueos)
                {
                    Console.WriteLine($"ID: {b.Id}");
                    Console.WriteLine($"Estacionamiento ID: {b.EstacionamientoId}");
                    Console.WriteLine($"Usuario (Dueño): {b.UserId}");
                    Console.WriteLine($"Plaza: {(b.PlazaId.HasValue ? b.PlazaId.ToString() : "Todas")}");
                    Console.WriteLine($"Tipo de Vehículo: {(b.TipoDeVehiculo.HasValue ? b.TipoDeVehiculo.ToString() : "Todos")}");
                    Console.WriteLine($"Fecha de Creación: {b.FechaDeCreacion:dddd dd/MM/yyyy HH:mm}");
                    Console.WriteLine($"Fecha Inicio: {b.FechaInicio:dddd dd/MM/yyyy HH:mm}");
                    Console.WriteLine($"Fecha Fin: {b.FechaFin:dddd dd/MM/yyyy HH:mm}");
                    Console.WriteLine($"Motivo: {b.Motivo}");
                    Console.WriteLine($"Observaciones: {b.Observaciones}");
                    Console.WriteLine($"Activo: {(b.Activo ? "Sí" : "No")}");
                    Console.WriteLine("");

                }


                Console.WriteLine("Add Reserva Ok");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw;
            }

        }

        static async Task AddReserva()
        {
            try
            {
                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "lucho2798@gmail.com", "f552u4r4", 3, 180);

                ReservaServiceWebApi reservaServiceWebApi = new ReservaServiceWebApi(webapiaccess);


                await reservaServiceWebApi.Add(GetReservaEjemplo());

                Console.WriteLine("Add Reserva Ok");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw ex;
            }
        }

        static async Task Add()
        {
            try
            {
                var webapiaccess = await WebApiAccess.GetAccessAsync(Uri, "EasyParkingAdmin", "luciano123", 3, 180);

                EstacionamientoServiceWebApi estacionamientoServiceWebApi = new EstacionamientoServiceWebApi(webapiaccess);

                var estacionamientoDTO = EstacionamientoDTOFactory.CrearInstanciaCompleta();

                await estacionamientoServiceWebApi.Add(estacionamientoDTO);

                Console.WriteLine("Add Estacionamiento Ok");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw ex;
            }

        }

 
        public class EstacionamientoFactory
        {
            public class EstacionamientoDTOFactory
            {
                public static EstacionamientoDTO CrearInstanciaCompleta()
                {
                    return new EstacionamientoDTO
                    {
                        // Propiedades heredadas de Estacionamiento
                        Id = 0,
                        UserId = null,
                        PublicacionPausada = false,
                        Inactivo = false,
                        Ciudad = "Corrientes",
                        Nombre = "sdvdvvg",
                        Direccion = "av 3 de abril 1456",
                        Latitud = -27.4763029,
                        Longitud = -58.833172399999995,
                        Jornadas = new List<Jornada>
                {
                    new Jornada
                    {
                        Id = 0,
                        EstacionamientoId = 0,
                        DiaDeLaSemana = Dia.VIERNES,
                        Horarios = new List<RangoH>
                        {
                            new RangoH
                            {
                                Id = 0,
                                JornadaId = 0,
                                DesdeHora = 8,
                                DesdeMinuto = 0,
                                HastaHora = 12,
                                HastaMinuto = 0
                            }
                        }
                    }
                },
                        TipoDeLugar = "Terreno parcialmente cubierto",
                        Plazas = new List<Plaza>
                {
                    new Plaza
                    {
                        Id = 0,
                        EstacionamientoId = 0,
                        TipoDeVehiculo = TipoDeVehiculo.CAMIONETA,
                        Activo = true
                    }
                },
                        Tarifas = new List<Tarifa>
                {
                    new Tarifa
                    {
                        Id = 0,
                        EstacionamientoId = 0,
                        TipoDeVehiculo = TipoDeVehiculo.CAMIONETA,
                        Tarifa_Hora = 11.0m,
                        Tarifa_Dia = 22.0m,
                        Tarifa_Semana = 44.0m,
                        Tarifa_Mes = 55.0m
                    }
                },
                        MontoReserva = 320.0m,
                        Observaciones = "",
                        URL = "",
                        TiempoDeEsperaEnMinutos = 30,
                        FechaCreacion = new DateTime(1, 1, 1, 0, 0, 0),

                        // Propiedades específicas del DTO
                        Distancia = 0.0,
                        ImageBytes = null,
                        Puntaje = 0.0,
                        Favorito = false,
                        ImagenFavorito = null
                    };
                }
            }
        }

        // Clases necesarias (ajusta según tu modelo real)

        //static MockData CrearMockData()
        //{
        //    var mockData = new MockData();
        //    int plazaIdCounter = 1;
        //    int reservaIdCounter = 1;

        //    // ESTACIONAMIENTO 1: 3 autos y 2 motos
        //    var est1 = new Estacionamiento2
        //    {
        //        Id = 1,
        //        UserId = "USER001",
        //        FechaCreacion = DateTime.Now.AddMonths(-3)
        //    };
        //    mockData.Estacionamientos.Add(est1);

        //    // Crear 3 plazas para auto
        //    for (int i = 0; i < 3; i++)
        //    {
        //        var plaza = new Plaza
        //        {
        //            Id = plazaIdCounter++,
        //            EstacionamientoId = 1,
        //            UserId = 1,
        //            TipoDeVehiculo = TipoDeVehiculo.AUTO
        //        };
        //        mockData.Plazas.Add(plaza);
        //        est1.Plazas.Add(plaza);
        //    }

        //    // Crear 2 plazas para moto
        //    for (int i = 0; i < 2; i++)
        //    {
        //        var plaza = new Plaza
        //        {
        //            Id = plazaIdCounter++,
        //            EstacionamientoId = 1,
        //            UserId = 1,
        //            TipoDeVehiculo = TipoDeVehiculo.MOTO
        //        };
        //        mockData.Plazas.Add(plaza);
        //        est1.Plazas.Add(plaza);
        //    }

        //    // ESTACIONAMIENTO 2: 1 camioneta y 2 autos
        //    var est2 = new Estacionamiento2
        //    {
        //        Id = 2,
        //        UserId = "USER002",
        //        FechaCreacion = DateTime.Now.AddMonths(-2)
        //    };
        //    mockData.Estacionamientos.Add(est2);

        //    // Crear 1 plaza para camioneta
        //    var plazaCamioneta = new Plaza
        //    {
        //        Id = plazaIdCounter++,
        //        EstacionamientoId = 2,
        //        UserId = 2,
        //        TipoDeVehiculo = TipoDeVehiculo.CAMIONETA
        //    };
        //    mockData.Plazas.Add(plazaCamioneta);
        //    est2.Plazas.Add(plazaCamioneta);

        //    // Crear 2 plazas para auto
        //    for (int i = 0; i < 2; i++)
        //    {
        //        var plaza = new Plaza
        //        {
        //            Id = plazaIdCounter++,
        //            EstacionamientoId = 2,
        //            UserId = 2,
        //            TipoDeVehiculo = TipoDeVehiculo.AUTO
        //        };
        //        mockData.Plazas.Add(plaza);
        //        est2.Plazas.Add(plaza);
        //    }

        //    // ESTACIONAMIENTO 3: 3 motos y 1 auto
        //    var est3 = new Estacionamiento2
        //    {
        //        Id = 3,
        //        UserId = "USER003",
        //        FechaCreacion = DateTime.Now.AddMonths(-1)
        //    };
        //    mockData.Estacionamientos.Add(est3);

        //    // Crear 3 plazas para moto
        //    for (int i = 0; i < 3; i++)
        //    {
        //        var plaza = new Plaza
        //        {
        //            Id = plazaIdCounter++,
        //            EstacionamientoId = 3,
        //            UserId = 3,
        //            TipoDeVehiculo = TipoDeVehiculo.MOTO
        //        };
        //        mockData.Plazas.Add(plaza);
        //        est3.Plazas.Add(plaza);
        //    }

        //    // Crear 1 plaza para auto
        //    var plazaAuto = new Plaza
        //    {
        //        Id = plazaIdCounter++,
        //        EstacionamientoId = 3,
        //        UserId = 3,
        //        TipoDeVehiculo = TipoDeVehiculo.AUTO
        //    };
        //    mockData.Plazas.Add(plazaAuto);
        //    est3.Plazas.Add(plazaAuto);

        //    // CREAR RESERVAS EXITOSAS
        //    DateTime proximoMartes = ObtenerProximoDiaSemana(DayOfWeek.Tuesday);
        //    DateTime proximoJueves = ObtenerProximoDiaSemana(DayOfWeek.Thursday);
        //    DateTime proximoViernes = ObtenerProximoDiaSemana(DayOfWeek.Friday);

        //    // Reserva 1: Estacionamiento 1, Auto, Martes
        //    var plaza1 = mockData.Plazas.First(p => p.EstacionamientoId == 1 && p.TipoDeVehiculo == TipoDeVehiculo.AUTO);
        //    mockData.ReservasExitosas.Add(new Reserva2
        //    {
        //        Id = reservaIdCounter++,
        //        PlazaId = plaza1.Id,
        //        ClienteUserId = "CLIENTE001",
        //        FechaInicio = proximoMartes.AddHours(9),
        //        FechaFin = proximoMartes.AddHours(18),
        //        TipoDeVehiculo = TipoDeVehiculo.AUTO,
        //        Estado = EstadoReserva.Confirmada
        //    });

        //    // Reserva 2: Estacionamiento 1, Auto (otra plaza), Jueves
        //    var plaza2 = mockData.Plazas.Where(p => p.EstacionamientoId == 1 && p.TipoDeVehiculo == TipoDeVehiculo.AUTO && p.Id != plaza1.Id).First();
        //    mockData.ReservasExitosas.Add(new Reserva2
        //    {
        //        Id = reservaIdCounter++,
        //        PlazaId = plaza2.Id,
        //        ClienteUserId = "CLIENTE002",
        //        FechaInicio = proximoJueves.AddHours(10),
        //        FechaFin = proximoJueves.AddHours(20),
        //        TipoDeVehiculo = TipoDeVehiculo.AUTO,
        //        Estado = EstadoReserva.Confirmada
        //    });

        //    // Reserva 3: Estacionamiento 2, Camioneta, Martes
        //    var plaza3 = mockData.Plazas.First(p => p.EstacionamientoId == 2 && p.TipoDeVehiculo == TipoDeVehiculo.CAMIONETA);
        //    mockData.ReservasExitosas.Add(new Reserva2
        //    {
        //        Id = reservaIdCounter++,
        //        PlazaId = plaza3.Id,
        //        ClienteUserId = "CLIENTE003",
        //        FechaInicio = proximoMartes.AddHours(8),
        //        FechaFin = proximoMartes.AddHours(17),
        //        TipoDeVehiculo = TipoDeVehiculo.CAMIONETA,
        //        Estado = EstadoReserva.Confirmada
        //    });

        //    // Reserva 4: Estacionamiento 3, Moto, Viernes
        //    var plaza4 = mockData.Plazas.First(p => p.EstacionamientoId == 3 && p.TipoDeVehiculo == TipoDeVehiculo.MOTO);
        //    mockData.ReservasExitosas.Add(new Reserva2
        //    {
        //        Id = reservaIdCounter++,
        //        PlazaId = plaza4.Id,
        //        ClienteUserId = "CLIENTE004",
        //        FechaInicio = proximoViernes.AddHours(14),
        //        FechaFin = proximoViernes.AddHours(22),
        //        TipoDeVehiculo = TipoDeVehiculo.MOTO,
        //        Estado = EstadoReserva.Confirmada
        //    });

        //    return mockData;
        //}

        //static void SimularReservasRechazadas(MockData mockData)
        //{
        //    Console.WriteLine("\n=== SIMULACIÓN DE RESERVAS RECHAZADAS ===\n");

        //    DateTime proximoMartes = ObtenerProximoDiaSemana(DayOfWeek.Tuesday);
        //    DateTime proximoJueves = ObtenerProximoDiaSemana(DayOfWeek.Thursday);

        //    // Intento 1: Reservar la misma plaza del martes (se solapa completamente)
        //    var reservaExistente1 = mockData.ReservasExitosas.First(r => r.FechaInicio.DayOfWeek == DayOfWeek.Tuesday);
        //    var intentoReserva1 = new Reserva2
        //    {
        //        Id = 999,
        //        PlazaId = reservaExistente1.PlazaId,
        //        ClienteUserId = "CLIENTE005",
        //        FechaInicio = proximoMartes.AddHours(10),
        //        FechaFin = proximoMartes.AddHours(16),
        //        TipoDeVehiculo = TipoDeVehiculo.AUTO,
        //        Estado = EstadoReserva.Pendiente
        //    };

        //    if (ValidarReserva(intentoReserva1, mockData.ReservasExitosas, mockData.Plazas))
        //    {
        //        Console.WriteLine("✓ Reserva aceptada (ERROR - no debería pasar)");
        //    }
        //    else
        //    {
        //        Console.WriteLine("✗ RESERVA RECHAZADA");
        //        Console.WriteLine($"   Plaza: {intentoReserva1.PlazaId}");
        //        Console.WriteLine($"   Fecha solicitada: {intentoReserva1.FechaInicio:dd/MM/yyyy HH:mm} - {intentoReserva1.FechaFin:HH:mm}");
        //        Console.WriteLine($"   Motivo: Se solapa con reserva existente (ID: {reservaExistente1.Id})");
        //        Console.WriteLine($"   Fecha ocupada: {reservaExistente1.FechaInicio:dd/MM/yyyy HH:mm} - {reservaExistente1.FechaFin:HH:mm}\n");
        //    }

        //    // Intento 2: Reservar solapamiento parcial (inicia antes y termina durante)
        //    var intentoReserva2 = new Reserva2
        //    {
        //        Id = 998,
        //        PlazaId = reservaExistente1.PlazaId,
        //        ClienteUserId = "CLIENTE006",
        //        FechaInicio = proximoMartes.AddHours(7),
        //        FechaFin = proximoMartes.AddHours(11),
        //        TipoDeVehiculo = TipoDeVehiculo.AUTO,
        //        Estado = EstadoReserva.Pendiente
        //    };

        //    if (ValidarReserva(intentoReserva2, mockData.ReservasExitosas, mockData.Plazas))
        //    {
        //        Console.WriteLine("✓ Reserva aceptada (ERROR - no debería pasar)");
        //    }
        //    else
        //    {
        //        Console.WriteLine("✗ RESERVA RECHAZADA");
        //        Console.WriteLine($"   Plaza: {intentoReserva2.PlazaId}");
        //        Console.WriteLine($"   Fecha solicitada: {intentoReserva2.FechaInicio:dd/MM/yyyy HH:mm} - {intentoReserva2.FechaFin:HH:mm}");
        //        Console.WriteLine($"   Motivo: Solapamiento parcial con reserva existente");
        //        Console.WriteLine($"   Fecha ocupada: {reservaExistente1.FechaInicio:dd/MM/yyyy HH:mm} - {reservaExistente1.FechaFin:HH:mm}\n");
        //    }

        //    // Intento 3: Reservar en estacionamiento 3, jueves (día diferente pero misma plaza que tiene reserva viernes)
        //    var reservaViernes = mockData.ReservasExitosas.First(r => r.FechaInicio.DayOfWeek == DayOfWeek.Friday);
        //    var intentoReserva3 = new Reserva2
        //    {
        //        Id = 997,
        //        PlazaId = reservaViernes.PlazaId,
        //        ClienteUserId = "CLIENTE007",
        //        FechaInicio = proximoJueves.AddHours(9),
        //        FechaFin = proximoJueves.AddHours(18),
        //        TipoDeVehiculo = TipoDeVehiculo.MOTO,
        //        Estado = EstadoReserva.Pendiente
        //    };

        //    if (ValidarReserva(intentoReserva3, mockData.ReservasExitosas, mockData.Plazas))
        //    {
        //        Console.WriteLine("✓ RESERVA ACEPTADA");
        //        Console.WriteLine($"   Plaza: {intentoReserva3.PlazaId}");
        //        Console.WriteLine($"   Fecha solicitada: {intentoReserva3.FechaInicio:dd/MM/yyyy HH:mm} - {intentoReserva3.FechaFin:HH:mm}");
        //        Console.WriteLine($"   Esta plaza está libre el jueves (ocupada solo el viernes)\n");
        //    }
        //    else
        //    {
        //        Console.WriteLine("✗ Reserva rechazada (ERROR - debería ser aceptada)");
        //    }

        //    // Intento 4: Solapamiento que engloba toda una reserva existente
        //    var intentoReserva4 = new Reserva2
        //    {
        //        Id = 996,
        //        PlazaId = reservaExistente1.PlazaId,
        //        ClienteUserId = "CLIENTE008",
        //        FechaInicio = proximoMartes.AddHours(8),
        //        FechaFin = proximoMartes.AddHours(19),
        //        TipoDeVehiculo = TipoDeVehiculo.AUTO,
        //        Estado = EstadoReserva.Pendiente
        //    };

        //    if (ValidarReserva(intentoReserva4, mockData.ReservasExitosas, mockData.Plazas))
        //    {
        //        Console.WriteLine("✓ Reserva aceptada (ERROR - no debería pasar)");
        //    }
        //    else
        //    {
        //        Console.WriteLine("✗ RESERVA RECHAZADA");
        //        Console.WriteLine($"   Plaza: {intentoReserva4.PlazaId}");
        //        Console.WriteLine($"   Fecha solicitada: {intentoReserva4.FechaInicio:dd/MM/yyyy HH:mm} - {intentoReserva4.FechaFin:HH:mm}");
        //        Console.WriteLine($"   Motivo: El período solicitado engloba una reserva existente");
        //        Console.WriteLine($"   Fecha ocupada: {reservaExistente1.FechaInicio:dd/MM/yyyy HH:mm} - {reservaExistente1.FechaFin:HH:mm}\n");
        //    }
        //}

        //static bool ValidarReserva(Reserva2 nuevaReserva, List<Reserva2> reservasExistentes, List<Plaza> plazas)
        //{
        //    // Verificar que la plaza existe
        //    var plaza = plazas.FirstOrDefault(p => p.Id == nuevaReserva.PlazaId);
        //    if (plaza == null) return false;

        //    // Verificar que el tipo de vehículo coincide con el de la plaza
        //    if (plaza.TipoDeVehiculo != nuevaReserva.TipoDeVehiculo) return false;

        //    // Verificar solapamiento con reservas existentes en la misma plaza
        //    var reservasPlaza = reservasExistentes.Where(r => r.PlazaId == nuevaReserva.PlazaId).ToList();

        //    foreach (var reservaExistente in reservasPlaza)
        //    {
        //        // Verificar si hay solapamiento
        //        bool haySolapamiento = nuevaReserva.FechaInicio < reservaExistente.FechaFin &&
        //                              nuevaReserva.FechaFin > reservaExistente.FechaInicio;

        //        if (haySolapamiento)
        //        {
        //            return false; // Rechazar la reserva
        //        }
        //    }

        //    return true; // Aceptar la reserva
        //}

        //static DateTime ObtenerProximoDiaSemana(DayOfWeek diaSemana)
        //{
        //    DateTime hoy = DateTime.Today;
        //    int diasHastaProximo = ((int)diaSemana - (int)hoy.DayOfWeek + 7) % 7;
        //    if (diasHastaProximo == 0) diasHastaProximo = 7; // Si es hoy, ir a la próxima semana
        //    return hoy.AddDays(diasHastaProximo);
        //}

        //static void MostrarEstacionamientos(List<Estacionamiento2> estacionamientos, List<Plaza> plazas)
        //{
        //    Console.WriteLine("=== ESTACIONAMIENTOS CREADOS ===\n");
        //    foreach (var est in estacionamientos)
        //    {
        //        Console.WriteLine($"Estacionamiento ID: {est.Id}");
        //        Console.WriteLine($"Usuario Dueño: {est.UserId}");
        //        Console.WriteLine($"Fecha Creación: {est.FechaCreacion:dd/MM/yyyy}");
        //        Console.WriteLine($"Total de plazas: {est.Plazas.Count}");
        //        Console.WriteLine("Distribución de plazas:");

        //        var plazasAgrupadas = est.Plazas.GroupBy(p => p.TipoDeVehiculo);
        //        foreach (var grupo in plazasAgrupadas)
        //        {
        //            Console.WriteLine($"  - {grupo.Key}: {grupo.Count()} plaza(s)");
        //        }
        //        Console.WriteLine();
        //    }
        //}

        //static void MostrarReservasExitosas(List<Reserva2> reservas, List<Plaza> plazas, List<Estacionamiento2> estacionamientos)
        //{
        //    Console.WriteLine("=== RESERVAS EXITOSAS CREADAS ===\n");
        //    foreach (var reserva in reservas)
        //    {
        //        var plaza = plazas.First(p => p.Id == reserva.PlazaId);
        //        Console.WriteLine($"Reserva ID: {reserva.Id}");
        //        Console.WriteLine($"Cliente: {reserva.ClienteUserId}");
        //        Console.WriteLine($"Estacionamiento: {plaza.EstacionamientoId}");
        //        Console.WriteLine($"Plaza: {reserva.PlazaId} (Tipo: {reserva.TipoDeVehiculo})");
        //        Console.WriteLine($"Desde: {reserva.FechaInicio:dddd dd/MM/yyyy HH:mm}");
        //        Console.WriteLine($"Hasta: {reserva.FechaFin:dddd dd/MM/yyyy HH:mm}");
        //        Console.WriteLine($"Estado: {reserva.Estado}\n");
        //    }
        //}
    }

    // Clases auxiliares
    public class MockData
    {
        public List<Estacionamiento2> Estacionamientos { get; set; } = new List<Estacionamiento2>();
        public List<Plaza> Plazas { get; set; } = new List<Plaza>();
        public List<Reserva2> ReservasExitosas { get; set; } = new List<Reserva2>();
    }

    public class Reserva2
    {
        public int Id { get; set; }
        public int PlazaId { get; set; }
        public string ClienteUserId { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public TipoDeVehiculo TipoDeVehiculo { get; set; }
        public EstadoReserva Estado { get; set; }
    }

    public enum EstadoReserva
    {
        Pendiente,
        Confirmada,
        Cancelada,
        Finalizada
    }

    
    public class Estacionamiento2
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public List<Jornada> Jornadas { get; set; } = new List<Jornada>();
        public List<Plaza> Plazas { get; set; } = new List<Plaza>(); // Ahora usa Plaza directamente
        public DateTime FechaCreacion { get; set; }
    }



}