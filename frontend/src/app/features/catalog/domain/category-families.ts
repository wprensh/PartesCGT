import { compareText, normalize } from '@shared/utils/text';

// Código corto por categoría principal, al estilo de las serigrafías de una placa (U1, R3…).
const CODES: Record<string, string> = {
  almacenamiento: 'SSD',
  memoria: 'RAM',
  procesadores: 'CPU',
  graficas: 'GPU',
  energia: 'PSU',
  'tarjetas madre': 'MB',
  accesorios: 'ACC',
  perifericos: 'PER'
};

export function categoryCode(category: string): string {
  const n = normalize(category);
  return CODES[n] ?? (n.replace(/[^a-z]/g, '').slice(0, 3).toUpperCase() || '—');
}

/** Familias del menú, en el orden en que se muestran. Una categoría nueva que no esté aquí cae en "Otros". */
const FAMILIES: { name: string; categories: string[] }[] = [
  { name: 'Componentes', categories: ['procesadores', 'tarjetas madre', 'memoria', 'graficas'] },
  { name: 'Almacenamiento y energía', categories: ['almacenamiento', 'energia'] },
  { name: 'Complementos', categories: ['perifericos', 'accesorios'] }
];

export interface CategoryFamily<T> { name: string; items: T[]; }

export function groupByFamily<T extends { name: string }>(categories: T[]): CategoryFamily<T>[] {
  const byName = new Map(categories.map(c => [normalize(c.name), c]));
  const known = new Set(FAMILIES.flatMap(f => f.categories));

  const groups = FAMILIES.map(f => ({
    name: f.name,
    items: f.categories.map(n => byName.get(n)).filter((c): c is T => !!c)
  }));
  const others = categories
    .filter(c => !known.has(normalize(c.name)))
    .sort((a, b) => compareText(a.name, b.name));

  return [...groups, { name: 'Otros', items: others }].filter(g => g.items.length > 0);
}
