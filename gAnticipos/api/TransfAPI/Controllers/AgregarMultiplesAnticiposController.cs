using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransfAPI.Models;
using MySql.Data.MySqlClient;
using System.Transactions;

namespace TransfAPI.Controllers
{
    public partial class AnticiposController : ControllerBase
    {

        [HttpPost("agregarMultiplesAnticipos")]
        public async Task<IActionResult> AgregarMultiplesAnticipos([FromBody] FichaCobroConMultiplesMovDto ficha)
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
                    var horaSistema = fechaSistema.ToString("HH:mm:ss");
                    Console.WriteLine("Fecha  del sistema:");
                    Console.WriteLine(fechaSistema);
                    //
                    Console.WriteLine("Hora del sistema:");
                    Console.WriteLine(horaSistema);

                    foreach (var asign in ficha.Asignaciones)
                    {
                        // Actualizar contadores
                        var sqlContadores = "UPDATE sistema_contadores SET a_cxc = a_cxc + 1, a_cxc_recibo = a_cxc_recibo + 1, a_cxc_recibo_numero = a_cxc_recibo_numero + 1";
                        var rowsAffected = await _context.Database.ExecuteSqlRawAsync(sqlContadores);
                        if (rowsAffected == 0)
                        {
                            return StatusCode(500, new { error = "PROBLEMA AL ACTUALIZAR TABLA CONTADORES" });
                        }
                        Console.WriteLine("ok1");

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

                        // Generar los números de recibo y documento
                        var largo = 10 - ficha.SucPrefijo.Length;
                        var autoCxCPago = ficha.SucPrefijo + aCxC.ToString().PadLeft(largo, '0');
                        var autoRecibo = ficha.SucPrefijo + aCxCRecibo.ToString().PadLeft(largo, '0');
                        var reciboNumero = ficha.SucPrefijo + aCxCReciboNumero.ToString().PadLeft(largo, '0');

                        // Agregar Lista de Anticipos Transferidos
                        foreach (var met in asign.MetodosPago)
                        {
                            // Crear nueva transferencia
                            var transferencia = new gCobroAnticipo
                            {
                                id_medio_cobro = met.AutoMedioPago,
                                codigo_medio_cobro = met.Codigo,
                                desc_medio_cobro = met.Medio,
                                monto_recibido = met.MontoRecibido,
                                fecha_gestion = met.OpFecha,
                                numero_referencia = met.Referencia,
                                estatus_comprometido = "1",
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
                            Console.WriteLine("ok3");

                            // Obtener el ID de la transferencia recién creada
                            var idTransferencia = transferencia.id;

                            // Insertar ASIGNACION DE COBRO /RECIBO
                            var asigCobro = @"INSERT INTO g_cobro_anticipo_recibos 
                                                (id, id_g_cobro_anticipo, id_cxc_recibo, estatus, comision, fecha_registro) 
                                            VALUES 
                                                (NULL, @idGCobroAnticipo, @idRecibo, @estatus, @comision, NULL)";
                            await _context.Database.ExecuteSqlRawAsync(asigCobro, new[]
                            {
                                new MySqlParameter("@idGCobroAnticipo", idTransferencia),
                                new MySqlParameter("@idRecibo", autoRecibo),
                                new MySqlParameter("@estatus", "0"),
                                new MySqlParameter("@comision", asign.comision)
                            });
                            Console.WriteLine("ok4");
                        }

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
                        Console.WriteLine("ok5");

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
                        Console.WriteLine("ok6");

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
                        Console.WriteLine("ok7");

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
                        Console.WriteLine("ok8");
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
    }
}