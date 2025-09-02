using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceWebApi.DTO
{
    public class EstacionamientoDTO : Model.Estacionamiento
    {
        public bool Inactivo { get; set; } // para poder pausar una publidad
        public bool Favorito { get; set; } // Para poder agregar a favortios del 
    }
}
