<!doctype html>
<html lang="es-AR">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title><?= e($titulo) ?> · GYMI</title>
    <link rel="stylesheet" href="assets/estilos.css">
    <script src="assets/interacciones.js" defer></script>
</head>
<body>
<?php if (rol() !== ''): ?>
    <?php require __DIR__ . '/navegacion.php'; ?>
<?php endif; ?>
<?php if ($seccion === 'acceso'): ?>
    <main class="login screen enter" id="contenido">
        <div class="login-side">
            <div class="brand" role="img" aria-label="GYMI"></div>
            <div class="login-content">
                <h1><?= e($titulo) ?></h1>
                <?php if ($aviso !== ''): ?><p class="notice" role="status"><?= e($aviso) ?></p><?php endif; ?>
                <?= $contenido ?>
            </div>
        </div>
        <div class="login-art" aria-hidden="true"><div class="circle"><img class="art-photo" src="assets/acceso.webp" alt=""></div></div>
    </main>
<?php else: ?>
    <?php if ($seccion === 'recepcion'): ?>
        <div class="shift-band enter"><div class="shift-date">Turno del <?= e(fecha(fechaHoy())) ?></div></div>
    <?php endif; ?>
    <main class="module <?= $seccion === 'recepcion' ? 'mesa-recepcion' : '' ?>" id="contenido" data-detail="<?= e($seccion === 'recepcion' ? '' : $seccion) ?>">
        <?php if ($aviso !== ''): ?><p class="notice" role="status"><?= e($aviso) ?></p><?php endif; ?>
        <?php if ($volver !== ''): ?><div class="crumb"><?= $volver ?></div><?php endif; ?>
        <header class="module-head">
            <div><h1><?= e($titulo) ?></h1><?php if ($descripcion !== ''): ?><p><?= e($descripcion) ?></p><?php endif; ?></div>
            <?php if ($acciones !== ''): ?><div class="module-actions"><?= $acciones ?></div><?php endif; ?>
        </header>
        <?= $contenido ?>
    </main>
<?php endif; ?>
</body>
</html>
