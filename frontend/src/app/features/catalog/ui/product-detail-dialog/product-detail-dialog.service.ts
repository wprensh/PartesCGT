import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Observable } from 'rxjs';
import { Product } from '../../domain/product.model';
import { ProductDetailData, ProductDetailDialogComponent, ProductDetailResult } from './product-detail-dialog.component';

/** Abre el detalle de cualquier producto. Emite 'add' al cerrar si el cliente pidió agregarlo al carrito. */
@Injectable({ providedIn: 'root' })
export class ProductDetailDialog {
  private dialog = inject(MatDialog);

  open(product: Product): Observable<ProductDetailResult> {
    return this.dialog.open<ProductDetailDialogComponent, ProductDetailData, ProductDetailResult>(ProductDetailDialogComponent, {
      data: { product },
      // Paleta clara, como las tarjetas del catálogo.
      panelClass: ['product-detail-panel'],
      width: '880px',
      maxWidth: 'calc(100vw - 2rem)',
      autoFocus: 'dialog'
    }).afterClosed();
  }
}
