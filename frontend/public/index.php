<?php
declare(strict_types=1);

$raiz = dirname(__DIR__);
foreach (['Configuracion', 'ClienteApi', 'Validacion', 'Flujo', 'Vista', 'Acceso', 'Recepcion', 'Catalogos', 'Membresias', 'Reportes'] as $archivo) {
    require $raiz . '/src/' . $archivo . '.php';
}

header('Content-Type: text/html; charset=UTF-8');
header('Cache-Control: no-store');
header('X-Content-Type-Options: nosniff');
header('Referrer-Policy: same-origin');
header("Content-Security-Policy: default-src 'self'; base-uri 'none'; frame-ancestors 'none'; form-action 'self'");

try {
    $configuracion = Configuracion::cargar($raiz);
    if (!extension_loaded('curl')) {
        throw new RuntimeException('Habilitá la extensión cURL de PHP.');
    }
} catch (RuntimeException $error) {
    http_response_code(500);
    exit('<!doctype html><html lang="es"><meta charset="utf-8"><title>Configuración de GYMI</title><p>' . e($error->getMessage()) . '</p></html>');
}

ini_set('session.use_strict_mode', '1');
ini_set('session.use_only_cookies', '1');
session_save_path($configuracion->rutaSesiones);
session_name('GYMI_FRONTEND');
session_set_cookie_params(['secure' => $configuracion->cookieSegura || (!empty($_SERVER['HTTPS']) && $_SERVER['HTTPS'] !== 'off'), 'httponly' => true, 'samesite' => 'Lax']);
if (!@session_start()) {
    http_response_code(500);
    exit('<!doctype html><html lang="es"><meta charset="utf-8"><title>Sesión de GYMI</title><p>No se pudo iniciar la sesión. Revisá GYMI_SESSION_PATH y sus permisos de escritura.</p></html>');
}
$_SESSION['csrf'] ??= bin2hex(random_bytes(32));
$api = new ClienteApi($configuracion);

$rutas = [
    'acceso' => ['pantallaAcceso', []], 'activar' => ['pantallaActivar', []], 'salir' => ['cerrarSesion', [...PERSONAL, 'cliente']],
    'recepcion' => ['pantallaRecepcion', PERSONAL], 'socios' => ['pantallaSocios', PERSONAL], 'socio' => ['pantallaSocio', PERSONAL],
    'socio-editar' => ['editarSocio', PERSONAL], 'planes' => ['pantallaPlanes', PERSONAL], 'plan' => ['pantallaPlan', PERSONAL],
    'plan-editar' => ['editarPlan', ['administrador']], 'equipo' => ['pantallaEquipo', ['administrador']],
    'recepcionista' => ['pantallaRecepcionista', ['administrador']], 'recepcionista-editar' => ['editarRecepcionista', ['administrador']],
    'membresia' => ['pantallaMembresia', PERSONAL], 'asignar' => ['asignarMembresia', PERSONAL], 'vencimientos' => ['pantallaVencimientos', PERSONAL],
    'pago' => ['registrarPago', PERSONAL], 'comprobante' => ['pantallaComprobante', PERSONAL],
    'mi-membresia' => ['pantallaMiMembresia', ['cliente']], 'mis-pagos' => ['pantallaMisPagos', ['cliente']],
    'reportes' => ['pantallaReportes', ['administrador']],
];

$pantalla = parametro('pantalla') ?: (rol() === '' ? 'acceso' : inicio());
$metodo = $_SERVER['REQUEST_METHOD'] ?? 'GET';
if (!in_array($metodo, ['GET', 'POST'], true)) {
    header('Allow: GET, POST');
    http_response_code(405);
    exit;
}
try {
    foreach ([$_GET, $_POST] as $campos) {
        foreach ($campos as $valor) {
            if (!is_string($valor)) throw new FalloApi(400, ['title' => 'Cada campo tiene que contener un solo valor.']);
        }
    }
    if (!isset($rutas[$pantalla])) throw new FalloApi(404, ['title' => 'No encontramos esa pantalla.']);
    [$controlador, $roles] = $rutas[$pantalla];
    if ($roles !== []) {
        if (rol() === '') redirigir('acceso');
        if (!in_array(rol(), $roles, true)) accesoDenegado();
    } elseif (rol() !== '') {
        redirigir(inicio());
    }
    if ($metodo === 'POST') validarCsrf();
    $controlador($api, $metodo === 'POST');
} catch (FalloApi $error) {
    if ($error->estado === 401 && rol() !== '') {
        borrarSesion();
        redirigir('acceso', mensaje: 'Tu sesión terminó. Volvé a ingresar.');
    }
    http_response_code($error->estado);
    pagina('No pudimos abrir la pantalla', mostrarError($error), acciones: enlace(rol() === '' ? 'acceso' : inicio(), 'Volver al inicio'));
} catch (Throwable $error) {
    error_log('GYMI frontend: ' . get_class($error));
    http_response_code(500);
    pagina('Ocurrió un problema', '<p class="empty-state">Volvé a abrir GYMI e intentá nuevamente.</p>');
}
