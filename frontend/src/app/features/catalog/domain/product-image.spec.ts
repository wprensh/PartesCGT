import { describe, expect, it } from 'vitest';
import { MAX_IMAGE_BYTES, safeImageUrl, validateImageFile } from './product-image';

describe('safeImageUrl', () => {
  it('acepta imágenes subidas y URL http(s)', () => {
    expect(safeImageUrl('/api/files/products/abc.webp')).toBe('/api/files/products/abc.webp');
    expect(safeImageUrl(' https://cdn.ejemplo.co/a.png ')).toBe('https://cdn.ejemplo.co/a.png');
  });

  it('descarta esquemas peligrosos, rutas arbitrarias y vacíos', () => {
    for (const url of ['javascript:alert(1)', 'data:image/png;base64,AAA', '/etc/passwd', '//evil.co/a.png', '', null, undefined]) {
      expect(safeImageUrl(url)).toBeNull();
    }
  });
});

describe('validateImageFile', () => {
  it('acepta JPG, PNG y WebP de hasta 2 MB', () => {
    expect(validateImageFile({ type: 'image/webp', size: MAX_IMAGE_BYTES })).toBeNull();
  });

  it('rechaza otros formatos y archivos grandes', () => {
    expect(validateImageFile({ type: 'image/gif', size: 10 })).toContain('JPG');
    expect(validateImageFile({ type: 'image/png', size: MAX_IMAGE_BYTES + 1 })).toContain('2 MB');
  });
});
