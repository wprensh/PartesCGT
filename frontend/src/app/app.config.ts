import { ApplicationConfig, LOCALE_ID, provideZoneChangeDetection } from '@angular/core';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling, withRouterConfig } from '@angular/router';
import { registerLocaleData } from '@angular/common';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import localeEsCo from '@angular/common/locales/es-CO';
import { provideSpanishPaginator } from '@shared/table/spanish-paginator-intl';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';

registerLocaleData(localeEsCo);

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes,
      withComponentInputBinding(),
      // Los enlaces del menú saltan a #kits y #catalogo, también si ya estás en esa misma URL.
      withInMemoryScrolling({ anchorScrolling: 'enabled' }),
      withRouterConfig({ onSameUrlNavigation: 'reload' })),
    provideHttpClient(withFetch(), withInterceptors([authInterceptor])),
    // Animaciones de Angular Material (tooltips del paginador); se cargan en diferido.
    provideAnimationsAsync(),
    // Textos del MatPaginator en español (catálogo y tablas del panel).
    provideSpanishPaginator(),
    { provide: LOCALE_ID, useValue: 'es-CO' }
  ]
};
