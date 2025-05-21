//const API_BASE_URL = 'http://192.168.0.131:9000'; // 
//const API_BASE_URL = 'http://10.10.100.155:5039'; // api corriendo en equipo oficina pita
//const API_BASE_URL = 'http://localhost:5039'; // api corriendo en equipo local
//const API_BASE_URL = ''; // star market


let medios = [];
let clientes = [];
let asignaciones = [];
let zelleOption = null;
let movimientosTemporales = []; // Array para almacenar movimientos temporales

document.addEventListener('DOMContentLoaded', async () => {
    // Establecer la fecha actual en los campos de fecha
    const today = new Date().toISOString().split('T')[0];
    document.querySelector('input[name="fecha"]').value = today;
    document.getElementById('fechaDesde').value = today;
    document.getElementById('fechaHasta').value = today;
    document.querySelector('#registerAssignForm input[name="fecha"]').value = today;

    // Cargar medios de pago desde el API
    try {
        const response = await fetch(`${API_BASE_URL}/api/MediosPago`);
        if (!response.ok) throw new Error('Error al cargar los medios de pago');
        medios = await response.json();

        // Llenar selects de registrar transferencia
        const select = document.querySelector('#transferForm select[name="tipo"]');
        select.innerHTML = '<option value="">Seleccione un medio de pago</option>';
        medios.forEach(medio => {
            const option = document.createElement('option');
            option.value = medio.codigoMedio;
            option.textContent = medio.descripcionMedio;
            select.appendChild(option);
        });

        // Seleccionar "ZELLE" por defecto si existe
        zelleOption = medios.find(medio => medio.descripcionMedio.toUpperCase() === 'ZELLE');
        if (zelleOption) {
            select.value = zelleOption.codigoMedio;
        }

        // Llenar select de filtro en gestión
        const selectFiltro = document.getElementById('tipoPagoFiltro');
        selectFiltro.innerHTML = '<option value="">Todos los métodos</option>';
        medios.forEach(medio => {
            const option = document.createElement('option');
            option.value = medio.codigoMedio;
            option.textContent = medio.descripcionMedio;
            selectFiltro.appendChild(option);
        });

        // Llenar select del formulario de edición
        const editSelect = document.querySelector('#editForm select[name="tipo"]');
        editSelect.innerHTML = '<option value="">Seleccione un medio de pago</option>';
        medios.forEach(medio => {
            const option = document.createElement('option');
            option.value = medio.codigoMedio;
            option.textContent = medio.descripcionMedio;
            editSelect.appendChild(option);
        });

        // Llenar select de registrar y asignar
        const registerAssignSelect = document.querySelector('#registerAssignForm select[name="tipo"]');
        registerAssignSelect.innerHTML = '<option value="">Seleccione un medio de pago</option>';
        medios.forEach(medio => {
            const option = document.createElement('option');
            option.value = medio.codigoMedio;
            option.textContent = medio.descripcionMedio;
            registerAssignSelect.appendChild(option);
        });
        if (zelleOption) {
            registerAssignSelect.value = zelleOption.codigoMedio;
        }

        // Cargar clientes
        const clientesResponse = await fetch(`${API_BASE_URL}/api/Clientes`);
        if (!clientesResponse.ok) throw new Error('Error al cargar los clientes');
        clientes = await clientesResponse.json();
        clientes.sort((a, b) => a.nombreRazonSocialCliente.localeCompare(b.nombreRazonSocialCliente));

        // Inicializar select de clientes
        updateClienteSelect();
    } catch (error) {
        console.error('Error:', error);
        const select = document.querySelector('#transferForm select[name="tipo"]');
        select.innerHTML = '<option value="">Error al cargar medios</option>';
        const registerAssignSelect = document.querySelector('#registerAssignForm select[name="tipo"]');
        registerAssignSelect.innerHTML = '<option value="">Error al cargar medios</option>';
    }
});

// Toggle Sidebar
document.getElementById('toggleSidebar').addEventListener('click', () => {
    const sidebar = document.getElementById('sidebar');
    const content = document.getElementById('content');
    sidebar.classList.toggle('hidden');
    sidebar.classList.toggle('visible');
    content.classList.toggle('full');
});

