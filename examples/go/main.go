package main

import (
	"context"
	"encoding/json"
	"fmt"
	"log/slog"
	"math/rand"
	"net/http"
	"os"
	"os/signal"
	"sync"
	"time"
)

// Configuration
const (
	Port      = ":5000"
	AuthToken = "secret-token" // In a real app, this should be validated securely
)

// --- Models ---

type ProductOptionValue struct {
	Value           string  `json:"value"`
	Label           any     `json:"label"`
	AdditionalPrice float64 `json:"additional_price,omitempty"`
}

type ProductOption struct {
	Name    string               `json:"name"`
	Label   any                  `json:"label"`
	Options []ProductOptionValue `json:"options"`
}

type Product struct {
	ID          string          `json:"id"`
	Name        any             `json:"name"` // Can be string or map[string]string
	Price       float64         `json:"price"`
	Description any             `json:"description,omitempty"`
	ImageURL    string          `json:"image_url,omitempty"`
	Currency    string          `json:"currency,omitempty"`
	Options     []ProductOption `json:"options,omitempty"`
}

type ProductsResponse struct {
	Products   []Product `json:"products"`
	NextCursor string    `json:"next_cursor,omitempty"`
}

type OrderItemOption struct {
	Name   string   `json:"name"`
	Values []string `json:"values"`
}

type OrderItem struct {
	ProductID string            `json:"product_id"`
	Quantity  int               `json:"quantity"`
	Note      string            `json:"note,omitempty"`
	Options   []OrderItemOption `json:"options,omitempty"`
}

type CreateOrderRequest struct {
	Items          []OrderItem `json:"items"`
	Location       string      `json:"location"`
	Note           string      `json:"note"`
	NumberOfPeople int         `json:"number_of_people"`
	ExistingOrder  string      `json:"existing_order_id,omitempty"`
}

type OrderStatus string

const (
	StatusAccepted   OrderStatus = "accepted"
	StatusReady      OrderStatus = "ready"
	StatusCompleted  OrderStatus = "completed"
	StatusCancelled  OrderStatus = "cancelled"
	StatusPending    OrderStatus = "pending"
	StatusInProgress OrderStatus = "in_progress"
	StatusFailed     OrderStatus = "failed"
	StatusUnknown    OrderStatus = "unknown"
)

type OrderResponse struct {
	ID         string      `json:"id"`
	Status     OrderStatus `json:"status"`
	IsAddition bool        `json:"is_addition"`
}

type CancelOrderRequest struct {
	Reason string `json:"reason"`
}

type CancelOrderResponse struct {
	Status OrderStatus `json:"status"`
}

type Location struct {
	ID   string `json:"id"`
	Name string `json:"name"`
}

// --- In-Memory Store ---

type StoredOrder struct {
	ID             string
	Status         OrderStatus
	Items          []OrderItem
	Location       string
	Note           string
	NumberOfPeople int
	CreatedAt      time.Time
}

// --- In-Memory Data ---

var (
	// Mock products database
	products = []Product{
		{
			ID:    "prod-1",
			Name:  map[string]string{"en": "Cheeseburger", "tr": "Peynirli Burger"},
			Price: 150.00,
			Description: map[string]string{
				"en": "Delicious burger with cheddar",
				"tr": "Çedarlı lezzetli burger",
			},
			Currency: "TRY",
			Options: []ProductOption{
				{
					Name:  "opt-doneness",
					Label: map[string]string{"en": "Doneness", "tr": "Pişme Derecesi"},
					Options: []ProductOptionValue{
						{Value: "rare", Label: map[string]string{"en": "Rare", "tr": "Az Pişmiş"}},
						{Value: "medium", Label: map[string]string{"en": "Medium", "tr": "Orta Pişmiş"}},
						{Value: "well-done", Label: map[string]string{"en": "Well Done", "tr": "İyi Pişmiş"}},
					},
				},
				{
					Name:  "opt-extras",
					Label: map[string]string{"en": "Extras", "tr": "Ekstralar"},
					Options: []ProductOptionValue{
						{Value: "extra-cheese", Label: "Extra Cheese", AdditionalPrice: 20.0},
						{Value: "bacon", Label: "Bacon", AdditionalPrice: 30.0},
					},
				},
			},
		},
		{
			ID:       "prod-2",
			Name:     "Coke", // Example of simple string name
			Price:    40.00,
			Currency: "TRY",
		},
		{
			ID:    "prod-3",
			Name:  map[string]string{"en": "Pizza Margherita", "tr": "Margarita Pizza"},
			Price: 200.00,
			Description: map[string]string{
				"en": "Classic pizza with tomato and mozzarella",
				"tr": "Domates ve mozzarella ile klasik pizza",
			},
			Currency: "TRY",
			Options: []ProductOption{
				{
					Name:  "opt-size",
					Label: map[string]string{"en": "Size", "tr": "Boyut"},
					Options: []ProductOptionValue{
						{Value: "small", Label: map[string]string{"en": "Small", "tr": "Küçük"}},
						{Value: "medium", Label: map[string]string{"en": "Medium", "tr": "Orta"}, AdditionalPrice: 50.0},
						{Value: "large", Label: map[string]string{"en": "Large", "tr": "Büyük"}, AdditionalPrice: 100.0},
					},
				},
			},
		},
	}

	// Mock orders database
	orders = make(map[string]StoredOrder) // ID -> Order

	// Idempotency-Key -> order ID, for HPI's create-order deduplication rule.
	//
	// In a real POS this MUST survive a restart: the window it closes is "we accepted the
	// order, the response was lost to a timeout, HeyHolo retried" — and a process that
	// restarts in between would take the retry as a new order. An in-memory map is fine
	// for an example and wrong for production.
	idempotencyKeys = make(map[string]string)

	mu sync.RWMutex
)

