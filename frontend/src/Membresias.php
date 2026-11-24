<?php
declare(strict_types=1);

function estadoMembresia(string $estado): string
{
    return match ($estado) {
        'vigente' => etiqueta('Vigente', 'good'), 'vencida' => etiqueta('Vencida', 'warm'),
        'programada' => etiqueta('Programada'), default => etiqueta('Sin datos'),
    };
}

function rutaMembresia(int $cliente, int $membresia): string
{
    return "/clientes/{$cliente}/membresias/{$membresia}";
}

function listadoMembresias(ClienteApi $api, int $cliente): string
{
    $respuesta = $api->solicitar('GET', "/clientes/{$cliente}/membresias", consulta: ['pagina' => paginaActual()])->datos;
    $nombres = [];
    if (($respuesta['membresias'] ?? []) !== []) {
        $planes = $api->solicitar('GET', '/planes', consulta: ['tamanoPagina' => 100])->datos;
        $nombres = array_column($planes['planes'], 'nombre', 'id');
    }
    $filas = [];
    foreach ($respuesta['membresias'] as $item) {
        $filas[] = [e($nombres[$item['planId']] ?? 'Plan ' . $item['planId']), e(fecha($item['fechaInicio'])), e(fecha($item['fechaVencimiento'])),
            estadoMembresia($item['estado']), enlace('membresia', 'Ver período', ['cliente' => $cliente, 'id' => $item['id']])];
    }
    return panel('Membresías', tabla(['Plan', 'Inicio', 'Vence', 'Estado', 'Acción'], $filas, 'Todavía no hay membresías asignadas.') . paginacion('socio', $respuesta, ['id' => $cliente]));
}

function pantallaMembresia(ClienteApi $api): void
{
    $cliente = identificador('cliente');
    $id = identificador('id');
    [$periodo, $plan] = informacionMembresia($api, $cliente, $id);
    $acciones = enlace('pago', 'Registrar pago', ['cliente' => $cliente, 'id' => $id], 'secondary button-link')
        . enlace('pago', 'Renovar membresía', ['cliente' => $cliente, 'id' => $id, 'tipo' => 'renovacion'], 'primary button-link');
    pagina('Membresía', presentarPeriodo($periodo, $plan['nombre']), 'membresias', $acciones, volver: enlace('socio', '‹ Ficha del socio', ['id' => $cliente]));
}

function informacionMembresia(ClienteApi $api, int $cliente, int $id): array
{
    $periodo = $api->solicitar('GET', rutaMembresia($cliente, $id))->datos;
    $plan = $api->solicitar('GET', '/planes/' . (int) $periodo['planId'])->datos;
    return [$periodo, $plan];
}

function presentarPeriodo(array $periodo, string $nombrePlan, string $enlaceDetalle = ''): string
{
    ob_start();
    require __DIR__ . '/../vistas/pase-membresia.php';
    return (string) ob_get_clean();
}

