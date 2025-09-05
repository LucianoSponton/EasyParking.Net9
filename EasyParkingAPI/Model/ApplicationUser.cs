using Microsoft.AspNetCore.Identity;
using Model.Enums;
using System;

namespace EasyParkingAPI.Model
{
    public class ApplicationUser : IdentityUser
    {
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Password { get; set; }
        public TipoDeDocumento TipoDeDocumento { get; set; }
        public string NumeroDeDocumento { get; set; }
        public DateTime FechaDeNacimiento { get; set; }
        public string Telefono { get; set; }

        public TipoDeSexo Sexo { get; set; }

        public byte[] FotoDePerfil { get; set; }

        public string Link_Foto { get; set; }
        public string Apodo { get; set; }
    }

}
