import { ChangeDetectionStrategy, Component, ElementRef, inject, input, output, signal, viewChild } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PercentPipe } from '@angular/common';
import { describeHttpError } from '@core/http/http-error';
import { CopPipe } from '@shared/pipes/cop.pipe';
import { AdminApi } from '../../data/admin.api';
import { SuppliedProduct, Supplier, SupplierInput, grossMargin } from '../../domain/supplier.model';

export interface SupplierSaved { supplier: Supplier; created: boolean; }

/** Crear, editar o consultar un proveedor. En modo lectura solo muestra los datos. */
@Component({
  selector: 'app-supplier-form-dialog',
  imports: [CopPipe, PercentPipe, ReactiveFormsModule],
  templateUrl: './supplier-form-dialog.component.html',
  styleUrl: './supplier-form-dialog.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SupplierFormDialogComponent {
  private api = inject(AdminApi);
  private dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  /** Sin permiso de gestión: el formulario se muestra deshabilitado. */
  readOnly = input(false);
  saved = output<SupplierSaved>();

  protected editingId = signal<number | null>(null);
  protected saving = signal(false);
  protected error = signal('');
  protected products = signal<SuppliedProduct[] | null>(null);
  protected readonly margin = grossMargin;

  protected form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(150)]],
    taxId: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(20)]],
    contactName: ['', Validators.maxLength(120)],
    email: ['', [Validators.email, Validators.maxLength(254)]],
    phone: ['', Validators.maxLength(40)],
    city: ['', Validators.maxLength(80)],
    address: ['', Validators.maxLength(200)],
    notes: ['', Validators.maxLength(1000)],
    isActive: [true]
  });

  open(supplier?: Supplier) {
    this.form.reset({
      name: supplier?.name ?? '', taxId: supplier?.taxId ?? '', contactName: supplier?.contactName ?? '',
      email: supplier?.email ?? '', phone: supplier?.phone ?? '', city: supplier?.city ?? '',
      address: supplier?.address ?? '', notes: supplier?.notes ?? '', isActive: supplier?.isActive ?? true
    });
    if (this.readOnly()) this.form.disable(); else this.form.enable();
    this.editingId.set(supplier?.id ?? null);
    this.error.set('');
    this.products.set(null);
    if (supplier) this.api.supplierProducts(supplier.id).subscribe(list => this.products.set(list));
    this.dialog().nativeElement.showModal();
  }

  protected close() {
    this.dialog().nativeElement.close();
  }

  protected save() {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.readOnly()) return;

    const id = this.editingId();
    const value = this.form.getRawValue();
    // Opcionales vacíos como null: el backend valida el formato de correo solo si viene uno.
    const optional = (text: string) => text.trim() || null;
    const input: SupplierInput = {
      ...value,
      contactName: optional(value.contactName), email: optional(value.email), phone: optional(value.phone),
      city: optional(value.city), address: optional(value.address), notes: optional(value.notes)
    };
    this.saving.set(true);
    this.error.set('');
    (id ? this.api.updateSupplier(id, input) : this.api.createSupplier(input)).subscribe({
      next: supplier => {
        this.saving.set(false);
        this.saved.emit({ supplier, created: !id });
        this.close();
      },
      error: (e: HttpErrorResponse) => {
        this.error.set(describeHttpError(e));
        this.saving.set(false);
      }
    });
  }
}
