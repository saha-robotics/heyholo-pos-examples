from typing import TypedDict, Literal, Union
from datetime import datetime

# Order statuses matching HPI specification
OrderStatus = Literal[
    "accepted",
    "ready",
    "completed",
    "cancelled",
    "pending",
    "in_progress",
    "failed",
]

# Translatable field - can be string or dict
Translatable = Union[str, dict[str, str]]


class ProductOptionValue(TypedDict, total=False):
    value: str  # Required
    label: Translatable  # Required
    ingredients: Translatable
    additional_price: float


class ProductOption(TypedDict, total=False):
    name: str  # Required
    label: Translatable  # Required
    description: Translatable
    default: str
    options: list[ProductOptionValue]  # Required
    multiple: bool
    required: bool


class Category(TypedDict, total=False):
    id: str  # Required
    name: Translatable  # Required
    image_url: str


class Product(TypedDict, total=False):
    id: str  # Required
    name: Translatable  # Required
    price: float  # Required
    description: Translatable
    ingredients: Translatable
    unit: Translatable
    image_url: str
    currency: str
    category: Category  # Required
    options: list[ProductOption]
    cross_sell_ids: list[str]


class ProductsResponse(TypedDict, total=False):
    products: list[Product]  # Required
    next_cursor: str


class OrderItemOption(TypedDict):
    name: str
    values: list[str]


class OrderItem(TypedDict, total=False):
    product_id: str  # Required
    quantity: int  # Required
    note: str
    options: list[OrderItemOption]


class CreateOrderRequest(TypedDict, total=False):
    items: list[OrderItem]  # Required
    location: str
    note: str
    number_of_people: int
    existing_order_id: str


class OrderResponse(TypedDict, total=False):
    id: str  # Required
    status: OrderStatus  # Required
    is_addition: bool  # Required


class CancelOrderRequest(TypedDict, total=False):
    reason: str


class CancelOrderResponse(TypedDict):
    status: OrderStatus


class Location(TypedDict):
    id: str
    name: str


class StoredOrder(TypedDict):
    id: str
    status: OrderStatus
    items: list[OrderItem]
    location: str
    note: str
    number_of_people: int
    created_at: datetime
