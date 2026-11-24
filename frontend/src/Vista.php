<?php
declare(strict_types=1);

function e(mixed $valor): string
{
    return htmlspecialchars((string) $valor, ENT_QUOTES | ENT_SUBSTITUTE, 'UTF-8');
}

function enlace(string $pantalla, string $texto, array $consulta = [], string $clase = 'quiet-link'): string
{
    return '<a class="' . e($clase) . '" href="' . e(url($pantalla, $consulta)) . '">' . e($texto) . '</a>';
}

function oculto(string $campo, mixed $valor): string
{
    return '<input type="hidden" name="' . e($campo) . '" value="' . e($valor) . '">';
}

function csrf(): string
{
    return oculto('_csrf', $_SESSION['csrf']);
}

function boton(string $texto, string $clase = 'primary', string $atributos = ''): string
{
    return '<button class="' . e($clase) . '" ' . $atributos . '>' . e($texto) . '</button>';
}

function errorCampo(?FalloApi $error, string $nombre): string
{
    $mensajes = $error?->problema['errors'][$nombre] ?? [];
    return is_array($mensajes) && $mensajes !== [] ? '<small class="error-campo">' . e(implode(' ', $mensajes)) . '</small>' : '';
}

function campo(string $nombre, string $etiqueta, mixed $valor = '', string $tipo = 'text', string $atributos = '', ?FalloApi $error = null): string
{
    $detalle = errorCampo($error, $nombre);
    return '<label class="form-field">' . e($etiqueta) . '<input class="input" name="' . e($nombre) . '" value="' . e($valor) . '" type="' . e($tipo) . '" ' . $atributos . ($detalle ? ' aria-invalid="true"' : '') . '>' . $detalle . '</label>';
}

function atributosDni(bool $conPuntos = false): string
{
    $patron = Validacion::DNI . ($conPuntos ? '|' . Validacion::DNI_CON_PUNTOS : '');
    return 'required inputmode="numeric" minlength="8" pattern="' . e($patron)
        . '" data-dni title="Ingresá exactamente 8 dígitos' . ($conPuntos ? '. Podés usar puntos en el formato 12.345.678' : ', sin puntos') . '."';
}

function campoEmail(string $valor, string $autocompletar, ?FalloApi $error): string
{
    return campo('email', 'Correo electrónico', $valor, atributos: 'required inputmode="email" maxlength="120" autocomplete="' . e($autocompletar)
        . '" pattern="[^@\s]+@[^@\s.]+(?:\.[^@\s.]+)+" placeholder="nombre@ejemplo.com"', error: $error);
}

function atributosImporte(): string
{
    return 'required inputmode="decimal" data-importe pattern="' . e(Validacion::IMPORTE) . '" title="Ingresá hasta ocho dígitos enteros y dos decimales, sin separador de miles."';
}

function campoEntero(string $nombre, string $etiqueta, mixed $valor, int $maximo, bool $requerido, ?FalloApi $error): string
{
    return campo($nombre, $etiqueta, $valor, atributos: 'inputmode="numeric" pattern="[1-9][0-9]{0,9}" data-entero-max="' . $maximo . '"' . ($requerido ? ' required' : ''), error: $error);
}

function selector(string $nombre, string $etiqueta, array $opciones, mixed $actual = '', string $atributos = ''): string
{
    $html = '<label class="form-field">' . e($etiqueta) . '<select class="input" name="' . e($nombre) . '" ' . $atributos . '>';
    foreach ($opciones as $valor => $texto) {
        $html .= '<option value="' . e($valor) . '"' . ((string) $valor === (string) $actual ? ' selected' : '') . '>' . e($texto) . '</option>';
    }
    return $html . '</select></label>';
}

function areaTexto(string $nombre, string $etiqueta, string $valor, string $atributos = '', ?FalloApi $error = null): string
{
    return '<label class="form-field">' . e($etiqueta) . '<textarea class="input" name="' . e($nombre) . '" rows="3" data-texto-visible ' . $atributos . '>' . e($valor) . '</textarea>' . errorCampo($error, $nombre) . '</label>';
}

