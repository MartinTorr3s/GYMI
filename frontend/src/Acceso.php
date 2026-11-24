<?php
declare(strict_types=1);

function pantallaAcceso(ClienteApi $api, bool $post): void
{
    $error = null;
    $email = '';
    if ($post) {
        $email = recortarEntrada(parametro('email', 'post'));
        try {
            $clave = parametro('contrasena', 'post');
            $validacion = new Validacion();
            $validacion->email($email);
            $validacion->contrasena($clave);
            $validacion->exigirValido();
            $respuesta = $api->solicitar('POST', '/acceso/iniciar-sesion', ['email' => $email, 'contrasena' => $clave], autenticada: false);
            autenticar($respuesta->datos);
            redirigir(inicio());
        } catch (FalloApi $error) {
            http_response_code($error->estado);
        }
    }
    ob_start();
    require __DIR__ . '/../vistas/acceso.php';
    pagina('Iniciar sesión', (string) ob_get_clean(), 'acceso');
}

function pantallaActivar(ClienteApi $api, bool $post): void
{
    $error = null;
    if ($post) {
        try {
            $clave = parametro('contrasena', 'post');
            $token = recortarEntrada(parametro('token', 'post'));
            $validacion = new Validacion();
            $validacion->patron('token', $token, Validacion::TOKEN, 'Ingresá los 64 caracteres hexadecimales de la invitación.');
            $validacion->contrasena($clave, nueva: true);
            if ($clave !== parametro('confirmacion', 'post')) {
                $validacion->agregar('confirmacion', 'Las contraseñas no coinciden.');
            }
            $validacion->exigirValido();
            $api->solicitar('POST', '/acceso/activar', ['token' => $token, 'contrasena' => $clave], autenticada: false);
            redirigir('acceso', mensaje: 'La cuenta está activa. Ya podés iniciar sesión.');
        } catch (FalloApi $error) {
            http_response_code($error->estado);
        }
    }
    $minimo = Validacion::CONTRASENA_NUEVA_MIN;
    $maximo = Validacion::CONTRASENA_MAX;
    $limites = 'required autocomplete="new-password" minlength="' . $minimo . '" maxlength="' . $maximo . '"';
    $campos = campo('token', 'Código de invitación', '', atributos: 'required autocomplete="off" minlength="64" maxlength="64" pattern="' . e(Validacion::TOKEN) . '"', error: $error)
        . campo('contrasena', 'Contraseña nueva', '', 'password', $limites . ' pattern="(?=.*\p{L})(?=.*[\p{Nd}\p{P}\p{S}])[^\p{Cc}]+" title="Incluí al menos una letra y un número o símbolo."', $error)
        . campo('confirmacion', 'Repetí la contraseña', '', 'password', $limites . ' data-confirmar-clave', $error);
    pagina('Activar cuenta', formulario($campos, 'Activar cuenta', cancelar: enlace('acceso', 'Volver al acceso'), error: $error), 'equipo');
}

function cerrarSesion(ClienteApi $api, bool $post): void
{
    if (!$post) throw new FalloApi(405, ['title' => 'Usá el botón Cerrar sesión.']);
    $aviso = '';
    try {
        $api->solicitar('POST', '/acceso/cerrar-sesion');
    } catch (FalloApi $error) {
        if ($error->estado !== 401) $aviso = 'Cerraste sesión en este navegador. El servidor no confirmó el cierre.';
    }
    borrarSesion();
    redirigir('acceso', mensaje: $aviso);
}
