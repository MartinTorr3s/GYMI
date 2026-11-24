<?php
declare(strict_types=1);

function catalogo(string $tipo): array
{
    return match ($tipo) {
        'clientes' => ['socios', 'socio', 'socio-editar', 'Socios', 'socio', 'socios'],
        'planes' => ['planes', 'plan', 'plan-editar', 'Planes', 'plan', 'planes'],
        'recepcionistas' => ['equipo', 'recepcionista', 'recepcionista-editar', 'Equipo', 'recepcionista', 'equipo'],
    };
}

function datosPersona(string $tipo): array
{
    return datosFormulario($tipo === 'clientes' ? ['nombre', 'apellido', 'email', 'dni', 'telefono'] : ['nombre', 'apellido', 'email']);
}

function camposPersona(string $tipo, array $datos, ?FalloApi $error): string
{
    $html = campo('nombre', 'Nombre', $datos['nombre'] ?? '', atributos: 'required maxlength="60" autocomplete="given-name" data-texto-visible', error: $error)
        . campo('apellido', 'Apellido', $datos['apellido'] ?? '', atributos: 'required maxlength="60" autocomplete="family-name" data-texto-visible', error: $error)
        . campoEmail($datos['email'] ?? '', 'email', $error);
    if ($tipo === 'clientes') {
        $html .= campo('dni', 'DNI', $datos['dni'] ?? '', atributos: atributosDni(), error: $error)
            . campo('telefono', 'Teléfono', $datos['telefono'] ?? '', 'tel', 'required maxlength="16" autocomplete="tel" pattern="' . e(Validacion::TELEFONO) . '" title="De 1 a 15 dígitos, con + al inicio si corresponde."', $error);
    }
    return $html;
}

function pantallaSocios(ClienteApi $api): void { listarCatalogo($api, 'clientes'); }
function pantallaPlanes(ClienteApi $api): void { listarCatalogo($api, 'planes'); }
function pantallaEquipo(ClienteApi $api): void { listarCatalogo($api, 'recepcionistas'); }
function pantallaSocio(ClienteApi $api, bool $post): void { abrirCatalogo($api, 'clientes', $post); }
function pantallaPlan(ClienteApi $api, bool $post): void { abrirCatalogo($api, 'planes', $post); }
function pantallaRecepcionista(ClienteApi $api, bool $post): void { abrirCatalogo($api, 'recepcionistas', $post); }
function editarSocio(ClienteApi $api, bool $post): void { editarCatalogo($api, 'clientes', $post); }
function editarPlan(ClienteApi $api, bool $post): void { editarCatalogo($api, 'planes', $post); }
function editarRecepcionista(ClienteApi $api, bool $post): void { editarCatalogo($api, 'recepcionistas', $post); }

