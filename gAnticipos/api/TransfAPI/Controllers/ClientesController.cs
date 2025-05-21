using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransfAPI.Data;
using TransfAPI.Models;

namespace TransfAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientesController : ControllerBase
    {
        private ApplicationDbContext _context;

        // Implementación de ClientesController
        public ClientesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Clientes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<clienteDto>>> GetClientesActivos()
        {
            var clientes = await _context.clientes
                .Where(c => c.estatus_credito == "1" && c.estatus.Trim().ToUpper() == "ACTIVO")
                .Select(c => new clienteDto
                {
                    ciRifCliente = c.ci_rif,
                    codigoCliente = c.codigo,
                    dirFiscalCliente = c.dir_fiscal,
                    estatusActivo = c.estatus,
                    estatusCredito = c.estatus_credito,
                    idCliente = c.auto,
                    nombreRazonSocialCliente = c.razon_social,
                }).ToListAsync();
            return Ok(clientes);
        }
    }
}