// --- Main ---

func main() {
	logger := slog.New(slog.NewJSONHandler(os.Stdout, nil))

	// Using Go 1.22+ ServeMux with method and path matching
	mux := http.NewServeMux()

	// Register routes
	// Note: The paths here assume the service is mounted at root or handles the /heyholo/v1 prefix itself.
	// Adjust based on your actual deployment (e.g. behind a proxy).
	mux.HandleFunc("GET /products", handleProducts(logger))
	mux.HandleFunc("POST /orders", handleCreateOrder(logger))
	mux.HandleFunc("GET /orders/{order_id}", handleGetOrder(logger))
	mux.HandleFunc("POST /orders/{order_id}/cancel", handleCancelOrder(logger))
	mux.HandleFunc("GET /locations", handleLocations(logger))

	// Middleware chain: Auth -> Logging -> Mux
	handler := authMiddleware(logger, loggingMiddleware(logger, mux))

	srv := &http.Server{
		Addr:         Port,
		Handler:      handler,
		ReadTimeout:  10 * time.Second,
		WriteTimeout: 10 * time.Second,
		IdleTimeout:  60 * time.Second,
	}

	// Graceful shutdown setup
	go func() {
		logger.Info("Starting POS Service Example", "addr", Port)
		if err := srv.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			logger.Error("Server failed", "error", err)
			os.Exit(1)
		}
	}()

	quit := make(chan os.Signal, 1)
	signal.Notify(quit, os.Interrupt)
	<-quit

	logger.Info("Shutting down server...")
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	if err := srv.Shutdown(ctx); err != nil {
		logger.Error("Server forced to shutdown", "error", err)
	}
	logger.Info("Server exited")
}

// --- Handlers ---

func handleProducts(logger *slog.Logger) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		cursor := r.URL.Query().Get("cursor")
		logger.Info("Fetching products", "cursor", cursor)

		// In a real app, use cursor to paginate 'products' slice
		// For this example, we just return all products if cursor is empty
		// or return empty list if cursor is "end"

		var respProducts []Product
		nextCursor := ""

		if cursor == "" {
			respProducts = products
			nextCursor = "end" // Simple pagination simulation
		} else {
			respProducts = []Product{}
		}

		resp := ProductsResponse{
			Products:   respProducts,
			NextCursor: nextCursor,
		}

		writeJSON(w, http.StatusOK, resp)
	}
}

