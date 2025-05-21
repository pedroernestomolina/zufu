using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransfAPI.Data;
using TransfAPI.Models;

namespace TransfAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransferenciasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TransferenciasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Transferencias?fecha=2025-03-31
        [HttpGet]
        public async Task<ActionResult<IEnumerable<gCobroAnticipoDto>>> GetTransferenciasPorFecha([FromQuery] DateTime fechaDesde, [FromQuery] DateTime fechaHasta)
        {
            var transferencias = await _context.g_cobro_antiipo
                .Where(t => t.fecha_registro.Date >= fechaDesde.Date &&
                 t.fecha_registro.Date <= fechaHasta.Date &&
                 t.estatus_anulado.Trim().ToUpper() != "1")
                .Select(s => new gCobroAnticipoDto
                {
                    id = s.id,
                    idMedio = s.id_medio_cobro,
                    codigoMedio = s.codigo_medio_cobro,
                    descMedio = s.desc_medio_cobro,
                    montoRecibido = s.monto_recibido,
                    fechaGestion = s.fecha_gestion,
                    numeroReferencia = s.numero_referencia,
                    estatusComprometida = s.estatus_comprometido.Trim().ToUpper(),
                })
                .ToListAsync();

            return Ok(transferencias);
        }

        // Nuevo endpoint: GET api/Transferencias/no-procesados
        [HttpGet("no-procesados")]
        public async Task<ActionResult<IEnumerable<gCobroAnticipoDto>>> GetMovimientosNoProcesados()
        {
            var transferencias = await _context.g_cobro_antiipo
                .Where(t => t.estatus_comprometido.Trim().ToUpper() != "1" &&
                t.estatus_anulado.Trim().ToUpper() != "1")
                .Select(s => new gCobroAnticipoDto
                {
                    id = s.id,
                    idMedio = s.id_medio_cobro,
                    codigoMedio = s.codigo_medio_cobro,
                    descMedio = s.desc_medio_cobro,
                    montoRecibido = s.monto_recibido,
                    fechaGestion = s.fecha_gestion,
                    numeroReferencia = s.numero_referencia,
                    estatusComprometida = s.estatus_comprometido.Trim().ToUpper()
                })
                .ToListAsync();

            return Ok(transferencias);
        }

        // Nuevo endpoint: GET api/Transferencias/no-procesados
        [HttpGet("comprometidas")]
        public async Task<ActionResult<IEnumerable<gCobroAnticipoAsignadoDto>>> GetMovimientosComprometidos()
        {
            var _sql = @"
                SELECT 
                    ant.id as id,
                    ant.id_medio_cobro as idMedio,
                    ant.codigo_medio_cobro as codigoMedio,
                    ant.desc_medio_cobro as descMedio,
                    ant.monto_recibido as montoRecibido,
                    ant.fecha_gestion as fechaGestion,
                    ant.numero_referencia as numeroReferencia,
                    ant.estatus_comprometido as estatusComprometida,
                    cxcrec.documento as docNumeroAsig,
                    cxcrec.cliente as entidadAsig,
                    cxcrec.ci_rif as ciRifEntidadAsig,
                    cxcrec.auto  as idCxcRecibo
                FROM g_cobro_antiipo as ant
                join g_cobro_anticipo_recibos as antRec on ant.id = antRec.id_g_cobro_anticipo
                join cxc_recibos as cxcrec on antRec.id_cxc_recibo = cxcrec.auto
                WHERE ant.estatus_comprometido = '1' AND ant.estatus_anulado <> '1'";
            var transferencias = new List<gCobroAnticipoAsignadoDto>();
            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = _sql;
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            transferencias.Add(new gCobroAnticipoAsignadoDto
                            {
                                id = reader.GetInt32(reader.GetOrdinal("id")),
                                idMedio = reader.GetString(reader.GetOrdinal("idMedio")),
                                codigoMedio = reader.GetString(reader.GetOrdinal("codigoMedio")),
                                descMedio = reader.GetString(reader.GetOrdinal("descMedio")),
                                montoRecibido = reader.GetDecimal(reader.GetOrdinal("montoRecibido")),
                                fechaGestion = reader.GetDateTime(reader.GetOrdinal("fechaGestion")),
                                numeroReferencia = reader.GetString(reader.GetOrdinal("numeroReferencia")),
                                estatusComprometida = reader.GetString(reader.GetOrdinal("estatusComprometida")),
                                docNumeroAsig = reader.GetString(reader.GetOrdinal("docNumeroAsig")),
                                entidadAsig = reader.GetString(reader.GetOrdinal("entidadAsig")),
                                ciRifEntidadAsig = reader.GetString(reader.GetOrdinal("ciRifEntidadAsig")),
                                idCxcRecibo = reader.GetString(reader.GetOrdinal("idCxcRecibo")), 
                            });
                        }
                    }
                }
            }
            return Ok(transferencias);

            /*
            var transferencias = await _context.g_cobro_antiipo
            .Where(t => t.estatus_comprometido.Trim().ToUpper() == "1")
            .Select(s => new gCobroAnticipoDto
            {
                id = s.id,
                idMedio = s.id_medio_cobro,
                codigoMedio = s.codigo_medio_cobro,
                descMedio = s.desc_medio_cobro,
                montoRecibido = s.monto_recibido,
                fechaGestion = s.fecha_gestion,
                numeroReferencia = s.numero_referencia,
                estatusComprometida = s.estatus_comprometido.Trim().ToUpper()
            })
            .ToListAsync();
            return Ok(transferencias);*/
        }


        [HttpPost]
        public async Task<IActionResult> RegistrarTransferencia([FromBody] gCobroAnticipoDto transfDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Crear nueva transferencia
            var transferencia = new gCobroAnticipo
            {
                id_medio_cobro = transfDto.idMedio,
                codigo_medio_cobro = transfDto.codigoMedio,
                desc_medio_cobro = transfDto.descMedio,
                monto_recibido = transfDto.montoRecibido,
                fecha_gestion = transfDto.fechaGestion,
                numero_referencia = transfDto.numeroReferencia,
                estatus_comprometido = "",
            };

            // Verificar si el número de referencia ya existe
            var referenciaExiste = await _context.g_cobro_antiipo
                .AnyAsync(t => t.id_medio_cobro == transferencia.id_medio_cobro &&
                t.numero_referencia == transferencia.numero_referencia &&
                t.estatus_anulado.Trim().ToUpper() == "");
            if (referenciaExiste)
            {
                return BadRequest(new { error = "El número de referencia ya está registrado" });
            }

            _context.g_cobro_antiipo.Add(transferencia);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Transferencia registrada con éxito" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarTransferencia(int id)
        {
            var transferencia = await _context.g_cobro_antiipo.FindAsync(id);
            if (transferencia == null)
            {
                return NotFound(new { error = "Transferencia no encontrada" });
            }
            if (transferencia.estatus_comprometido.Trim().ToUpper() == "1")
            {
                return NotFound(new { error = "Transferencia Comprometida no Puede ser Eliminada" });
            }

            // Actualizar los campos
            transferencia.estatus_anulado = "1";
            // No modificamos FechaRegistro, se mantiene como estaba

            _context.g_cobro_antiipo.Update(transferencia);
            //_context.g_cobro_antiipo.Remove(transferencia);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Transferencia eliminada con éxito" });
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> EditarTransferencia(int id, [FromBody] gCobroAnticipoDto transferenciaDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var transferencia = await _context.g_cobro_antiipo.FindAsync(id);
            if (transferencia == null)
            {
                return NotFound(new { error = "Transferencia no encontrada" });
            }
            if (transferencia.estatus_comprometido.Trim().ToUpper() == "1")
            {
                return NotFound(new { error = "Transferencia Comprometida No Puede ser Editada" });
            }

            // Verificar si el número de referencia ya existe en otro registro
            var referenciaExiste = await _context.g_cobro_antiipo
                .AnyAsync(t => t.numero_referencia == transferenciaDto.numeroReferencia &&
                t.id_medio_cobro == transferencia.id_medio_cobro &&
                t.estatus_anulado.Trim().ToUpper() == "" &&
                t.id != id);
            if (referenciaExiste)
            {
                return BadRequest(new { error = "El número de referencia ya está registrado en otra transferencia" });
            }

            // Actualizar los campos
            transferencia.id_medio_cobro = transferenciaDto.idMedio.ToString(); // Ajustar si idMedio es string en la tabla
            transferencia.codigo_medio_cobro = transferenciaDto.codigoMedio;
            transferencia.desc_medio_cobro = transferenciaDto.descMedio;
            transferencia.monto_recibido = transferenciaDto.montoRecibido;
            transferencia.fecha_gestion = transferenciaDto.fechaGestion;
            transferencia.numero_referencia = transferenciaDto.numeroReferencia;
            transferencia.estatus_comprometido = "";
            // No modificamos FechaRegistro, se mantiene como estaba

            _context.g_cobro_antiipo.Update(transferencia);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Transferencia actualizada con éxito" });
        }

        [HttpGet("infoMovAsignado/{id}")]
        public async Task<ActionResult<IEnumerable<gInfoMovAsignadoDto>>> GetInfoMovimientoAsignado(int id)
        {
            var query = @"
                SELECT 
                    antrec.fecha_registro AS FechaAsig,
                    cxcrec.ci_rif AS CiRifEntidadAsig,
                    cxcrec.cliente AS NombreEntidadAsig,
                    cxcrec.direccion AS DireccionEntidadAsig,
                    antrec.comision AS ComisionAsig,
                    cxcrec.documento AS DocNumeroAsig,
                    cxcrec.monto_recibido_divisa AS MontoConComisionAsig
                FROM g_cobro_antiipo ant
                INNER JOIN g_cobro_anticipo_recibos antrec ON ant.id = antrec.id_g_cobro_anticipo and antrec.estatus <> '1'
                INNER JOIN cxc_recibos cxcrec ON antrec.id_cxc_recibo = cxcrec.auto
                WHERE ant.id = @id and ant.estatus_anulado <> '1' and ant.estatus_comprometido = '1'";

            var movimientosAsignados = new List<gInfoMovAsignadoDto>();

            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@id";
                    parameter.Value = id;
                    command.Parameters.Add(parameter);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            movimientosAsignados.Add(new gInfoMovAsignadoDto
                            {
                                fechaAsig = reader.GetDateTime(reader.GetOrdinal("FechaAsig")),
                                ciRifEntidadAsig = reader.GetString(reader.GetOrdinal("CiRifEntidadAsig")),
                                nombreEntidadAsig = reader.GetString(reader.GetOrdinal("NombreEntidadAsig")),
                                direccionEntidadAsig = reader.GetString(reader.GetOrdinal("DireccionEntidadAsig")),
                                comisionAsig = reader.GetDecimal(reader.GetOrdinal("ComisionAsig")),
                                docNumeroAsig = reader.GetString(reader.GetOrdinal("DocNumeroAsig")),
                                montoConComisionAsig = reader.GetDecimal(reader.GetOrdinal("MontoConComisionAsig"))
                            });
                        }
                    }
                }
            }

            return Ok(movimientosAsignados);
        }
    }
}