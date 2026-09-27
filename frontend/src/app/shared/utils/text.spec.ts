import { describe, expect, it } from 'vitest';
import { compareText, normalize, slugify } from './text';

describe('texto', () => {
  it('normalize quita tildes, mayúsculas y espacios de los extremos', () => {
    expect(normalize('  Gráficas ')).toBe('graficas');
  });

  it('slugify genera URLs limpias', () => {
    expect(slugify('Tarjetas madre')).toBe('tarjetas-madre');
    expect(slugify('RAM portátil')).toBe('ram-portatil');
    expect(slugify('  80+ Bronze!! ')).toBe('80-bronze');
  });

  it('compareText ordena números de forma natural', () => {
    expect(['16 GB', '8 GB', '480 GB'].sort(compareText)).toEqual(['8 GB', '16 GB', '480 GB']);
  });
});