function asignarMembresia(ClienteApi $api, bool $post): void
{
    $cliente = identificador('cliente');
    $error = null;
    $datos = [];
    $desdeRecepcion = parametro('volver') === 'recepcion';
    if ($post) {
        $datos = datosFormulario(['planId', 'fechaInicio']);
        try {
            $validacion = new Validacion();
            $planId = $validacion->entero('planId', $datos['planId']);
            $validacion->fecha('fechaInicio', $datos['fechaInicio']);
            $validacion->exigirValido();
            $guardado = $api->solicitar('POST', "/clientes/{$cliente}/membresias", ['planId' => $planId, 'fechaInicio' => $datos['fechaInicio']])->datos;
            if ($desdeRecepcion) {
                $dni = $_SESSION['recepcion']['dni'] ?? '';
                unset($_SESSION['recepcion']);
                if ($dni !== '') {
                    try { $_SESSION['recepcion'] = consultarDni($api, $dni); }
                    catch (FalloApi $fallo) { if ($fallo->estado === 401) throw $fallo; }
                }
                redirigir('recepcion', mensaje: 'Membresía asignada.');
            }
            redirigir('membresia', ['cliente' => $cliente, 'id' => $guardado['id']], 'Membresía asignada.');
        } catch (FalloApi $error) {
            if ($error->estado === 401) throw $error;
            http_response_code($error->estado);
        }
    }
    $persona = $api->solicitar('GET', '/clientes/' . $cliente)->datos;
    $respuesta = $api->solicitar('GET', '/planes', consulta: ['activo' => 'true', 'pagina' => paginaActual(), 'tamanoPagina' => 25])->datos;
    $planes = [];
    foreach ($respuesta['planes'] as $item) $planes[$item['id']] = $item['nombre'] . ' · ' . dinero($item['precio']);
    if (!isset($datos['fechaInicio'])) {
        $ultima = $api->solicitar('GET', "/clientes/{$cliente}/membresias", consulta: ['tamanoPagina' => 1])->datos['membresias'][0] ?? [];
        $datos['fechaInicio'] = max(fechaHoy(), $ultima['fechaVencimiento'] ?? '');
    }
    $volver = $desdeRecepcion ? enlace('recepcion', '‹ Recepción') : enlace('socio', '‹ Ficha del socio', ['id' => $cliente]);
    $campos = selector('planId', 'Plan disponible', $planes, $datos['planId'] ?? array_key_first($planes), 'required')
        . campo('fechaInicio', 'Fecha de inicio', $datos['fechaInicio'], 'date', 'required min="0001-01-01" max="9999-12-31"', $error);
    $html = $planes ? formulario($campos, 'Asignar membresía', cancelar: $volver, error: $error) : '<p class="empty-state">No hay planes activos para asignar.</p>';
    pagina('Asignar membresía', $html . paginacion('asignar', $respuesta, ['cliente' => $cliente] + ($desdeRecepcion ? ['volver' => 'recepcion'] : [])), 'membresias', descripcion: nombre($persona));
}

function pantallaVencimientos(ClienteApi $api): void
{
    $respuesta = $api->solicitar('GET', '/membresias/vencidas', consulta: ['pagina' => paginaActual()])->datos;
    $html = $respuesta['alertas'] ? '<ol class="renewal-queue" aria-label="Membresías vencidas">' : '<p class="empty-state">No hay membresías vencidas en esta página.</p>';
    foreach ($respuesta['alertas'] as $alerta) {
        $html .= '<li><div class="renewal-when"><time datetime="' . e($alerta['fechaVencimiento']) . '">' . e(fecha($alerta['fechaVencimiento'])) . '</time></div><div class="renewal-person">'
            . enlace('socio', nombre($alerta), ['id' => $alerta['clienteId']], 'entity-link') . '<span>' . e($alerta['nombrePlan']) . '</span></div>'
            . enlace('pago', 'Renovar', ['cliente' => $alerta['clienteId'], 'id' => $alerta['membresiaId'], 'tipo' => 'renovacion'], 'secondary button-link') . '</li>';
    }
    if ($respuesta['alertas']) $html .= '</ol>';
    pagina('Vencimientos', $html . paginacion('vencimientos', $respuesta), 'vencimientos', descripcion: 'Fecha de referencia ' . fecha($respuesta['fechaReferencia']));
}

