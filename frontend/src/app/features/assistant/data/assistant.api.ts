import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { AssistantReply, ChatMessage } from '../domain/chat.model';

@Injectable({ providedIn: 'root' })
export class AssistantApi {
  private http = inject(HttpClient);

  chat(messages: ChatMessage[]) {
    // Al backend solo viaja rol + texto; los productos son para la UI.
    const payload = messages.map(({ role, content }) => ({ role, content }));
    return this.http.post<AssistantReply>('/api/assistant/chat', { messages: payload });
  }
}
