// @ts-check
const eslint = require('@eslint/js');
const tseslint = require('typescript-eslint');
const angular = require('angular-eslint');
const boundaries = require('eslint-plugin-boundaries');

/**
 * Reglas de arquitectura (ver ARCHITECTURE.md):
 * - shared y core no dependen de nadie más; las features pueden usar core y shared.
 * - Entre features solo se importa desde index.ts (API pública), testing.ts (en pruebas) o *.routes.ts.
 */
const architecture = {
  plugins: { boundaries },
  settings: {
    'import/resolver': { typescript: { project: './tsconfig.json' } },
    'boundaries/include': ['src/**/*.ts'],
    'boundaries/elements': [
      { type: 'core', pattern: 'src/app/core/*' },
      { type: 'shared', pattern: 'src/app/shared/*' },
      { type: 'layout', pattern: 'src/app/layout/*' },
      { type: 'feature', pattern: 'src/app/features/*', capture: ['feature'] },
      { type: 'app', pattern: 'src/app/*.ts', mode: 'file' }
    ]
  },
  rules: {
    'boundaries/element-types': ['error', {
      default: 'disallow',
      message: '${file.type} no puede depender de ${dependency.type} (ver ARCHITECTURE.md)',
      rules: [
        { from: 'shared', allow: ['shared'] },
        { from: 'core', allow: ['core', 'shared'] },
        { from: 'feature', allow: ['core', 'shared', 'feature'] },
        { from: 'layout', allow: ['core', 'shared', 'feature', 'layout'] },
        { from: 'app', allow: ['core', 'shared', 'feature', 'layout', 'app'] }
      ]
    }],
    'boundaries/entry-point': ['error', {
      default: 'allow',
      message: 'Importa la feature "${dependency.feature}" desde su index.ts, no desde ${dependency.internalPath}',
      rules: [
        { target: ['feature'], disallow: ['**'] },
        { target: ['feature'], allow: ['index.ts', 'testing.ts', '*.routes.ts'] }
      ]
    }],
    'boundaries/no-unknown-files': 'off'
  }
};

/**
 * Angular 19 terminó su soporte (LTS hasta mayo de 2026) y la 19.2.25 tiene vulnerabilidades sin parche.
 * La tienda no usa las APIs afectadas; estas reglas impiden empezar a usarlas mientras sigamos en v19.
 */
const V19_UNPATCHED = 'Vulnerable en Angular 19.2.25 y sin parche en v19 (ver ARCHITECTURE.md, "Seguridad con Angular 19").';
const angular19Guards = {
  rules: {
    'no-restricted-imports': ['error', {
      paths: [
        { name: '@angular/common', importNames: ['formatDate', 'DatePipe'],
          message: `${V19_UNPATCHED} Para fechas usa Intl.DateTimeFormat.` },
        { name: '@angular/platform-browser', importNames: ['provideClientHydration', 'withHttpTransferCacheOptions'],
          message: V19_UNPATCHED },
        { name: '@angular/common/http', importNames: ['withHttpTransferCache'], message: V19_UNPATCHED },
        { name: '@angular/localize', message: V19_UNPATCHED },
        { name: '@angular/ssr', message: V19_UNPATCHED }
      ]
    }],
    'no-restricted-globals': ['error', { name: '$localize', message: V19_UNPATCHED }],
    'no-restricted-syntax': ['error', {
      selector: "MemberExpression[property.name=/^bypassSecurityTrust/]",
      message: `${V19_UNPATCHED} No desactives la sanitización de Angular.`
    }]
  }
};

module.exports = tseslint.config(
  { ignores: ['dist/', '.angular/', 'node_modules/'] },
  {
    files: ['src/**/*.ts'],
    extends: [
      eslint.configs.recommended,
      ...tseslint.configs.recommended,
      ...tseslint.configs.stylistic,
      ...angular.configs.tsRecommended
    ],
    processor: angular.processInlineTemplates,
    rules: {
      '@angular-eslint/component-selector': ['error', { type: 'element', prefix: 'app', style: 'kebab-case' }],
      '@angular-eslint/directive-selector': ['error', { type: 'attribute', prefix: 'app', style: 'camelCase' }],
      '@angular-eslint/prefer-on-push-component-change-detection': 'error',
      '@angular-eslint/prefer-standalone': 'error',
      '@typescript-eslint/no-unused-vars': ['error', { argsIgnorePattern: '^_' }]
    }
  },
  { files: ['src/app/**/*.ts'], ...architecture },
  { files: ['src/**/*.ts'], ...angular19Guards },
  {
    files: ['src/**/*.html'],
    extends: [...angular.configs.templateRecommended, ...angular.configs.templateAccessibility]
  }
);
