import http from 'node:http';
import { URL } from 'node:url';
import {
  CreateOrderRequest,
  OrderResponse,
  CancelOrderRequest,
  CancelOrderResponse,
  ProductsResponse,
} from './types.js';
import { products, orders, locations } from './database.js';

const PORT = 5000;
const AUTH_TOKEN = 'secret-token'; // In a real app, validate this properly

// Logger helper
const log = (level: string, message: string, meta?: Record<string, unknown>) => {
  console.log(JSON.stringify({ time: new Date().toISOString(), level, message, ...meta }));
};

// Middleware: Authentication
const authenticate = (req: http.IncomingMessage): boolean => {
  const authHeader = req.headers.authorization;
  if (!authHeader) {
    log('WARN', 'Missing Authorization header');
    return false;
  }
  // In a real app, validate the token properly
  return true;
};

// Helper: Parse JSON body
const parseBody = async <T>(req: http.IncomingMessage): Promise<T> => {
  return new Promise((resolve, reject) => {
    let body = '';
    req.on('data', (chunk) => (body += chunk.toString()));
    req.on('end', () => {
      try {
        resolve(JSON.parse(body) as T);
      } catch (err) {
        reject(new Error('Invalid JSON'));
      }
    });
    req.on('error', reject);
  });
};

// Helper: Send JSON response
const sendJSON = (res: http.ServerResponse, status: number, data: unknown) => {
  res.writeHead(status, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify(data));
};

// Helper: Send error
const sendError = (res: http.ServerResponse, status: number, message: string) => {
  res.writeHead(status, { 'Content-Type': 'text/plain' });
  res.end(message);
};

// Route handlers
const handleProducts = (req: http.IncomingMessage, res: http.ServerResponse, url: URL) => {
  const cursor = url.searchParams.get('cursor');
  log('INFO', 'Fetching products', { cursor });

  // Simple pagination simulation
  let respProducts = products;
  let nextCursor: string | undefined = undefined;

  if (cursor === '' || cursor === null) {
    nextCursor = 'end';
  } else if (cursor === 'end') {
    respProducts = [];
  }

  const response: ProductsResponse = {
    products: respProducts,
    next_cursor: nextCursor,
  };

  sendJSON(res, 200, response);
};

/**
 * Idempotency-Key -> order id, for HPI's create-order deduplication rule.
 *
 * In a real POS this MUST survive a restart: the window it closes is "we accepted the
 * order, the response was lost to a timeout, HeyHolo retried" — and a process that
 * restarts in between would take the retry as a new order. An in-memory Map is fine
 * for an example and wrong for production.
 */
const idempotencyKeys = new Map<string, string>();

const handleCreateOrder = async (req: http.IncomingMessage, res: http.ServerResponse) => {
  try {
    // HPI rule: every create carries an Idempotency-Key equal to HeyHolo's internal order
    // id. Deduplicating is OPTIONAL in the contract and strongly recommended in practice —
    // without it, a timed-out-but-successful create becomes a real double order on retry.
    // Checked before the body is even read: a replay needs no parsing.
    const idempotencyKey = req.headers['idempotency-key'];
    if (typeof idempotencyKey === 'string' && idempotencyKeys.has(idempotencyKey)) {
      const known = orders.get(idempotencyKeys.get(idempotencyKey)!);
      if (known) {
        log('INFO', 'Idempotent replay — returning the original order', { id: known.id, key: idempotencyKey });
        sendJSON(res, 200, { id: known.id, status: known.status, is_addition: false });
        return;
      }
    }

    const body = await parseBody<CreateOrderRequest>(req);

    let orderId: string;
    let status: 'accepted' | 'pending' = 'accepted';
    let isAddition = false;

    if (body.existing_order_id) {
      const existing = orders.get(body.existing_order_id);
      if (!existing) {
        sendError(res, 404, 'Existing order not found');
        return;
      }

      // Append items to existing order
      existing.items.push(...body.items);
      if (body.note) {
        existing.note = body.note;
      }
      orders.set(body.existing_order_id, existing);

      orderId = existing.id;
      status = existing.status as 'accepted' | 'pending';
      isAddition = true;
      log('INFO', 'Added items to order', { id: orderId, items_added: body.items.length });
    } else {
      // Create new order
      orderId = `ord-${Math.floor(Math.random() * 100000)}`;

      orders.set(orderId, {
        id: orderId,
        status,
        items: body.items,
        location: body.location,
        note: body.note,
        number_of_people: body.number_of_people,
        created_at: new Date(),
      });

      log('INFO', 'Order created', { id: orderId, items_count: body.items.length });
    }

    if (typeof idempotencyKey === 'string') {
      idempotencyKeys.set(idempotencyKey, orderId);
    }

    const response: OrderResponse = {
      id: orderId,
      status,
      is_addition: isAddition,
    };

    sendJSON(res, 201, response);
  } catch (err) {
    sendError(res, 400, 'Invalid request body');
  }
};