function listarCatalogo(ClienteApi $api, string $tipo): void
{
    [$lista, $ficha, $editar, $titulo, $singular, $seccion] = catalogo($tipo);
    $consulta = ['pagina' => paginaActual(), 'tamanoPagina' => 25];
    $activo = array_key_exists('estado', $_GET) ? parametro('estado') : 'true';
    $filtro = recortarEntrada(parametro('q'));
    $validacion = new Validacion();
    if ($tipo === 'clientes') $validacion->texto('q', $filtro, 100, opcional: true);
    else $validacion->opcion('estado', $activo, ['true', 'false', 'todos']);
    $validacion->exigirValido();
    if ($tipo !== 'clientes' && $activo !== 'todos') $consulta['activo'] = $activo;
    $respuesta = $api->solicitar('GET', '/' . $tipo, consulta: $consulta)->datos;
    $elementos = $respuesta[$tipo] ?? [];
    $filtros = $tipo === 'clientes'
        ? campo('q', 'Filtrar esta página', $filtro, atributos: 'placeholder="Nombre o DNI" maxlength="100"') . oculto('pagina', $consulta['pagina'])
        : selector('estado', 'Estado', ['true' => 'Activos', 'false' => 'Inactivos', 'todos' => 'Todos'], $activo);
    $html = '<form class="filter-form" method="get">' . oculto('pantalla', $lista) . $filtros . boton('Aplicar', 'secondary') . '</form>';
    if ($tipo === 'planes') {
        $html .= $elementos ? '<div class="plan-cards">' : '<p class="empty-state">No hay planes con ese estado.</p>';
        foreach ($elementos as $plan) {
            $html .= '<article class="plan-card"><div class="plan-card-heading">' . etiqueta($plan['activo'] ? 'Activo' : 'Archivado', $plan['activo'] ? 'good' : '') . '<h2>' . e($plan['nombre']) . '</h2><p>' . e($plan['descripcion']) . '</p></div><p class="plan-price">' . e(dinero($plan['precio'])) . '</p>'
                . detalle(['Duración' => duracionPlan($plan), 'Sesiones' => $plan['cantidadSesiones']]) . enlace('plan', 'Ver plan', ['id' => $plan['id']], 'secondary button-link') . '</article>';
        }
        if ($elementos) $html .= '</div>';
    } else {
        $filas = [];
        foreach ($elementos as $item) {
            if ($tipo === 'clientes' && $filtro !== '' && stripos(nombre($item) . ' ' . $item['dni'], $filtro) === false) continue;
            $filas[] = [enlace($ficha, nombre($item), ['id' => $item['id']], 'entity-link'),
                $tipo === 'clientes' ? e($item['dni']) : e($item['email']),
                $tipo === 'clientes' ? e(fecha($item['fechaAlta'])) : etiqueta($item['activo'] ? ($item['accesoWebHabilitado'] ? 'Habilitado' : 'Pendiente') : 'Inactivo', $item['activo'] && $item['accesoWebHabilitado'] ? 'good' : ''),
                enlace($ficha, 'Ver ficha', ['id' => $item['id']])];
        }
        $html .= tabla($tipo === 'clientes' ? ['Socio', 'DNI', 'Alta', 'Acción'] : ['Recepcionista', 'Correo', 'Acceso web', 'Acción'], $filas);
    }
    $html .= paginacion($lista, $respuesta, $tipo === 'clientes' ? ['q' => $filtro] : ['estado' => $activo]);
    $nuevo = ($tipo !== 'planes' || rol() === 'administrador') ? enlace($editar, 'Nuevo ' . $singular, clase: 'primary button-link') : '';
    pagina($titulo, $html, $seccion, $nuevo);
}

function duracionPlan(array $plan): string
{
    return ($plan['tipoDuracion'] ?? '') === 'mesCalendario' ? 'Mes calendario' : (string) ($plan['duracionDias'] ?? '') . ' días';
}

