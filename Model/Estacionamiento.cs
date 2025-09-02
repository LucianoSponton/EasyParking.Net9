using System.Collections.Generic;

namespace Model
{
    public class Estacionamiento
    {
        public Estacionamiento()
        {
            for (int i = 0; i < DiasConHorarios.Length; i++)
            {
                DiasConHorarios[i] = new Dia();
            }
        }
        public int Id { get; set; }
        public string UserId { get; set; }
        public string applicationUserId { get; set; }
      
        public byte[] Imagen { get; set; } // podria ser lista // imagen del lugar
        public string Ciudad { get; set; } // ciudad del lugar
        public string Nombre { get; set; } // nombre del lugar
        public string Direccion { get; set; } // Direccion del lugar
        public Dia[] DiasConHorarios { get; set; } = new Dia[7]; // horarios en los que opera
        public string TipoDeLugar { get; set; } // descripcion del lugar - galpon, aire libre etc..
        public List<DataVehiculoAlojado> TiposDeVehiculosAdmitidos { get; set; } = new List<DataVehiculoAlojado>();// vehiculos aceptados
        public decimal MontoReserva { get; set; } // precio de la reserva si es que tiene 
        public string Observaciones { get; set; } // datos extra que el dueño quiera informar sobre el estacionamiento

    }

    //public class TipoDeLugar // Tablas -- podrian haber mas de los inicalmente planteados
    //{
    //    public int Id { get; set; }

    //    public string Descripcion { get; set; } // terreno aire abierto - etc..
    //}

    //public class TipoDeVehiculo // Tablas -- podrian haber mas de los inicalmente planteados
    //{
    //    public int Id { get; set; }

    //    public string Descripcion { get; set; } // auto - moto - camioneta
    //}
}
