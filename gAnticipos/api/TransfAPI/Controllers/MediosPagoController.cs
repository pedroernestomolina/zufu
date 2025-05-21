using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransfAPI.Data;
using TransfAPI.Models;


namespace TransfAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MediosPagoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MediosPagoController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<empresaMediosDto>>> GetMediosPago()
        {
            var medios = await _context.empresa_medios
            .Where(w => w.estatus_cobro_gAnticipo.Trim().ToUpper() == "1")
            .Select(s => new empresaMediosDto
            {
                idMedio = s.auto,
                codigoMedio = s.codigo,
                descripcionMedio = s.nombre
            })
            .ToListAsync();
            return Ok(medios);
        }
    }
    
}