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
using ServiceWebApi.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace EasyParkingAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class BloqueoPlazaController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _UserId;
        private readonly UserManager<ApplicationUser> _userManager;

        public BloqueoPlazaController(IConfiguration configuration,
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
            catch (Exception)
            {
                throw;
            }
        }


        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<BloqueoPlaza>>> GetMisBloqueos()
        {
            try
            {
                DataContext dataContext = new DataContext();
                var lista = await dataContext.BloqueoPlazas.Where(x=> x.UserId == _UserId && x.Activo == true && x.FechaInicio > DateTime.Now).ToListAsync();

                if (lista == null || !lista.Any())
                {
                    return NoContent();
                }
                else
                {
                    return lista;
                }
            }
            catch (Exception e)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }


        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult> AddAsync([FromBody] BloqueoPlaza  bloqueoPlaza)
        {
            try
            {
                DataContext dataContext = new DataContext();
                bloqueoPlaza.UserId = _UserId;
                await dataContext.BloqueoPlazas.AddAsync(bloqueoPlaza);
                await dataContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult> AddMultipleAsync([FromBody] List<BloqueoPlaza> bloqueoPlazas)
        {
            try
            {
                DataContext dataContext = new DataContext();

                foreach (var item in bloqueoPlazas)
                {
                    item.UserId = _UserId;
                }

                dataContext.BloqueoPlazas.AddRange(bloqueoPlazas);
                await dataContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }


        [HttpDelete("[action]/{bloqueoPlazaId}")]
        public async Task<ActionResult> DeleteAsync(int bloqueoPlazaId)
        {
            try
            {
                DataContext dataContext = new DataContext();
                var bloqueoPlaza = await dataContext.BloqueoPlazas.FirstOrDefaultAsync(x => x.Id == bloqueoPlazaId && x.UserId == _UserId);
                dataContext.BloqueoPlazas.Remove(bloqueoPlaza);
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
