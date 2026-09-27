import type { HttpErrorResponse } from '@angular/common/http';
import { describe, expect, it } from 'vitest';
import { describeHttpError } from './http-error';

// Objeto simple con la forma de HttpErrorResponse: instanciarlo cargaría HttpClient (necesita el compilador JIT).
const error = (status: number, body: unknown = null) =>
  ({ status, error: body, statusText: '', message: '' }) as HttpErrorResponse;

describe('describeHttpError', () => {
  it('en desarrollo da pistas técnicas', () => {
    expect(describeHttpError(error(0), true)).toContain('5080');
    expect(describeHttpError(error(200), true)).toContain('proxy');
  });

  it('en producción no habla de puertos ni de proxy', () => {
    for (const status of [0, 200, 500]) {
      const message = describeHttpError(error(status), false);
      expect(message).not.toMatch(/5080|proxy|Error \d+/);
    }
  });

  it('si la API está despertando, pide esperar', () => {
    for (const status of [502, 503, 504]) {
      expect(describeHttpError(error(status), false)).toContain('iniciando');
    }
  });

  it('muestra el detalle de negocio y los errores de validación de la API', () => {
    expect(describeHttpError(error(409, { detail: 'Solo quedan 2 unidades' }), false)).toBe('Solo quedan 2 unidades');
    expect(describeHttpError(error(400, { errors: { Email: ['Correo inválido'] } }), false)).toBe('Correo inválido');
  });
});
