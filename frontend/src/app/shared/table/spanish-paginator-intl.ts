import { Injectable, Provider } from '@angular/core';
import { MatPaginatorIntl } from '@angular/material/paginator';
import { rangeLabel } from './pagination';

/** Textos del MatPaginator en español (botones, lectores de pantalla y "1 – 10 de 24"). */
@Injectable()
export class SpanishPaginatorIntl extends MatPaginatorIntl {
  override itemsPerPageLabel = 'Filas por página';
  override nextPageLabel = 'Página siguiente';
  override previousPageLabel = 'Página anterior';
  override firstPageLabel = 'Primera página';
  override lastPageLabel = 'Última página';
  override getRangeLabel = rangeLabel;
}

export function provideSpanishPaginator(): Provider {
  return { provide: MatPaginatorIntl, useClass: SpanishPaginatorIntl };
}
