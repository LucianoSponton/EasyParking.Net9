using EasyParkingAPI.Data;
using EasyParkingAPI.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Model;
using NuGet.Protocol.Plugins;
using ServiceWebApi.DTO;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace EasyParkingAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IOptions<IdentityOptions> _identityOptions;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly EasyParkingAuthContext _EasyParkingAuthContext;

        private string _From_SmtpServer;
        private int _From_SmtpServerPort;
        private string _From_Name;
        private string _From_EmailAdress;
        private string _From_EmailPassword;


        public AccountController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<ApplicationUser> signInManager,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            IOptions<IdentityOptions> identityOptions,
            EasyParkingAuthContext EasyParkingAuthContext)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _identityOptions = identityOptions;
            _EasyParkingAuthContext = EasyParkingAuthContext;

            _From_SmtpServer = _configuration.GetValue<string>("EmailAccount:From_SmtpServer");
            _From_SmtpServerPort = _configuration.GetValue<int>("EmailAccount:From_SmtpServerPort");
            _From_Name = _configuration.GetValue<string>("EmailAccount:From_Name");
            _From_EmailAdress = _configuration.GetValue<string>("EmailAccount:From_EmailAdress");
            _From_EmailPassword = _configuration.GetSection("EmailAccount")["From_EmailPassword"];


        }

        //[HttpPost]
        //[Route("[action]")]
        //public async Task<ActionResult> CreateUser([FromBody] UserInfo userinfo)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        string estado = "Iniciando";
        //        if (userinfo == null)
        //        {
        //            return BadRequest("ERROR. Datos No Válidos ...");
        //        }

        //        var strategy = _EasyParkingAuthContext.Database.CreateExecutionStrategy();
        //        try
        //        {
        //            var result = await strategy.ExecuteAsync<ActionResult>(async () =>
        //            {
        //                using (var transaction = _EasyParkingAuthContext.Database.BeginTransaction())
        //                {
        //                    try
        //                    {
        //                        estado = "Creando Usuario";
        //                        var appuser = new ApplicationUser
        //                        {
        //                            UserName = userinfo.Email.ToLower(),
        //                            Email = userinfo.Email.ToLower(),
        //                            Apellido = userinfo.Apellido,
        //                            Nombre = userinfo.Nombre,
        //                            FechaDeNacimiento = userinfo.FechaDeNacimiento,
        //                            NumeroDeDocumento = userinfo.NumeroDeDocumento,
        //                            TipoDeDocumento = userinfo.TipoDeDocumento,
        //                            Telefono = userinfo.Telefono,
        //                            Sexo = userinfo.Sexo,
        //                            EmailConfirmed = true
        //                        };

        //                        var result = await _userManager.CreateAsync(appuser, userinfo.Password);
        //                        if (result.Succeeded)
        //                        {
        //                            // Guardar foto si viene
        //                            if (userinfo.FotoDePerfil != null && userinfo.FotoDePerfil.Length > 0)
        //                            {
        //                                var folder = _configuration.GetValue<string>("EasyParkingAPI:Images:Usuarios_Folder");

        //                                if (string.IsNullOrEmpty(folder))
        //                                    return BadRequest("Ruta de almacenamiento no configurada");

        //                                if (!Directory.Exists(folder))
        //                                    Directory.CreateDirectory(folder);

        //                                var fileName = appuser.Id + ".jpg";
        //                                var path = Path.Combine(folder, fileName);

        //                                await System.IO.File.WriteAllBytesAsync(path, userinfo.FotoDePerfil);

        //                                // Actualizar Link_Foto con la URL pública
        //                                appuser.Link_Foto = $"http://40.118.242.96:12595/images/usuarios/{fileName}";
        //                                await _userManager.UpdateAsync(appuser);
        //                            }

        //                            estado = "Adhiriendo Usuario a Rol";
        //                            var result02 = await _userManager.AddToRoleAsync(appuser, "AppUser");
        //                            if (result02.Succeeded)
        //                            {
        //                                if (!appuser.EmailConfirmed)
        //                                {
        //                                    //estado = "Enviando eMail de Confirmacion";
        //                                    //var token = await _userManager.GenerateEmailConfirmationTokenAsync(appuser);
        //                                    //var callbackUrl = new Uri(Url.Link("ConfirmEmailRoute", new { userId = appuser.Id, token = token }));
        //                                    //Sender mailKit = new Sender(_From_SmtpServer, _From_SmtpServerPort, true, _From_Name, _From_EmailAdress, _From_EmailPassword);
        //                                    //mailKit.Send(appuser.UserName, appuser.Email, "Confirma tu Cuenta",
        //                                    //    $"<h2>{appuser.UserName}</h2>" + Environment.NewLine +
        //                                    //    $"<a href=\"{callbackUrl}\"> Por favor confirme su cuenta haciendo click aqui. </a>");
        //                                }

        //                                _EasyParkingAuthContext.Database.CommitTransaction();
        //                                return Ok(new { appuser.Id, appuser.Link_Foto });
        //                            }
        //                            else
        //                            {
        //                                throw new Exception(result02.Errors.ToString());
        //                            }
        //                        }
        //                        else
        //                        {
        //                            _EasyParkingAuthContext.Database.RollbackTransaction();
        //                            return BadRequest(result.Errors.ToList());
        //                        }
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _EasyParkingAuthContext.Database.RollbackTransaction();
        //                        return BadRequest("ERROR ... " + estado + " - Error message: " + ex.Message + (ex.InnerException?.Message ?? ""));
        //                    }
        //                }
        //            });
        //            return result;
        //        }
        //        catch (Exception ex)
        //        {
        //            return BadRequest("ERROR ... " + ex.Message);
        //        }
        //    }
        //    else
        //    {
        //        return BadRequest(ModelState);
        //    }
        //}


        [HttpPost]
        [Route("[action]")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, AppUser")]
        public async Task<ActionResult> UpdateUserAsync([FromBody] UserInfo userinfo)
        {
            try
            {
                var user = _httpContextAccessor.HttpContext.User;
                ApplicationUser appuser = await _userManager.FindByNameAsync(user.Identity.Name);

                if (appuser == null)
                    return NotFound("Usuario no encontrado");

                // Actualizamos propiedades básicas
                appuser.Telefono = userinfo.Telefono;
                appuser.Apodo = userinfo.Apodo;

                // Guardar/Actualizar imagen de perfil
                if (userinfo.FotoDePerfil != null && userinfo.FotoDePerfil.Length > 0)
                {
                    var folder = _configuration.GetValue<string>("EasyParkingAPI:Images:Usuarios_Folder");

                    if (string.IsNullOrEmpty(folder))
                        return BadRequest("Ruta de almacenamiento no configurada");

                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    // Nombre del archivo = UserId.jpg
                    var fileName = appuser.Id + ".jpg";
                    var path = Path.Combine(folder, fileName);

                    // Sobrescribir la imagen si ya existe
                    await System.IO.File.WriteAllBytesAsync(path, userinfo.FotoDePerfil);

                    // Actualizar URL pública de la foto
                    appuser.Link_Foto = $"http://40.118.242.96:12595/images/usuarios/{fileName}";
                }

                await _userManager.UpdateAsync(appuser);
                return Ok(new { appuser.Id, appuser.Link_Foto });
            }
            catch (Exception ex)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(ex));
            }
        }


        [HttpPost]
        [Route("[action]")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        public async Task<ActionResult> CreateRole([FromBody] RoleInfo model)
        {
            if (ModelState.IsValid)
            {
                var role = new IdentityRole() { Name = model.roleName };
                var result = await _roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    return Ok();
                }
                else
                {
                    return BadRequest("Nombre de Rol NO VALIDO ...");
                }
            }
            else
            {
                return BadRequest(ModelState);
            }

        }

        //[HttpPost]
        //[Route("[action]")]
        //public async Task<ActionResult> Login([FromBody] UserInfo userInfo)
        //{
        //    try
        //    {
        //        var result = _signInManager.PasswordSignInAsync(userInfo.UserName, userInfo.Password, isPersistent: false, lockoutOnFailure: false).Result;
        //        if (result.Succeeded)
        //        {
        //            ApplicationUser user = await _userManager.FindByNameAsync(userInfo.UserName);
        //            IList<String> roles = await _userManager.GetRolesAsync(user);
        //            return BuildToken(user, roles);

        //        }
        //        else
        //        {
        //            return BadRequest("Intento de login NO VALIDO ...");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(Tools.Tools.ExceptionMessage(ex));
        //    }
        //}

        [HttpGet("[action]/{username},{currentPassword},{newPassword}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, AppUser")]
        public async Task<ActionResult> ChangePassword(string username, string currentPassword, string newPassword)
        {
            try
            {
                var result = _signInManager.PasswordSignInAsync(username, currentPassword, isPersistent: false, lockoutOnFailure: false).Result;
                if (result.Succeeded)
                {
                    ApplicationUser user = await _userManager.FindByNameAsync(username);
                    var identityResult = _userManager.ChangePasswordAsync(user, currentPassword, newPassword).Result;
                    if (identityResult.Succeeded)
                    {
                        return Ok();
                    }
                    else
                    {
                        return BadRequest(identityResult.Errors);
                    }

                }
                else
                {
                    return BadRequest("Fallo en intento de Cambiar Contraseña ...");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(ex));
            }
        }

        [HttpGet("[action]/{username}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, AppUser")]
        public async Task<ActionResult> UserLock(string username)
        {
            try
            {
                ApplicationUser user = await _userManager.FindByNameAsync(username);
                var identityResult = _userManager.SetLockoutEnabledAsync(user, true).Result;
                if (identityResult.Succeeded)
                {
                    DateTime lockEnd = new DateTime(2200, 12, 31, 23, 59, 59);
                    DateTimeOffset lockend2 = lockEnd;

                    var identityResult2 = _userManager.SetLockoutEndDateAsync(user, lockend2).Result;
                    if (identityResult.Succeeded)
                    {
                        return Ok();
                    }
                    else
                    {
                        return BadRequest(identityResult.Errors);
                    }
                }
                else
                {
                    return BadRequest(identityResult.Errors);
                }

            }
            catch (Exception ex)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(ex));
            }
        }

        [HttpGet("[action]/{username}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, AppUser")]
        public async Task<ActionResult> UserUnLock(string username)
        {
            try
            {
                ApplicationUser user = await _userManager.FindByNameAsync(username);
                var identityResult = _userManager.SetLockoutEnabledAsync(user, true).Result;
                if (identityResult.Succeeded)
                {
                    var identityResult2 = _userManager.SetLockoutEndDateAsync(user, null).Result;
                    if (identityResult.Succeeded)
                    {
                        return Ok();
                    }
                    else
                    {
                        return BadRequest(identityResult.Errors);
                    }
                }
                else
                {
                    return BadRequest(identityResult.Errors);
                }

            }
            catch (Exception ex)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(ex));
            }
        }

        private ActionResult BuildToken(ApplicationUser user, IList<String> roles)
        {
            var claims = new[]
{
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("LSClaim_UserId", user.Id),
            };
            try
            {
                foreach (string role in roles)
                {
                    Array.Resize(ref claims, claims.Length + 1);
                    var i = claims.Count();
                    claims[i - 1] = new Claim(ClaimTypes.Role, role);
                }

            }
            catch (Exception ex)
            {

                throw ex;
            }



            //List<Claim> claims = new List<Claim>();
            //claims.Add(new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName));
            //claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
            //claims.Add(new Claim("LSClaim_UserId", user.Id));

            //foreach (string role in roles)
            //{
            //    claims.Add(new Claim(ClaimTypes.Role, role));
            //}




            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetValue<string>("Security:SymmetricSecurityKey")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiration = DateTime.UtcNow.AddHours(2);

            JwtSecurityToken token = new JwtSecurityToken(
               issuer: "yourdomain.com",
               audience: "yourdomain.com",
               claims: claims,
               expires: expiration,
               signingCredentials: creds);

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = expiration
            });

        }

        //[HttpGet]
        //[AllowAnonymous]
        //[Route("[action]", Name = "ConfirmEmailRoute")]
        //public ActionResult ConfirmEmail(string userid, string token)
        //{
        //    ApplicationUser user = _userManager.FindByIdAsync(userid).Result;
        //    IdentityResult result = _userManager.ConfirmEmailAsync(user, token).Result;
        //    if (result.Succeeded)
        //    {
        //        return Ok("Cuenta confirmada ...");
        //    }
        //    else
        //    {
        //        return BadRequest("Error al intentar confirmar Cuenta ...");
        //    }
        //}

        [AllowAnonymous]
        [HttpGet("[action]/{userName}")]
        public ActionResult IsValidUserName(string userName)
        {
            try
            {
                ApplicationUser user = _userManager.FindByNameAsync(userName).Result;
                if (user == null)
                {
                    return Ok("TRUE");
                }
                else
                {
                    return Ok("FALSE");
                }

            }
            catch (Exception ex)
            {

                return BadRequest(ex.ToString());
            }
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("[action]", Name = "ConfirmPasswordResetRoute")]
        public IActionResult ConfirmPasswordReset(string userid, string token, string newpassword)
        {

            ApplicationUser user = _userManager.FindByIdAsync(userid).Result;
            IdentityResult result = _userManager.ResetPasswordAsync(user, token, newpassword).Result;
            if (result.Succeeded)
            {
                return Ok("Contraseña confirmada ...");
            }
            else
            {
                return BadRequest("Error al intentar confirmar Contraseña ...");
            }
        }

        /// <summary>
        /// Generates a Random Password
        /// respecting the given strength requirements.
        /// </summary>
        /// <param name="opts">A valid PasswordOptions object
        /// containing the password strength requirements.</param>
        /// <returns>A random password</returns>

        [HttpGet("[action]/{username}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "AppUser")]
        public async Task<ActionResult> UserLockItSelf(string username)
        {
            try
            {
                var userlogged = _httpContextAccessor.HttpContext.User; // usuario logeado
                if (userlogged.Identity.Name == username)
                {
                    ApplicationUser user = await _userManager.FindByNameAsync(username);
                    var identityResult = _userManager.SetLockoutEnabledAsync(user, true).Result;
                    if (identityResult.Succeeded)
                    {
                        DateTime lockEnd = new DateTime(2200, 12, 31, 23, 59, 59);
                        DateTimeOffset lockend2 = lockEnd;

                        var identityResult2 = _userManager.SetLockoutEndDateAsync(user, lockend2).Result;
                        if (identityResult.Succeeded)
                        {
                            return Ok();
                        }
                        else
                        {
                            return BadRequest(identityResult.Errors);
                        }
                    }
                    else
                    {
                        return BadRequest(identityResult.Errors);
                    }
                }
                else
                {
                    return BadRequest("Accion no permitida");

                }



            }
            catch (Exception ex)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(ex));
            }
        }

        [HttpGet("[action]/{username}")]
        public async Task<ActionResult<UserInfo>> GetUserInfo(string username)
        {
            try
            {
                var userlogged = _httpContextAccessor.HttpContext.User; // usuario logeado
                if (userlogged.Identity.Name == username)
                {
                    var user = _httpContextAccessor.HttpContext.User;
                    ApplicationUser appuser = _userManager.FindByNameAsync(user.Identity.Name).Result;
                    UserInfo userInfo = new UserInfo();
                    userInfo.UserId = appuser.Id;
                    userInfo.Nombre = appuser.Nombre;
                    userInfo.Apellido = appuser.Apellido;
                    userInfo.Email = appuser.Email;
                    userInfo.TipoDeDocumento = appuser.TipoDeDocumento;
                    userInfo.NumeroDeDocumento = appuser.NumeroDeDocumento;
                    userInfo.Telefono = appuser.Telefono;
                    userInfo.UserName = appuser.UserName;
                    userInfo.Apodo = appuser.Apodo;
                    userInfo.FotoDePerfil = appuser.FotoDePerfil;
                    userInfo.FechaDeNacimiento = appuser.FechaDeNacimiento;
                    userInfo.Sexo = appuser.Sexo;
                    userInfo.Link_Foto = appuser.Link_Foto;
                    return userInfo;
                }
                else
                {
                    return BadRequest("Accion no permitida");
                }

            }
            catch (Exception ex)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(ex));
            }
        }


        // ============================================
        // 1. MÉTODO EN EL CONTROLLER (AccountController.cs)
        // ============================================

        [HttpPost]
        [Route("[action]")]
        [AllowAnonymous]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Email))
                {
                    return BadRequest("Debe proporcionar un email válido");
                }

                // Buscar usuario por email
                ApplicationUser user = await _userManager.FindByEmailAsync(request.Email);

                if (user == null)
                {
                    // Por seguridad, siempre devolvemos éxito aunque el usuario no exista
                    return Ok("Si el email está registrado, recibirá un correo con la nueva contraseña");
                }

                // Generar nueva contraseña aleatoria
                string newPassword = GenerateRandomPassword();

                var strategy = _EasyParkingAuthContext.Database.CreateExecutionStrategy();
                try
                {
                    var result = await strategy.ExecuteAsync<ActionResult>(async () =>
                    {
                        using (var transaction = _EasyParkingAuthContext.Database.BeginTransaction())
                        {
                            try
                            {
                                // Generar token de reseteo
                                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                                // Cambiar la contraseña directamente
                                var resetResult = await _userManager.ResetPasswordAsync(user, token, newPassword);

                                if (!resetResult.Succeeded)
                                {
                                    return BadRequest("Error al generar la nueva contraseña");
                                }

                                // Enviar email con la nueva contraseña
                                bool emailSent = await SendPasswordResetEmail(user.Email, user.Nombre ?? user.UserName, newPassword);

                                if (!emailSent)
                                {
                                    // Aunque el email falle, la contraseña ya fue cambiada
                                    return Ok("Contraseña actualizada pero hubo un error al enviar el email. Contacte al administrador.");
                                }

                                _EasyParkingAuthContext.Database.CommitTransaction();
                                return Ok("Se ha enviado un correo con su nueva contraseña");
                            }
                            catch (Exception ex)
                            {
                                _EasyParkingAuthContext.Database.RollbackTransaction();
                                return BadRequest("ERROR ... " + ex.Message + (ex.InnerException?.Message ?? ""));
                            }
                        }
                    });
                    return result;
                }
                catch (Exception ex)
                {
                    _EasyParkingAuthContext.Database.RollbackTransaction();
                    return BadRequest("ERROR ... " + ex.Message);
                }


            }
            catch (Exception ex)
            {
                _EasyParkingAuthContext.Database.RollbackTransaction();
                return BadRequest($"Error al procesar la solicitud: {ex.Message}");
            }
        }

        // ============================================
        // 2. MÉTODO PARA ENVIAR EMAIL (en AccountController.cs)
        // ============================================

        private async Task<bool> SendPasswordResetEmail(string toEmail, string userName, string newPassword)
        {
            try
            {
                using (var smtpClient = new System.Net.Mail.SmtpClient(_From_SmtpServer))
                {
                    // Configuración correcta para Gmail
                    smtpClient.Port = 587; // Puerto TLS
                    smtpClient.EnableSsl = true;
                    smtpClient.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new System.Net.NetworkCredential(_From_EmailAdress, _From_EmailPassword);
                    smtpClient.Timeout = 20000; // 20 segundos

                    var mailMessage = new System.Net.Mail.MailMessage
                    {
                        From = new System.Net.Mail.MailAddress(_From_EmailAdress, _From_Name),
                        Subject = "Recuperación de Contraseña - EasyParking",
                        Body = GenerateEmailBody(userName, newPassword),
                        IsBodyHtml = true,
                        Priority = System.Net.Mail.MailPriority.High
                    };

                    mailMessage.To.Add(toEmail);

                    await smtpClient.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar email: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return false;
            }
        }        // ============================================
        // 3. PLANTILLA HTML DEL EMAIL (en AccountController.cs)
        // ============================================

        private string GenerateEmailBody(string userName, string newPassword)
        {
            return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <style>
                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                .content {{ background-color: #f9f9f9; padding: 30px; border: 1px solid #ddd; }}
                .password-box {{ background-color: #fff; border: 2px solid #4CAF50; padding: 20px; margin: 20px 0; text-align: center; border-radius: 5px; }}
                .password {{ font-size: 24px; font-weight: bold; color: #4CAF50; letter-spacing: 2px; }}
                .footer {{ background-color: #f1f1f1; padding: 15px; text-align: center; font-size: 12px; color: #666; border-radius: 0 0 5px 5px; }}
                .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1>🅿️ EasyParking</h1>
                    <p>Recuperación de Contraseña</p>
                </div>
                <div class='content'>
                    <h2>Hola, {userName}</h2>
                    <p>Has solicitado restablecer tu contraseña. Tu nueva contraseña temporal es:</p>
                    
                    <div class='password-box'>
                        <div class='password'>{newPassword}</div>
                    </div>

                    <div class='warning'>
                        <strong>⚠️ Importante:</strong>
                        <ul style='margin: 10px 0; padding-left: 20px;'>
                            <li>Esta es tu nueva contraseña temporal</li>
                            <li>Te recomendamos cambiarla después de iniciar sesión</li>
                            <li>No compartas esta contraseña con nadie</li>
                        </ul>
                    </div>

                    <p>Si no solicitaste este cambio, contacta inmediatamente con nuestro equipo de soporte.</p>
                </div>
                <div class='footer'>
                    <p>Este es un correo automático, por favor no respondas a este mensaje.</p>
                    <p>&copy; 2025 EasyParking. Todos los derechos reservados.</p>
                </div>
            </div>
        </body>
        </html>
    ";
        }

        // ============================================
        // 5. ACTUALIZACIÓN DEL MÉTODO GenerateRandomPassword
        // ============================================

        private string GenerateRandomPassword(PasswordOptions opts = null)
        {
            if (opts == null) opts = new PasswordOptions()
            {
                RequiredLength = _identityOptions.Value.Password.RequiredLength,
                RequiredUniqueChars = _identityOptions.Value.Password.RequiredUniqueChars,
                RequireDigit = _identityOptions.Value.Password.RequireDigit,
                RequireLowercase = _identityOptions.Value.Password.RequireLowercase,
                RequireNonAlphanumeric = _identityOptions.Value.Password.RequireNonAlphanumeric,
                RequireUppercase = _identityOptions.Value.Password.RequireUppercase
            };

            //        string[] randomChars = new[]
            //        {
            //    "ABCDEFGHJKLMNOPQRSTUVWXYZ",    // uppercase 
            //    "abcdefghijkmnopqrstuvwxyz",    // lowercase
            //    "0123456789",                   // digits
            //    "!@$?_-"                        // non-alphanumeric
            //};


            string[] randomChars = new[]
            {
                    //"ABCDEFGHJKLMNOPQRSTUVWXYZ",    // uppercase 
                    "abcdefghijkmnopqrstuvwxyz",    // lowercase
                    "0123456789"                   // digits
            };

            Random rand = new Random(Environment.TickCount);
            List<char> chars = new List<char>();

            if (opts.RequireUppercase)
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[0][rand.Next(0, randomChars[0].Length)]);

            if (opts.RequireLowercase)
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[1][rand.Next(0, randomChars[1].Length)]);

            if (opts.RequireDigit)
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[2][rand.Next(0, randomChars[2].Length)]);

            if (opts.RequireNonAlphanumeric)
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[3][rand.Next(0, randomChars[3].Length)]);

            for (int i = chars.Count; i < opts.RequiredLength
                || chars.Distinct().Count() < opts.RequiredUniqueChars; i++)
            {
                string rcs = randomChars[rand.Next(0, randomChars.Length)];
                chars.Insert(rand.Next(0, chars.Count),
                    rcs[rand.Next(0, rcs.Length)]);
            }

            return new string(chars.ToArray());
        }


        // ============================================
        // 1 MÉTODO PARA CAMBIAR CONTRASEÑA DEL USUARIO AUTENTICADO
        // ============================================

        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult> ChangeUserPassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                // Validar que vengan los datos necesarios
                if (string.IsNullOrWhiteSpace(request?.CurrentPassword) ||
                    string.IsNullOrWhiteSpace(request?.NewPassword))
                {
                    return BadRequest("Debe proporcionar la contraseña actual y la nueva contraseña");
                }

                // Obtener el usuario autenticado desde el token
                var userIdentity = _httpContextAccessor.HttpContext.User;
                ApplicationUser user = await _userManager.FindByNameAsync(userIdentity.Identity.Name);

                if (user == null)
                {
                    return NotFound("Usuario no encontrado");
                }

                // Verificar que la contraseña actual sea correcta
                var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, request.CurrentPassword, false);

                if (!passwordCheck.Succeeded)
                {
                    return BadRequest("La contraseña actual es incorrecta");
                }

                // Validar que la nueva contraseña no sea igual a la actual
                if (request.CurrentPassword == request.NewPassword)
                {
                    return BadRequest("La nueva contraseña debe ser diferente a la actual");
                }

                // Cambiar la contraseña
                var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

                if (!result.Succeeded)
                {
                    // Retornar los errores específicos de validación de contraseña
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest($"Error al cambiar la contraseña: {errors}");
                }

                // Opcional: Enviar email de notificación
                try
                {
                    await SendPasswordChangedNotification(user.Email, user.Nombre ?? user.UserName);
                }
                catch (Exception emailEx)
                {
                    // Si falla el email, no afecta el cambio de contraseña
                    Console.WriteLine($"Error al enviar notificación: {emailEx.Message}");
                }

                return Ok("Contraseña cambiada exitosamente");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al cambiar la contraseña: {Tools.Tools.ExceptionMessage(ex)}");
            }
        }

        // ============================================
        // 2 MÉTODO AUXILIAR PARA ENVIAR NOTIFICACIÓN
        // ============================================

        private async Task<bool> SendPasswordChangedNotification(string toEmail, string userName)
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
                        Subject = "Contraseña Actualizada - EasyParking",
                        Body = GeneratePasswordChangedEmailBody(userName),
                        IsBodyHtml = true,
                        Priority = System.Net.Mail.MailPriority.Normal
                    };

                    mailMessage.To.Add(toEmail);

                    await smtpClient.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar email: {ex.Message}");
                return false;
            }
        }

        private string GeneratePasswordChangedEmailBody(string userName)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border: 1px solid #ddd; }}
        .info-box {{ background-color: #e8f5e9; border-left: 4px solid #4CAF50; padding: 15px; margin: 20px 0; }}
        .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
        .footer {{ background-color: #f1f1f1; padding: 15px; text-align: center; font-size: 12px; color: #666; border-radius: 0 0 5px 5px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🅿️ EasyParking</h1>
            <p>Notificación de Seguridad</p>
        </div>
        <div class='content'>
            <h2>Hola, {userName}</h2>
            
            <div class='info-box'>
                <strong>✓ Contraseña Actualizada</strong>
                <p style='margin: 10px 0 0 0;'>Tu contraseña ha sido cambiada exitosamente el {DateTime.Now:dd/MM/yyyy} a las {DateTime.Now:HH:mm}.</p>
            </div>

            <div class='warning'>
                <strong>⚠️ ¿No fuiste tú?</strong>
                <p style='margin: 10px 0 0 0;'>Si no realizaste este cambio, contacta inmediatamente con nuestro equipo de soporte para proteger tu cuenta.</p>
            </div>

            <p>Gracias por usar EasyParking.</p>
        </div>
        <div class='footer'>
            <p>Este es un correo automático, por favor no respondas a este mensaje.</p>
            <p>&copy; 2025 EasyParking. Todos los derechos reservados.</p>
        </div>
    </div>
</body>
</html>";
        }

        // ------------------------------------------------------------------

        // ============================================
        // 1. MÉTODO CreateUser MEJORADO - VERSIÓN CORREGIDA
        // ============================================

        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult> CreateUser([FromBody] UserInfo userinfo)
        {
            if (ModelState.IsValid)
            {
                string estado = "Iniciando";
                if (userinfo == null)
                {
                    return BadRequest("ERROR. Datos No Válidos ...");
                }

                var strategy = _EasyParkingAuthContext.Database.CreateExecutionStrategy();
                try
                {
                    var result = await strategy.ExecuteAsync<ActionResult>(async () =>
                    {
                        using (var transaction = _EasyParkingAuthContext.Database.BeginTransaction())
                        {
                            try
                            {
                                estado = "Creando Usuario";
                                var appuser = new ApplicationUser
                                {
                                    UserName = userinfo.Email.ToLower(),
                                    Email = userinfo.Email.ToLower(),
                                    Apellido = userinfo.Apellido,
                                    Nombre = userinfo.Nombre,
                                    FechaDeNacimiento = userinfo.FechaDeNacimiento,
                                    NumeroDeDocumento = userinfo.NumeroDeDocumento,
                                    TipoDeDocumento = userinfo.TipoDeDocumento,
                                    Telefono = userinfo.Telefono,
                                    Sexo = userinfo.Sexo,
                                    EmailConfirmed = false,
                                    LockoutEnabled = true,
                                    LockoutEnd = DateTimeOffset.MaxValue
                                };

                                var createResult = await _userManager.CreateAsync(appuser, userinfo.Password);
                                if (createResult.Succeeded)
                                {
                                    // Guardar foto temporalmente si viene
                                    if (userinfo.FotoDePerfil != null && userinfo.FotoDePerfil.Length > 0)
                                    {
                                        var folder = _configuration.GetValue<string>("EasyParkingAPI:Images:Usuarios_Folder");

                                        if (string.IsNullOrEmpty(folder))
                                            return BadRequest("Ruta de almacenamiento no configurada");

                                        if (!Directory.Exists(folder))
                                            Directory.CreateDirectory(folder);

                                        var fileName = appuser.Id + ".jpg";
                                        var path = Path.Combine(folder, fileName);

                                        await System.IO.File.WriteAllBytesAsync(path, userinfo.FotoDePerfil);

                                        appuser.Link_Foto = $"http://40.118.242.96:12595/images/usuarios/{fileName}";
                                        await _userManager.UpdateAsync(appuser);
                                    }

                                    estado = "Adhiriendo Usuario a Rol";
                                    var roleResult = await _userManager.AddToRoleAsync(appuser, "AppUser");
                                    if (roleResult.Succeeded)
                                    {
                                        estado = "Generando Token de Confirmación";
                                        var token = await _userManager.GenerateEmailConfirmationTokenAsync(appuser);

                                        // OPCIÓN 1: Si tienes frontend separado
                                        // var callbackUrl = $"http://tu-frontend.com/confirm-email?userId={appuser.Id}&token={System.Net.WebUtility.UrlEncode(token)}";

                                        // OPCIÓN 2: Usando el mismo backend (RECOMENDADO)
                                        var callbackUrl = $"http://40.118.242.96:12595/api/Account/ConfirmEmail?userId={appuser.Id}&token={System.Net.WebUtility.UrlEncode(token)}";

                                        estado = "Enviando Email de Confirmación";
                                        bool emailSent = await SendConfirmationEmail(appuser.Email, appuser.Nombre ?? appuser.UserName, callbackUrl);

                                        if (!emailSent)
                                        {
                                            await _userManager.DeleteAsync(appuser);
                                            _EasyParkingAuthContext.Database.RollbackTransaction();
                                            return BadRequest("Error al enviar el correo de confirmación. Por favor, intente nuevamente.");
                                        }

                                        _EasyParkingAuthContext.Database.CommitTransaction();
                                        return Ok(new
                                        {
                                            message = "Usuario registrado. Por favor, revisa tu correo electrónico para confirmar tu cuenta.",
                                            userId = appuser.Id,
                                            email = appuser.Email
                                        });
                                    }
                                    else
                                    {
                                        throw new Exception(roleResult.Errors.FirstOrDefault()?.Description ?? "Error al asignar rol");
                                    }
                                }
                                else
                                {
                                    _EasyParkingAuthContext.Database.RollbackTransaction();
                                    return BadRequest(createResult.Errors.ToList());
                                }
                            }
                            catch (Exception ex)
                            {
                                _EasyParkingAuthContext.Database.RollbackTransaction();
                                return BadRequest("ERROR ... " + estado + " - Error message: " + ex.Message + (ex.InnerException?.Message ?? ""));
                            }
                        }
                    });
                    return result;
                }
                catch (Exception ex)
                {
                    return BadRequest("ERROR ... " + ex.Message);
                }
            }
            else
            {
                return BadRequest(ModelState);
            }
        }

        // ============================================
        // 2. MÉTODO PARA ENVIAR EMAIL DE CONFIRMACIÓN
        // ============================================

        private async Task<bool> SendConfirmationEmail(string toEmail, string userName, string confirmationUrl)
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
                        Subject = "Confirma tu cuenta - EasyParking",
                        Body = GenerateConfirmationEmailBody(userName, confirmationUrl),
                        IsBodyHtml = true,
                        Priority = System.Net.Mail.MailPriority.High
                    };

                    mailMessage.To.Add(toEmail);

                    await smtpClient.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar email de confirmación: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        // ============================================
        // 3. PLANTILLA HTML DEL EMAIL DE CONFIRMACIÓN
        // ============================================

        private string GenerateConfirmationEmailBody(string userName, string confirmationUrl)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border: 1px solid #ddd; }}
        .button-container {{ text-align: center; margin: 30px 0; }}
        .confirm-button {{ 
            display: inline-block;
            padding: 15px 40px;
            background-color: #4CAF50;
            color: white;
            text-decoration: none;
            border-radius: 5px;
            font-size: 18px;
            font-weight: bold;
        }}
        .confirm-button:hover {{ background-color: #45a049; }}
        .info-box {{ background-color: #e3f2fd; border-left: 4px solid #2196F3; padding: 15px; margin: 20px 0; }}
        .footer {{ background-color: #f1f1f1; padding: 15px; text-align: center; font-size: 12px; color: #666; border-radius: 0 0 5px 5px; }}
        .warning {{ color: #666; font-size: 14px; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🅿️ EasyParking</h1>
            <p>Bienvenido a EasyParking</p>
        </div>
        <div class='content'>
            <h2>¡Hola, {userName}!</h2>
            <p>Gracias por registrarte en EasyParking. Estás a un paso de completar tu registro.</p>
            
            <div class='info-box'>
                <strong>📧 Confirma tu correo electrónico</strong>
                <p style='margin: 10px 0 0 0;'>Para activar tu cuenta y poder iniciar sesión, necesitamos que confirmes tu dirección de correo electrónico.</p>
            </div>

            <div class='button-container'>
                <a href='{confirmationUrl}' class='confirm-button'>Confirmar mi cuenta</a>
            </div>

            <div class='warning'>
                <p><strong>⚠️ Importante:</strong></p>
                <ul style='margin: 10px 0; padding-left: 20px;'>
                    <li>Este enlace es válido por 24 horas</li>
                    <li>Si no solicitaste este registro, ignora este mensaje</li>
                    <li>No compartas este enlace con nadie</li>
                </ul>
            </div>

            <p style='margin-top: 30px; font-size: 14px; color: #666;'>
                Si el botón no funciona, copia y pega el siguiente enlace en tu navegador:<br>
                <span style='color: #4CAF50; word-break: break-all;'>{confirmationUrl}</span>
            </p>
        </div>
        <div class='footer'>
            <p>Este es un correo automático, por favor no respondas a este mensaje.</p>
            <p>&copy; 2025 EasyParking. Todos los derechos reservados.</p>
        </div>
    </div>
</body>
</html>";
        }

        // ============================================
        // 4. MÉTODO PARA CONFIRMAR EMAIL - VERSIÓN MEJORADA
        // ============================================

        [HttpGet]
        [AllowAnonymous]
        [Route("[action]")]
        public async Task<ActionResult> ConfirmEmail(string userId, string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
                {
                    return BadRequest("Parámetros de confirmación inválidos");
                }

                ApplicationUser user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    return BadRequest("Usuario no encontrado");
                }

                if (user.EmailConfirmed)
                {
                    // Retornar HTML en lugar de texto plano
                    return Content(GenerateSuccessHtml("Tu cuenta ya ha sido confirmada anteriormente. Puedes iniciar sesión."), "text/html");
                }

                IdentityResult result = await _userManager.ConfirmEmailAsync(user, token);

                if (result.Succeeded)
                {
                    // Desbloquear el usuario después de confirmar el email
                    await _userManager.SetLockoutEndDateAsync(user, null);

                    // Retornar página HTML de éxito
                    return Content(GenerateSuccessHtml("¡Cuenta confirmada exitosamente! Ya puedes iniciar sesión en EasyParking."), "text/html");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Content(GenerateErrorHtml($"Error al confirmar cuenta: {errors}"), "text/html");
                }
            }
            catch (Exception ex)
            {
                return Content(GenerateErrorHtml($"Error al confirmar cuenta: {ex.Message}"), "text/html");
            }
        }

        // ============================================
        // 5. PÁGINA HTML DE ÉXITO
        // ============================================

        private string GenerateSuccessHtml(string message)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Confirmación Exitosa - EasyParking</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            justify-content: center;
            align-items: center;
            margin: 0;
            padding: 20px;
        }}
        .container {{
            background: white;
            border-radius: 10px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.1);
            max-width: 500px;
            width: 100%;
            padding: 40px;
            text-align: center;
        }}
        .success-icon {{
            width: 80px;
            height: 80px;
            background: #4CAF50;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            margin: 0 auto 20px;
            animation: scaleIn 0.5s ease-out;
        }}
        .success-icon::after {{
            content: '✓';
            font-size: 50px;
            color: white;
            font-weight: bold;
        }}
        h1 {{
            color: #333;
            margin-bottom: 20px;
            font-size: 28px;
        }}
        p {{
            color: #666;
            font-size: 16px;
            line-height: 1.6;
            margin-bottom: 30px;
        }}
        .button {{
            display: inline-block;
            background: #4CAF50;
            color: white;
            padding: 12px 30px;
            text-decoration: none;
            border-radius: 5px;
            font-weight: bold;
            transition: background 0.3s;
        }}
        .button:hover {{
            background: #45a049;
        }}
        @keyframes scaleIn {{
            from {{
                transform: scale(0);
                opacity: 0;
            }}
            to {{
                transform: scale(1);
                opacity: 1;
            }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='success-icon'></div>
        <h1>🅿️ EasyParking</h1>
        <h2>¡Éxito!</h2>
        <p>{message}</p>
        <a href='#' class='button' onclick='closeWindow()'>Cerrar</a>
    </div>
    <script>
        function closeWindow() {{
            window.close();
            // Si no se puede cerrar, mostrar mensaje
            setTimeout(() => {{
                alert('Puedes cerrar esta ventana y proceder a iniciar sesión en la aplicación.');
            }}, 100);
        }}
    </script>
</body>
</html>";
        }

        // ============================================
        // 6. PÁGINA HTML DE ERROR
        // ============================================

        private string GenerateErrorHtml(string message)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Error - EasyParking</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            justify-content: center;
            align-items: center;
            margin: 0;
            padding: 20px;
        }}
        .container {{
            background: white;
            border-radius: 10px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.1);
            max-width: 500px;
            width: 100%;
            padding: 40px;
            text-align: center;
        }}
        .error-icon {{
            width: 80px;
            height: 80px;
            background: #f44336;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            margin: 0 auto 20px;
            animation: shake 0.5s ease-out;
        }}
        .error-icon::after {{
            content: '✕';
            font-size: 50px;
            color: white;
            font-weight: bold;
        }}
        h1 {{
            color: #333;
            margin-bottom: 20px;
            font-size: 28px;
        }}
        p {{
            color: #666;
            font-size: 16px;
            line-height: 1.6;
            margin-bottom: 30px;
        }}
        .button {{
            display: inline-block;
            background: #f44336;
            color: white;
            padding: 12px 30px;
            text-decoration: none;
            border-radius: 5px;
            font-weight: bold;
            transition: background 0.3s;
        }}
        .button:hover {{
            background: #da190b;
        }}
        @keyframes shake {{
            0%, 100% {{ transform: translateX(0); }}
            25% {{ transform: translateX(-10px); }}
            75% {{ transform: translateX(10px); }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='error-icon'></div>
        <h1>🅿️ EasyParking</h1>
        <h2>Error</h2>
        <p>{message}</p>
        <a href='#' class='button' onclick='closeWindow()'>Cerrar</a>
    </div>
    <script>
        function closeWindow() {{
            window.close();
            setTimeout(() => {{
                alert('Puedes cerrar esta ventana. Si el problema persiste, contacta con soporte.');
            }}, 100);
        }}
    </script>
</body>
</html>";
        }

        // ============================================
        // 7. MÉTODO PARA REENVIAR EMAIL DE CONFIRMACIÓN
        // ============================================

        [HttpPost]
        [Route("[action]")]
        [AllowAnonymous]
        public async Task<ActionResult> ResendConfirmationEmail([FromBody] ResendEmailRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Email))
                {
                    return BadRequest("Debe proporcionar un email válido");
                }

                ApplicationUser user = await _userManager.FindByEmailAsync(request.Email);

                if (user == null)
                {
                    return Ok("Si el email está registrado y no confirmado, recibirá un correo de confirmación");
                }

                if (user.EmailConfirmed)
                {
                    return Ok("Esta cuenta ya ha sido confirmada. Puedes iniciar sesión.");
                }

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = $"http://40.118.242.96:12595/api/Account/ConfirmEmail?userId={user.Id}&token={System.Net.WebUtility.UrlEncode(token)}";

                bool emailSent = await SendConfirmationEmail(user.Email, user.Nombre ?? user.UserName, callbackUrl);

                if (!emailSent)
                {
                    return BadRequest("Error al enviar el correo. Por favor, intente más tarde.");
                }

                return Ok("Correo de confirmación reenviado. Por favor, revisa tu bandeja de entrada.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

        // ============================================
        // MÉTODO Login MEJORADO
        // Reemplaza tu método Login actual con este
        // ============================================

        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult> Login([FromBody] UserInfo userInfo)
        {
            try
            {
                // Primero buscamos al usuario
                ApplicationUser user = await _userManager.FindByNameAsync(userInfo.UserName);

                if (user == null)
                {
                    return BadRequest("Usuario o contraseña incorrectos");
                }

                // Verificamos si el email está confirmado
                if (!user.EmailConfirmed)
                {
                    return BadRequest(new
                    {
                        error = "EmailNotConfirmed",
                        message = "Debes confirmar tu correo electrónico antes de iniciar sesión. Revisa tu bandeja de entrada.",
                        email = user.Email
                    });
                }

                // Intentamos el login
                var result = await _signInManager.PasswordSignInAsync(userInfo.UserName, userInfo.Password, isPersistent: false, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    IList<String> roles = await _userManager.GetRolesAsync(user);
                    return BuildToken(user, roles);
                }
                else if (result.IsLockedOut)
                {
                    return BadRequest("Tu cuenta está bloqueada. Contacta con el administrador.");
                }
                else
                {
                    return BadRequest("Usuario o contraseña incorrectos");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(ex));
            }
        }
    }
}