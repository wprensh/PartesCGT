import { describe, expect, it } from 'vitest';
import { clampPageIndex, lastPageIndex, pageSlice, PAGE_SIZE, rangeLabel } from './pagination';

const rows = Array.from({ length: 24 }, (_, i) => i + 1);

describe('pagination', () => {
  it('lista como máximo 10 filas por página', () => {
    expect(PAGE_SIZE).toBe(10);
    expect(pageSlice(rows, 0)).toEqual([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);
    expect(pageSlice(rows, 2)).toEqual([21, 22, 23, 24]);
  });

  it('calcula la última página', () => {
    expect(lastPageIndex(0)).toBe(0);
    expect(lastPageIndex(10)).toBe(0);
    expect(lastPageIndex(11)).toBe(1);
  });

  it('una página fuera de rango muestra la última con datos', () => {
    expect(clampPageIndex(3, 20)).toBe(1);
    expect(clampPageIndex(-1, 20)).toBe(0);
    expect(pageSlice(rows.slice(0, 20), 2)).toEqual(rows.slice(10, 20));
  });

  it('describe el rango en español', () => {
    expect(rangeLabel(0, 10, 24)).toBe('1 – 10 de 24');
    expect(rangeLabel(2, 10, 24)).toBe('21 – 24 de 24');
    expect(rangeLabel(0, 10, 0)).toBe('0 de 0');
  });
});
