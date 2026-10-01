package ai.heyholo.hpi.model;

import java.time.Instant;
import java.util.ArrayList;
import java.util.List;

public class StoredOrder {
    private String id;
    private OrderStatus status;
    private List<OrderItem> items;
    private String location;
    private String note;
    private Integer numberOfPeople;
    private Instant createdAt;

    public StoredOrder(String id, OrderStatus status, List<OrderItem> items, String location, String note, Integer numberOfPeople) {
        this.id = id;
        this.status = status;
        this.items = new ArrayList<>(items);
        this.location = location;
        this.note = note;
        this.numberOfPeople = numberOfPeople;
        this.createdAt = Instant.now();
    }

    public String getId() { return id; }
    public OrderStatus getStatus() { return status; }
    public void setStatus(OrderStatus status) { this.status = status; }
    public List<OrderItem> getItems() { return items; }
    public synchronized void addItems(List<OrderItem> more) { items.addAll(more); }
    public String getLocation() { return location; }
    public String getNote() { return note; }
    public Integer getNumberOfPeople() { return numberOfPeople; }
    public Instant getCreatedAt() { return createdAt; }
}