function mostrarError(?FalloApi $error): string
{
    if (!$error) {
        return '';
    }
    $mensaje = $error->estado === 412 ? 'Esta ficha cambió mientras la estabas editando. Abrila de nuevo antes de guardar.' : $error->getMessage();
    $espera = $error->cabeceras['retry-after'] ?? '';
    $traza = $error->problema['traceId'] ?? '';
    $detalle = $error->problema['errors'] ?? [];
    $lista = '';
    if (is_array($detalle)) {
        foreach ($detalle as $mensajes) {
            if (is_array($mensajes)) {
                foreach ($mensajes as $texto) $lista .= '<li>' . e($texto) . '</li>';
            }
        }
    }
    return '<div class="form-feedback" role="alert">' . e($mensaje)
        . ($lista !== '' ? '<ul>' . $lista . '</ul>' : '')
        . (ctype_digit($espera) ? ' Volvé a intentar en ' . e($espera) . ' segundos.' : '')
        . ($error->estado >= 500 && $traza !== '' ? '<small>Referencia ' . e($traza) . '</small>' : '') . '</div>';
}

function formulario(string $campos, string $accion, string $anexos = '', string $cancelar = '', ?FalloApi $error = null): string
{
    return '<form class="module-form" method="post">' . csrf() . $anexos . mostrarError($error) . '<div class="fields">' . $campos
        . '</div><div class="form-actions">' . boton($accion) . $cancelar . '</div></form>';
}

function tabla(array $columnas, array $filas, string $sinDatos = 'No hay datos para mostrar.'): string
{
    if ($filas === []) return '<p class="empty-state">' . e($sinDatos) . '</p>';
    $html = '<div class="table-wrap" tabindex="0" role="region" aria-label="Listado"><table><thead><tr>';
    foreach ($columnas as $columna) $html .= '<th scope="col">' . e($columna) . '</th>';
    $html .= '</tr></thead><tbody>';
    foreach ($filas as $fila) $html .= '<tr><td>' . implode('</td><td>', $fila) . '</td></tr>';
    return $html . '</tbody></table></div>';
}

function panel(string $titulo, string $contenido, string $clase = ''): string
{
    return '<section class="module-panel ' . e($clase) . '"><h2>' . e($titulo) . '</h2>' . $contenido . '</section>';
}

function detalle(array $filas): string
{
    $html = '<dl class="detail-lines">';
    foreach ($filas as $etiqueta => $valor) $html .= '<div><dt>' . e($etiqueta) . '</dt><dd>' . e($valor) . '</dd></div>';
    return $html . '</dl>';
}

function paginacion(string $pantalla, array $respuesta, array $consulta = []): string
{
    $actual = (int) ($respuesta['pagina'] ?? 1);
    if ($actual <= 1 && empty($respuesta['hayMas'])) return '';
    return '<nav class="pagination" aria-label="Paginación">'
        . ($actual > 1 ? enlace($pantalla, 'Anterior', ['pagina' => $actual - 1] + $consulta) : '')
        . '<span>Página ' . e($actual) . '</span>'
        . (!empty($respuesta['hayMas']) ? enlace($pantalla, 'Siguiente', ['pagina' => $actual + 1] + $consulta) : '') . '</nav>';
}

function nombre(array $fila): string
{
    return trim(($fila['nombre'] ?? '') . ' ' . ($fila['apellido'] ?? ''));
}

function fecha(?string $valor): string
{
    return preg_match('/^(\d{4})-(\d{2})-(\d{2})$/D', $valor ?? '', $partes) ? $partes[3] . '/' . $partes[2] . '/' . $partes[1] : '—';
}

function fechaBreve(?string $valor): string
{
    if (!preg_match('/^(\d{4})-(\d{2})-(\d{2})$/D', $valor ?? '', $partes)) return '—';
    $meses = [1 => 'ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];
    $mes = $meses[(int) $partes[2]] ?? null;
    return $mes === null ? '—' : $partes[3] . ' ' . $mes . ' ' . $partes[1];
}

function dinero(mixed $valor): string
{
    return '$ ' . number_format((float) $valor, 2, ',', '.');
}

function etiqueta(string $texto, string $tono = ''): string
{
    return '<span class="label-status ' . e($tono) . '">' . e($texto) . '</span>';
}

function pagina(string $titulo, string $contenido, string $seccion = '', string $acciones = '', string $descripcion = '', string $volver = ''): void
{
    $aviso = $_SESSION['aviso'] ?? '';
    unset($_SESSION['aviso']);
    require __DIR__ . '/../vistas/base.php';
}
