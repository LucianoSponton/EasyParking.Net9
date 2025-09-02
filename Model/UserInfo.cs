using Model.Enums;
using System;

namespace Model
{
    public class UserInfo
    {
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string UserName { get; set; }
        public TipoDeDocumento TipoDeDocumento { get; set; }
        public string NumeroDeDocumento { get; set; }
        public DateTime FechaDeNacimiento { get; set; }
        public string Telefono { get; set; }
        public TipoDeSexo Sexo { get; set; }
        public byte[] FotoDePerfil { get; set; }

        public string Apodo { get; set; }

    }
}
