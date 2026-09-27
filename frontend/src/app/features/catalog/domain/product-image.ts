/** Ruta donde el backend sirve las imágenes subidas desde el panel. */
export const UPLOADED_IMAGES_PATH = '/api/files/';

/** Límites de subida; los mismos que valida el backend (ProductImageService). */
export const MAX_IMAGE_BYTES = 2 * 1024 * 1024;
export const ACCEPTED_IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp'] as const;

/**
 * Devuelve la URL solo si es segura para <img src>: http(s) absoluta o una imagen subida.
 * El backend ya lo valida al guardar; esto es una segunda barrera (y Angular además sanitiza [src]).
 */
export function safeImageUrl(url: string | null | undefined): string | null {
  const value = url?.trim();
  if (!value) return null;
  if (value.startsWith(UPLOADED_IMAGES_PATH)) return value;
  return /^https?:\/\/[^\s]+$/i.test(value) ? value : null;
}

/** Mensaje de error si el archivo no se puede subir; null si está bien. */
export function validateImageFile(file: Pick<File, 'size' | 'type'>): string | null {
  if (!(ACCEPTED_IMAGE_TYPES as readonly string[]).includes(file.type)) return 'Usa una imagen JPG, PNG o WebP.';
  if (file.size > MAX_IMAGE_BYTES) return 'La imagen supera los 2 MB. Redúcela o usa formato WebP.';
  return null;
}
