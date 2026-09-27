import { describe, expect, it } from 'vitest';
import { PERMISSIONS } from '@core/auth/permissions';
import { passwordProblem, PermissionDefinition } from './access.model';
import { allowedSections } from './admin-sections';
import { groupByModule, isImpliedOnly, togglePermission, withImplied } from './permission-grid';
import { grossMargin } from './supplier.model';

const catalog: PermissionDefinition[] = [
  { code: 'products.view', module: 'Productos', label: 'Ver', implies: null },
  { code: 'products.manage', module: 'Productos', label: 'Gestionar', implies: 'products.view' },
  { code: 'users.manage', module: 'Usuarios', label: 'Administrar', implies: null }
];

describe('grilla de permisos', () => {
  it('agrupa por módulo en el orden recibido', () => {
    expect(groupByModule(catalog).map(m => [m.name, m.permissions.length])).toEqual([['Productos', 2], ['Usuarios', 1]]);
  });

  it('gestionar incluye ver, y ver queda bloqueado mientras gestionar esté marcado', () => {
    expect([...withImplied(['products.manage'], catalog)].sort()).toEqual(['products.manage', 'products.view']);
    expect(isImpliedOnly('products.view', ['products.manage'], catalog)).toBe(true);
    expect(isImpliedOnly('products.view', ['products.view'], catalog)).toBe(false);
  });

  it('marcar gestionar no duplica ver; desmarcarlo conserva ver', () => {
    const marked = togglePermission(['products.view'], 'products.manage', catalog);
    expect(marked).toEqual(['products.manage']);
    expect(togglePermission(marked, 'products.manage', catalog)).toEqual(['products.view']);
    expect(togglePermission(['users.manage'], 'users.manage', catalog)).toEqual([]);
  });
});

describe('secciones del panel', () => {
  it('muestra solo las secciones permitidas, en orden', () => {
    const can = (p: string) => [PERMISSIONS.suppliersView, PERMISSIONS.productsView].includes(p as never);
    expect(allowedSections(can).map(s => s.path)).toEqual(['productos', 'proveedores']);
  });
});

describe('contraseñas y márgenes', () => {
  it('exige 10 caracteres con letras y números', () => {
    expect(passwordProblem('corta1')).toContain('10');
    expect(passwordProblem('solamenteletras')).toContain('letras y números');
    expect(passwordProblem('tienda2026ok')).toBeNull();
  });

  it('calcula el margen bruto sobre el precio de venta', () => {
    expect(grossMargin(200, 150)).toBe(0.25);
    expect(grossMargin(0, 10)).toBeNull();
  });
});
