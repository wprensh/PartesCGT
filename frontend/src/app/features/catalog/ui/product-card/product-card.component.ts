import { ChangeDetectionStrategy, Component, DestroyRef, booleanAttribute, computed, inject, input, output, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CopPipe } from '@shared/pipes/cop.pipe';
import { IconComponent } from '@shared/ui/icon/icon.component';
import { productCode, productSku } from '../../domain/product-labels';
import { Product, stockStatus } from '../../domain/product.model';
import { productSpecs } from '../../domain/product-specs';
import { safeImageUrl } from '../../domain/product-image';
import { ProductDetailDialog } from '../product-detail-dialog/product-detail-dialog.service';

/**
 * Tarjeta de producto. Avisa con (add) y quien la usa decide qué hacer.
 * "Ver" abre el detalle; si ahí piden agregarlo, también se avisa con (add).
 */
@Component({
  selector: 'app-product-card',
  imports: [CopPipe, DecimalPipe, IconComponent],
  templateUrl: './product-card.component.html',
  styleUrl: './product-card.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProductCardComponent {
  product = input.required<Product>();
  /** Versión reducida para el chat del asistente. */
  compact = input(false, { transform: booleanAttribute });
  add = output<Product>();

  private detail = inject(ProductDetailDialog);
  private destroyRef = inject(DestroyRef);

  code = computed(() => productCode(this.product()));
  specs = computed(() => productSpecs(this.product()));
  stock = computed(() => stockStatus(this.product()));
  sku = computed(() => productSku(this.product()));

  protected openDetail() {
    const product = this.product();
    this.detail.open(product)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(result => { if (result === 'add') this.add.emit(product); });
  }

  /** Última URL que no cargó (404, sin conexión…): se muestra el recuadro de reemplazo. */
  protected failedSrc = signal<string | null>(null);
  protected imageSrc = computed(() => {
    const src = safeImageUrl(this.product().imageUrl);
    return src && src !== this.failedSrc() ? src : null;
  });
}
