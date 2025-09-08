using EasyParkingAPI.Data;
using EasyParkingAPI.Model;
using LSEmailSender.MailKitEmailSender;
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
                                    EmailConfirmed = true
                                };

                                var result = await _userManager.CreateAsync(appuser, userinfo.Password);
                                if (result.Succeeded)
                                {
                                    // Guardar foto si viene
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

                                        // Actualizar Link_Foto con la URL pública
                                        appuser.Link_Foto = $"http://40.118.242.96:12595/images/usuarios/{fileName}";
                                        await _userManager.UpdateAsync(appuser);
                                    }

                                    estado = "Adhiriendo Usuario a Rol";
                                    var result02 = await _userManager.AddToRoleAsync(appuser, "AppUser");
                                    if (result02.Succeeded)
                                    {
                                        if (!appuser.EmailConfirmed)
                                        {
                                            estado = "Enviando eMail de Confirmacion";
                                            var token = await _userManager.GenerateEmailConfirmationTokenAsync(appuser);
                                            var callbackUrl = new Uri(Url.Link("ConfirmEmailRoute", new { userId = appuser.Id, token = token }));
                                            Sender mailKit = new Sender(_From_SmtpServer, _From_SmtpServerPort, true, _From_Name, _From_EmailAdress, _From_EmailPassword);
                                            mailKit.Send(appuser.UserName, appuser.Email, "Confirma tu Cuenta",
                                                $"<h2>{appuser.UserName}</h2>" + Environment.NewLine +
                                                $"<a href=\"{callbackUrl}\"> Por favor confirme su cuenta haciendo click aqui. </a>");
                                        }

                                        _EasyParkingAuthContext.Database.CommitTransaction();
                                        return Ok(new { appuser.Id, appuser.Link_Foto });
                                    }
                                    else
                                    {
                                        throw new Exception(result02.Errors.ToString());
                                    }
                                }
                                else
                                {
                                    _EasyParkingAuthContext.Database.RollbackTransaction();
                                    return BadRequest(result.Errors.ToList());
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

        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult> Login([FromBody] UserInfo userInfo)
        {
            try
            {
                var result = _signInManager.PasswordSignInAsync(userInfo.UserName, userInfo.Password, isPersistent: false, lockoutOnFailure: false).Result;
                if (result.Succeeded)
                {
                    ApplicationUser user = await _userManager.FindByNameAsync(userInfo.UserName);
                    IList<String> roles = await _userManager.GetRolesAsync(user);
                    return BuildToken(user, roles);

                }
                else
                {
                    return BadRequest("Intento de login NO VALIDO ...");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(ex));
            }
        }

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

        [HttpGet]
        [AllowAnonymous]
        [Route("[action]", Name = "ConfirmEmailRoute")]
        public ActionResult ConfirmEmail(string userid, string token)
        {
            ApplicationUser user = _userManager.FindByIdAsync(userid).Result;
            IdentityResult result = _userManager.ConfirmEmailAsync(user, token).Result;
            if (result.Succeeded)
            {
                return Ok("Cuenta confirmada ...");
            }
            else
            {
                return BadRequest("Error al intentar confirmar Cuenta ...");
            }
        }

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

        [HttpPost("[action]/{UserName}")]
        public async Task<ActionResult> ResetPassword(string UserName)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser user = null;
                user = _userManager.FindByNameAsync(UserName).Result;
                if (!(user == null))
                {
                    PasswordOptions passwordOptions = new PasswordOptions();
                    string newpassword = GenerateRandomPassword();
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var callbackUrl = new Uri(Url.Link("ConfirmPasswordResetRoute", new { userId = user.Id, token = token, newpassword = newpassword }));


                    //Sender mailKit = new Sender(_From_SmtpServer, _From_SmtpServerPort, _From_Name, _From_EmailAdress, _From_EmailPassword);
                    Sender mailKit = new Sender(_From_SmtpServer, _From_SmtpServerPort, true, _From_Name, _From_EmailAdress, _From_EmailPassword);

                    mailKit.Send(user.UserName, user.Email, "Cambio de Contraseña",
                        $"<p> Esta es su nueva contraseña: </p><h2>" + newpassword + "</h2>" + Environment.NewLine +
                        "<a href=\"" + callbackUrl + "\"> Por favor confirme el cambio haciendo click aqui. </a>");
                    return Ok();
                }
                else
                {
                    return BadRequest("Username invalid");
                }
            }
            else
            {
                return BadRequest(ModelState);
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

            string[] randomChars = new[]
            {
                "ABCDEFGHJKLMNOPQRSTUVWXYZ",    // uppercase 
                "abcdefghijkmnopqrstuvwxyz",    // lowercase
                "0123456789",                   // digits
                "!@$?_-"                        // non-alphanumeric
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, AppUser")]
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

    }
}