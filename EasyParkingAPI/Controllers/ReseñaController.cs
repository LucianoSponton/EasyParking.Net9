using EasyParkingAPI.Data;
using EasyParkingAPI.DTO;
using EasyParkingAPI.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Model;
using ServiceWebApi.DTO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EasyParkingAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class ReseñaController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _UserId;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReseñaController(IConfiguration configuration,
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
        [Route("[action]/{estacionamientoId}")]
        public async Task<ActionResult<List<ReseñaDTO>>> GetByEstacionamientoAsync(int estacionamientoId) // consulta por nombre o direccion del estacionamiento
        {
            try
            {
                DataContext dataContext = new DataContext();
                var reseñas = await dataContext.Reseñas.Where(x => x.EstacionamientoId == estacionamientoId).ToListAsync();


                if (reseñas == null)
                {
                    return NotFound();
                }

                List<ServiceWebApi.DTO.ReseñaDTO> listaDTO = new List<ServiceWebApi.DTO.ReseñaDTO>();


                foreach (var item in reseñas)
                {
                    ServiceWebApi.DTO.ReseñaDTO reseñaDTO = new ServiceWebApi.DTO.ReseñaDTO();
                    reseñaDTO = Tools.Tools.PropertyCopier<Reseña, ServiceWebApi.DTO.ReseñaDTO>.Copy(item, reseñaDTO);

                    ApplicationUser appuserCliente = _userManager.FindByIdAsync(reseñaDTO.EmisorId).Result; // Obtengo los datos del usuario logeado
                    reseñaDTO.NombreCliente = appuserCliente.Nombre;
                    reseñaDTO.URLImagenCliente = "";

                    listaDTO.Add(reseñaDTO);
                }




                return listaDTO;

            }
            catch (Exception e)
            {

                return BadRequest(Tools.Tools.ExceptionMessage(e));
            }
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult> AddAsync([FromBody] Reseña reseña)
        {
            try
            {
                DataContext dataContext = new DataContext();


                reseña.FechaHora = DateTime.Now;
                await dataContext.Reseñas.AddAsync(reseña);
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

