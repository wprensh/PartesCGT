import { ChangeDetectionStrategy, Component, booleanAttribute, input } from '@angular/core';
import { IconName } from './icon-names';

/**
 * Icono de Material Symbols. Decorativo por defecto (aria-hidden): el texto accesible
 * lo debe dar el botón o enlace que lo contiene.
 *
 * Uso: <app-icon name="shopping_cart" />  ·  <app-icon name="star" filled />
 */
@Component({
  selector: 'app-icon',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'ms', '[class.fill]': 'filled()', 'aria-hidden': 'true' },
  template: `{{ name() }}`
})
export class IconComponent {
  name = input.required<IconName>();
  filled = input(false, { transform: booleanAttribute });
}
