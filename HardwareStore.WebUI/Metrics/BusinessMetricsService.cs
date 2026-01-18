using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace HardwareStore.WebUI.Metrics;

/// <summary>
/// Custom business metrics service for tracking domain-specific operations
/// </summary>
public class BusinessMetricsService
{
    private readonly Meter _meter;
    private readonly ILogger<BusinessMetricsService> _logger;
    
    // Counters for business operations
    private readonly Counter<long> _productsCreatedCounter;
    private readonly Counter<long> _productsUpdatedCounter;
    private readonly Counter<long> _productsDeletedCounter;
    private readonly Counter<long> _productsViewedCounter;
    private readonly Counter<long> _customersCreatedCounter;
    private readonly Counter<long> _ordersCreatedCounter;
    private readonly Counter<long> _ordersCompletedCounter;
    private readonly Counter<long> _ordersCancelledCounter;
    
    // Histograms for latency tracking
    private readonly Histogram<double> _productOperationDuration;
    private readonly Histogram<double> _orderOperationDuration;
    private readonly Histogram<double> _databaseQueryDuration;
    
    // Observable gauges
    private int _activeUsers;
    private double _averageOrderValue;
    private int _pendingOrders;

    public BusinessMetricsService(ILogger<BusinessMetricsService> logger)
    {
        _logger = logger;
        _meter = new Meter("HardwareStore.Business", "1.0.0");
        
        // Initialize counters
        _productsCreatedCounter = _meter.CreateCounter<long>(
            "hardwarestore.products.created",
            unit: "{products}",
            description: "Total number of products created");
        
        _productsUpdatedCounter = _meter.CreateCounter<long>(
            "hardwarestore.products.updated",
            unit: "{products}",
            description: "Total number of products updated");
        
        _productsDeletedCounter = _meter.CreateCounter<long>(
            "hardwarestore.products.deleted",
            unit: "{products}",
            description: "Total number of products deleted");
        
        _productsViewedCounter = _meter.CreateCounter<long>(
            "hardwarestore.products.viewed",
            unit: "{products}",
            description: "Total number of product views");
        
        _customersCreatedCounter = _meter.CreateCounter<long>(
            "hardwarestore.customers.created",
            unit: "{customers}",
            description: "Total number of customers created");
        
        _ordersCreatedCounter = _meter.CreateCounter<long>(
            "hardwarestore.orders.created",
            unit: "{orders}",
            description: "Total number of orders created");
        
        _ordersCompletedCounter = _meter.CreateCounter<long>(
            "hardwarestore.orders.completed",
            unit: "{orders}",
            description: "Total number of orders completed");
        
        _ordersCancelledCounter = _meter.CreateCounter<long>(
            "hardwarestore.orders.cancelled",
            unit: "{orders}",
            description: "Total number of orders cancelled");
        
        // Initialize histograms
        _productOperationDuration = _meter.CreateHistogram<double>(
            "hardwarestore.products.operation.duration",
            unit: "ms",
            description: "Duration of product operations in milliseconds");
        
        _orderOperationDuration = _meter.CreateHistogram<double>(
            "hardwarestore.orders.operation.duration",
            unit: "ms",
            description: "Duration of order operations in milliseconds");
        
        _databaseQueryDuration = _meter.CreateHistogram<double>(
            "hardwarestore.database.query.duration",
            unit: "ms",
            description: "Duration of database queries in milliseconds");
        
        // Initialize observable gauges
        _meter.CreateObservableGauge(
            "hardwarestore.users.active",
            () => _activeUsers,
            unit: "{users}",
            description: "Number of currently active users");
        
        _meter.CreateObservableGauge(
            "hardwarestore.orders.average_value",
            () => _averageOrderValue,
            unit: "USD",
            description: "Average order value");
        
        _meter.CreateObservableGauge(
            "hardwarestore.orders.pending",
            () => _pendingOrders,
            unit: "{orders}",
            description: "Number of pending orders");
    }

