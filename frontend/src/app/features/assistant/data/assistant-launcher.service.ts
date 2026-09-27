import { Injectable, signal } from '@angular/core';

/** Abre el asistente desde cualquier parte, opcionalmente con una pregunta sugerida en el borrador. */
@Injectable({ providedIn: 'root' })
export class AssistantLauncher {
  readonly open = signal(false);
  readonly suggestion = signal('');

  ask(question = '') {
    this.suggestion.set(question);
    this.open.set(true);
  }
}
