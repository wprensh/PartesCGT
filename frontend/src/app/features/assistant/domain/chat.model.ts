import type { Product } from '@features/catalog';

export type ChatRole = 'user' | 'assistant';

export interface ChatMessage {
  role: ChatRole;
  content: string;
  /** Productos recomendados; solo para la UI, no viajan al backend. */
  products?: Product[];
}

export interface AssistantReply {
  answer: string;
  products: Product[];
}

/** Cuántos mensajes recientes se envían como contexto (acota el costo por pregunta). */
export const CHAT_CONTEXT_SIZE = 10;
