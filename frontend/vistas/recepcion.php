<?php $consulta = $resultado['consulta'] ?? null; ?>
<div class="module-split reception-split">
    <div>
        <?php if ($altaAbierta): ?>
            <?= panel('Nuevo socio', formulario(camposPersona('clientes', $datosAlta, $error), 'Dar de alta', oculto('accion', 'alta-rapida'), enlace('recepcion', 'Cancelar'), $error)) ?>
        <?php else: ?>
            <?= mostrarError($error) ?>
            <form method="post" action="<?= e(url('recepcion')) ?>">
                <?= csrf() . oculto('accion', 'buscar') ?>
                <label for="dni">DNI del socio</label>
                <div class="search-row">
                    <input class="input" id="dni" name="dni" value="<?= e($resultado['dni'] ?? '') ?>" placeholder="Ingresá el DNI" <?= atributosDni(true) ?>>
                    <button class="<?= $consulta ? 'secondary' : 'primary' ?>">Buscar socio</button>
                </div>
            </form>
            <?php if ($consulta || !empty($resultado['sinSocio'])): ?>
                <section class="result reveal" aria-label="Resultado de la búsqueda">
                    <div class="result-summary">
                        <div class="member-summary">
                            <strong><?= e($consulta ? nombre($consulta) : 'Socio no encontrado') ?></strong>
                            <p class="result-status <?= !empty($consulta['puedeIngresar']) ? 'status' : 'blocked' ?>"><?= e(isset($resultado['asistencia']) ? 'Ingreso registrado' : ($consulta ? (!empty($consulta['puedeIngresar']) ? 'Puede ingresar' : mensajeIngreso($consulta['codigoImpedimento'] ?? '')) : 'No encontramos un socio con ese DNI.')) ?></p>
                        </div>
                        <?php if (isset($consulta['membresia']['sesionesDisponibles'])): ?>
                            <div class="session-balance"><span class="session-count"><?= e(str_pad((string) ($resultado['asistencia']['sesionesRestantes'] ?? $consulta['membresia']['sesionesDisponibles']), 2, '0', STR_PAD_LEFT)) ?></span><span>sesiones disponibles</span></div>
                        <?php endif; ?>
                    </div>
                    <div class="result-actions">
                        <?php if (!empty($resultado['sinSocio'])): ?>
                            <?= enlace('recepcion', 'Dar de alta a este socio', ['alta' => 1, 'precargar' => 1], 'primary button-link') ?>
                        <?php elseif (!empty($consulta['puedeIngresar']) && !isset($resultado['asistencia'])): ?>
                            <form method="post"><?= csrf() . oculto('accion', 'confirmar') . oculto('operacion', $resultado['operacion']) . boton('Confirmar ingreso') ?></form>
                        <?php elseif (($consulta['codigoImpedimento'] ?? '') === 'membresia_no_asignada'): ?>
                            <?= enlace('asignar', 'Asignar membresía', ['cliente' => $consulta['clienteId'], 'volver' => 'recepcion'], 'secondary button-link') ?>
                        <?php endif; ?>
                        <form method="post"><?= csrf() . oculto('accion', 'limpiar') . boton('Nueva búsqueda', 'quiet-link') ?></form>
                    </div>
                </section>
            <?php endif; ?>
        <?php endif; ?>
    </div>
    <aside class="alerts" aria-labelledby="vencimientos-titulo">
        <h2 id="vencimientos-titulo">Membresías vencidas</h2>
        <?php if ($errorAlertas): ?><p class="muted">No pudimos cargar los vencimientos.</p>
        <?php elseif (!$vencimientos): ?><p class="muted">No hay vencimientos para revisar.</p>
        <?php else: ?><ul><?php foreach ($vencimientos as $fila): ?><li><?= enlace('socio', nombre($fila), ['id' => $fila['clienteId']], 'entity-link') ?><time datetime="<?= e($fila['fechaVencimiento']) ?>"><?= e(fecha($fila['fechaVencimiento'])) ?></time></li><?php endforeach; ?></ul><?php endif; ?>
        <?= enlace('vencimientos', 'Ver vencimientos', clase: 'quiet-link due-link') ?>
    </aside>
</div>
