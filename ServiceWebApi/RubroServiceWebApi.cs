using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ServiceWebApi
{
    public class RubroServiceWebApi
    {
    //    private WebApiAccess _webApiAccess;
    //    public RubroServiceWebApi(WebApiAccess webApiAccess)
    //    {
    //        _webApiAccess = webApiAccess;
    //    }

    //    public async Task<List<Rubro>> GetAll()
    //    {
    //        try
    //        {
    //            WebApiGet<List<Rubro>> webApiGet = new WebApiGet<List<Rubro>>(_webApiAccess);
    //            List<Rubro> lista = await webApiGet.GetAsync($"api/rubro/GetAll");
    //            return lista;
    //        }
    //        catch (Exception ex)
    //        {
    //            throw ex;
    //        }
    //    }
    //    public async Task<Rubro> Get(string rubroCodigo)
    //    {
    //        try
    //        {
    //            WebApiGet<Rubro> webApiGet = new WebApiGet<Rubro>(_webApiAccess);
    //            Rubro rubro = await webApiGet.GetAsync($"api/rubro/Get/{rubroCodigo}");
    //            return rubro;
    //        }
    //        catch (Exception ex)
    //        {
    //            throw ex;
    //        }
    //    }
    //    public async Task Add(Rubro rubro)
    //    {
    //        try
    //        {
    //            WebApiPost<Rubro> webApiPost = new WebApiPost<Rubro>(_webApiAccess);
    //            await webApiPost.PostAsync($"api/rubro/Add", rubro);
    //            return;
    //        }
    //        catch (Exception ex)
    //        {
    //            throw ex;
    //        }
    //    }
    //    public async Task<string> AddAutonumeric(Rubro rubro)
    //    {
    //        try
    //        {
    //            WebApiPost<Rubro, string> webApiPost = new WebApiPost<Rubro, string>(_webApiAccess);
    //            string codigo = await webApiPost.PostAsync($"api/rubro/AddAutonumeric", rubro);
    //            return codigo;
    //        }
    //        catch (Exception ex)
    //        {
    //            throw ex;
    //        }
    //    }
    //    public async Task Update(Rubro rubro)
    //    {
    //        try
    //        {
    //            WebApiPost<Rubro> webApiPost = new WebApiPost<Rubro>(_webApiAccess);
    //            await webApiPost.PostAsync($"api/rubro/Update", rubro);
    //            return;
    //        }
    //        catch (Exception ex)
    //        {
    //            throw ex;
    //        }
    //    }
    //    public async Task Delete(string rubroCodigo)
    //    {
    //        try
    //        {
    //            WebApiGet<string> webApiGet = new WebApiGet<string>(_webApiAccess);
    //            await webApiGet.GetAsync($"api/rubro/Delete/{rubroCodigo}");
    //            return;
    //        }
    //        catch (Exception ex)
    //        {
    //            throw ex;
    //        }
    //    }
    //    public async Task<List<Rubro>> ListaPorTipoDePedido(TipoDePedido tipoDePedido)
    //    {
    //        try
    //        {
    //            WebApiGet<List<Rubro>> webApiGet = new WebApiGet<List<Rubro>>(_webApiAccess);
    //            List<Rubro> lista = await webApiGet.GetAsync($"api/rubro/ListaPorTipoDePedido/{tipoDePedido.ToString()}");
    //            return lista;
    //        }
    //        catch (Exception ex)
    //        {
    //            throw ex;
    //        }
    //    }
    //    public async Task<List<DTORubroConFamilias>> ListaConFamiliasPorTipoDePedido(TipoDePedido tipoDePedido)
    //    {
    //        try
    //        {
    //            WebApiGet<List<DTORubroConFamilias>> webApiGet = new WebApiGet<List<DTORubroConFamilias>>(_webApiAccess);
    //            List<DTORubroConFamilias> lista = await webApiGet.GetAsync($"api/rubro/ListaConFamiliasPorTipoDePedido/{tipoDePedido.ToString()}");
    //            return lista;
    //        }
    //        catch (Exception ex)
    //        {
    //            throw ex;
    //        }
    //    }
    //    public async Task<List<DTORubroConFamilias>> ListaConFamiliasPorTipoDePedidoFiltradoPorPrecioyStock(TipoDePedido tipoDePedido, bool conImagen = true)
    //    {
    //        try
    //        {
    //            WebApiGet<List<DTORubroConFamilias>> webApiGet = new WebApiGet<List<DTORubroConFamilias>>(_webApiAccess);
    //            List<DTORubroConFamilias> lista = await webApiGet.GetAsync($"api/rubro/ListaConFamiliasPorTipoDePedidoFiltradoPorPrecioyStock/{tipoDePedido},{conImagen}");
    //            return lista;
    //        }
    //        catch (Exception ex)
    //        {
    //            throw ex;
    //        }
    //    }

    //    public async Task<PaginatedList<Rubro>> BusquedaPorTodasLasPalabrasAsync(string palabras, int pageIndex, int pageSize)
    //    {
    //        try
    //        {
    //            WebApiGet<PaginatedList<Rubro>> webApiGet = new WebApiGet<PaginatedList<Rubro>>(_webApiAccess);
    //            PaginatedList<Rubro> lista = await webApiGet.GetAsync($"api/rubro/BusquedaPorTodasLasPalabras/{palabras},{pageIndex},{pageSize}");
    //            return lista;
    //        }
    //        catch (Exception ex)
    //        {
    //            throw ex;
    //        }

    //    }
    }


}
