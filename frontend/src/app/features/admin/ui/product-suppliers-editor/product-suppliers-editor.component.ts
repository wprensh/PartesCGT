import { ChangeDetectionStrategy, Component, computed, input, model } from '@angular/core';
import { PercentPipe } from '@angular/common';
import { CopPipe } from '@shared/pipes/cop.pipe';
import { ProductSupplierInput, Supplier, grossMargin } from '../../domain/supplier.model';

/** Lista editable de proveedores de un producto: proveedor, costo, código y cuál es el preferido. */
@Component({
  selector: 'app-product-suppliers-editor',
  imports: [CopPipe, PercentPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <fieldset class="suppliers">
      <legend>Proveedores y costos</legend>
      @if (offers().length) {
        <div class="grid" role="table" aria-label="Proveedores del producto">
          <div class="row head" role="row">
            <span role="columnheader">Proveedor</span><span role="columnheader">Costo (COP)</span>
            <span role="columnheader">Código</span><span role="columnheader">Margen</span>
            <span role="columnheader">Preferido</span><span></span>
          </div>
          @for (offer of offers(); track $index; let i = $index) {
            <div class="row" role="row">
              <select [value]="offer.supplierId" [disabled]="readOnly()" aria-label="Proveedor"
                      (change)="update(i, { supplierId: +$any($event.target).value })">
                <option value="0" disabled>Elige…</option>
                @for (s of choicesFor(i); track s.id) {
                  <option [value]="s.id">{{ s.name }}{{ s.isActive ? '' : ' (inactivo)' }}</option>
                }
              </select>
              <input type="number" min="0" step="100" inputmode="numeric" aria-label="Costo" [value]="offer.cost"
                     [disabled]="readOnly()" (change)="update(i, { cost: Math.max(0, +$any($event.target).value || 0) })">
              <input maxlength="60" aria-label="Código del proveedor" placeholder="Opcional" [value]="offer.supplierSku ?? ''"
                     [disabled]="readOnly()" (change)="update(i, { supplierSku: $any($event.target).value.trim() || null })">
              <span class="margin" [class.negative]="(margin(offer.cost) ?? 0) < 0">{{ margin(offer.cost) | percent:'1.0-1' }}</span>
              <input type="radio" name="preferred-supplier" aria-label="Proveedor preferido" [checked]="offer.isPreferred"
                     [disabled]="readOnly()" (change)="setPreferred(i)">
              @if (!readOnly()) {
                <button type="button" class="btn-danger" (click)="remove(i)" [attr.aria-label]="'Quitar proveedor ' + (i + 1)">Quitar</button>
              } @else { <span></span> }
            </div>
          }
        </div>
        @if (cheapest(); as best) {
          <small class="muted">Costo más bajo: {{ best | cop }} · margen sobre el precio actual ({{ price() | cop }}).</small>
        }
      } @else {
        <p class="muted">Sin proveedores asignados.</p>
      }
      @if (!readOnly()) {
        <button type="button" class="btn-link add" (click)="add()" [disabled]="!availableSuppliers().length">
          + Agregar proveedor
        </button>
        @if (!suppliers().length) { <small class="muted">Primero crea proveedores en la sección Proveedores.</small> }
      }
    </fieldset>
  `,
  styles: `
    .suppliers { border: 1px solid var(--line); border-radius: 8px; padding: .75rem; margin: 0; display: grid; gap: .5rem; }
    .suppliers legend { font-weight: 600; padding: 0 .3rem; }
    .grid { display: grid; gap: .4rem; overflow-x: auto; }
    .row { display: grid; grid-template-columns: minmax(8rem, 1.6fr) 7rem minmax(5rem, 1fr) 4rem 4.5rem auto; gap: .4rem; align-items: center; min-width: 34rem; }
    .row.head { font-size: .78rem; color: var(--muted); font-weight: 600; }
    .row select, .row input:not([type=radio]) { min-width: 0; padding: .45rem .55rem; border: 1px solid var(--line-2); border-radius: 8px; background: var(--bg); color: var(--ink); }
    .row input[type=radio] { justify-self: center; accent-color: var(--pcb); width: 1rem; height: 1rem; }
    .margin { font-family: var(--mono); font-size: .85rem; text-align: right; }
    .negative { color: var(--danger); font-weight: 600; }
    .muted { color: var(--muted); margin: 0; }
    .add { justify-self: start; padding-left: 0; }
  `
})
export class ProductSuppliersEditorComponent {
  /** Todos los proveedores (para elegir). Los inactivos solo se muestran si ya estaban asignados. */
  suppliers = input.required<Supplier[]>();
  /** Precio de venta actual del producto, para el margen. */
  price = input(0);
  readOnly = input(false);
  offers = model.required<ProductSupplierInput[]>();

  protected readonly Math = Math;
  protected availableSuppliers = computed(() =>
    this.suppliers().filter(s => s.isActive && !this.offers().some(o => o.supplierId === s.id)));
  protected cheapest = computed(() => {
    const costs = this.offers().filter(o => o.supplierId > 0).map(o => o.cost);
    return costs.length ? Math.min(...costs) : null;
  });

  protected margin(cost: number): number | null {
    return grossMargin(this.price(), cost);
  }

  /** Opciones de la fila i: activos no usados en otras filas, más el propio (aunque esté inactivo). */
  protected choicesFor(index: number): Supplier[] {
    const current = this.offers()[index]?.supplierId;
    return this.suppliers().filter(s =>
      s.id === current || (s.isActive && !this.offers().some((o, i) => i !== index && o.supplierId === s.id)));
  }

  protected add() {
    const first = this.availableSuppliers()[0];
    if (!first) return;
    this.offers.update(list => [...list, { supplierId: first.id, cost: 0, supplierSku: null, isPreferred: list.length === 0 }]);
  }

  protected remove(index: number) {
    this.offers.update(list => list.filter((_, i) => i !== index));
  }

  protected update(index: number, changes: Partial<ProductSupplierInput>) {
    this.offers.update(list => list.map((o, i) => (i === index ? { ...o, ...changes } : o)));
  }

  protected setPreferred(index: number) {
    this.offers.update(list => list.map((o, i) => ({ ...o, isPreferred: i === index })));
  }
}
