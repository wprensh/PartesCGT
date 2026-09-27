import { LOCALE_ID, Pipe, PipeTransform, inject } from '@angular/core';

/**
 * Fecha y hora legibles ("27 sept 2026, 1:39 p. m.") con Intl.DateTimeFormat.
 * Reemplaza a DatePipe/formatDate, vulnerables en Angular 19 (ver frontend/ARCHITECTURE.md).
 */
@Pipe({ name: 'dateTime' })
export class DateTimePipe implements PipeTransform {
  private format = new Intl.DateTimeFormat(inject(LOCALE_ID), { dateStyle: 'medium', timeStyle: 'short' });

  transform(value: string | Date | null | undefined, empty = ''): string {
    if (!value) return empty;
    const date = value instanceof Date ? value : new Date(value);
    return Number.isNaN(date.getTime()) ? empty : this.format.format(date);
  }
}
