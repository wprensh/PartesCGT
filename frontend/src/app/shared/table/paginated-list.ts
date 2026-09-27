import { computed, Signal, signal } from '@angular/core';
import type { PageEvent } from '@angular/material/paginator';
import { clampPageIndex, pageSlice, PAGE_SIZE } from './pagination';

/**
 * Paginación en el cliente sobre una lista reactiva (ya filtrada).
 * Uso: `<table mat-table [dataSource]="page.rows()">` + `<mat-paginator [pageIndex]="page.pageIndex()" (page)="page.onPage($event)">`.
 * Llama a `reset()` al cambiar un filtro para volver a la primera página.
 */
export function paginatedList<T>(source: Signal<readonly T[]>, pageSize = PAGE_SIZE) {
  const requested = signal(0);
  const length = computed(() => source().length);
  const pageIndex = computed(() => clampPageIndex(requested(), length(), pageSize));

  return {
    pageSize,
    length,
    pageIndex,
    rows: computed(() => pageSlice(source(), pageIndex(), pageSize)),
    onPage: (event: PageEvent) => requested.set(event.pageIndex),
    reset: () => requested.set(0)
  };
}

export type PaginatedList<T> = ReturnType<typeof paginatedList<T>>;
