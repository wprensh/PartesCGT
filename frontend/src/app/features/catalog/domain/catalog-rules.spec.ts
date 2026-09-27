import { describe, expect, it } from 'vitest';
import { categoryCode, groupByFamily } from './category-families';
import { stockStatus } from './product.model';
import { productSpecs } from './product-specs';
import { sortProducts } from './sorting';
import { aProduct } from './testing/product.fixture';

describe('categoryCode', () => {
  it('usa el código conocido sin importar tildes ni mayúsculas', () => {
    expect(categoryCode('Gráficas')).toBe('GPU');
    expect(categoryCode('TARJETAS MADRE')).toBe('MB');
  });

  it('para categorías nuevas usa las tres primeras letras', () => {
    expect(categoryCode('Monitores')).toBe('MON');
  });
});

describe('groupByFamily', () => {
  it('agrupa en el orden del menú y manda las desconocidas a "Otros"', () => {
    const groups = groupByFamily([{ name: 'Memoria' }, { name: 'Monitores' }, { name: 'Procesadores' }, { name: 'Energía' }]);
    expect(groups.map(g => [g.name, g.items.map(i => i.name)])).toEqual([
      ['Componentes', ['Procesadores', 'Memoria']],
      ['Almacenamiento y energía', ['Energía']],
      ['Otros', ['Monitores']]
    ]);
  });
});

describe('productSpecs', () => {
  it('prefiere los atributos cargados en el panel (máximo 3)', () => {
    const p = aProduct({ attributes: ['a', 'b', 'c', 'd'].map(v => ({ name: v, value: v.toUpperCase() })) });
    expect(productSpecs(p)).toEqual(['A', 'B', 'C']);
  });

  it('sin atributos, lee el texto y deja una especificación por unidad', () => {
    const p = aProduct({ name: 'Tarjeta gráfica RTX 4060 8 GB', description: 'Consumo de 115 W. Requiere fuente de 550 W.' });
    expect(productSpecs(p)).toEqual(['8 GB', '115 W']);
  });
});

describe('stockStatus', () => {
  it('distingue agotado, pocas unidades y disponible', () => {
    expect(stockStatus({ stock: 0 })).toBe('out');
    expect(stockStatus({ stock: 5 })).toBe('low');
    expect(stockStatus({ stock: 6 })).toBe('ok');
  });
});

describe('sortProducts', () => {
  const list = [
    aProduct({ id: 1, price: 300, rating: 4, reviewCount: 1 }),
    aProduct({ id: 2, price: 100, rating: null }),
    aProduct({ id: 3, price: 200, rating: 4, reviewCount: 9 })
  ];
  const ids = (key: Parameters<typeof sortProducts>[1]) => sortProducts(list, key).map(p => p.id);

  it('ordena por precio en ambos sentidos', () => {
    expect(ids('price-asc')).toEqual([2, 3, 1]);
    expect(ids('price-desc')).toEqual([1, 3, 2]);
  });

  it('en empate de calificación gana el que tiene más reseñas', () => {
    expect(ids('rating')).toEqual([3, 1, 2]);
  });

  it('"por categoría" respeta el orden del backend y no muta la lista', () => {
    expect(ids('relevance')).toEqual([1, 2, 3]);
    sortProducts(list, 'price-asc');
    expect(list.map(p => p.id)).toEqual([1, 2, 3]);
  });
});
