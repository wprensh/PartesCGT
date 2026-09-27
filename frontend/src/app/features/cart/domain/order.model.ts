export interface OrderLine {
  productId: number;
  productName: string;
  unitPrice: number;
  quantity: number;
}

export interface Order {
  id: number;
  customerName: string;
  customerEmail: string;
  createdAt: string;
  total: number;
  items: OrderLine[];
}

export interface CreateOrderRequest {
  customerName: string;
  customerEmail: string;
  items: { productId: number; quantity: number }[];
}
