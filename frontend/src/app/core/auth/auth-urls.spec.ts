import { describe, expect, it } from 'vitest';
import { shouldAttachToken } from './auth-urls';

describe('shouldAttachToken', () => {
  it('el perfil y el cambio de contraseña llevan token (aunque estén bajo /api/auth)', () => {
    expect(shouldAttachToken('/api/auth/me')).toBe(true);
    expect(shouldAttachToken('/api/auth/change-password')).toBe(true);
    expect(shouldAttachToken('/api/suppliers')).toBe(true);
  });

  it('el login y las URL externas no', () => {
    expect(shouldAttachToken('/api/auth/login')).toBe(false);
    expect(shouldAttachToken('https://otro-sitio.co/api/x')).toBe(false);
  });
});
