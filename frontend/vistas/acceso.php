<?= mostrarError($error) ?>
<form method="post" class="login-form">
    <?= csrf() ?>
    <div class="field"><?= campoEmail($email, 'username', $error) ?></div>
    <div class="field">
        <label for="contrasena">Contraseña</label>
        <div class="password">
            <input class="input" id="contrasena" type="password" name="contrasena" autocomplete="current-password" required minlength="1" maxlength="128" pattern="[^\p{Cc}]+">
            <button class="quiet password-toggle" type="button" data-password-toggle aria-controls="contrasena" aria-label="Mostrar contraseña" title="Mostrar contraseña" hidden>
                <svg class="kettlebell" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                    <path class="kettlebell-plain" fill-rule="evenodd" d="M8.5 2h7c2.65 0 4.31 2.44 3.39 4.9l-.92 2.47c1.94 1.62 3.03 3.81 3.03 6.28 0 3.83-2.65 6.35-6.63 6.35H9.63C5.65 22 3 19.48 3 15.65c0-2.47 1.09-4.66 3.03-6.28L5.11 6.9C4.19 4.44 5.85 2 8.5 2Zm.08 2.05c-1.26 0-2.03 1.12-1.61 2.25l.75 2.01a9.53 9.53 0 0 1 8.56 0l.75-2.01c.42-1.13-.35-2.25-1.61-2.25H8.58Z"/>
                    <g class="kettlebell-with-eye">
                        <path fill-rule="evenodd" d="M8.5 2h7c2.65 0 4.31 2.44 3.39 4.9l-.92 2.47c1.94 1.62 3.03 3.81 3.03 6.28 0 3.83-2.65 6.35-6.63 6.35H9.63C5.65 22 3 19.48 3 15.65c0-2.47 1.09-4.66 3.03-6.28L5.11 6.9C4.19 4.44 5.85 2 8.5 2Zm.08 2.05c-1.26 0-2.03 1.12-1.61 2.25l.75 2.01a9.53 9.53 0 0 1 8.56 0l.75-2.01c.42-1.13-.35-2.25-1.61-2.25H8.58ZM6.9 15.35c1.44-1.96 3.15-3.1 5.1-3.1s3.66 1.14 5.1 3.1c-1.44 1.96-3.15 3.1-5.1 3.1s-3.66-1.14-5.1-3.1Z"/>
                        <path d="m8.35 18.55 7.3-6.4" fill="none" stroke="currentColor" stroke-width="1.2"/>
                    </g>
                </svg>
            </button>
        </div>
        <?= errorCampo($error, 'contrasena') ?>
    </div>
    <?= boton('Ingresar') ?>
    <p class="activation-link"><?= enlace('activar', 'Activar cuenta') ?></p>
</form>
