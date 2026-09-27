import { PermissionDefinition } from './access.model';

export interface PermissionModule { name: string; permissions: PermissionDefinition[]; }

/** Agrupa el catálogo por módulo, conservando el orden en que llega del backend. */
export function groupByModule(catalog: PermissionDefinition[]): PermissionModule[] {
  const modules = new Map<string, PermissionDefinition[]>();
  for (const permission of catalog) modules.set(permission.module, [...(modules.get(permission.module) ?? []), permission]);
  return [...modules].map(([name, permissions]) => ({ name, permissions }));
}

/** Permisos que quedan marcados, sumando los implícitos (gestionar ⇒ ver). */
export function withImplied(selected: string[], catalog: PermissionDefinition[]): Set<string> {
  const result = new Set(selected);
  for (const code of selected) {
    const implied = catalog.find(p => p.code === code)?.implies;
    if (implied) result.add(implied);
  }
  return result;
}

/** ¿Está marcado solo porque otro permiso lo incluye? Entonces no se puede desmarcar por separado. */
export function isImpliedOnly(code: string, selected: string[], catalog: PermissionDefinition[]): boolean {
  return !selected.includes(code) && withImplied(selected, catalog).has(code);
}

/**
 * Marca o desmarca un permiso. Al marcar "gestionar", "ver" pasa a implícito (se quita de la lista explícita
 * para no guardarlo dos veces); al desmarcar "gestionar", "ver" se conserva marcado.
 */
export function togglePermission(selected: string[], code: string, catalog: PermissionDefinition[]): string[] {
  const implied = catalog.find(p => p.code === code)?.implies ?? null;
  if (selected.includes(code)) return [...selected.filter(c => c !== code), ...(implied ? [implied] : [])];
  return [...selected.filter(c => c !== implied), code];
}
