using System.Collections.Generic;

namespace Model
{
    public class Dia // Representa El dia de la semana con los horarios en los que opera el estacionamiento
    {
        public int Id { get; set; }
        public Enums.Dia DiaDeLaSemana { get; set; }
        public List<RangoH> Horarios { get; set; } = new List<RangoH>();
    }
}
