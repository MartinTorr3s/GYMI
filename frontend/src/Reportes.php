<?php
declare(strict_types=1);

function fechaHoraLocal(?string $valor): string
{
    if (!$valor) return '—';
    return (new DateTimeImmutable($valor))->setTimezone(new DateTimeZone('America/Argentina/Buenos_Aires'))->format('d/m/Y · H:i');
}

function pantallaReportes(ClienteApi $api): void
{
    $tipos = ['clientes-activos' => 'Socios activos', 'pagos-registrados' => 'Pagos registrados', 'asistencias' => 'Asistencias'];
    $tipo = array_key_exists('tipo', $_GET) ? parametro('tipo') : 'clientes-activos';
    if (!isset($tipos[$tipo])) throw new FalloApi(400, ['title' => 'Elegí un tipo de reporte disponible.']);
    $desde = parametro('fechaDesde');
    $hasta = parametro('fechaHasta');
    $cursor = parametro('cursor');
    $filtros = array_filter(['fechaDesde' => $desde, 'fechaHasta' => $hasta], fn(string $valor) => $valor !== '');
    $opciones = $cursor !== '' ? ['cursor' => $cursor] : ['tamanoPagina' => 25] + $filtros;
    $menu = '<nav class="report-tabs" aria-label="Tipos de reporte">';
    foreach ($tipos as $ruta => $etiqueta) {
        $menu .= '<a href="' . e(url('reportes', ['tipo' => $ruta] + $filtros)) . '"' . ($ruta === $tipo ? ' aria-current="page"' : '') . '>' . e($etiqueta) . '</a>';
    }
    $menu .= '</nav>';
    $formulario = '<form class="filter-form" method="get">' . oculto('pantalla', 'reportes') . oculto('tipo', $tipo)
        . campo('fechaDesde', 'Desde', $desde, 'date', 'min="0001-01-01" max="9999-12-31" data-fecha-desde')
        . campo('fechaHasta', 'Hasta', $hasta, 'date', 'min="0001-01-01" max="9999-12-31" data-fecha-hasta') . boton('Aplicar fechas', 'secondary') . '</form>';
    $html = $menu . $formulario;
    try {
        $validacion = new Validacion();
        if ($desde !== '' || $hasta !== '') {
            $validacion->fecha('fechaDesde', $desde);
            $validacion->fecha('fechaHasta', $hasta);
        }
        if ($cursor !== '') $validacion->patron('cursor', $cursor, '[a-fA-F0-9]{8}(?:-[a-fA-F0-9]{4}){3}-[a-fA-F0-9]{12}', 'La continuación del reporte no es válida.');
        $validacion->exigirValido();
        if ($desde > $hasta) throw new FalloApi(400, ['title' => 'La fecha Hasta tiene que ser igual o posterior a Desde.']);
        $respuesta = $api->solicitar('GET', '/reportes/' . $tipo, consulta: $opciones)->datos;
        $filas = $respuesta['filas'];
        $html .= resumenReporte($filas, $tipo, $tipos[$tipo]);
        $html .= match ($tipo) {
            'clientes-activos' => tabla(['Socio', 'Fecha de alta'], array_map(fn(array $fila) => [enlace('socio', nombre($fila), ['id' => $fila['clienteId']]), e(fecha($fila['fechaAlta']))], $filas)),
            'pagos-registrados' => tabla(['Socio', 'Fecha', 'Importe', 'Medio'], array_map(fn(array $fila) => [e(nombre($fila)), e(fecha($fila['fechaPago'])), e(dinero($fila['monto'])), e($fila['medioPago'])], $filas)),
            'asistencias' => tabla(['Socio', 'Ingreso (Buenos Aires)', 'Membresía'], array_map(fn(array $fila) => [e(nombre($fila)), e(fechaHoraLocal($fila['fechaHora'])), e($fila['membresiaId'])], $filas)),
        };
        $parametros = ['tipo' => $tipo] + $filtros;
        if ($cursor !== '' || !empty($respuesta['cursorSiguiente'])) {
            $html .= '<nav class="pagination" aria-label="Paginación del reporte">' . ($cursor !== '' ? enlace('reportes', 'Primera página', $parametros) : '')
                . '<span>Página ' . e($respuesta['pagina']) . '</span>'
                . (!empty($respuesta['cursorSiguiente']) ? enlace('reportes', 'Siguiente', ['cursor' => $respuesta['cursorSiguiente']] + $parametros) : '') . '</nav>';
        }
    } catch (FalloApi $error) {
        if ($error->estado === 401) throw $error;
        http_response_code($error->estado);
        $html .= mostrarError($error);
        if ($cursor !== '') $html .= enlace('reportes', 'Volver a la primera página', ['tipo' => $tipo] + $filtros);
    }
    pagina('Reportes', $html, 'reportes', descripcion: 'Consultá la actividad del gimnasio por período.');
}

function resumenReporte(array $filas, string $tipo, string $etiqueta): string
{
    if (!$filas) return '';
    $agrupadas = [];
    foreach ($filas as $fila) {
        $dia = match ($tipo) {
            'clientes-activos' => $fila['fechaAlta'], 'pagos-registrados' => $fila['fechaPago'],
            default => (new DateTimeImmutable($fila['fechaHora']))->setTimezone(new DateTimeZone('America/Argentina/Buenos_Aires'))->format('Y-m-d'),
        };
        $agrupadas[$dia] = ($agrupadas[$dia] ?? 0) + 1;
    }
    krsort($agrupadas);
    $dias = array_reverse(array_slice($agrupadas, 0, 7, true), true);
    $total = $tipo === 'pagos-registrados' ? dinero(array_sum(array_column($filas, 'monto'))) : (string) count($filas);
    $titulo = $tipo === 'pagos-registrados' ? 'Importe registrado' : $etiqueta;
    $html = '<section class="report-overview" aria-label="Resumen de esta página"><div class="report-total"><span>' . e($titulo)
        . ' en esta página</span><strong>' . e($total) . '</strong></div><div class="report-breakdown"><h2>'
        . (count($agrupadas) > 7 ? 'Últimas 7 fechas de esta página' : 'Registros por fecha') . '</h2><ol>';
    $maximo = max($dias);
    foreach ($dias as $dia => $cantidad) {
        $html .= '<li><time datetime="' . e($dia) . '">' . e(substr(fecha($dia), 0, 5)) . '</time><meter min="0" max="' . e($maximo)
            . '" value="' . e($cantidad) . '" aria-hidden="true"></meter><strong>' . e($cantidad) . '</strong></li>';
    }
    return $html . '</ol></div></section>';
}
