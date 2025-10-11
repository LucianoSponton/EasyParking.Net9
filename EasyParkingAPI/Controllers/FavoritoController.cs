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
    public class FavoritoController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _UserId;
        private readonly UserManager<ApplicationUser> _userManager;

        public FavoritoController(IConfiguration configuration,
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
        public async Task<ActionResult<List<Favorito>>> GetAllAsync()
        {
            try
            {
                DataContext dataContext = new DataContext();
                var lista = await dataContext.Favoritos.AsNoTracking().ToListAsync();
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
        public async Task<ActionResult<List<ServiceWebApi.DTO.EstacionamientoDTO>>> GetMisFavoritosAsync()
        {
            try
            {
                await using var dataContext = new DataContext();

                var estacionamientos = await (
                    from est in dataContext.Estacionamientos
                        .Include(e => e.TiposDeVehiculosAdmitidos)
                        .Where(x => !x.Inactivo && !x.PublicacionPausada)
                    join fav in dataContext.Favoritos
                        .Where(x => x.UserId == _UserId)
                    on est.Id equals fav.EstacionamientoId
                    select new ServiceWebApi.DTO.EstacionamientoDTO
                    {
                        Id = est.Id,
                        UserId = est.UserId,
                        Ciudad = est.Ciudad,
                        Nombre = est.Nombre,
                        Direccion = est.Direccion,
                        Latitud = est.Latitud,
                        Longitud = est.Longitud,
                        TipoDeLugar = est.TipoDeLugar,
                        MontoReserva = est.MontoReserva,
                        URL = est.URL,
                        Observaciones = est.Observaciones,
                        TiposDeVehiculosAdmitidos = est.TiposDeVehiculosAdmitidos, // si necesitas proyectar más profundo conviene un mapper
                        Favorito = true
                    }
                ).AsNoTracking().ToListAsync();

                if (estacionamientos == null || estacionamientos.Count == 0)
                {
                    return NotFound("No tienes favoritos.");
                }

                return estacionamientos;
            }
            catch (Exception e)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }


        [HttpGet("[action]/{estacionamientoId}")]
        [Route("[action]")]
        public async Task<ActionResult<bool>> IsFavoriteAsync(int estacionamientoId)
        {
            try
            {
                DataContext dataContext = new DataContext();

                bool result = dataContext.Favoritos.Any(x => x.UserId == _UserId && x.EstacionamientoId == estacionamientoId);

                return result;
            }
            catch (Exception e)
            {

                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }

        [HttpPost]
        [Route("[action]")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, AppUser")]
        public async Task<ActionResult> AddAsync([FromBody] int estacionamientoId)
        {
            try
            {
                DataContext dataContext = new DataContext();
                Favorito favorito = new Favorito();
                favorito.EstacionamientoId = estacionamientoId;
                favorito.UserId = _UserId;
                await dataContext.Favoritos.AddAsync(favorito);
                await dataContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }


        [HttpDelete("[action]/{estacionamientoId}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, AppUser")]
        public async Task<ActionResult> DeleteAsync(int estacionamientoId)
        {
            try
            {
                DataContext dataContext = new DataContext();
                var favorito = await dataContext.Favoritos.FirstOrDefaultAsync(x => x.EstacionamientoId == estacionamientoId && x.UserId == _UserId);
                dataContext.Favoritos.Remove(favorito);
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
