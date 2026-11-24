<?php
declare(strict_types=1);

final class Validacion
{
    public const DNI = '[0-9]{8}';
    public const DNI_CON_PUNTOS = '[0-9]{2}\.[0-9]{3}\.[0-9]{3}';
    public const TELEFONO = '\+?[0-9]{1,15}';
    public const IMPORTE = '[0-9]{1,8}(?:[.,][0-9]{1,2})?';
    public const TOKEN = '[a-fA-F0-9]{64}';
    public const CONTRASENA_NUEVA_MIN = 8;
    public const CONTRASENA_MAX = 128;
    public const INT_MAX = 2147483647;
    public const DIAS_MAX = 3652058;
    public const FRECUENCIA_MAX = 536870911;
    private array $errores = [];

    public function agregar(string $campo, string $mensaje): void
    {
        $this->errores[$campo][] = $mensaje;
    }

    public function patron(string $campo, string $valor, string $expresion, string $mensaje): void
    {
        if (preg_match('~^(?:' . $expresion . ')$~uD', $valor) !== 1) $this->agregar($campo, $mensaje);
    }

    public function exigirValido(): void
    {
        if ($this->errores) throw new FalloApi(400, ['code' => 'validacion_frontend', 'title' => 'Revisá los datos enviados.', 'errors' => $this->errores]);
    }

    private static function longitud(string $valor): int
    {
        // HTML y el backend .NET cuentan unidades UTF-16.
        $caracteres = preg_match_all('/./su', $valor);
        return $caracteres === false ? PHP_INT_MAX : $caracteres + preg_match_all('/[\x{10000}-\x{10FFFF}]/u', $valor);
    }

    public function texto(string $campo, string $valor, int $maximo, bool $opcional = false): void
    {
        if ($opcional && $valor === '') return;
        $sinUniones = str_replace(["\u{200C}", "\u{200D}"], '', $valor);
        $visible = str_replace(["\u{2800}", "\u{115F}", "\u{1160}", "\u{3164}", "\u{FFA0}"], '', $valor);
        if (self::longitud($valor) > $maximo) $this->agregar($campo, "Usá hasta {$maximo} caracteres.");
        elseif (preg_match('/[\p{Cc}\p{Cf}]/u', $sinUniones) === 1) $this->agregar($campo, 'No se admiten saltos de línea ni caracteres de control.');
        elseif (preg_match('/[\p{L}\p{N}\p{P}\p{S}]/u', $visible) !== 1) $this->agregar($campo, 'Ingresá al menos un carácter visible.');
    }

    public function email(string $email): void
    {
        $this->texto('email', $email, 120);
        if (isset($this->errores['email'])) return;
        $partes = explode('@', $email);
        $etiqueta = '[\p{L}\p{N}](?:[\p{L}\p{N}\p{M}-]{0,61}[\p{L}\p{N}\p{M}])?';
        if (count($partes) !== 2 || !filter_var($partes[0] . '@example.com', FILTER_VALIDATE_EMAIL, FILTER_FLAG_EMAIL_UNICODE)
            || preg_match('~^' . $etiqueta . '(?:\.' . $etiqueta . ')+$~uD', $partes[1]) !== 1) {
            $this->agregar('email', 'Ingresá un correo válido con un dominio que incluya un punto.');
        }
    }

    public function contrasena(string $valor, bool $nueva = false): void
    {
        $minimo = $nueva ? self::CONTRASENA_NUEVA_MIN : 1;
        $maximo = self::CONTRASENA_MAX;
        $longitud = self::longitud($valor);
        if ($longitud < $minimo || $longitud > $maximo || preg_match('/\p{Cc}/u', $valor) === 1
            || ($nueva && (preg_match('/(?=[\x{0}-\x{FFFF}])\p{L}/u', $valor) !== 1 || preg_match('/(?=[\x{0}-\x{FFFF}])[\p{Nd}\p{P}\p{S}]/u', $valor) !== 1))) {
            $mensaje = $nueva
                ? "Usá entre {$minimo} y {$maximo} caracteres, con una letra y un número o símbolo."
                : "Ingresá una contraseña de {$minimo} a {$maximo} caracteres, sin controles.";
            $this->agregar('contrasena', $mensaje);
        }
    }

    public function entero(string $campo, string $valor, int $maximo = self::INT_MAX): int
    {
        if (preg_match('/^[1-9][0-9]{0,9}$/D', $valor) !== 1 || (float) $valor > $maximo) {
            $this->agregar($campo, "Ingresá un número entero entre 1 y {$maximo}.");
            return 0;
        }
        return (int) $valor;
    }

    public function opcion(string $campo, string $valor, array $opciones): void
    {
        if (!in_array($valor, $opciones, true)) $this->agregar($campo, 'Elegí una de las opciones disponibles.');
    }

    public function fecha(string $campo, string $valor, ?string $maximo = null): void
    {
        if (!preg_match('/^(\d{4})-(\d{2})-(\d{2})$/D', $valor, $partes) || !checkdate((int) $partes[2], (int) $partes[3], (int) $partes[1])) {
            $this->agregar($campo, 'Ingresá una fecha válida.');
        } elseif ($maximo !== null && $valor > $maximo) {
            $this->agregar($campo, 'La fecha no puede ser posterior a hoy.');
        }
    }

    public function importe(string $campo, string $valor, bool $admiteCero): float
    {
        $this->patron($campo, $valor, self::IMPORTE, 'Ingresá hasta ocho dígitos enteros y dos decimales, sin separador de miles.');
        if (isset($this->errores[$campo])) return 0;
        $numero = (float) str_replace(',', '.', $valor);
        if (!$admiteCero && $numero < 0.01) $this->agregar($campo, 'El importe mínimo es $ 0,01.');
        return $numero;
    }
}

function recortarEntrada(string $valor): string
{
    return preg_replace('/^\p{Z}+|\p{Z}+$/u', '', $valor) ?? $valor;
}

function validarPersona(string $tipo, array $datos): void
{
    $validacion = new Validacion();
    $validacion->texto('nombre', $datos['nombre'], 60);
    $validacion->texto('apellido', $datos['apellido'], 60);
    $validacion->email($datos['email']);
    if ($tipo === 'clientes') {
        $validacion->patron('dni', $datos['dni'], Validacion::DNI, 'Ingresá exactamente 8 dígitos de DNI, sin puntos.');
        $validacion->patron('telefono', $datos['telefono'], Validacion::TELEFONO, 'Ingresá de 1 a 15 dígitos, con + al comienzo si corresponde.');
    }
    $validacion->exigirValido();
}
