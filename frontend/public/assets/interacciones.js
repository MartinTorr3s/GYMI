'use strict';

const mostrarClave = document.querySelector('[data-password-toggle]');
if (mostrarClave) {
  const entrada = document.getElementById(mostrarClave.getAttribute('aria-controls'));
  mostrarClave.hidden = false;
  mostrarClave.addEventListener('click', () => {
    const visible = entrada.type === 'password';
    const etiqueta = visible ? 'Ocultar contraseña' : 'Mostrar contraseña';
    entrada.type = visible ? 'text' : 'password';
    mostrarClave.dataset.visible = String(visible);
    mostrarClave.setAttribute('aria-label', etiqueta);
    mostrarClave.title = etiqueta;
  });
}

const tipoDuracion = document.querySelector('[data-plan-tipo]');
if (tipoDuracion) {
  const bloqueDias = document.querySelector('[data-plan-dias]');
  const sugerencia = document.querySelector('[data-plan-sugerir]');
  const campoDias = bloqueDias.querySelector('input');
  const frecuencia = sugerencia.querySelector('input');
  sugerencia.querySelector('button').addEventListener('click', (evento) => {
    frecuencia.required = true;
    if (!frecuencia.reportValidity()) evento.preventDefault();
    frecuencia.required = false;
  });
  function actualizarDuracion() {
    const fija = tipoDuracion.value === 'dias';
    bloqueDias.hidden = !fija;
    campoDias.disabled = !fija;
    campoDias.required = fija;
    sugerencia.hidden = fija;
  }
  tipoDuracion.addEventListener('change', actualizarDuracion);
  actualizarDuracion();
}

const imprimir = document.querySelector('[data-print]');
if (imprimir) {
  imprimir.hidden = false;
  imprimir.addEventListener('click', () => window.print());
}

for (const dni of document.querySelectorAll('[data-dni]')) {
  dni.addEventListener('input', () => dni.setCustomValidity(''));
  dni.addEventListener('invalid', () => {
    dni.setCustomValidity(dni.value.trim() ? dni.title : 'Ingresá el DNI.');
  });
}

const confirmacion = document.querySelector('[data-confirmar-clave]');
if (confirmacion) {
  const nueva = confirmacion.form.elements.contrasena;
  const comparar = () => confirmacion.setCustomValidity(confirmacion.value && confirmacion.value !== nueva.value ? 'Las contraseñas no coinciden.' : '');
  nueva.addEventListener('input', comparar);
  confirmacion.addEventListener('input', comparar);
}

const desde = document.querySelector('[data-fecha-desde]');
const hasta = document.querySelector('[data-fecha-hasta]');
if (desde && hasta) {
  const validarFechas = () => {
    desde.required = Boolean(hasta.value);
    hasta.required = Boolean(desde.value);
    hasta.setCustomValidity(desde.value && hasta.value && desde.value > hasta.value ? 'La fecha Hasta tiene que ser igual o posterior a Desde.' : '');
  };
  desde.addEventListener('input', validarFechas);
  hasta.addEventListener('input', validarFechas);
  validarFechas();
}

for (const importe of document.querySelectorAll('[data-importe]')) {
  const validarImporte = () => {
    let mensaje = '';
    if (importe.validity.patternMismatch) {
      mensaje = importe.title;
    } else if (importe.hasAttribute('data-importe-positivo') && importe.value && Number(importe.value.replace(',', '.')) === 0) {
      mensaje = 'El importe mínimo es $ 0,01.';
    }
    importe.setCustomValidity(mensaje);
  };
  importe.addEventListener('input', validarImporte);
  validarImporte();
}

for (const campo of document.querySelectorAll('[data-entero-max]')) {
  const maximo = Number(campo.dataset.enteroMax);
  const validarEntero = () => campo.setCustomValidity(campo.value && (!/^[1-9]\d*$/.test(campo.value) || Number(campo.value) > maximo) ? `Ingresá un entero entre 1 y ${maximo}.` : '');
  campo.addEventListener('input', validarEntero);
  validarEntero();
}

for (const campo of document.querySelectorAll('[data-texto-visible]')) {
  const validarTexto = () => {
    const valor = campo.value.replace(/^\p{Z}+|\p{Z}+$/gu, '');
    let error = '';
    if (/[\p{Cc}\p{Cf}]/u.test(valor.replace(/[\u200C\u200D]/gu, ''))) {
      error = 'No se admiten saltos de línea ni caracteres de control.';
    } else if ((campo.required || valor) && !/[\p{L}\p{N}\p{P}\p{S}]/u.test(valor.replace(/[\u2800\u115F\u1160\u3164\uFFA0]/gu, ''))) {
      error = 'Ingresá al menos un carácter visible.';
    }
    campo.setCustomValidity(error);
  };
  campo.addEventListener('input', validarTexto);
  validarTexto();
}
