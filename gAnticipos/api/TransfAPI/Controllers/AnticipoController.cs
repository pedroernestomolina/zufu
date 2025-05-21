using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransfAPI.Data;
using TransfAPI.Models;
using MySql.Data.MySqlClient;
using System.Transactions;

namespace TransfAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public partial class AnticiposController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AnticiposController(ApplicationDbContext context)
        {
            _context = context;
        }

        public class contadores
        {
            public int a_cxc { get; set; }
            public int a_cxc_recibo { get; set; }
            public int a_cxc_recibo_numero { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> ProcesarAnticipo([FromBody] FichaCobroDto ficha)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    var fechaSistema = DateTime.Now; // En MySQL, usabas NOW(), aquí usamos DateTime.Now

                    var sqlgCobroAnticipo = @"UPDATE g_cobro_antiipo SET estatus_comprometido = @estatusComprometido  
                    WHERE id = @idGCobroAnticipo";
                    var rows = await _context.Database.ExecuteSqlRawAsync(sqlgCobroAnticipo, new[]
                    {
                            new MySqlParameter("@estatusComprometido", "1" ),
                            new MySqlParameter("@idGCobroAnticipo", ficha.idGCobroAnticipo)
                        });
                    if (rows == 0)
                    {
                        return StatusCode(500, new { error = "ERROR AL MOVIMIENTO ANTICIPO" });
                    }
                    Console.WriteLine("ok1");

                    foreach (var asign in ficha.Asignaciones)
                    {
                        // Actualizar contadores
                        var sqlContadores = "UPDATE sistema_contadores SET a_cxc = a_cxc + 1, a_cxc_recibo = a_cxc_recibo + 1, a_cxc_recibo_numero = a_cxc_recibo_numero + 1";
                        var rowsAffected = await _context.Database.ExecuteSqlRawAsync(sqlContadores);
                        if (rowsAffected == 0)
                        {
                            return StatusCode(500, new { error = "PROBLEMA AL ACTUALIZAR TABLA CONTADORES" });
                        }

                        var cntId = await _context.Database.SqlQueryRaw<contadores>("SELECT a_cxc, a_cxc_recibo, a_cxc_recibo_numero FROM sistema_contadores").FirstOrDefaultAsync();
                        if (cntId == null)
                        {
                            return StatusCode(500, new { error = "ERROR AL OBTENER CONTADORES" });
                        }
                        var aCxC = cntId.a_cxc;
                        var aCxCRecibo = cntId.a_cxc_recibo;
                        var aCxCReciboNumero = cntId.a_cxc_recibo_numero;
                        Console.WriteLine("ok2");
                        Console.WriteLine($"aCxC: {aCxC}, aCxCRecibo: {aCxCRecibo}, aCxCReciboNumero: {aCxCReciboNumero}");

                        //var aCxC = await _context.Database.SqlQueryRaw<int?>("SELECT a_cxc FROM sistema_contadores").FirstOrDefaultAsync();
                        //var aCxCRecibo = await _context.Database.SqlQueryRaw<int?>("SELECT a_cxc_recibo FROM sistema_contadores").FirstOrDefaultAsync();
                        //var aCxCReciboNumero = await _context.Database.SqlQueryRaw<int?>("SELECT a_cxc_recibo_numero FROM sistema_contadores").FirstOrDefaultAsync();

                        var largo = 10 - ficha.SucPrefijo.Length;
                        var autoCxCPago = ficha.SucPrefijo + aCxC.ToString().PadLeft(largo, '0');
                        var autoRecibo = ficha.SucPrefijo + aCxCRecibo.ToString().PadLeft(largo, '0');
                        var reciboNumero = ficha.SucPrefijo + aCxCReciboNumero.ToString().PadLeft(largo, '0');

                        // Insertar ASIGNACION DE COBRO /RECIBO
                        var asigCobro = @"INSERT INTO g_cobro_anticipo_recibos 
                        (id, id_g_cobro_anticipo, id_cxc_recibo, estatus, comision, fecha_registro) 
                        VALUES (NULL, @idGCobroAnticipo, @idRecibo, @estatus, @comision, NULL)";
                        await _context.Database.ExecuteSqlRawAsync(asigCobro, new[]
                        {
                        new MySqlParameter("@idGCobroAnticipo", ficha.idGCobroAnticipo),
                        new MySqlParameter("@idRecibo", autoRecibo),
                        new MySqlParameter("@estatus", "0"),
                        new MySqlParameter("@comision", asign.comision)
                        });
                        Console.WriteLine("ok3");

                        // Insertar documento de pago (cxc)
                        var sqlCxc = @"INSERT INTO cxc (
                        auto, c_cobranza, c_cobranzap, fecha, tipo_documento, documento, fecha_vencimiento, 
                        nota, importe, acumulado, auto_cliente, cliente, ci_rif, codigo_cliente, 
                        estatus_cancelado, resta, estatus_anulado, auto_documento, numero, auto_agencia, 
                        agencia, signo, auto_vendedor, c_departamento, c_ventas, c_ventasp, serie, 
                        importe_neto, dias, castigop, cierre_ftp, monto_divisa, tasa_divisa, 
                        acumulado_divisa, codigo_sucursal, resta_divisa, importe_neto_divisa, estatus_doc_cxc)
                        VALUES (@auto, 0, 0, @fecha, @tipoDocumento, @documento, @fechaVencimiento, 
                        @nota, 0, 0, @autoCliente, @cliente, @ciRif, @codigoCliente, 
                        @estatusCancelado, 0, @estatusAnulado, @autoDocumento, @numero, @autoAgencia, 
                        @agencia, @signo, @autoVendedor, 0, 0, 0, @serie, 
                        0, 0, 0, @cierreFtp, 0, @tasaDivisa, 
                        0, @codigoSucursal, 0, 0, @estatusDocCxc)";

                        await _context.Database.ExecuteSqlRawAsync(sqlCxc, new[]
                        {
                        new MySqlParameter("@auto", autoCxCPago),
                        new MySqlParameter("@cCobranza", 0m),
                        new MySqlParameter("@cCobranzap", 0m),
                        new MySqlParameter("@fecha", fechaSistema.Date),
                        new MySqlParameter("@tipoDocumento", "PAG"),
                        new MySqlParameter("@documento", reciboNumero),
                        new MySqlParameter("@fechaVencimiento", fechaSistema.Date),
                        new MySqlParameter("@nota", asign.Recibo.Nota),
                        new MySqlParameter("@importe", 0m),
                        new MySqlParameter("@acumulado", 0m),
                        new MySqlParameter("@autoCliente", asign.Recibo.idCliente),
                        new MySqlParameter("@cliente", asign.Recibo.nombreCliente),
                        new MySqlParameter("@ciRif", asign.Recibo.ciRifCliente),
                        new MySqlParameter("@codigoCliente", asign.Recibo.codigoCliente),
                        new MySqlParameter("@estatusCancelado", "0"),
                        new MySqlParameter("@resta", 0m),
                        new MySqlParameter("@estatusAnulado", "0"),
                        new MySqlParameter("@autoDocumento", autoRecibo),
                        new MySqlParameter("@numero", ""),
                        new MySqlParameter("@autoAgencia", ficha.idAgencia),
                        new MySqlParameter("@agencia", ""),
                        new MySqlParameter("@signo", -1),
                        new MySqlParameter("@autoVendedor", ficha.idVendedor),
                        new MySqlParameter("@cDepartamento", 0m),
                        new MySqlParameter("@cVentas", 0m),
                        new MySqlParameter("@cVentasp", 0m),
                        new MySqlParameter("@serie", ""),
                        new MySqlParameter("@importeNeto", 0m),
                        new MySqlParameter("@dias", 0),
                        new MySqlParameter("@castigop", 0m),
                        new MySqlParameter("@cierreFtp", ""),
                        new MySqlParameter("@montoDivisa", 0m),
                        new MySqlParameter("@tasaDivisa", asign.factorCambio),
                        new MySqlParameter("@acumuladoDivisa", 0m),
                        new MySqlParameter("@codigoSucursal", ficha.SucPrefijo),
                        new MySqlParameter("@restaDivisa", 0m),
                        new MySqlParameter("@importeNetoDivisa", 0m),
                        new MySqlParameter("@estatusDocCxc", "1")
                        });
                        Console.WriteLine("ok4");

                        // Insertar recibo de cobro (cxc_recibos)
                        var sqlRecibo = @"INSERT INTO cxc_recibos (
                        auto, documento, fecha, auto_usuario, importe, usuario, monto_recibido, cobrador, 
                        auto_cliente, cliente, ci_rif, codigo, estatus_anulado, direccion, telefono, 
                        auto_cobrador, anticipos, cambio, nota, codigo_cobrador, auto_cxc, retenciones, 
                        descuentos, hora, cierre, cierre_ftp, importe_divisa, monto_recibido_divisa, 
                        cambio_divisa, estatus_doc_cxc, codigo_sucursal, tasa_cambio, anticipo_cargar, monto_ntcredito)
                        VALUES (@auto, @documento, @fecha, @autoUsuario, 0, @usuario, @montoRecibido, @cobrador, 
                        @autoCliente, @cliente, @ciRif, @codigo, @estatusAnulado, @direccion, @telefono, 
                        @autoCobrador, 0, 0, @nota, @codigoCobrador, @autoCxc, 0, 
                        0, @hora, @cierre, @cierreFtp, 0, @montoRecibidoDivisa, 
                        0, @estatusDocCxc, @codigoSucursal, @tasaCambio, @anticipoCargar, 0)";

                        // COMO PUEDO MINIMIZAR EL TAMANO DE LA HORA DEL SISTEMA A 24 HORAS
                        //var fechaSistema = DateTime.Now;
                        var horaSistema = fechaSistema.ToString("HH:mm:ss");
                        Console.WriteLine("Hora del sistema:");
                        Console.WriteLine(horaSistema);

                        await _context.Database.ExecuteSqlRawAsync(sqlRecibo, new[]
                        {
                        new MySqlParameter("@auto", autoRecibo),
                        new MySqlParameter("@documento", reciboNumero),
                        new MySqlParameter("@fecha", fechaSistema.Date),
                        new MySqlParameter("@autoUsuario", ficha.idUsuario),
                        new MySqlParameter("@importe", 0m),
                        new MySqlParameter("@usuario", ficha.nombreUsuario),
                        new MySqlParameter("@montoRecibido", asign.Recibo.montoRecibidoMonDiv),
                        new MySqlParameter("@cobrador", ficha.nombreCobrador),
                        new MySqlParameter("@autoCliente", asign.Recibo.idCliente),
                        new MySqlParameter("@cliente", asign.Recibo.nombreCliente),
                        new MySqlParameter("@ciRif", asign.Recibo.ciRifCliente),
                        new MySqlParameter("@codigo", asign.Recibo.codigoCliente),
                        new MySqlParameter("@estatusAnulado", "0"),
                        new MySqlParameter("@direccion", asign.Recibo.direccionCliente),
                        new MySqlParameter("@telefono", asign.Recibo.telefonoCliente),
                        new MySqlParameter("@autoCobrador", ficha.idCobrador),
                        new MySqlParameter("@anticipos", 0m),
                        new MySqlParameter("@cambio", 0m),
                        new MySqlParameter("@nota", ""),
                        new MySqlParameter("@codigoCobrador", ficha.codigoCobrador),
                        new MySqlParameter("@autoCxc", autoCxCPago),
                        new MySqlParameter("@retenciones", 0m),
                        new MySqlParameter("@descuentos", 0m),
                        new MySqlParameter("@hora", horaSistema),
                        new MySqlParameter("@cierre", ""),
                        new MySqlParameter("@cierreFtp", ""),
                        new MySqlParameter("@importeDivisa", 0m),
                        new MySqlParameter("@montoRecibidoDivisa", asign.Recibo.montoRecibidoMonDiv),
                        new MySqlParameter("@cambioDivisa", 0m),
                        new MySqlParameter("@estatusDocCxc", "1"),
                        new MySqlParameter("@codigoSucursal", ficha.SucPrefijo),
                        new MySqlParameter("@tasaCambio", asign.factorCambio),
                        new MySqlParameter("@anticipoCargar", asign.Recibo.montoAnticipoCargarMonDiv),
                        new MySqlParameter("@montoNtCredito", 0m)
                        });
                        Console.WriteLine("ok5");

                        // INSERTA METODOS DE PAGO PARA EL RECIBO
                        foreach (var fp in asign.MetodosPago)
                        {
                            var sqlMedioPago = @"INSERT INTO cxc_medio_pago (
                            auto_recibo, auto_medio_pago, auto_agencia, medio, codigo, monto_recibido, 
                            fecha, estatus_anulado, numero, agencia, auto_usuario, lote, referencia, 
                            auto_cobrador, cierre, fecha_agencia, cierre_ftp, opBanco, opNroCta, 
                            opNroRef, opFecha, opDetalle, opMonto, opTasa, opAplicaConversion, 
                            estatus_doc_cxc, codigo_sucursal)
                            VALUES (@autoRecibo, @autoMedioPago, @autoAgencia, @medio, @codigo, @montoRecibido, 
                            @fecha, @estatusAnulado, @numero, @agencia, @autoUsuario, @lote, @referencia, 
                            @autoCobrador, @cierre, @fechaAgencia, @cierreFtp, @opBanco, @opNroCta, 
                            @opNroRef, @opFecha, @opDetalle, @opMonto, @opTasa, @opAplicaConversion, 
                            @estatusDocCxc, @codigoSucursal)";
                            await _context.Database.ExecuteSqlRawAsync(sqlMedioPago, new[]
                            {
                            new MySqlParameter("@autoRecibo", autoRecibo),
                            new MySqlParameter("@autoMedioPago", fp.AutoMedioPago),
                            new MySqlParameter("@autoAgencia", ""),
                            new MySqlParameter("@medio", fp.Medio),
                            new MySqlParameter("@codigo", fp.Codigo),
                            new MySqlParameter("@montoRecibido", fp.MontoRecibido),
                            new MySqlParameter("@fecha", fechaSistema.Date),
                            new MySqlParameter("@estatusAnulado", "0"),
                            new MySqlParameter("@numero", ""),
                            new MySqlParameter("@agencia", ""),
                            new MySqlParameter("@autoUsuario", ficha.idUsuario),
                            new MySqlParameter("@lote", ""),
                            new MySqlParameter("@referencia", fp.Referencia),
                            new MySqlParameter("@autoCobrador", ficha.idCobrador),
                            new MySqlParameter("@cierre", ""),
                            new MySqlParameter("@fechaAgencia", new DateTime(2000, 1, 1)),
                            new MySqlParameter("@cierreFtp", ""),
                            new MySqlParameter("@opBanco", ""),
                            new MySqlParameter("@opNroCta", ""),
                            new MySqlParameter("@opNroRef", fp.Referencia),
                            new MySqlParameter("@opFecha", fp.OpFecha.Date),
                            new MySqlParameter("@opDetalle", ""),
                            new MySqlParameter("@opMonto", fp.OpMonto),
                            new MySqlParameter("@opTasa", fp.OpTasa),
                            new MySqlParameter("@opAplicaConversion", fp.OpAplicaConversion),
                            new MySqlParameter("@estatusDocCxc", "1"),
                            new MySqlParameter("@codigoSucursal", ficha.SucPrefijo)
                            });
                        }
                        Console.WriteLine("ok6");

                        //ACTUALIZA MONTO ANTICIPO CLIENTE
                        var sqlAnticipoCarg = "UPDATE clientes SET anticipos = anticipos + @montoAnticipo WHERE auto = @idCliente";
                        var row = await _context.Database.ExecuteSqlRawAsync(sqlAnticipoCarg, new[]
                        {
                            new MySqlParameter("@idCliente", asign.Recibo.idCliente),
                            new MySqlParameter("@montoAnticipo", asign.Recibo.montoAnticipoCargarMonDiv)
                        });
                        if (row == 0)
                        {
                            return StatusCode(500, new { error = "ERROR AL ACTUALIZAR ANTICIPO-CLIENTE" });
                        }
                        Console.WriteLine("ok7");
                    }

                    transaction.Complete();
                    return Ok(new { message = "Cobro procesado con éxito" });
                }
            }
            catch (MySqlException ex)
            {
                return StatusCode(500, new { error = $"Error MySQL: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        [HttpGet("infoMovAnular/{id}")]
        public async Task<ActionResult<IEnumerable<DataAnular>>> GetInfoMovAnular(int id)
        {
            var query = @"
                SELECT 
                    cxcrec.auto as idCxcRecibo,
                    cxcrec.auto_cxc as idCxcPago,
                    cxcrec.auto_cliente as idCliente,
                    cxcrec.anticipo_cargar as montoAnticipoRestarMonDiv
                FROM g_cobro_antiipo ant
                INNER JOIN g_cobro_anticipo_recibos antrec ON ant.id = antrec.id_g_cobro_anticipo and antrec.estatus<>'1'
                INNER JOIN cxc_recibos cxcrec ON antrec.id_cxc_recibo = cxcrec.auto
                WHERE ant.id = @id and ant.estatus_anulado<>'1'";
            //
            var dataMovAnular = new List<DataAnular>();
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
                    //
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            dataMovAnular.Add(new DataAnular
                            {
                                idCxcRecibo = reader.GetString(reader.GetOrdinal("idCxcRecibo")),
                                idCxcPago = reader.GetString(reader.GetOrdinal("idCxcPago")),
                                idCliente = reader.GetString(reader.GetOrdinal("idCliente")),
                                montoAnticipoRestarMonDiv = reader.GetDecimal(reader.GetOrdinal("montoAnticipoRestarMonDiv"))
                            });
                        }
                    }
                }
            }
            //
            return Ok(dataMovAnular);
        }

        public class entMov
        {
            public int idMov { get; set; }
            public string estatusMov { get; set; } = "";
        }
        public class entCli
        {
            public int idCliente { get; set; }
            public decimal saldoAnticipo { get; set; } = 0m;
        }

        [HttpPost("anular")]
        public async Task<IActionResult> AnularAnticipo([FromBody] FichaCobroAnularDto ficha)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            Console.WriteLine(ficha.idAnticipo);

            try
            {
                using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    var fechaSistema = DateTime.Now; // En MySQL, usabas NOW(), aquí usamos DateTime.Now
                    var horaSistema = fechaSistema.ToString("HH:mm:ss");
                    Console.WriteLine("Fecha / Hora del sistema:");
                    Console.WriteLine(fechaSistema);
                    Console.WriteLine(horaSistema);

                    /*
                    //VERIFICAR SI EL MOVIMIENTO coboro anticipo EXISTE /YA FUE ANULADO
                    var sql = @"select 
                                    id as idMov,
                                    estatus_comprometido as estatusMov
                                from  g_cobro_antiipo 
                                WHERE id = @idAnticipo";
                    var p1 = new MySql.Data.MySqlClient.MySqlParameter("@idAnticipo", ficha.idAnticipo);
                    var ent = await _context.Database.SqlQueryRaw<entMov>(sql, p1).FirstOrDefaultAsync();
                    if (ent == null)
                    {
                        return StatusCode(500, new { error = "MOVIMIENTO ANTICIPO NO ENCONTRADO" });
                    }
                    if (ent.estatusMov.Trim().ToUpper() != "1")
                    {
                        return StatusCode(500, new { error = "MOVIMIENTO ANTICIPO NO HA SIDO ASIGNADO" });
                    }

                    // anular gestion cobro anticipo recibo
                    var sqlCobroAnticipo = @"UPDATE g_cobro_antiipo SET estatus_comprometido = ''
                                            WHERE id = @idAnticipo";
                    p1 = new MySql.Data.MySqlClient.MySqlParameter("@idAnticipo", ficha.idAnticipo);
                    var vrt = await _context.Database.ExecuteSqlRawAsync(sqlCobroAnticipo, p1);
                    if (vrt == 0)
                    {
                        return StatusCode(500, new { error = "ERROR AL ANULAR MOVIMIENTO ANTICIPO" });
                    }
                    Console.WriteLine("ok1");


                    //VERIFICAR SI EL MOVIMIENTO coboro anticipo recibo  EXISTE /YA FUE ANULADO
                    sql = @"select 
                                    id as idMov, 
                                    estatus as estatusMov
                                from g_cobro_anticipo_recibos
                                WHERE id_g_cobro_anticipo = @idAnticipo and estatus<>'1'";
                    p1 = new MySql.Data.MySqlClient.MySqlParameter("@idAnticipo", ficha.idAnticipo);
                    ent = await _context.Database.SqlQueryRaw<entMov>(sql, p1).FirstOrDefaultAsync();
                    if (ent == null)
                    {
                        return StatusCode(500, new { error = "MOVIMIENTO COBRO ANTICIPO RECIBO NO ENCONTRADO" });
                    }

                    // anular gestion cobro anticipo recibo
                    var sqlCobroAnticipoRecibo = @"UPDATE g_cobro_anticipo_recibos SET estatus = '1'
                                            WHERE id_g_cobro_anticipo = @idAnticipo";
                    p1 = new MySql.Data.MySqlClient.MySqlParameter("@idAnticipo", ficha.idAnticipo);
                    vrt = await _context.Database.ExecuteSqlRawAsync(sqlCobroAnticipoRecibo, p1);
                    if (vrt == 0)
                    {
                        return StatusCode(500, new { error = "ERROR AL ANULAR COBRO ANTICIPO RECIBO" });
                    }
                    Console.WriteLine("ok2");
                    */


                    foreach (var asign in ficha.Asignaciones)
                    {
                        // anular recibo de cobro
                        var sqlRecibo = @"UPDATE cxc_recibos SET estatus_anulado = '1'
                                            WHERE auto = @idRecibo";
                        var p1 = new MySql.Data.MySqlClient.MySqlParameter("@idRecibo", asign.idCxcRecibo);
                        var vrt = await _context.Database.ExecuteSqlRawAsync(sqlRecibo, p1);
                        if (vrt == 0)
                        {
                            return StatusCode(500, new { error = "ERROR AL ANULAR CXC RECIBO" });
                        }
                        Console.WriteLine("ok3");

                        // anular medio de pago
                        var sqlMedioPago = @"UPDATE cxc_medio_pago SET estatus_anulado = '1'
                                            WHERE auto_recibo = @idRecibo";
                        p1 = new MySql.Data.MySqlClient.MySqlParameter("@idRecibo", asign.idCxcRecibo);
                        await _context.Database.ExecuteSqlRawAsync(sqlMedioPago, p1);
                        Console.WriteLine("ok4");

                        // anular documento de pago
                        sqlMedioPago = @"UPDATE cxc SET estatus_anulado = '1'
                                            WHERE auto = @idPago";
                        p1 = new MySql.Data.MySqlClient.MySqlParameter("@idPago", asign.idCxcPago);
                        vrt = await _context.Database.ExecuteSqlRawAsync(sqlMedioPago, p1);
                        if (vrt == 0)
                        {
                            return StatusCode(500, new { error = "ERROR AL ANULAR CXC PAGO" });
                        }
                        Console.WriteLine("ok5");


                        //VERIFICAR EXISTENCIA /SALDO DEL CLIENTE EN ANTICIPO 
                        var sql = @"select 
                                    auto as idCliente, 
                                    anticipos as saldoAnticipo
                                from clientes 
                                WHERE  auto = @idCliente";
                        p1 = new MySql.Data.MySqlClient.MySqlParameter("@idCliente", asign.idCliente);
                        var _entCli = await _context.Database.SqlQueryRaw<entCli>(sql, p1).FirstOrDefaultAsync();
                        if (_entCli == null)
                        {
                            return StatusCode(500, new { error = "ID DEL CLIENTE NO ENCONTRADO" });
                        }
                        if (_entCli.saldoAnticipo < asign.montoAnticipoRestarMonDiv)
                        {
                            return StatusCode(500, new { error = "SALDO ANTICIPO CLIENTE INSUFICIENTE PARA SER RESTAURADO" });
                        }

                        //ACTUALIZA MONTO ANTICIPO CLIENTE
                        var sqlAnticipoCarg = @"UPDATE clientes SET anticipos = anticipos - @montoAnticipo 
                                            WHERE auto = @idCliente";
                        var row = await _context.Database.ExecuteSqlRawAsync(sqlAnticipoCarg, new[]
                        {
                            new MySqlParameter("@idCliente", asign.idCliente),
                            new MySqlParameter("@montoAnticipo", asign.montoAnticipoRestarMonDiv)
                        });
                        if (row == 0)
                        {
                            return StatusCode(500, new { error = "ERROR AL ACTUALIZAR ANTICIPO-CLIENTE" });
                        }
                        Console.WriteLine("ok6");


                        var sqlCobroAnticipoRecibo = @"UPDATE g_cobro_anticipo_recibos SET estatus = '1'
                                            WHERE id_cxc_recibo = @idCxcRecibo";
                        p1 = new MySql.Data.MySqlClient.MySqlParameter("@idCxcRecibo", asign.idCxcRecibo);
                        vrt = await _context.Database.ExecuteSqlRawAsync(sqlCobroAnticipoRecibo, p1);
                        if (vrt == 0)
                        {
                            return StatusCode(500, new { error = "ERROR AL ANULAR RECIBO ANTICIPO " });
                        }
                        Console.WriteLine("ok7");


                        var sqlCobroAnticipo = @"UPDATE g_cobro_antiipo as t1 
                                                join g_cobro_anticipo_recibos as t2 on t1.id = t2.id_g_cobro_anticipo
                                                SET                        
                                                    t1.estatus_comprometido = '', 
                                                    t1.estatus_anulado ='1'
                                                where t2.id_cxc_recibo = @idCxcRecibo";
                        p1 = new MySql.Data.MySqlClient.MySqlParameter("@idCxcRecibo", asign.idCxcRecibo);
                        vrt = await _context.Database.ExecuteSqlRawAsync(sqlCobroAnticipo, p1);
                        if (vrt == 0)
                        {
                            return StatusCode(500, new { error = "ERROR AL ANULAR MOVIMIENTO ANTICIPO" });
                        }
                        Console.WriteLine("ok8");

                    }

                    transaction.Complete();
                    return Ok(new { message = "MOVIMIENTO ANULADO con éxito" });
                }
            }
            catch (MySqlException ex)
            {
                return StatusCode(500, new { error = $"Error MySQL: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}