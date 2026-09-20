import { Product, StoredOrder, Location } from './types.js';

// Mock products database
export const products: Product[] = [
  {
    id: 'prod-1',
    name: { en: 'Cheeseburger', tr: 'Peynirli Burger' },
    price: 150.0,
    description: {
      en: 'Delicious burger with cheddar',
      tr: 'Çedarlı lezzetli burger',
    },
    currency: 'TRY',
    category: {
      id: 'cat-1',
      name: { en: 'Burgers', tr: 'Burgerler' },
    },
    options: [
      {
        name: 'opt-doneness',
        label: { en: 'Doneness', tr: 'Pişme Derecesi' },
        options: [
          { value: 'rare', label: { en: 'Rare', tr: 'Az Pişmiş' } },
          { value: 'medium', label: { en: 'Medium', tr: 'Orta Pişmiş' } },
          { value: 'well-done', label: { en: 'Well Done', tr: 'İyi Pişmiş' } },
        ],
      },
      {
        name: 'opt-extras',
        label: { en: 'Extras', tr: 'Ekstralar' },
        options: [
          { value: 'extra-cheese', label: 'Extra Cheese', additional_price: 20.0 },
          { value: 'bacon', label: 'Bacon', additional_price: 30.0 },
        ],
      },
    ],
  },
  {
    id: 'prod-2',
    name: 'Coke', // Example of simple string name
    price: 40.0,
    currency: 'TRY',
    category: {
      id: 'cat-2',
      name: { en: 'Beverages', tr: 'İçecekler' },
    },
  },
  {
    id: 'prod-3',
    name: { en: 'Pizza Margherita', tr: 'Margarita Pizza' },
    price: 200.0,
    description: {
      en: 'Classic pizza with tomato and mozzarella',
      tr: 'Domates ve mozzarella ile klasik pizza',
    },
    currency: 'TRY',
    category: {
      id: 'cat-3',
      name: { en: 'Pizza', tr: 'Pizza' },
    },
    options: [
      {
        name: 'opt-size',
        label: { en: 'Size', tr: 'Boyut' },
        options: [
          { value: 'small', label: { en: 'Small', tr: 'Küçük' } },
          { value: 'medium', label: { en: 'Medium', tr: 'Orta' }, additional_price: 50.0 },
          { value: 'large', label: { en: 'Large', tr: 'Büyük' }, additional_price: 100.0 },
        ],
      },
    ],
  },
];

// Mock orders database
export const orders = new Map<string, StoredOrder>();

// Mock locations
export const locations: Location[] = [
  { id: 'loc-1', name: 'Table 1' },
  { id: 'loc-2', name: 'Table 2' },
  { id: 'loc-bar', name: 'Bar' },
];