// Navegación entre secciones
document.querySelectorAll('.nav-link').forEach(link => {
    link.addEventListener('click', (e) => {
        e.preventDefault();
        document.querySelectorAll('.nav-link').forEach(l => l.classList.remove('active'));
        link.classList.add('active');
        document.querySelectorAll('.section').forEach(section => section.classList.remove('active'));
        const sectionId = link.getAttribute('data-section');
        document.getElementById(sectionId).classList.add('active');

        if (sectionId === 'movimientosNoProcesados') {
            loadMovimientosNoProcesados();
        } else if (sectionId === 'movimientosComprometidos') {
            loadMovimientosComprometidos();
        } else if (sectionId === 'registrarAsignar') {
            movimientosTemporales = []; // Resetear movimientos temporales
            updateMovimientosTemporalesTable();
            updateClienteSelect();
            document.getElementById('saveAllBtn').disabled = true;
        }
    });
});

// Enviar formulario de Registrar Transferencia
document.getElementById('transferForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    const form = e.target;
    const formData = new FormData(form);
    const data = Object.fromEntries(formData);

    const medioSeleccionado = medios.find(m => m.codigoMedio === data.tipo);
    if (!medioSeleccionado) {
        document.getElementById('mensaje').innerHTML = '<div class="alert alert-danger">Medio de pago no válido</div>';
        return;
    }

    const transferenciaDto = {
        idMedio: medioSeleccionado.idMedio,
        codigoMedio: medioSeleccionado.codigoMedio,
        descMedio: medioSeleccionado.descripcionMedio,
        montoRecibido: parseFloat(data.monto),
        fechaGestion: data.fecha,
        numeroReferencia: data.referencia
    };

    try {
        const response = await fetch(`${API_BASE_URL}/api/Transferencias`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(transferenciaDto)
        });

        const result = await response.json();

        if (response.ok) {
            const modal = new bootstrap.Modal(document.getElementById('successModal'));
            modal.show();
            document.getElementById('confirmSuccess').onclick = () => {
                form.reset();
                const today = new Date().toISOString().split('T')[0];
                document.querySelector('input[name="fecha"]').value = today;
                const select = document.querySelector('#transferForm select[name="tipo"]');
                select.value = zelleOption ? zelleOption.codigoMedio : '';
                document.getElementById('mensaje').innerHTML = '';
                modal.hide();
            };
        } else {
            document.getElementById('mensaje').innerHTML = '<div class="alert alert-danger">' + result.error + '</div>';
        }
    } catch (error) {
        console.error('Error:', error);
        document.getElementById('mensaje').innerHTML = '<div class="alert alert-danger">Error al registrar la transferencia</div>';
    }
});

// Botón Pausar
document.getElementById('pauseBtn').addEventListener('click', () => {
    const form = document.getElementById('transferForm');
    form.querySelectorAll('input, select, button').forEach(el => el.disabled = !el.disabled);
    document.getElementById('pauseBtn').textContent = form.querySelector('button').disabled ? 'Reanudar' : 'Pausar';
});

// Botón Limpiar
document.getElementById('clearBtn').addEventListener('click', () => {
    resetForm();
});

// Enviar formulario de Registrar y Asignar
document.getElementById('registerAssignForm').addEventListener('submit', (e) => {
    e.preventDefault();
    const form = e.target;
    const formData = new FormData(form);
    const data = Object.fromEntries(formData);

    const medioSeleccionado = medios.find(m => m.codigoMedio === data.tipo);
    if (!medioSeleccionado) {
        alert('Medio de pago no válido');
        return;
    }

    movimientosTemporales.push({
        id: Date.now(), // ID temporal único
        idMedio: medioSeleccionado.idMedio,
        codigoMedio: medioSeleccionado.codigoMedio,
        descMedio: medioSeleccionado.descripcionMedio,
        montoRecibido: parseFloat(data.monto),
        fechaGestion: data.fecha,
        numeroReferencia: data.referencia
    });

    updateMovimientosTemporalesTable();
    form.reset();
    const today = new Date().toISOString().split('T')[0];
    document.querySelector('#registerAssignForm input[name="fecha"]').value = today;
    const select = document.querySelector('#registerAssignForm select[name="tipo"]');
    select.value = zelleOption ? zelleOption.codigoMedio : '';
});

// Botón Limpiar en Registrar y Asignar
document.getElementById('clearRegisterAssignBtn').addEventListener('click', () => {
    const form = document.getElementById('registerAssignForm');
    form.reset();
    const today = new Date().toISOString().split('T')[0];
    document.querySelector('#registerAssignForm input[name="fecha"]').value = today;
    const select = document.querySelector('#registerAssignForm select[name="tipo"]');
    select.value = zelleOption ? zelleOption.codigoMedio : '';
});