function abrirCatalogo(ClienteApi $api, string $tipo, bool $post): void
{
    [$lista, $ficha, $editar, $titulo, $singular, $seccion] = catalogo($tipo);
    $id = identificador('id');
    $ruta = '/' . $tipo . '/' . $id;
    if ($post) {
        $accion = parametro('accion', 'post');
        if ($tipo === 'planes' && rol() !== 'administrador') accesoDenegado();
        if ($accion === 'baja') {
            $api->solicitar('DELETE', $ruta, version: validarVersion(parametro('version', 'post')));
            redirigir($lista, mensaje: 'La ficha quedó inactiva.');
        }
        if ($accion !== 'invitar' || $tipo === 'planes') throw new FalloApi(400, ['title' => 'La acción no está disponible.']);
        $_SESSION['invitacion'] = ['tipo' => $tipo, 'id' => $id, 'datos' => $api->solicitar('POST', $ruta . '/activacion')->datos];
        redirigir($ficha, ['id' => $id]);
    }

    $respuesta = $api->solicitar('GET', $ruta);
    $item = $respuesta->datos;
    $activo = (bool) $item['activo'];
    $permiteEditar = $tipo !== 'planes' || rol() === 'administrador';
    $acciones = $permiteEditar ? enlace($editar, 'Editar ' . $singular, ['id' => $id], 'secondary button-link') : '';
    if ($tipo === 'clientes') $acciones .= enlace('asignar', 'Asignar membresía', ['cliente' => $id], 'primary button-link');
    $html = match ($tipo) {
        'clientes' => panel('Datos del socio', detalle(['DNI' => $item['dni'], 'Correo' => $item['email'], 'Teléfono' => $item['telefono'], 'Alta' => fecha($item['fechaAlta'])])),
        'planes' => panel('Condiciones del plan', detalle(['Descripción' => $item['descripcion'], 'Duración' => duracionPlan($item), 'Sesiones incluidas' => $item['cantidadSesiones'], 'Precio' => dinero($item['precio']), 'Estado' => $activo ? 'Activo' : 'Archivado'])),
        'recepcionistas' => panel('Datos de la cuenta', detalle(['Correo' => $item['email'], 'Estado' => $activo ? 'Activo' : 'Inactivo', 'Acceso web' => $item['accesoWebHabilitado'] ? 'Habilitado' : 'Pendiente'])),
    };

    $invitacion = $_SESSION['invitacion'] ?? [];
    unset($_SESSION['invitacion']);
    if (($invitacion['tipo'] ?? '') === $tipo && ($invitacion['id'] ?? 0) === $id) {
        $html .= panel('Invitación de acceso', '<p>Compartí este código con el titular para que active su cuenta.</p>' . campo('invitacion', 'Código de invitación', $invitacion['datos']['token'], atributos: 'readonly') . '<p class="muted">Vence el ' . e(fechaHoraLocal($invitacion['datos']['venceEn'])) . '.</p>');
    }
    if ($activo && $tipo !== 'planes') $html .= '<form class="inline-action" method="post">' . csrf() . oculto('accion', 'invitar') . boton('Generar invitación', 'secondary') . '</form>';
    if ($activo && $permiteEditar) {
        $html .= '<details class="danger-zone"><summary>' . e($tipo === 'planes' ? 'Archivar plan' : 'Dar de baja') . '</summary><p>La ficha dejará de aparecer en los listados activos.</p>'
            . '<form method="post">' . csrf() . oculto('accion', 'baja') . oculto('version', $respuesta->etag()) . boton('Confirmar baja', 'secondary') . '</form></details>';
    }
    if ($tipo === 'clientes') {
        $html = '<div class="module-split"><section>' . listadoMembresias($api, $id) . '</section><section>' . $html . '</section></div>';
    }
    pagina($tipo === 'planes' ? $item['nombre'] : nombre($item), $html, $seccion, $acciones, volver: enlace($lista, '‹ ' . $titulo));
}

function editarCatalogo(ClienteApi $api, string $tipo, bool $post): void
{
    [$lista, $ficha, $editar, $titulo, $singular, $seccion] = catalogo($tipo);
    $id = parametro('id') !== '' ? identificador('id') : null;
    $ruta = '/' . $tipo . ($id ? '/' . $id : '');
    $datos = [];
    $version = '';
    $error = null;
    if ($post) {
        $version = parametro('version', 'post');
        $datos = $tipo === 'planes' ? datosFormulario(['nombre', 'descripcion', 'precio', 'tipoDuracion', 'duracionDias', 'cantidadSesiones', 'sesionesPorSemana', 'activo']) : datosPersona($tipo);
        try {
            if ($tipo === 'planes' && parametro('accion', 'post') === 'sugerir') {
                $validacion = new Validacion();
                $semana = $validacion->entero('sesionesPorSemana', $datos['sesionesPorSemana'], Validacion::FRECUENCIA_MAX);
                $validacion->opcion('tipoDuracion', $datos['tipoDuracion'], ['mesCalendario']);
                $validacion->exigirValido();
                $vista = $api->solicitar('POST', '/planes/previsualizar', ['sesionesPorSemana' => $semana])->datos;
                $datos['cantidadSesiones'] = $vista['cantidadSesiones'];
            } else {
                if ($tipo !== 'planes') validarPersona($tipo, $datos);
                $cuerpo = $tipo === 'planes' ? prepararPlan($datos) : $datos;
                $guardado = $api->solicitar($id ? 'PUT' : 'POST', $ruta, $cuerpo, version: $id ? validarVersion($version) : '')->datos;
                redirigir($ficha, ['id' => $guardado['id']], $id ? 'Cambios guardados.' : 'Alta registrada.');
            }
        } catch (FalloApi $error) {
            if ($error->estado === 401) throw $error;
            http_response_code($error->estado);
        }
    } elseif ($id) {
        $respuesta = $api->solicitar('GET', $ruta);
        $datos = $respuesta->datos;
        $version = $respuesta->etag();
    }
    $volver = $id ? enlace($ficha, '‹ Volver a la ficha', ['id' => $id]) : enlace($lista, '‹ ' . $titulo);
    $campos = $tipo === 'planes' ? camposPlan($datos, $error) : camposPersona($tipo, $datos, $error);
    pagina(($id ? 'Editar ' : 'Nuevo ') . $singular, formulario($campos, $id ? 'Guardar cambios' : 'Dar de alta', oculto('version', $version), $volver, $error), $seccion);
}

