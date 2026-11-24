<div class="module-split branded-detail membership-layout">
    <section class="session-pass brand-cut" aria-labelledby="pase-plan">
        <h2 id="pase-plan"><?= e($nombrePlan) ?></h2>
        <p class="session-pass-label">Sesiones disponibles</p>
        <p class="session-pass-count"><strong><?= e(str_pad((string) $periodo['sesionesDisponibles'], 2, '0', STR_PAD_LEFT)) ?></strong><span>de <?= e($periodo['sesionesIncluidas']) ?></span></p>
        <meter min="0" max="<?= e($periodo['sesionesIncluidas']) ?>" value="<?= e($periodo['sesionesDisponibles']) ?>" aria-label="Sesiones disponibles"></meter>
    </section>
    <section class="period-detail" aria-labelledby="periodo-titulo">
        <h2 id="periodo-titulo"><?= $periodo['estado'] === 'vigente' ? 'Período actual' : 'Período' ?></h2>
        <dl class="detail-lines">
            <div><dt>Estado</dt><dd><?= estadoMembresia($periodo['estado']) ?></dd></div>
            <div><dt>Inicio</dt><dd><time datetime="<?= e($periodo['fechaInicio']) ?>"><?= e(fechaBreve($periodo['fechaInicio'])) ?></time></dd></div>
            <div><dt>Vence</dt><dd><time datetime="<?= e($periodo['fechaVencimiento']) ?>"><?= e(fechaBreve($periodo['fechaVencimiento'])) ?></time></dd></div>
        </dl>
        <?php if ($enlaceDetalle !== ''): ?><p class="period-return"><?= $enlaceDetalle ?></p><?php endif; ?>
    </section>
</div>
