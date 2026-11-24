<?php
declare(strict_types=1);

function consultarDni(ClienteApi $api, string $dni): array
{
    $validacion = new Validacion();
    $validacion->patron('dni', $dni, Validacion::DNI, 'Ingresá exactamente 8 dígitos de DNI.');
    $validacion->exigirValido();
    $resultado = ['dni' => $dni];
    try {
        $resultado['consulta'] = $api->solicitar('POST', '/asistencias/consultar-cliente', ['dni' => $dni])->datos;
        $resultado['operacion'] = uuidOperacion();
    } catch (FalloApi $error) {
        if ($error->estado !== 404 || $error->codigo() !== 'cliente_no_encontrado') throw $error;
        $resultado['sinSocio'] = true;
    }
    return $resultado;
}

function pantallaRecepcion(ClienteApi $api, bool $post): void
{
    $resultado = $_SESSION['recepcion'] ?? [];
    $error = null;
    $datosAlta = ['dni' => ($resultado['sinSocio'] ?? false) && parametro('precargar') === '1' ? $resultado['dni'] : ''];
    $altaAbierta = parametro('alta') === '1';
    if ($post) {
        $accion = parametro('accion', 'post');
        try {
            switch ($accion) {
                case 'buscar':
                    unset($_SESSION['recepcion']);
                    $dni = recortarEntrada(parametro('dni', 'post'));
                    $resultado = ['dni' => $dni];
                    if (preg_match('~^' . Validacion::DNI_CON_PUNTOS . '$~D', $dni)) $dni = str_replace('.', '', $dni);
                    $_SESSION['recepcion'] = consultarDni($api, $dni);
                    redirigir('recepcion');
                case 'limpiar':
                    unset($_SESSION['recepcion']);
                    redirigir('recepcion');
                case 'confirmar':
                    $consulta = $resultado['consulta'] ?? [];
                    if (empty($consulta['puedeIngresar']) || ($resultado['operacion'] ?? '') !== parametro('operacion', 'post') || isset($resultado['asistencia'])) {
                        throw new FalloApi(409, ['title' => 'Esta búsqueda cambió. Consultá de nuevo antes de confirmar.']);
                    }
                    $resultado['asistencia'] = $api->solicitar('POST', '/clientes/' . (int) $consulta['clienteId'] . '/asistencias', ['operacionId' => $resultado['operacion']])->datos;
                    $_SESSION['recepcion'] = $resultado;
                    redirigir('recepcion', mensaje: 'Ingreso registrado.');
                case 'alta-rapida':
                    $altaAbierta = true;
                    $datosAlta = datosPersona('clientes');
                    validarPersona('clientes', $datosAlta);
                    $nuevo = $api->solicitar('POST', '/clientes', $datosAlta)->datos;
                    $_SESSION['recepcion'] = ['dni' => $nuevo['dni'], 'consulta' => ['clienteId' => $nuevo['id'], 'nombre' => $nuevo['nombre'], 'apellido' => $nuevo['apellido'], 'puedeIngresar' => false, 'codigoImpedimento' => 'membresia_no_asignada']];
                    redirigir('recepcion', mensaje: 'Socio dado de alta.');
                default:
                    throw new FalloApi(400, ['title' => 'Acción de Recepción desconocida.']);
            }
        } catch (FalloApi $error) {
            if ($error->estado === 401) throw $error;
            http_response_code($error->estado);
            if ($accion === 'confirmar' && $error->estado === 409 && $error->codigo() === 'sesiones_agotadas') {
                $resultado['consulta']['puedeIngresar'] = false;
                $resultado['consulta']['codigoImpedimento'] = 'sesiones_agotadas';
                $resultado['consulta']['membresia']['sesionesDisponibles'] = 0;
                unset($resultado['operacion']);
                $_SESSION['recepcion'] = $resultado;
            }
        }
    }

    $vencimientos = [];
    $errorAlertas = false;
    try {
        $vencimientos = $api->solicitar('GET', '/membresias/vencidas', consulta: ['tamanoPagina' => 3])->datos['alertas'];
    } catch (FalloApi $fallo) {
        if ($fallo->estado === 401) throw $fallo;
        $errorAlertas = true;
    }
    ob_start();
    require __DIR__ . '/../vistas/recepcion.php';
    pagina('Registrar asistencia', (string) ob_get_clean(), 'recepcion', enlace('recepcion', 'Nuevo socio', ['alta' => 1], 'secondary button-link'));
}

function mensajeIngreso(string $codigo): string
{
    return match ($codigo) {
        'cliente_inactivo' => 'La ficha está dada de baja.',
        'membresia_no_asignada' => 'Todavía no tiene una membresía asignada.',
        'membresia_programada' => 'La membresía todavía no empezó.',
        'membresia_vencida' => 'La membresía está vencida.',
        'sesiones_agotadas' => 'Sin sesiones disponibles',
        default => 'No se puede confirmar el ingreso.',
    };
}