function prepararPlan(array $datos): array
{
    $validacion = new Validacion();
    $validacion->texto('nombre', $datos['nombre'], 80);
    $validacion->texto('descripcion', $datos['descripcion'], 255);
    $validacion->opcion('tipoDuracion', $datos['tipoDuracion'], ['dias', 'mesCalendario']);
    $validacion->opcion('activo', $datos['activo'], ['', '1']);
    $cuerpo = ['nombre' => $datos['nombre'], 'descripcion' => $datos['descripcion'], 'precio' => $validacion->importe('precio', $datos['precio'], admiteCero: true), 'tipoDuracion' => $datos['tipoDuracion'], 'cantidadSesiones' => $validacion->entero('cantidadSesiones', $datos['cantidadSesiones']), 'activo' => $datos['activo'] === '1'];
    if ($datos['tipoDuracion'] === 'dias') $cuerpo['duracionDias'] = $validacion->entero('duracionDias', $datos['duracionDias'], Validacion::DIAS_MAX);
    $validacion->exigirValido();
    return $cuerpo;
}

function camposPlan(array $datos, ?FalloApi $error): string
{
    return campo('nombre', 'Nombre del plan', $datos['nombre'] ?? '', atributos: 'required maxlength="80" data-texto-visible', error: $error)
        . campo('precio', 'Precio en pesos', $datos['precio'] ?? '', atributos: atributosImporte(), error: $error)
        . selector('tipoDuracion', 'Duración', ['mesCalendario' => 'Mes calendario', 'dias' => 'Días corridos'], $datos['tipoDuracion'] ?? 'mesCalendario', 'data-plan-tipo')
        . '<div data-plan-dias>' . campoEntero('duracionDias', 'Días corridos (para duración fija)', $datos['duracionDias'] ?? '', Validacion::DIAS_MAX, false, $error) . '</div>'
        . campoEntero('cantidadSesiones', 'Sesiones incluidas', $datos['cantidadSesiones'] ?? '', Validacion::INT_MAX, true, $error)
        . '<div class="plan-suggest" data-plan-sugerir>' . campoEntero('sesionesPorSemana', 'Visitas por semana', $datos['sesionesPorSemana'] ?? '', Validacion::FRECUENCIA_MAX, false, $error)
        . boton('Calcular cupo', 'secondary', 'name="accion" value="sugerir" formnovalidate') . '</div><div class="wide-field">'
        . areaTexto('descripcion', 'Descripción', $datos['descripcion'] ?? '', 'required maxlength="255"', $error) . '</div>'
        . '<label class="check-field"><input type="checkbox" name="activo" value="1" ' . (!isset($datos['activo']) || $datos['activo'] === '1' || $datos['activo'] === true ? 'checked' : '') . '> Plan activo</label>';
}
