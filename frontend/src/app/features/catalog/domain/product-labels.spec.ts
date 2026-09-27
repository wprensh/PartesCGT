import { describe, expect, it } from 'vitest';
import { categoryPath, productCode, productSku } from './product-labels';
import { aProduct } from './testing/product.fixture';

describe('product-labels', () => {
  it('la referencia rellena con ceros', () => {
    expect(productSku(aProduct({ id: 7 }))).toBe('#0007');
    expect(productSku(aProduct({ id: 12345 }))).toBe('#12345');
  });

  it('en una subcategoría usa el código y la ruta del padre', () => {
    const ssd = aProduct({ category: 'SSD NVMe', parentCategory: 'Almacenamiento' });
    expect(productCode(ssd)).toBe(productCode(aProduct({ category: 'Almacenamiento' })));
    expect(categoryPath(ssd)).toBe('Almacenamiento › SSD NVMe');
  });

  it('una categoría principal se muestra sola', () => {
    expect(categoryPath(aProduct({ category: 'Gráficas' }))).toBe('Gráficas');
    expect(productCode(aProduct({ category: 'Gráficas' }))).toBe('GPU');
  });
});
