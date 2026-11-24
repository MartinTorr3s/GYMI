<?php
declare(strict_types=1);

const PERSONAL = ['administrador', 'recepcionista'];

function url(string $pantalla, array $consulta = []): string
{
    return './?' . http_build_query(['pantalla' => $pantalla] + $consulta, '', '&', PHP_QUERY_RFC3986);
}

function redirigir(string $pantalla, array $consulta = [], string $mensaje = ''): never
{
    if ($mensaje !== '') {
        $_SESSION['aviso'] = $mensaje;
    }
    header('Location: ' . url($pantalla, $consulta), true, 303);
    exit;
}

function parametro(string $campo, string $desde = 'get'): string
{
    $origen = $desde === 'post' ? $_POST : $_GET;
    return is_string($origen[$campo] ?? null) ? $origen[$campo] : '';
}

function identificador(string $campo): int
{
    $validacion = new Validacion();
    $id = $validacion->entero($campo, parametro($campo));
    $validacion->exigirValido();
    return $id;
}

function paginaActual(): int
{
    $validacion = new Validacion();
    $pagina = $validacion->entero('pagina', array_key_exists('pagina', $_GET) ? parametro('pagina') : '1', intdiv(Validacion::INT_MAX, 25) + 1);
    $validacion->exigirValido();
    return $pagina;
}

function fechaHoy(): string
{
    return (new DateTimeImmutable('now', new DateTimeZone('America/Argentina/Buenos_Aires')))->format('Y-m-d');
}

function rol(): string
{
    return $_SESSION['sesion']['rol'] ?? '';
}

function inicio(): string
{
    return rol() === 'cliente' ? 'mi-membresia' : 'recepcion';
}

function autenticar(array $sesion): void
{
    if (!in_array($sesion['rol'] ?? '', [...PERSONAL, 'cliente'], true)
        || !is_string($sesion['accessToken'] ?? null) || $sesion['accessToken'] === '') {
        throw new FalloApi(502, ['title' => 'La respuesta de inicio de sesión está incompleta.']);
    }
    session_regenerate_id(true);
    $_SESSION = ['sesion' => $sesion, 'csrf' => bin2hex(random_bytes(32))];
}

function borrarSesion(): void
{
    $_SESSION = ['csrf' => bin2hex(random_bytes(32))];
    session_regenerate_id(true);
}

function validarCsrf(): void
{
    if (!hash_equals($_SESSION['csrf'], parametro('_csrf', 'post'))) {
        throw new FalloApi(403, ['title' => 'El formulario venció. Volvé a abrir la pantalla e intentá de nuevo.']);
    }
}

function datosFormulario(array $campos): array
{
    $resultado = [];
    foreach ($campos as $campo) {
        $resultado[$campo] = recortarEntrada(parametro($campo, 'post'));
    }
    return $resultado;
}

function uuidOperacion(): string
{
    $bytes = random_bytes(16);
    $bytes[6] = chr((ord($bytes[6]) & 0x0f) | 0x40);
    $bytes[8] = chr((ord($bytes[8]) & 0x3f) | 0x80);
    return vsprintf('%s%s-%s-%s-%s-%s%s%s', str_split(bin2hex($bytes), 4));
}

function accesoDenegado(): never
{
    throw new FalloApi(403, ['title' => 'Tu cuenta no tiene permiso para hacer esta operación.']);
}

function validarVersion(string $version): string
{
    if ($version === '') {
        throw new FalloApi(428, ['title' => 'Falta la versión de la ficha. Volvé a abrirla.']);
    }
    return $version;
}
