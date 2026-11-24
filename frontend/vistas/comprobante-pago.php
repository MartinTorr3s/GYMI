<div class="module-split branded-detail receipt-layout">
    <article class="payment-ticket brand-cut" aria-label="Datos del comprobante">
        <header class="payment-ticket-amount">
            <span class="print-brand">GYMI</span>
            <span>Importe registrado</span>
            <strong><?= e(dinero($pago['monto'])) ?></strong>
        </header>
        <div class="payment-ticket-body">
            <?= detalle([
                'Socio' => $nombreSocio,
                'Fecha' => fechaBreve($pago['fechaPago']),
                'Medio' => $pago['medioPago'],
                'Tipo' => $pago['tipo'] === 'renovacion' ? 'Renovación' : 'Primer pago',
            ] + (($pago['observacion'] ?? '') !== '' ? ['Observación' => $pago['observacion']] : [])) ?>
        </div>
    </article>
    <section class="period-detail" aria-labelledby="periodo-cubierto-titulo">
        <h2 id="periodo-cubierto-titulo">Período cubierto</h2>
        <?= detalle(['Inicio' => fechaBreve($periodo['fechaInicio']), 'Vence' => fechaBreve($periodo['fechaVencimiento'])]) ?>
        <p class="period-return"><?= $volver ?></p>
    </section>
</div>
