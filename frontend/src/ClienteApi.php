<?php
declare(strict_types=1);

final class FalloApi extends RuntimeException
{
    public function __construct(
        public readonly int $estado,
        public readonly array $problema,
        public readonly array $cabeceras = [],
    ) {
        parent::__construct((string) ($problema['title'] ?? 'GYMI no pudo completar la operación.'));
    }

    public function codigo(): string
    {
        return (string) ($this->problema['code'] ?? 'respuesta_invalida');
    }
}

final class RespuestaApi
{
    public function __construct(public readonly array $datos, public readonly array $cabeceras) {}

    public function etag(): string
    {
        return $this->cabeceras['etag'] ?? '';
    }
}

final class ClienteApi
{
    public function __construct(private Configuracion $configuracion) {}

    public function solicitar(string $metodo, string $ruta, ?array $datos = null, array $consulta = [], string $version = '', bool $autenticada = true): RespuestaApi
    {
        $url = $this->configuracion->apiUrl . $ruta;
        if ($consulta !== []) {
            $url .= '?' . http_build_query($consulta, '', '&', PHP_QUERY_RFC3986);
        }
        $cabeceras = ['Accept: application/json, application/problem+json'];
        if ($autenticada && isset($_SESSION['sesion']['accessToken'])) {
            $cabeceras[] = 'Authorization: Bearer ' . $_SESSION['sesion']['accessToken'];
        }
        if ($version !== '') {
            if (!preg_match('/^"[a-f0-9-]{36}"$/iD', $version)) {
                throw new FalloApi(400, ['code' => 'version_invalida', 'title' => 'La versión de la ficha no es válida. Volvé a abrirla.']);
            }
            $cabeceras[] = 'If-Match: ' . $version;
        }
        $conexion = curl_init($url);
        $recibidas = [];
        $opciones = [
            CURLOPT_CUSTOMREQUEST => $metodo,
            CURLOPT_HTTPHEADER => $cabeceras,
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_FOLLOWLOCATION => false,
            CURLOPT_CONNECTTIMEOUT => min(3, $this->configuracion->apiTimeout),
            CURLOPT_TIMEOUT => $this->configuracion->apiTimeout,
            CURLOPT_HEADERFUNCTION => static function ($conexion, string $linea) use (&$recibidas): int {
                if (preg_match('/^HTTP\//i', $linea)) {
                    $recibidas = [];
                } elseif (str_contains($linea, ':')) {
                    [$nombre, $valor] = explode(':', $linea, 2);
                    $recibidas[strtolower(trim($nombre))] = trim($valor);
                }
                return strlen($linea);
            },
        ];
        if ($datos !== null) {
            $cabeceras[] = 'Content-Type: application/json';
            $opciones[CURLOPT_HTTPHEADER] = $cabeceras;
            $opciones[CURLOPT_POSTFIELDS] = json_encode($datos, JSON_THROW_ON_ERROR | JSON_INVALID_UTF8_SUBSTITUTE);
        }
        curl_setopt_array($conexion, $opciones);
        $cuerpo = curl_exec($conexion);
        $estado = (int) curl_getinfo($conexion, CURLINFO_RESPONSE_CODE);
        curl_close($conexion);
        if ($cuerpo === false) {
            throw new FalloApi(503, ['code' => 'conexion_api', 'title' => 'No pudimos comunicarnos con el servidor de GYMI. Podés reintentar.']);
        }

        $contenido = $cuerpo === '' ? [] : json_decode($cuerpo, true);
        if ($estado < 200 || $estado > 299) {
            $problema = is_array($contenido) ? $contenido : ['title' => 'El servidor de GYMI no pudo completar la operación.'];
            throw new FalloApi($estado >= 400 ? $estado : 502, $problema, $recibidas);
        }
        if (!is_array($contenido) || ($estado !== 204 && $cuerpo === '')) {
            throw new FalloApi(502, ['code' => 'respuesta_invalida', 'title' => 'El servidor de GYMI respondió en un formato inesperado.']);
        }
        return new RespuestaApi($contenido, $recibidas);
    }
}
