<?php
declare(strict_types=1);

final class Configuracion
{
    public function __construct(
        public readonly string $apiUrl,
        public readonly int $apiTimeout,
        public readonly bool $cookieSegura,
        public readonly string $rutaSesiones,
    ) {}

    public static function cargar(string $directorio): self
    {
        $archivo = $directorio . '/.env';
        $valores = is_file($archivo) ? @parse_ini_file($archivo, false, INI_SCANNER_RAW) : [];
        if ($valores === false) {
            throw new RuntimeException('El archivo .env no tiene un formato válido.');
        }

        $obtener = static function (string $clave, string $predeterminado) use ($valores): string {
            $externo = getenv($clave);
            return trim($externo !== false ? $externo : (string) ($valores[$clave] ?? $predeterminado));
        };
        $url = rtrim($obtener('GYMI_API_URL', 'http://127.0.0.1:5180'), '/');
        $partes = parse_url($url);
        if (!filter_var($url, FILTER_VALIDATE_URL) || !in_array($partes['scheme'] ?? '', ['http', 'https'], true)
            || array_intersect(['user', 'pass', 'query', 'fragment'], array_keys($partes ?: []))) {
            throw new RuntimeException('Configurá GYMI_API_URL como una URL HTTP o HTTPS sin credenciales.');
        }
        $timeout = filter_var($obtener('GYMI_API_TIMEOUT', '8'), FILTER_VALIDATE_INT, ['options' => ['min_range' => 1, 'max_range' => 60]]);
        $segura = filter_var($obtener('GYMI_COOKIE_SEGURA', 'false'), FILTER_VALIDATE_BOOL, FILTER_NULL_ON_FAILURE);
        if ($timeout === false || $segura === null) {
            throw new RuntimeException('Revisá GYMI_API_TIMEOUT y GYMI_COOKIE_SEGURA en .env.');
        }
        $rutaSesiones = $obtener('GYMI_SESSION_PATH', 'var/sesiones');
        if ($rutaSesiones === '') throw new RuntimeException('Configurá una carpeta para las sesiones de PHP.');
        if (!preg_match('~^(?:[a-z]:[\\\\/]|[\\\\/])~i', $rutaSesiones)) {
            $rutaSesiones = $directorio . '/' . $rutaSesiones;
        }
        if ((!is_dir($rutaSesiones) && !@mkdir($rutaSesiones, 0700, true) && !is_dir($rutaSesiones)) || !is_writable($rutaSesiones)) {
            throw new RuntimeException('La carpeta de sesiones de PHP necesita permisos de escritura.');
        }
        $resuelta = realpath($rutaSesiones);
        $publica = realpath($directorio . '/public');
        if ($resuelta === false || $publica === false || str_starts_with(strtolower($resuelta) . DIRECTORY_SEPARATOR, strtolower($publica) . DIRECTORY_SEPARATOR)) {
            throw new RuntimeException('La carpeta de sesiones tiene que estar fuera de public/.');
        }
        return new self($url, $timeout, $segura, $resuelta);
    }
}
