/** Modelos de usuarios, roles y permisos del panel (espejo de los DTOs del backend). */

export interface AdminUser {
  id: number;
  email: string;
  fullName: string;
  roleId: number;
  role: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt: string | null;
}

export interface UserCreateInput { email: string; fullName: string; roleId: number; password: string; }
export interface UserUpdateInput { fullName: string; roleId: number; isActive: boolean; }

export interface AdminRole {
  id: number;
  name: string;
  description: string | null;
  /** El rol Administrador: tiene todos los permisos, no se edita ni se borra. */
  isSystem: boolean;
  userCount: number;
  /** Marcados explícitamente. */
  permissions: string[];
  /** Los que aplica de verdad (incluye gestionar ⇒ ver). */
  effectivePermissions: string[];
}

export interface RoleInput { name: string; description: string | null; permissions: string[]; }

export interface PermissionDefinition {
  code: string;
  module: string;
  label: string;
  /** Permiso que este incluye (p. ej. "gestionar productos" incluye "ver productos"). */
  implies: string | null;
}

/** Mismas reglas que PasswordPolicy en el backend, para avisar antes de enviar. */
export const PASSWORD_MIN_LENGTH = 10;

export function passwordProblem(password: string): string | null {
  if (password.length < PASSWORD_MIN_LENGTH) return `Mínimo ${PASSWORD_MIN_LENGTH} caracteres.`;
  if (!/\p{L}/u.test(password) || !/\d/.test(password)) return 'Combina letras y números.';
  return null;
}
