namespace Model
{
    public class DataVehiculoAlojado // Representa 1 tipo de vehiculo determinado (auto, moto, camioneta)
    {
        public int Id { get; set; }
        public string TipoDeVehiculo { get; set; } // auto, moto, camioneta
        public int CapacidadDeAlojamiento { get; set; } // cuantos de ese tiempo se aceptan
        public decimal Tarifa_Hora { get; set; }
        public decimal Tarifa_Dia { get; set; }
        public decimal Tarifa_Semana { get; set; }
        public decimal Tarifa_Mes { get; set; }
    }
}
