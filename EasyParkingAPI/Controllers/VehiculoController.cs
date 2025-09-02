using EasyParkingAPI.Data;
using EasyParkingAPI.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace EasyParkingAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class VehiculoController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _UserId;
        private readonly UserManager<ApplicationUser> _userManager;

        public VehiculoController(IConfiguration configuration,
                                            IHttpContextAccessor httpContextAccessor,
                                            UserManager<ApplicationUser> userManager)
        {
            try
            {
                _configuration = configuration;
                _userManager = userManager;
                HttpContext http = httpContextAccessor.HttpContext;
                var user = http.User;

                ApplicationUser appuser = _userManager.FindByNameAsync(user.Identity.Name).Result; // Obtengo los datos del usuario logeado

                _UserId = appuser.Id; // Obtengo el ID del usuario logeado

                if (String.IsNullOrEmpty(_UserId) | String.IsNullOrWhiteSpace(_UserId))
                {
                    throw new Exception("ERROR ... Usuario sin permisos necesarios.");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<Vehiculo>>> GetAllAsync()
        {
            try
            {
                DataContext dataContext = new DataContext();
                var lista = await dataContext.Vehiculos.AsNoTracking().ToListAsync();
                if (lista == null)
                {
                    return NotFound();
                }
                return lista;

            }
            catch (Exception e)
            {

                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<Vehiculo>>> GetMisVehiculosAsync()
        {
            try
            {
                DataContext dataContext = new DataContext();


                var vehiculos = await dataContext.Vehiculos.Where(x => x.UserId == _UserId).AsNoTracking().ToListAsync();

                if (vehiculos == null)
                {
                    return NotFound();
                }

                return vehiculos;
            }
            catch (Exception e)
            {

                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }

        [HttpGet("[action]/{patente}")]
        [Route("[action]")]
        public async Task<ActionResult<Vehiculo>> GetVehiculoByPatente(string patente)
        {
            try
            {
                DataContext dataContext = new DataContext();


                var vehiculo = await dataContext.Vehiculos.Where(x => x.UserId == _UserId && x.Patente == patente).AsNoTracking().FirstOrDefaultAsync();

                if (vehiculo == null)
                {
                    return NotFound();
                }

                return vehiculo;
            }
            catch (Exception e)
            {

                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<Vehiculo>> GetFirstAsync()
        {
            try
            {
                DataContext dataContext = new DataContext();
                var vehiculo = await dataContext.Vehiculos.Where(x => x.UserId == _UserId).AsNoTracking().FirstOrDefaultAsync();

                if (vehiculo == null)
                {
                    return NotFound();
                }
                return vehiculo;

            }
            catch (Exception e)
            {

                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }


        [HttpPost]
        [Route("[action]")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, AppUser")]
        public async Task<ActionResult> AddAsync([FromBody] Vehiculo vehiculo)
        {
            try
            {
                DataContext dataContext = new DataContext();
                vehiculo.UserId = _UserId;
                await dataContext.Vehiculos.AddAsync(vehiculo);
                await dataContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }


        [HttpDelete("[action]/{vehiculoId}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, AppUser")]
        public async Task<ActionResult> DeleteAsync(int vehiculoId)
        {
            try
            {
                DataContext dataContext = new DataContext();
                var vehiculo = await dataContext.Vehiculos.FirstOrDefaultAsync(x => x.Id == vehiculoId && x.UserId == _UserId);
                dataContext.Vehiculos.Remove(vehiculo);
                await dataContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }
    }


}
