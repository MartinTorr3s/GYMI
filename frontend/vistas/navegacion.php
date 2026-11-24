<?php
$menu = match (rol()) {
    'administrador' => ['recepcion' => 'Recepción', 'socios' => 'Socios', 'planes' => 'Planes', 'vencimientos' => 'Vencimientos', 'equipo' => 'Equipo', 'reportes' => 'Reportes'],
    'recepcionista' => ['recepcion' => 'Recepción', 'socios' => 'Socios', 'planes' => 'Planes', 'vencimientos' => 'Vencimientos'],
    'cliente' => ['mi-membresia' => 'Mi membresía', 'mis-pagos' => 'Mis pagos'],
};
$activo = match ($seccion) {
    'membresias', 'pagos' => 'socios',
    'cuenta' => parametro('pantalla') ?: inicio(),
    default => $seccion,
};
?>
<header class="top">
    <a class="brand" href="<?= e(url(inicio())) ?>" aria-label="Inicio de GYMI"></a>
    <nav aria-label="Secciones de GYMI">
        <?php foreach ($menu as $ruta => $etiqueta): ?>
            <a href="<?= e(url($ruta)) ?>" <?= $ruta === $activo ? 'aria-current="page"' : '' ?>><?= e($etiqueta) ?></a>
        <?php endforeach; ?>
    </nav>
    <div class="top-account">
        <span><?= e(nombre($_SESSION['sesion'])) ?></span>
        <form method="post" action="<?= e(url('salir')) ?>"><?= csrf() ?><button class="quiet">Cerrar sesión</button></form>
    </div>
</header>