function registrarPago(ClienteApi $api, bool $post): void
{
    $cliente = identificador('cliente');
    $id = identificador('id');
    $datos = [];
    $error = null;
    if ($post) {
        $datos = datosFormulario(['tipo', 'fechaPago', 'monto', 'medioPago', 'observacion']);
        try {
            $validacion = new Validacion();
            $validacion->opcion('tipo', $datos['tipo'], ['primerPago', 'renovacion']);
            $validacion->fecha('fechaPago', $datos['fechaPago'], fechaHoy());
            $importe = $validacion->importe('monto', $datos['monto'], admiteCero: false);
            $validacion->texto('medioPago', $datos['medioPago'], 40);
            $validacion->texto('observacion', $datos['observacion'], 255, opcional: true);
            $validacion->exigirValido();
            $cuerpo = array_replace($datos, ['monto' => $importe, 'observacion' => $datos['observacion'] !== '' ? $datos['observacion'] : null]);
            $pago = $api->solicitar('POST', rutaMembresia($cliente, $id) . '/pagos', $cuerpo)->datos;
            redirigir('comprobante', ['cliente' => $cliente, 'id' => $pago['membresiaOrigenId'], 'pago' => $pago['id']], 'Pago registrado.');
        } catch (FalloApi $error) {
            if ($error->estado === 401) throw $error;
            if (in_array($error->codigo(), ['membresia_ya_pagada', 'membresia_ya_renovada'], true) && isset($error->problema['pagoId'], $error->problema['membresiaOrigenId'])) {
                redirigir('comprobante', ['cliente' => $cliente, 'id' => $error->problema['membresiaOrigenId'], 'pago' => $error->problema['pagoId']], 'Este pago ya estaba registrado.');
            }
            http_response_code($error->estado);
        }
    }
    [$periodo, $plan] = informacionMembresia($api, $cliente, $id);
    $persona = $api->solicitar('GET', '/clientes/' . $cliente)->datos;
    $campos = selector('tipo', 'Tipo de pago', ['primerPago' => 'Primer pago', 'renovacion' => 'Renovación'], $datos['tipo'] ?? parametro('tipo'))
        . campo('fechaPago', 'Fecha de pago', $datos['fechaPago'] ?? fechaHoy(), 'date', 'required min="0001-01-01" max="' . e(fechaHoy()) . '"', $error)
        . campo('monto', 'Monto en pesos', $datos['monto'] ?? $plan['precio'], atributos: atributosImporte() . ' data-importe-positivo', error: $error)
        . campo('medioPago', 'Medio de pago', $datos['medioPago'] ?? '', atributos: 'required maxlength="40" list="medios" data-texto-visible', error: $error)
        . '<datalist id="medios"><option value="Transferencia"><option value="Efectivo"><option value="Tarjeta"></datalist><div class="wide-field">'
        . areaTexto('observacion', 'Observación (opcional)', $datos['observacion'] ?? '', 'maxlength="255"', $error) . '</div>';
    $volver = enlace('membresia', '‹ Membresía', ['cliente' => $cliente, 'id' => $id]);
    $html = '<div class="module-split">' . formulario($campos, 'Confirmar pago', cancelar: $volver, error: $error)
        . panel('Membresía de origen', detalle(['Plan' => $plan['nombre'], 'Inicio' => fecha($periodo['fechaInicio']), 'Vence' => fecha($periodo['fechaVencimiento'])]), 'receipt-side') . '</div>';
    pagina('Registrar pago', $html, 'pagos', descripcion: nombre($persona));
}

function pantallaComprobante(ClienteApi $api): void
{
    $cliente = identificador('cliente');
    $id = identificador('id');
    $pago = $api->solicitar('GET', rutaMembresia($cliente, $id) . '/pagos/' . identificador('pago'))->datos;
    $periodo = $pago['membresiaCubierta'];
    $nombreSocio = 'Socio n.º ' . $cliente;
    try {
        $persona = $api->solicitar('GET', '/clientes/' . $cliente)->datos;
        $nombreSocio = nombre($persona);
        $volver = enlace('membresia', '‹ Volver a la membresía', ['cliente' => $cliente, 'id' => $id]);
    } catch (FalloApi $error) {
        if ($error->estado === 401) throw $error;
        $volver = enlace('socios', '‹ Volver a Socios');
    }
    ob_start();
    require __DIR__ . '/../vistas/comprobante-pago.php';
    pagina('Comprobante de pago', (string) ob_get_clean(), 'pagos', boton('Imprimir', 'secondary print-action', 'type="button" data-print hidden'), 'N.º ' . str_pad((string) $pago['id'], 4, '0', STR_PAD_LEFT));
}

function pantallaMiMembresia(ClienteApi $api): void
{
    $cuenta = $api->solicitar('GET', '/mi-membresia')->datos;
    $pagos = enlace('mis-pagos', 'Ver mis pagos');
    $html = isset($cuenta['membresia'])
        ? presentarPeriodo($cuenta['membresia'], $cuenta['nombrePlan'] ?? 'Mi plan', $pagos)
        : '<p class="empty-state">Todavía no tenés una membresía asignada.</p>';
    pagina('Mi membresía', $html, 'cuenta', isset($cuenta['membresia']) ? '' : $pagos);
}

function pantallaMisPagos(ClienteApi $api): void
{
    $respuesta = $api->solicitar('GET', '/mi-membresia/pagos', consulta: ['pagina' => paginaActual()])->datos;
    $filas = array_map(fn(array $pago) => [e(fecha($pago['fechaPago'])), e(dinero($pago['monto'])), e($pago['medioPago'])], $respuesta['pagos']);
    pagina('Mis pagos', tabla(['Fecha', 'Importe', 'Medio'], $filas, 'Todavía no hay pagos registrados.') . paginacion('mis-pagos', $respuesta), 'cuenta');
}