    // Counter methods
    public void RecordProductCreated(string category)
    {
        _productsCreatedCounter.Add(1, 
            new KeyValuePair<string, object?>("category", category));
        _logger.LogDebug("Metric recorded: product created in category {Category}", category);
    }

    public void RecordProductUpdated(string category)
    {
        _productsUpdatedCounter.Add(1,
            new KeyValuePair<string, object?>("category", category));
        _logger.LogDebug("Metric recorded: product updated in category {Category}", category);
    }

    public void RecordProductDeleted(string category)
    {
        _productsDeletedCounter.Add(1,
            new KeyValuePair<string, object?>("category", category));
        _logger.LogDebug("Metric recorded: product deleted from category {Category}", category);
    }

    public void RecordProductViewed(string productId, string category)
    {
        _productsViewedCounter.Add(1,
            new KeyValuePair<string, object?>("product_id", productId),
            new KeyValuePair<string, object?>("category", category));
    }

    public void RecordCustomerCreated(string customerType = "regular")
    {
        _customersCreatedCounter.Add(1,
            new KeyValuePair<string, object?>("customer_type", customerType));
        _logger.LogDebug("Metric recorded: customer created with type {CustomerType}", customerType);
    }

    public void RecordOrderCreated(string status, decimal orderValue)
    {
        _ordersCreatedCounter.Add(1,
            new KeyValuePair<string, object?>("status", status),
            new KeyValuePair<string, object?>("value_range", GetValueRange(orderValue)));
        _logger.LogDebug("Metric recorded: order created with value {OrderValue}", orderValue);
    }

    public void RecordOrderCompleted(string paymentMethod, decimal orderValue)
    {
        _ordersCompletedCounter.Add(1,
            new KeyValuePair<string, object?>("payment_method", paymentMethod),
            new KeyValuePair<string, object?>("value_range", GetValueRange(orderValue)));
        _logger.LogDebug("Metric recorded: order completed via {PaymentMethod}", paymentMethod);
    }

    public void RecordOrderCancelled(string reason)
    {
        _ordersCancelledCounter.Add(1,
            new KeyValuePair<string, object?>("reason", reason));
        _logger.LogDebug("Metric recorded: order cancelled with reason {Reason}", reason);
    }

    // Histogram methods
    public void RecordProductOperationDuration(string operation, double durationMs, bool success)
    {
        _productOperationDuration.Record(durationMs,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("status", success ? "success" : "failure"));
    }

    public void RecordOrderOperationDuration(string operation, double durationMs, bool success)
    {
        _orderOperationDuration.Record(durationMs,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("status", success ? "success" : "failure"));
    }

    public void RecordDatabaseQueryDuration(string queryType, string collection, double durationMs)
    {
        _databaseQueryDuration.Record(durationMs,
            new KeyValuePair<string, object?>("query_type", queryType),
            new KeyValuePair<string, object?>("collection", collection));
    }

    // Gauge update methods
    public void SetActiveUsers(int count) => _activeUsers = count;
    public void SetAverageOrderValue(double value) => _averageOrderValue = value;
    public void SetPendingOrders(int count) => _pendingOrders = count;

    // Helper method for timing operations
    public IDisposable MeasureOperation(string operationType, string details)
    {
        return new OperationTimer(this, operationType, details);
    }

    private static string GetValueRange(decimal value)
    {
        return value switch
        {
            < 50 => "0-50",
            < 100 => "50-100",
            < 500 => "100-500",
            < 1000 => "500-1000",
            _ => "1000+"
        };
    }

    private class OperationTimer : IDisposable
    {
        private readonly BusinessMetricsService _metrics;
        private readonly string _operationType;
        private readonly string _details;
        private readonly Stopwatch _stopwatch;

        public OperationTimer(BusinessMetricsService metrics, string operationType, string details)
        {
            _metrics = metrics;
            _operationType = operationType;
            _details = details;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _metrics.RecordDatabaseQueryDuration(_operationType, _details, _stopwatch.ElapsedMilliseconds);
        }
    }
}
