import { LOCALE_ID, Pipe, PipeTransform, inject } from '@angular/core';
import { formatCop } from '../utils/format';

/** Precio en pesos colombianos sin decimales: 245000 → "$ 245.000". */
@Pipe({ name: 'cop' })
export class CopPipe implements PipeTransform {
  private locale = inject(LOCALE_ID);

  transform(value: number | null | undefined): string {
    return value == null ? '' : formatCop(value, this.locale);
  }
}