// Actualizar tabla de movimientos temporales
function updateMovimientosTemporalesTable() {
    const tbody = document.getElementById('movimientosTemporalesTable');
    tbody.innerHTML = '';
    movimientosTemporales.forEach((m, index) => {
        const row = document.createElement('tr');
        const fecha = new Date(m.fechaGestion).toISOString().split('T')[0];
        row.innerHTML = `
            <td class="medio-pago">${m.descMedio}</td>
            <td class="monto-recibido">${m.montoRecibido.toLocaleString('es-ES', { style: 'currency', currency: 'USD' })}</td>
            <td class="numero-referencia">${m.numeroReferencia}</td>
            <td>${fecha}</td>
            <td>
                <button class="btn btn-warning btn-sm edit-temp-btn" data-index="${index}">Editar</button>
                <button class="btn btn-danger btn-sm delete-temp-btn" data-index="${index}">Eliminar</button>
            </td>
        `;
        tbody.appendChild(row);

        row.querySelector('.edit-temp-btn').addEventListener('click', () => {
            const movimiento = movimientosTemporales[index];
            const form = document.getElementById('editForm');
            form.querySelector('select[name="tipo"]').value = movimiento.codigoMedio;
            form.querySelector('input[name="monto"]').value = movimiento.montoRecibido;
            form.querySelector('input[name="fecha"]').value = movimiento.fechaGestion;
            form.querySelector('input[name="referencia"]').value = movimiento.numeroReferencia;

            const modal = new bootstrap.Modal(document.getElementById('editModal'));
            modal.show();

            document.getElementById('saveEdit').onclick = () => {
                const formData = new FormData(form);
                const data = Object.fromEntries(formData);
                const medioSeleccionado = medios.find(m => m.codigoMedio === data.tipo);
                if (!medioSeleccionado) {
                    alert('Medio de pago no válido');
                    return;
                }

                movimientosTemporales[index] = {
                    id: movimiento.id,
                    idMedio: medioSeleccionado.idMedio,
                    codigoMedio: medioSeleccionado.codigoMedio,
                    descMedio: medioSeleccionado.descripcionMedio,
                    montoRecibido: parseFloat(data.monto),
                    fechaGestion: data.fecha,
                    numeroReferencia: data.referencia
                };

                updateMovimientosTemporalesTable();
                modal.hide();
            };
        });

        row.querySelector('.delete-temp-btn').addEventListener('click', () => {
            movimientosTemporales.splice(index, 1);
            updateMovimientosTemporalesTable();
            document.getElementById('saveAllBtn').disabled = movimientosTemporales.length === 0 || !document.getElementById('clienteSelect').value;
        });
    });

    // Calcular y mostrar el total de movimientos
    const totalMonto = movimientosTemporales.reduce((sum, m) => sum + m.montoRecibido, 0);
    document.getElementById('totalMovimientos').innerHTML = `
        <strong>Total de Movimientos:</strong> ${totalMonto.toLocaleString('es-ES', { style: 'currency', currency: 'USD' })}
    `;

    document.getElementById('saveAllBtn').disabled = movimientosTemporales.length === 0 || !document.getElementById('clienteSelect').value;
}

// Actualizar select de clientes
function updateClienteSelect() {
    const select = document.getElementById('clienteSelect');
    select.innerHTML = '<option value="">Seleccione un cliente</option>';
    clientes.forEach(cliente => {
        const option = document.createElement('option');
        option.value = cliente.idCliente;
        option.textContent = `${cliente.nombreRazonSocialCliente} (${cliente.ciRifCliente})`;
        select.appendChild(option);
    });

    select.addEventListener('change', () => {
        document.getElementById('saveAllBtn').disabled = movimientosTemporales.length === 0 || !select.value;
    });
}

// Búsqueda de clientes
document.getElementById('searchClienteBtn').addEventListener('click', () => {
    const searchTerm = document.getElementById('clienteSearch').value.toLowerCase();
    const select = document.getElementById('clienteSelect');
    select.innerHTML = '<option value="">Seleccione un cliente</option>';
    const filteredClientes = clientes.filter(c =>
        c.nombreRazonSocialCliente.toLowerCase().includes(searchTerm) ||
        c.ciRifCliente.toLowerCase().includes(searchTerm)
    );
    filteredClientes.forEach(cliente => {
        const option = document.createElement('option');
        option.value = cliente.idCliente;
        option.textContent = `${cliente.nombreRazonSocialCliente} (${cliente.ciRifCliente})`;
        select.appendChild(option);
    });
});