func handleCreateOrder(logger *slog.Logger) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		// HPI rule: every create carries an Idempotency-Key equal to HeyHolo's internal
		// order ID. Deduplicating is OPTIONAL in the contract and strongly recommended in
		// practice — without it, a timed-out-but-successful create becomes a real double
		// order on retry. Checked before the body is decoded: a replay needs no parsing.
		idempotencyKey := r.Header.Get("Idempotency-Key")
		if idempotencyKey != "" {
			mu.RLock()
			knownID, seen := idempotencyKeys[idempotencyKey]
			known, exists := orders[knownID]
			mu.RUnlock()
			if seen && exists {
				logger.Info("Idempotent replay — returning the original order", "id", known.ID, "key", idempotencyKey)
				writeJSON(w, http.StatusOK, OrderResponse{ID: known.ID, Status: known.Status, IsAddition: false})
				return
			}
		}

		var req CreateOrderRequest
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, "Invalid request body", http.StatusBadRequest)
			return
		}

		mu.Lock()
		defer mu.Unlock()

		var orderID string
		var status OrderStatus
		isAddition := false

		if req.ExistingOrder != "" {
			// Check if existing order exists
			existing, exists := orders[req.ExistingOrder]
			if !exists {
				http.Error(w, "Existing order not found", http.StatusNotFound)
				return
			}

			// Append items to existing order
			existing.Items = append(existing.Items, req.Items...)
			// Update other fields if needed, e.g. Note
			if req.Note != "" {
				existing.Note = req.Note
			}

			orders[req.ExistingOrder] = existing

			orderID = existing.ID
			status = existing.Status
			isAddition = true
			logger.Info("Added items to order", "id", orderID, "items_added", len(req.Items))
		} else {
			// Create new order
			orderID = fmt.Sprintf("ord-%d", rand.Intn(100000))
			status = StatusAccepted // Auto-accept for demo

			newOrder := StoredOrder{
				ID:             orderID,
				Status:         status,
				Items:          req.Items,
				Location:       req.Location,
				Note:           req.Note,
				NumberOfPeople: req.NumberOfPeople,
				CreatedAt:      time.Now(),
			}
			orders[orderID] = newOrder
			logger.Info("Order created", "id", orderID, "items_count", len(req.Items))
		}

		if idempotencyKey != "" {
			idempotencyKeys[idempotencyKey] = orderID
		}

		resp := OrderResponse{
			ID:         orderID,
			Status:     status,
			IsAddition: isAddition,
		}

		writeJSON(w, http.StatusCreated, resp)
	}
}

func handleGetOrder(_ *slog.Logger) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		orderID := r.PathValue("order_id")

		mu.RLock()
		order, exists := orders[orderID]
		mu.RUnlock()

		if !exists {
			http.Error(w, "Order not found", http.StatusNotFound)
			return
		}

		resp := OrderResponse{
			ID:     order.ID,
			Status: order.Status,
		}

		writeJSON(w, http.StatusOK, resp)
	}
}

func handleCancelOrder(logger *slog.Logger) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		orderID := r.PathValue("order_id")

		var req CancelOrderRequest
		_ = json.NewDecoder(r.Body).Decode(&req) // Reason is optional/logging only

		mu.Lock()
		order, exists := orders[orderID]
		if !exists {
			mu.Unlock()
			http.Error(w, "Order not found", http.StatusNotFound)
			return
		}

		order.Status = StatusCancelled
		orders[orderID] = order
		mu.Unlock()

		logger.Info("Order cancelled", "id", orderID, "reason", req.Reason)

		resp := CancelOrderResponse{
			Status: StatusCancelled,
		}

		writeJSON(w, http.StatusOK, resp)
	}
}

func handleLocations(_ *slog.Logger) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		locations := []Location{
			{ID: "loc-1", Name: "Table 1"},
			{ID: "loc-2", Name: "Table 2"},
			{ID: "loc-bar", Name: "Bar"},
		}
		writeJSON(w, http.StatusOK, locations)
	}
}

// --- Middleware ---

func loggingMiddleware(logger *slog.Logger, next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		start := time.Now()
		logger.Info("Request started", "method", r.Method, "path", r.URL.Path)

		next.ServeHTTP(w, r)

		logger.Info("Request completed", "method", r.Method, "path", r.URL.Path, "duration", time.Since(start))
	})
}

func authMiddleware(logger *slog.Logger, next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		// Check for Authorization header
		// Format: "Bearer <token>" or just "<token>" depending on spec.
		// The Postman collection uses "Bearer <token>".

		authHeader := r.Header.Get("Authorization")
		if authHeader == "" {
			// For this example, we might be lenient or strict.
			// Let's be strict but log it.
			logger.Warn("Missing Authorization header")
			http.Error(w, "Unauthorized", http.StatusUnauthorized)
			return
		}

		// In a real app, validate the token properly
		// if !strings.Contains(authHeader, AuthToken) { ... }

		next.ServeHTTP(w, r)
	})
}

// --- Helpers ---

func writeJSON(w http.ResponseWriter, status int, v any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	if err := json.NewEncoder(w).Encode(v); err != nil {
		slog.Error("Failed to write response", "error", err)
	}
}
