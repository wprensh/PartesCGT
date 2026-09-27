import { formatCurrency } from '@angular/common';

export function formatCop(value: number, locale: string): string {
  return formatCurrency(value, locale, '$', 'COP', '1.0-0');
}

/** "1 producto" / "3 productos". */
export function plural(count: number, singular: string, pluralForm = `${singular}s`): string {
  return `${count} ${count === 1 ? singular : pluralForm}`;
}
