import { ChangeDetectionStrategy, Component, ElementRef, computed, inject, input, output, signal, viewChild } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable, map, of, switchMap } from 'rxjs';
import { describeHttpError } from '@core/http/http-error';
import {
  ACCEPTED_IMAGE_TYPES, safeImageUrl, validateImageFile, type Category, type Product, type ProductAttribute
} from '@features/catalog';
import { IconComponent } from '@shared/ui/icon/icon.component';
import { AdminApi } from '../../data/admin.api';
import { categoryOptions } from '../../domain/category-tree';
import { MAX_ATTRIBUTES, MAX_IMAGE_URL_LENGTH, ProductInput } from '../../domain/product-input';
import { ProductSupplierInput, Supplier } from '../../domain/supplier.model';
import { ProductSuppliersEditorComponent } from '../product-suppliers-editor/product-suppliers-editor.component';

type AttributeGroup = FormGroup<{ name: FormControl<string>; value: FormControl<string> }>;

export interface ProductSaved { product: Product; created: boolean; }

/** Diálogo para crear o editar un producto. Guarda contra la API y avisa con (saved). */
@Component({
  selector: 'app-product-form-dialog',
  imports: [IconComponent, ProductSuppliersEditorComponent, ReactiveFormsModule],
  templateUrl: './product-form-dialog.component.html',
  styleUrl: './product-form-dialog.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProductFormDialogComponent {
  private api = inject(AdminApi);
  private fb = inject(NonNullableFormBuilder);
  private dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  categories = input.required<Category[]>();
  /** Sugerencias para escribir marcas y atributos siempre igual (si no, el filtro de la tienda los separa). */
  brands = input<string[]>([]);
  attributeNames = input<string[]>([]);
  /** Sin permiso para gestionar productos: los datos del producto se muestran deshabilitados. */
  readOnly = input(false);
  /** Proveedores y costos: se muestran con suppliers.view y se editan con suppliers.manage. */
  canViewSuppliers = input(false);
  canManageSuppliers = input(false);
  allSuppliers = input<Supplier[]>([]);
  saved = output<ProductSaved>();

  // ---------- Proveedores ----------
  protected offers = signal<ProductSupplierInput[]>([]);
  private loadedOffers = '[]';
  protected suppliersLoading = signal(false);
  private suppliersChanged = computed(() => JSON.stringify(this.offers()) !== this.loadedOffers);
  /** Hay algo que guardar: el producto (si se puede editar) o solo sus proveedores. */
  protected canSave = computed(() => !this.readOnly() || this.canManageSuppliers());

  protected readonly maxAttributes = MAX_ATTRIBUTES;
  protected editingId = signal<number | null>(null);
  protected saving = signal(false);
  protected error = signal('');
  protected options = computed(() => categoryOptions(this.categories()));

  protected form = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(150)]],
    description: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(1000)]],
    brand: ['', Validators.maxLength(60)],
    categoryId: [0, Validators.min(1)],
    price: [0, [Validators.required, Validators.min(0)]],
    stock: [0, [Validators.required, Validators.min(0)]],
    isActive: [true],
    imageUrl: ['', Validators.maxLength(MAX_IMAGE_URL_LENGTH)],
    attributes: this.fb.array<AttributeGroup>([])
  });

  // ---------- Imagen ----------
  protected readonly acceptedImageTypes = ACCEPTED_IMAGE_TYPES.join(',');
  protected uploading = signal(false);
  protected imageError = signal('');
  /** Solo se previsualizan URL seguras (http(s) o subidas); el resto se valida al guardar. */
  protected imagePreview = toSignal(this.form.controls.imageUrl.valueChanges.pipe(map(safeImageUrl)), { initialValue: null });
  /** Precio que se está editando, para calcular el margen de cada proveedor. */
  protected currentPrice = toSignal(this.form.controls.price.valueChanges, { initialValue: 0 });

  constructor() {
    this.form.controls.imageUrl.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.imageError.set(''));
  }

  protected get attributes() {
    return this.form.controls.attributes;
  }

  /** Abre vacío para crear, o con los datos de `product` para editar. */
  open(product?: Product, defaultCategoryId = 0) {
    const { name = '', description = '', brand = '', categoryId = defaultCategoryId, price = 0, stock = 0, isActive = true } = product ?? {};
    this.form.reset({ name, description, brand, categoryId, price, stock, isActive, imageUrl: product?.imageUrl ?? '' });
    this.setAttributes(product?.attributes ?? []);
    if (this.readOnly()) this.form.disable(); else this.form.enable();
    this.editingId.set(product?.id ?? null);
    this.error.set('');
    this.loadSuppliers(product?.id ?? null);
    this.dialog().nativeElement.showModal();
  }

  private loadSuppliers(productId: number | null) {
    this.setLoadedOffers([]);
    if (!productId || !this.canViewSuppliers()) return;
    this.suppliersLoading.set(true);
    this.api.productSuppliers(productId).subscribe({
      next: list => {
        this.setLoadedOffers(list.map(({ supplierId, cost, supplierSku, isPreferred }) => ({ supplierId, cost, supplierSku, isPreferred })));
        this.suppliersLoading.set(false);
      },
      error: (e: HttpErrorResponse) => { this.error.set(describeHttpError(e)); this.suppliersLoading.set(false); }
    });
  }

  private setLoadedOffers(offers: ProductSupplierInput[]) {
    this.offers.set(offers);
    this.loadedOffers = JSON.stringify(offers);
  }

  protected close() {
    this.dialog().nativeElement.close();
  }

  protected addAttribute(attribute: ProductAttribute = { name: '', value: '' }) {
    this.attributes.push(this.fb.group({
      name: [attribute.name, [Validators.required, Validators.maxLength(60)]],
      value: [attribute.value, [Validators.required, Validators.maxLength(80)]]
    }));
  }

  /**
   * Guarda el producto (si se puede editar) y, después, sus proveedores (si cambiaron y hay permiso).
   * Son dos llamadas porque los costos son de Compras: un rol puede tener una sin la otra.
   */
  protected save() {
    if (!this.canSave()) return;
    this.form.markAllAsTouched();
    if (!this.readOnly() && this.form.invalid) return;
    if (this.offers().some(o => !o.supplierId)) return this.error.set('Elige el proveedor en cada fila o quítala.');

    const id = this.editingId();
    const saveProduct$: Observable<Product | null> = this.readOnly()
      ? of(null)
      : id ? this.api.updateProduct(id, this.productInput()) : this.api.createProduct(this.productInput());

    this.saving.set(true);
    this.error.set('');
    saveProduct$.pipe(
      switchMap(product => this.saveSuppliers(product?.id ?? id).pipe(map(() => product)))
    ).subscribe({
      next: product => {
        this.saving.set(false);
        if (product) this.saved.emit({ product, created: !id });
        this.close();
      },
      error: (e: HttpErrorResponse) => {
        this.error.set(describeHttpError(e));
        this.saving.set(false);
      }
    });
  }

  /** Guarda los proveedores solo si cambiaron y el usuario puede gestionarlos. */
  private saveSuppliers(productId: number | null): Observable<unknown> {
    if (!productId || !this.canManageSuppliers() || !this.suppliersChanged()) return of(null);
    return this.api.replaceProductSuppliers(productId, this.offers());
  }

  private productInput(): ProductInput {
    return this.form.getRawValue();
  }

  /** Sube la foto elegida y deja su URL en el formulario; se guarda en el producto al pulsar "Guardar". */
  protected uploadImage(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';   // permite volver a elegir el mismo archivo
    if (!file) return;

    const problem = validateImageFile(file);
    if (problem) return this.imageError.set(problem);

    this.uploading.set(true);
    this.api.uploadProductImage(file).subscribe({
      next: ({ url }) => {
        this.form.controls.imageUrl.setValue(url);
        this.form.controls.imageUrl.markAsDirty();
        this.uploading.set(false);
      },
      error: (e: HttpErrorResponse) => {
        this.imageError.set(describeHttpError(e));
        this.uploading.set(false);
      }
    });
  }

  protected clearImage() {
    this.form.controls.imageUrl.setValue('');
  }

  private setAttributes(list: ProductAttribute[]) {
    this.attributes.clear();
    list.forEach(a => this.addAttribute(a));
  }
}
