import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { CopPipe } from '@shared/pipes/cop.pipe';
import { IconComponent } from '@shared/ui/icon/icon.component';
import { safeImageUrl } from '../../domain/product-image';
import { categoryPath, productCode, productSku } from '../../domain/product-labels';
import { Product, stockStatus } from '../../domain/product.model';

/** Lo que recibe el diálogo: cualquier producto del catálogo. */
export interface ProductDetailData {
  product: Product;
}

/** 'add' si el cliente pidió agregarlo al carrito; undefined si solo cerró. */
export type ProductDetailResult = 'add' | undefined;

/**
 * Detalle ampliado de un producto (foto grande, todas las especificaciones, precio y stock).
 * Presentacional: no conoce el carrito; al pulsar "Agregar" se cierra con 'add' y decide quien lo abrió.
 * Ábrelo con ProductDetailDialog.open(producto).
 */
@Component({
  selector: 'app-product-detail-dialog',
  imports: [CopPipe, DecimalPipe, IconComponent, MatDialogModule],
  templateUrl: './product-detail-dialog.component.html',
  styleUrl: './product-detail-dialog.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProductDetailDialogComponent {
  private ref = inject<MatDialogRef<ProductDetailDialogComponent, ProductDetailResult>>(MatDialogRef);
  protected readonly product = inject<ProductDetailData>(MAT_DIALOG_DATA).product;

  protected readonly code = productCode(this.product);
  protected readonly sku = productSku(this.product);
  protected readonly path = categoryPath(this.product);
  protected readonly stock = stockStatus(this.product);

  /** Si la foto no carga, se muestra el recuadro con el código de categoría. */
  protected failed = signal(false);
  protected imageSrc = computed(() => this.failed() ? null : safeImageUrl(this.product.imageUrl));

  protected addToCart() {
    this.ref.close('add');
  }
}