const handleGetOrder = (req: http.IncomingMessage, res: http.ServerResponse, orderId: string) => {
  const order = orders.get(orderId);
  if (!order) {
    sendError(res, 404, 'Order not found');
    return;
  }

  const response: OrderResponse = {
    id: order.id,
    status: order.status,
    is_addition: false,
  };

  sendJSON(res, 200, response);
};

const handleCancelOrder = async (
  req: http.IncomingMessage,
  res: http.ServerResponse,
  orderId: string
) => {
  try {
    const body = await parseBody<CancelOrderRequest>(req);
    const order = orders.get(orderId);

    if (!order) {
      sendError(res, 404, 'Order not found');
      return;
    }

    order.status = 'cancelled';
    orders.set(orderId, order);

    log('INFO', 'Order cancelled', { id: orderId, reason: body.reason });

    const response: CancelOrderResponse = {
      status: 'cancelled',
    };

    sendJSON(res, 200, response);
  } catch (err) {
    sendError(res, 400, 'Invalid request body');
  }
};

const handleLocations = (req: http.IncomingMessage, res: http.ServerResponse) => {
  sendJSON(res, 200, locations);
};

// Main request handler
const server = http.createServer((req, res) => {
  const start = Date.now();
  const url = new URL(req.url || '/', `http://${req.headers.host}`);

  log('INFO', 'Request started', { method: req.method, path: url.pathname });

  // Authentication middleware
  if (!authenticate(req)) {
    sendError(res, 401, 'Unauthorized');
    return;
  }

  // Route handling
  if (req.method === 'GET' && url.pathname === '/products') {
    handleProducts(req, res, url);
  } else if (req.method === 'POST' && url.pathname === '/orders') {
    handleCreateOrder(req, res);
  } else if (req.method === 'GET' && url.pathname.startsWith('/orders/')) {
    const orderId = url.pathname.split('/')[2];
    if (orderId && !url.pathname.includes('/cancel')) {
      handleGetOrder(req, res, orderId);
    } else {
      sendError(res, 404, 'Not Found');
    }
  } else if (req.method === 'POST' && url.pathname.match(/^\/orders\/[^/]+\/cancel$/)) {
    const orderId = url.pathname.split('/')[2];
    handleCancelOrder(req, res, orderId);
  } else if (req.method === 'GET' && url.pathname === '/locations') {
    handleLocations(req, res);
  } else {
    sendError(res, 404, 'Not Found');
  }

  const duration = Date.now() - start;
  log('INFO', 'Request completed', { method: req.method, path: url.pathname, duration });
});

// Graceful shutdown
process.on('SIGINT', () => {
  log('INFO', 'Shutting down server...');
  server.close(() => {
    log('INFO', 'Server exited');
    process.exit(0);
  });
});

server.listen(PORT, () => {
  log('INFO', 'Starting POS Service Example', { port: PORT });
});