// Guardar todos los movimientos
document.getElementById('saveAllBtn').addEventListener('click', async () => {
    const clienteId = document.getElementById('clienteSelect').value;
    if (!clienteId) {
        alert('Por favor, seleccione un cliente.');
        return;
    }

    const cliente = clientes.find(c => c.idCliente === clienteId);
    if (!cliente) {
        alert('Cliente no válido.');
        return;
    }

    //sumar los montos de los movimientos temporales
    const totalMontoRecibido = movimientosTemporales.reduce((sum, m) => sum + m.montoRecibido, 0);

    const payload = {
        idCobrador: "0000000001",
        codigoCobrador: "",
        nombreCobrador: "DIRECTO",
        idUsuario: "0000000001",
        nombreUsuario: "ADM",
        idVendedor: "0000000001",
        idAgencia: "0000000001",
        SucPrefijo: "01",
        Asignaciones: [
            {
                factorCambio: 1,
                comision: 0,
                Recibo: {
                    idCliente: clienteId,
                    nombreCliente: cliente.nombreRazonSocialCliente,
                    ciRifCliente: cliente.ciRifCliente,
                    codigoCliente: cliente.codigoCliente,
                    direccionCliente: cliente.dirFiscalCliente,
                    telefonoCliente: "",
                    montoRecibidoMonDiv: totalMontoRecibido,
                    montoAnticipoCargarMonDiv: totalMontoRecibido,
                    Nota: `ANTICIPO POR MONTO TOTAL DE: (${totalMontoRecibido}$)`
                },
                MetodosPago: movimientosTemporales.map(m => ({
                    AutoMedioPago: m.idMedio,
                    Medio: m.descMedio,
                    Codigo: m.codigoMedio,
                    MontoRecibido: m.montoRecibido,
                    Referencia: m.numeroReferencia,
                    OpFecha: new Date(m.fechaGestion).toISOString(),
                    OpMonto: m.montoRecibido,
                    OpTasa: 1,
                    OpAplicaConversion: "0"
                }))
            }
        ]
    };
    console.log('Ficha Cobro:', payload);

    try {
        const response = await fetch(`${API_BASE_URL}/api/Anticipos/agregarMultiplesAnticipos`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        const result = await response.json();

        if (response.ok) {
            const modal = new bootstrap.Modal(document.getElementById('successModal'));
            modal.show();
            document.getElementById('confirmSuccess').onclick = () => {
                movimientosTemporales = [];
                updateMovimientosTemporalesTable();
                document.getElementById('clienteSelect').value = '';
                document.getElementById('clienteSearch').value = '';
                updateClienteSelect();
                document.getElementById('saveAllBtn').disabled = true;
                modal.hide();
            };
        } else {
            alert('Error: ' + result.error);
        }
    } catch (error) {
        console.error('Error al guardar movimientos:', error);
        alert('Error al guardar los movimientos');
    }
});

// Resetear formulario de Registrar Transferencia
function resetForm() {
    const form = document.getElementById('transferForm');
    form.reset();
    const today = new Date().toISOString().split('T')[0];
    document.querySelector('input[name="fecha"]').value = today;
    const select = document.querySelector('#transferForm select[name="tipo"]');
    select.value = zelleOption ? zelleOption.codigoMedio : '';
    document.getElementById('mensaje').innerHTML = '';
}

// Buscar Transferencias por Fecha y Filtro
document.getElementById('buscarTransferencias').addEventListener('click', async () => {
    const fecha = document.getElementById('fechaDesde').value;
    const fechaHasta = document.getElementById('fechaHasta').value;
    const tipoFiltro = document.getElementById('tipoPagoFiltro').value;

    try {
        const response = await fetch(`${API_BASE_URL}/api/Transferencias?fechaDesde=${fecha}&fechaHasta=${fechaHasta}`);
        if (!response.ok) throw new Error('Error al cargar las transferencias');
        let transferencias = await response.json();

        if (tipoFiltro) {
            transferencias = transferencias.filter(t => t.codigoMedio === tipoFiltro);
        }

        const tbody = document.getElementById('transferenciasTable');
        tbody.innerHTML = '';

        transferencias.forEach(t => {
            const isComprometida = t.estatusComprometida === "1";
            const row = document.createElement('tr');
            row.innerHTML = `
                <td class="medio-pago">${t.descMedio}</td>
                <td class="monto-recibido">${t.montoRecibido.toLocaleString('es-ES', { style: 'currency', currency: 'USD' })}</td>
                <td class="numero-referencia">${t.numeroReferencia}</td>
                <td class="estatus">${isComprometida ? 'Comprometida' : ''}</td>
                <td>
                    <button class="btn btn-warning btn-sm edit-btn" data-id="${t.id}" ${isComprometida ? 'disabled' : ''}>Editar</button>
                    <button class="btn btn-danger btn-sm delete-btn" data-id="${t.id}" ${isComprometida ? 'disabled' : ''}>Eliminar</button>
                </td>
            `;
            tbody.appendChild(row);

            const editBtn = row.querySelector('.edit-btn');
            editBtn.addEventListener('click', () => {
                const id = editBtn.dataset.id;
                const transferencia = transferencias.find(t => t.id == id);
                const fechaGestion = new Date(transferencia.fechaGestion).toISOString().split('T')[0];
                const editForm = document.getElementById('editForm');
                editForm.querySelector('select[name="tipo"]').value = transferencia.codigoMedio || '';
                editForm.querySelector('input[name="monto"]').value = transferencia.montoRecibido || '';
                editForm.querySelector('input[name="fecha"]').value = fechaGestion || '';
                editForm.querySelector('input[name="referencia"]').value = transferencia.numeroReferencia || '';

                const modal = new bootstrap.Modal(document.getElementById('editModal'));
                modal.show();

                document.getElementById('saveEdit').onclick = async () => {
                    const formData = new FormData(editForm);
                    const data = Object.fromEntries(formData);
                    const medioSeleccionado = medios.find(m => m.codigoMedio === data.tipo);
                    if (!medioSeleccionado) {
                        alert('Medio de pago no válido');
                        return;
                    }

                    const transferenciaDto = {
                        id: parseInt(id),
                        idMedio: medioSeleccionado.idMedio,
                        codigoMedio: medioSeleccionado.codigoMedio,
                        descMedio: medioSeleccionado.descripcionMedio,
                        montoRecibido: parseFloat(data.monto),
                        fechaGestion: data.fecha,
                        numeroReferencia: data.referencia,
                        estatusComprometida: transferencia.estatusComprometida
                    };

                    try {
                        const response = await fetch(`${API_BASE_URL}/api/Transferencias/${id}`, {
                            method: 'PUT',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify(transferenciaDto)
                        });

                        const result = await response.json();

                        if (response.ok) {
                            modal.hide();
                            document.getElementById('buscarTransferencias').click();
                        } else {
                            alert('Error: ' + result.error);
                        }
                    } catch (error) {
                        console.error('Error al editar:', error);
                        alert('Error al guardar los cambios');
                    }
                };
            });

            const deleteBtn = row.querySelector('.delete-btn');
            deleteBtn.addEventListener('click', () => {
                const id = deleteBtn.dataset.id;
                const modal = new bootstrap.Modal(document.getElementById('deleteModal'));
                modal.show();

                const confirmDeleteBtn = document.getElementById('confirmDelete');
                const newConfirmDeleteBtn = confirmDeleteBtn.cloneNode(true);
                confirmDeleteBtn.parentNode.replaceChild(newConfirmDeleteBtn, confirmDeleteBtn);

                newConfirmDeleteBtn.onclick = async () => {
                    try {
                        const response = await fetch(`${API_BASE_URL}/api/Transferencias/${id}`, {
                            method: 'DELETE',
                            headers: { 'Content-Type': 'application/json' }
                        });

                        const result = await response.json();

                        if (response.ok) {
                            modal.hide();
                            document.getElementById('buscarTransferencias').click();
                        } else {
                            alert('Error: ' + result.error);
                            modal.hide();
                        }
                    } catch (error) {
                        console.error('Error al eliminar:', error);
                        alert('Error al eliminar la transferencia');
                        modal.hide();
                    }
                };
            });
        });

        const montoTotal = transferencias.reduce((sum, t) => sum + t.montoRecibido, 0);
        document.getElementById('montoTotal').innerHTML = `
            <strong>Total Recibido:</strong> ${montoTotal.toLocaleString('es-ES', { style: 'currency', currency: 'USD' })}
        `;
    } catch (error) {
        console.error('Error:', error);
        document.getElementById('transferenciasTable').innerHTML = '<tr><td colspan="5">Error al cargar transferencias</td></tr>';
        document.getElementById('montoTotal').innerHTML = '';
    }
});

// Reporte Diario
document.getElementById('reporteDiario').addEventListener('click', async () => {
    const response = await fetch('/reporte/diario');
    const data = await response.json();
    document.getElementById('reporteContent').innerHTML = `
        <h3>Reporte Diario</h3>
        <pre>${JSON.stringify(data, null, 2)}</pre>
    `;
});

// Cargar Movimientos No Procesados
async function loadMovimientosNoProcesados() {
    try {
        const response = await fetch(`${API_BASE_URL}/api/Transferencias/no-procesados`);
        if (!response.ok) throw new Error('Error al cargar los movimientos no procesados');
        const movimientos = await response.json();

        const tbody = document.getElementById('movimientosNoProcesadosTable');
        tbody.innerHTML = '';

        movimientos.forEach(m => {
            const row = document.createElement('tr');
            const fecha = new Date(m.fechaGestion).toISOString().split('T')[0];
            row.innerHTML = `
                <td class="medio-pago">${m.descMedio}</td>
                <td class="monto-recibido">${m.montoRecibido.toLocaleString('es-ES', { style: 'currency', currency: 'USD' })}</td>
                <td class="numero-referencia">${m.numeroReferencia}</td>
                <td>${fecha}</td>
                <td>
                    <button class="btn btn-primary btn-sm asignar-btn" data-id="${m.id}">Asignar</button>
                </td>
            `;
            tbody.appendChild(row);

            const asignarBtn = row.querySelector('.asignar-btn');
            asignarBtn.addEventListener('click', () => {
                asignaciones = [];
                const modal = new bootstrap.Modal(document.getElementById('asignarModal'));
                document.getElementById('asignarTipo').textContent = m.descMedio;
                document.getElementById('asignarMontoTotal').textContent = m.montoRecibido.toLocaleString('es-ES', { style: 'currency', currency: 'USD' });
                document.getElementById('asignarMontoRestante').textContent = m.montoRecibido.toLocaleString('es-ES', { style: 'currency', currency: 'USD' });
                document.getElementById('asignarFecha').textContent = fecha;
                document.getElementById('asignarReferencia').textContent = m.numeroReferencia;
                document.getElementById('procesarMovimiento').disabled = true;

                const clienteSelect = document.querySelector('#asignarForm select[name="cliente"]');
                clienteSelect.innerHTML = '<option value="">Seleccione un cliente</option>';
                clientes.forEach(cliente => {
                    const option = document.createElement('option');
                    option.value = cliente.idCliente;
                    option.textContent = `${cliente.nombreRazonSocialCliente} (${cliente.ciRifCliente})`;
                    clienteSelect.appendChild(option);
                });

                updateAsignacionesTable();
                modal.show();

                document.getElementById('asignarForm').onsubmit = (e) => {
                    e.preventDefault();
                    const formData = new FormData(e.target);
                    const data = Object.fromEntries(formData);
                    const montoAsignar = parseFloat(data.monto);
                    const comision = parseFloat(data.comision) || 0;
                    const montoConComision = montoAsignar * (1 - comision / 100);
                    let totalAsignado = asignaciones.reduce((sum, a) => sum + a.monto, 0);
                    let saldoPend = m.montoRecibido - totalAsignado;

                    if (montoAsignar > saldoPend) {
                        alert('El monto a asignar excede el monto restante.');
                        return;
                    }

                    asignaciones.push({
                        clienteId: data.cliente,
                        clienteNombre: e.target.querySelector('select[name="cliente"]').selectedOptions[0].text,
                        monto: montoAsignar,
                        comision: comision,
                        montoConComision: montoConComision
                    });

                    totalAsignado = asignaciones.reduce((sum, a) => sum + a.monto, 0);
                    const nuevoMontoRestante = m.montoRecibido - totalAsignado;
                    document.getElementById('asignarMontoRestante').textContent = nuevoMontoRestante.toLocaleString('es-ES', { style: 'currency', currency: 'USD' });
                    document.getElementById('procesarMovimiento').disabled = nuevoMontoRestante !== 0;
                    updateAsignacionesTable();
                    e.target.reset();
                };

                document.getElementById('procesarMovimiento').onclick = async () => {
                    const movimientoId = m.id;
                    const fichaCobro = {
                        idCobrador: "0000000001",
                        codigoCobrador: "",
                        nombreCobrador: "DIRECTO",
                        idUsuario: "0000000001",
                        nombreUsuario: "ADM",
                        idVendedor: "0000000001",
                        idAgencia: "0000000001",
                        SucPrefijo: "01",
                        idGCobroAnticipo: movimientoId,
                        Asignaciones: asignaciones.map(a => {
                            const cli = clientes.find(c => c.idCliente === a.clienteId) || {
                                nombreRazonSocialCliente: "Cliente no encontrado",
                                ciRifCliente: "",
                                codigoCliente: "",
                                dirFiscalCliente: ""
                            };
                            return {
                                factorCambio: 1,
                                comision: a.comision,
                                Recibo: {
                                    idCliente: a.clienteId,
                                    nombreCliente: cli.nombreRazonSocialCliente,
                                    ciRifCliente: cli.ciRifCliente,
                                    codigoCliente: cli.codigoCliente,
                                    direccionCliente: cli.dirFiscalCliente,
                                    telefonoCliente: "",
                                    montoRecibidoMonDiv: a.montoConComision,
                                    montoAnticipoCargarMonDiv: a.montoConComision,
                                    Nota: `ANTICIPO POR: (${a.monto}$), COMISION(${a.comision}%), TOTAL: (${a.montoConComision}$) según Asignación de ${m.descMedio} - Ref: ${m.numeroReferencia}`
                                },
                                MetodosPago: [{
                                    AutoMedioPago: m.idMedio,
                                    Medio: m.descMedio,
                                    Codigo: m.codigoMedio,
                                    MontoRecibido: a.monto,
                                    Referencia: m.numeroReferencia,
                                    OpFecha: new Date(m.fechaGestion).toISOString(),
                                    OpMonto: a.monto,
                                    OpTasa: 1,
                                    OpAplicaConversion: "0"
                                }]
                            };
                        })
                    };

                    try {
                        const response = await fetch(`${API_BASE_URL}/api/Anticipos`, {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify(fichaCobro)
                        });
                        const result = await response.json();

                        if (response.ok) {
                            modal.hide();
                            loadMovimientosNoProcesados();
                        } else {
                            alert('Error al procesar el cobro: ' + result.error);
                        }
                    } catch (error) {
                        console.error('Error al procesar el cobro:', error);
                        alert('Error al procesar el cobro');
                    }
                };
            });
        });
    } catch (error) {
        console.error('Error al cargar movimientos no procesados:', error);
        document.getElementById('movimientosNoProcesadosTable').innerHTML = '<tr><td colspan="5">Error al cargar movimientos</td></tr>';
    }
}

function updateAsignacionesTable() {
    const tbody = document.getElementById('asignacionesTable');
    tbody.innerHTML = '';
    asignaciones.forEach((a, index) => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${a.clienteNombre}</td>
            <td>${a.monto.toLocaleString('es-ES', { style: 'currency', currency: 'USD' })}</td>
            <td>${a.comision}% (${a.montoConComision.toLocaleString('es-ES', { style: 'currency', currency: 'USD' })})</td>
            <td><button class="btn btn-danger btn-sm revertir-btn" data-index="${index}">Revertir</button></td>
        `;
        tbody.appendChild(row);

        row.querySelector('.revertir-btn').addEventListener('click', () => {
            const montoRevertido = asignaciones[index].monto;
            asignaciones.splice(index, 1);
            const montoRestante = parseFloat(document.getElementById('asignarMontoRestante').textContent.replace(/[^0-9.-]+/g, '')) + montoRevertido;
            document.getElementById('asignarMontoRestante').textContent = montoRestante.toLocaleString('es-ES', { style: 'currency', currency: 'USD' });
            document.getElementById('procesarMovimiento').disabled = montoRestante !== 0;
            updateAsignacionesTable();
        });
    });
}

// Cargar Movimientos Comprometidos
async function loadMovimientosComprometidos() {
    try {
        const response = await fetch(`${API_BASE_URL}/api/Transferencias/comprometidas`);
        const movimientos = await response.json();
        const tbody = document.getElementById("movimientosComprometidosTable");
        tbody.innerHTML = '';

        movimientos.forEach(m => {
            const row = document.createElement('tr');
            const fecha = new Date(m.fechaGestion).toISOString().split('T')[0];
            row.innerHTML = `
                <td class="medio-pago">${m.descMedio}</td>
                <td class="monto-recibido">${m.montoRecibido.toLocaleString('es-ES', { style: 'currency', currency: 'USD' })}</td>
                <td class="numero-referencia-mov-comprometido">${m.numeroReferencia+"/"+m.docNumeroAsig}</td>
                <td>${fecha}</td>
                <td>
                    <button class="btn btn-info btn-sm visualizar-btn" data-id="${m.id}">Visualizar</button>
                    <button class="btn btn-danger btn-sm delete-btn" data-id="${m.id}">Eliminar</button>
                </td>
            `;
            tbody.appendChild(row);

            const visualizarBtn = row.querySelector('.visualizar-btn');
            visualizarBtn.addEventListener('click', async () => {
                const id = visualizarBtn.dataset.id;
                try {
                    const response = await fetch(`${API_BASE_URL}/api/Transferencias/infoMovAsignado/${id}`);
                    if (!response.ok) throw new Error('Error al cargar los detalles de las asignaciones');
                    const asignacionesDetails = await response.json();

                    const tbodyDetails = document.getElementById('asignacionesDetailsTable');
                    tbodyDetails.innerHTML = '';

                    if (asignacionesDetails.length === 0) {
                        tbodyDetails.innerHTML = '<tr><td colspan="8">No hay asignaciones para este movimiento</td></tr>';
                    } else {
                        asignacionesDetails.forEach(a => {
                            const fechaAsig = new Date(a.fechaAsig).toLocaleString('es-ES', {
                                dateStyle: 'short',
                                timeStyle: 'short'
                            });
                            const rowDetail = document.createElement('tr');
                            rowDetail.innerHTML = `
                                <td>${fechaAsig}</td>
                                <td>${a.ciRifEntidadAsig}</td>
                                <td>${a.nombreEntidadAsig}</td>
                                <td>${a.direccionEntidadAsig || 'Sin dirección'}</td>
                                <td>${a.comisionAsig}%</td>
                                <td>${a.docNumeroAsig}</td>
                                <td>${a.montoConComisionAsig.toLocaleString('es-ES', { style: 'currency', currency: 'USD' })}</td>
                                <td>${a.montoSinComisionAsig.toLocaleString('es-ES', { style: 'currency', currency: 'USD' })}</td>
                            `;
                            tbodyDetails.appendChild(rowDetail);
                        });
                    }

                    const modal = new bootstrap.Modal(document.getElementById('visualizarAsignacionesModal'));
                    modal.show();
                } catch (error) {
                    console.error('Error al cargar los detalles de las asignaciones:', error);
                    const tbodyDetails = document.getElementById('asignacionesDetailsTable');
                    tbodyDetails.innerHTML = '<tr><td colspan="8">Error al cargar los detalles</td></tr>';
                    const modal = new bootstrap.Modal(document.getElementById('visualizarAsignacionesModal'));
                    modal.show();
                }
            });

            const deleteBtn = row.querySelector('.delete-btn');
            deleteBtn.addEventListener('click', () => {
                const id = deleteBtn.dataset.id;
                const modal = new bootstrap.Modal(document.getElementById('deleteModal'));
                modal.show();

                const confirmDeleteBtn = document.getElementById('confirmDelete');
                const newConfirmDeleteBtn = confirmDeleteBtn.cloneNode(true);
                confirmDeleteBtn.parentNode.replaceChild(newConfirmDeleteBtn, confirmDeleteBtn);

                newConfirmDeleteBtn.onclick = async () => {
                    try {
                        const infoResponse = await fetch(`${API_BASE_URL}/api/Anticipos/infoMovAnular/${id}`);
                        if (!infoResponse.ok) throw new Error('Error al obtener información del movimiento');
                        const asignacionesAnular = await infoResponse.json();

                        const anularPayload = {
                            idAnticipo: parseInt(id),
                            asignaciones: asignacionesAnular
                        };

                        const anularResponse = await fetch(`${API_BASE_URL}/api/Anticipos/anular`, {
                            method: 'POST',
                            headers: {
                                'Content-Type': 'application/json',
                                'Accept': '*/*'
                            },
                            body: JSON.stringify(anularPayload)
                        });

                        const result = await anularResponse.json();

                        if (anularResponse.ok) {
                            modal.hide();
                            loadMovimientosComprometidos();
                        } else {
                            alert('Error al anular el movimiento: ' + result.error);
                            modal.hide();
                        }
                    } catch (error) {
                        console.error('Error al anular el movimiento:', error);
                        alert('Error al anular el movimiento');
                        modal.hide();
                    }
                };
            });
        });
    } catch (error) {
        console.error('Error al cargar movimientos comprometidos:', error);
        document.getElementById('movimientosComprometidosTable').innerHTML = '<tr><td colspan="5">Error al cargar movimientos</td></tr>';
    }
}