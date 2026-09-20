// Order statuses matching HPI specification
export type OrderStatus =
  | 'accepted'
  | 'ready'
  | 'completed'
  | 'cancelled'
  | 'pending'
  | 'in_progress'
  | 'failed';

// Translatable field - can be string or map
export type Translatable = string | Record<string, string>;

export interface ProductOptionValue {
  value: string;
  label: Translatable;
  ingredients?: Translatable;
  additional_price?: number;
}

export interface ProductOption {
  name: string;
  label: Translatable;
  description?: Translatable;
  default?: string;
  options: ProductOptionValue[];
  multiple?: boolean;
  required?: boolean;
}

export interface Category {
  id: string;
  name: Translatable;
  image_url?: string;
}

export interface Product {
  id: string;
  name: Translatable;
  price: number;
  description?: Translatable;
  ingredients?: Translatable;
  unit?: Translatable;
  image_url?: string;
  currency?: string;
  category: Category;
  options?: ProductOption[];
  cross_sell_ids?: string[];
}

export interface ProductsResponse {
  products: Product[];
  next_cursor?: string;
}

export interface OrderItemOption {
  name: string;
  values: string[];
}

export interface OrderItem {
  product_id: string;
  quantity: number;
  note?: string;
  options?: OrderItemOption[];
}

export interface CreateOrderRequest {
  items: OrderItem[];
  location?: string;
  note?: string;
  number_of_people?: number;
  existing_order_id?: string;
}

export interface OrderResponse {
  id: string;
  status: OrderStatus;
  is_addition: boolean;
}

export interface CancelOrderRequest {
  reason?: string;
}

export interface CancelOrderResponse {
  status: OrderStatus;
}

export interface Location {
  id: string;
  name: string;
}

export interface StoredOrder {
  id: string;
  status: OrderStatus;
  items: OrderItem[];
  location?: string;
  note?: string;
  number_of_people?: number;
  created_at: Date;
}
