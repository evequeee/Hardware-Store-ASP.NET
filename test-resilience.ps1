# =============================================================================
# Hardware Store - Resilience, Metrics & Health Checks Testing Script
# Lab 7 - Testing resilience patterns, custom metrics, and health checks
# =============================================================================

param(
    [string]$GatewayUrl = "http://localhost:5000",
    [string]$WebApiUrl = "http://localhost:5186",
    [string]$AggregatorUrl = "http://localhost:5183"
)

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " Hardware Store - Resilience, Metrics & Health Checks Testing" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

# Helper function to make requests with error handling
function Invoke-SafeRequest {
    param(
        [string]$Uri,
        [string]$Method = "GET",
        [object]$Body = $null,
        [string]$Description = ""
    )
    
    Write-Host "Testing: $Description" -ForegroundColor Yellow
    Write-Host "  $Method $Uri" -ForegroundColor Gray
    
    try {
        $params = @{
            Uri = $Uri
            Method = $Method
            ContentType = "application/json"
            TimeoutSec = 30
        }
        
        if ($Body) {
            $params.Body = $Body | ConvertTo-Json -Depth 10
        }
        
        $response = Invoke-RestMethod @params
        Write-Host "  ✓ SUCCESS" -ForegroundColor Green
        return @{ Success = $true; Data = $response }
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        $errorMessage = $_.Exception.Message
        Write-Host "  ✗ FAILED ($statusCode): $errorMessage" -ForegroundColor Red
        return @{ Success = $false; Error = $errorMessage; StatusCode = $statusCode }
    }
}

# =============================================================================
# PART 1: Health Checks Testing
# =============================================================================
Write-Host ""
Write-Host "Part 1: Health Checks Testing" -ForegroundColor Magenta
Write-Host "-----------------------------" -ForegroundColor Magenta

# Test WebAPI Health Endpoints
Write-Host ""
Write-Host "[WebAPI Health Checks]" -ForegroundColor Cyan

$healthResult = Invoke-SafeRequest -Uri "$WebApiUrl/health" -Description "Combined Health Check"
if ($healthResult.Success) {
    Write-Host "  Status: $($healthResult.Data.status)" -ForegroundColor $(if ($healthResult.Data.status -eq "Healthy") { "Green" } else { "Yellow" })
    Write-Host "  Total Duration: $($healthResult.Data.totalDuration)ms"
    foreach ($check in $healthResult.Data.checks) {
        $color = switch ($check.status) {
            "Healthy" { "Green" }
            "Degraded" { "Yellow" }
            default { "Red" }
        }
        Write-Host "    - $($check.name): $($check.status) ($($check.duration)ms)" -ForegroundColor $color
    }
}

$liveResult = Invoke-SafeRequest -Uri "$WebApiUrl/health/live" -Description "Liveness Probe"
if ($liveResult.Success) {
    Write-Host "  Liveness Status: $($liveResult.Data.status)" -ForegroundColor Green
}

$readyResult = Invoke-SafeRequest -Uri "$WebApiUrl/health/ready" -Description "Readiness Probe"
if ($readyResult.Success) {
    Write-Host "  Readiness Status: $($readyResult.Data.status)" -ForegroundColor Green
}

# Test Aggregator Health Endpoints
Write-Host ""
Write-Host "[Aggregator Health Checks]" -ForegroundColor Cyan

$aggHealthResult = Invoke-SafeRequest -Uri "$AggregatorUrl/health" -Description "Aggregator Health Check"
if ($aggHealthResult.Success) {
    Write-Host "  Status: $($aggHealthResult.Data.status)" -ForegroundColor $(if ($aggHealthResult.Data.status -eq "Healthy") { "Green" } else { "Yellow" })
    foreach ($check in $aggHealthResult.Data.checks) {
        $color = switch ($check.status) {
            "Healthy" { "Green" }
            "Degraded" { "Yellow" }
            default { "Red" }
        }
        Write-Host "    - $($check.name): $($check.status)" -ForegroundColor $color
    }
}

# Test Gateway Health Endpoints
Write-Host ""
Write-Host "[Gateway Health Checks]" -ForegroundColor Cyan

$gwHealthResult = Invoke-SafeRequest -Uri "$GatewayUrl/health" -Description "Gateway Health Check"
if ($gwHealthResult.Success) {
    Write-Host "  Status: $($gwHealthResult.Data.status)" -ForegroundColor $(if ($gwHealthResult.Data.status -eq "Healthy") { "Green" } else { "Yellow" })
    foreach ($check in $gwHealthResult.Data.checks) {
        $color = switch ($check.status) {
            "Healthy" { "Green" }
            "Degraded" { "Yellow" }
            default { "Red" }
        }
        Write-Host "    - $($check.name): $($check.status)" -ForegroundColor $color
    }
}

# =============================================================================
# PART 2: API Operations Testing (Generate Metrics)
# =============================================================================
Write-Host ""
Write-Host "Part 2: API Operations Testing (Metrics Generation)" -ForegroundColor Magenta
Write-Host "---------------------------------------------------" -ForegroundColor Magenta

# Test Product Operations
Write-Host ""
Write-Host "[Product CRUD Operations]" -ForegroundColor Cyan

# Get All Products
$productsResult = Invoke-SafeRequest -Uri "$WebApiUrl/api/products" -Description "Get All Products"
if ($productsResult.Success) {
    Write-Host "  Retrieved $($productsResult.Data.Count) products"
    $testProductId = if ($productsResult.Data.Count -gt 0) { $productsResult.Data[0].id } else { $null }
}

# Create Product
$newProduct = @{
    name = "Test Drill - Lab7"
    description = "Test product for resilience testing"
    category = "Power Tools"
    price = 149.99
    currency = "USD"
    stockQuantity = 25
    manufacturer = "TestBrand"
}

$createResult = Invoke-SafeRequest -Uri "$WebApiUrl/api/products" -Method "POST" -Body $newProduct -Description "Create Product"
if ($createResult.Success) {
    Write-Host "  Created Product ID: $($createResult.Data)"
    $createdProductId = $createResult.Data
}

# Get Single Product
if ($testProductId) {
    $getResult = Invoke-SafeRequest -Uri "$WebApiUrl/api/products/$testProductId" -Description "Get Single Product"
    if ($getResult.Success) {
        Write-Host "  Product: $($getResult.Data.name) - $($getResult.Data.category)"
    }
}

# Update Product
if ($createdProductId) {
    $updateProduct = @{
        id = $createdProductId
        name = "Updated Test Drill - Lab7"
        description = "Updated test product"
        category = "Power Tools"
        price = 179.99
        currency = "USD"
        stockQuantity = 30
        manufacturer = "TestBrand"
        isAvailable = $true
    }
    
    $updateResult = Invoke-SafeRequest -Uri "$WebApiUrl/api/products/$createdProductId" -Method "PUT" -Body $updateProduct -Description "Update Product"
}

# Delete Product
if ($createdProductId) {
    $deleteResult = Invoke-SafeRequest -Uri "$WebApiUrl/api/products/$createdProductId" -Method "DELETE" -Description "Delete Product"
}

# =============================================================================
# PART 3: Aggregator Service Testing
# =============================================================================
Write-Host ""
Write-Host "Part 3: Aggregator Service Testing" -ForegroundColor Magenta
Write-Host "----------------------------------" -ForegroundColor Magenta

# Get Aggregated Dashboard
Write-Host ""
Write-Host "[Aggregator Endpoints]" -ForegroundColor Cyan

$dashboardResult = Invoke-SafeRequest -Uri "$AggregatorUrl/api/aggregator/dashboard" -Description "Get Aggregated Dashboard"
if ($dashboardResult.Success) {
    Write-Host "  Total Products: $($dashboardResult.Data.totalProducts)"
    Write-Host "  Total Inventory Value: `$$($dashboardResult.Data.totalInventoryValue)"
    Write-Host "  Categories:"
    foreach ($category in $dashboardResult.Data.productsByCategory.GetEnumerator()) {
        Write-Host "    - $($category.Key): $($category.Value) products"
    }
}

# Get Enriched Product
if ($testProductId) {
    $enrichedResult = Invoke-SafeRequest -Uri "$AggregatorUrl/api/aggregator/product/$testProductId" -Description "Get Enriched Product"
    if ($enrichedResult.Success) {
        Write-Host "  Product: $($enrichedResult.Data.product.name)"
        Write-Host "  Is Low Stock: $($enrichedResult.Data.isLowStock)"
        Write-Host "  Stock Value: `$$($enrichedResult.Data.stockValue)"
    }
}

# =============================================================================
# PART 4: Gateway Testing (Proxy Requests)
# =============================================================================
Write-Host ""
Write-Host "Part 4: Gateway Testing" -ForegroundColor Magenta
Write-Host "----------------------" -ForegroundColor Magenta

Write-Host ""
Write-Host "[Gateway Proxy Requests]" -ForegroundColor Cyan

# Test products through gateway
$gwProductsResult = Invoke-SafeRequest -Uri "$GatewayUrl/api/products" -Description "Get Products via Gateway"
if ($gwProductsResult.Success) {
    Write-Host "  Retrieved $($gwProductsResult.Data.Count) products via Gateway"
}

# Test aggregator through gateway
$gwDashboardResult = Invoke-SafeRequest -Uri "$GatewayUrl/api/aggregator/dashboard" -Description "Get Dashboard via Gateway"
if ($gwDashboardResult.Success) {
    Write-Host "  Dashboard retrieved via Gateway successfully"
}

# =============================================================================
# PART 5: Resilience Testing
# =============================================================================
Write-Host ""
Write-Host "Part 5: Resilience Pattern Testing" -ForegroundColor Magenta
Write-Host "----------------------------------" -ForegroundColor Magenta

Write-Host ""
Write-Host "[Testing Retry and Timeout Behavior]" -ForegroundColor Cyan

# Make multiple rapid requests to trigger resilience metrics
Write-Host "  Making 10 rapid requests to test resilience..."
$successCount = 0
$failCount = 0

for ($i = 1; $i -le 10; $i++) {
    try {
        $null = Invoke-RestMethod -Uri "$AggregatorUrl/api/aggregator/dashboard" -Method GET -TimeoutSec 5
        $successCount++
        Write-Host "    Request $i`: Success" -ForegroundColor Green
    }
    catch {
        $failCount++
        Write-Host "    Request $i`: Failed" -ForegroundColor Red
    }
    Start-Sleep -Milliseconds 100
}

Write-Host ""
Write-Host "  Resilience Test Results:" -ForegroundColor Yellow
Write-Host "    Successful Requests: $successCount/10" -ForegroundColor $(if ($successCount -eq 10) { "Green" } else { "Yellow" })
Write-Host "    Failed Requests: $failCount/10" -ForegroundColor $(if ($failCount -eq 0) { "Green" } else { "Red" })

# =============================================================================
# SUMMARY
# =============================================================================
Write-Host ""
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " Testing Summary" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Health Checks:" -ForegroundColor Yellow
Write-Host "  - /health - Combined health check (all dependencies)"
Write-Host "  - /health/live - Liveness probe (app is running)"
Write-Host "  - /health/ready - Readiness probe (dependencies available)"
Write-Host ""
Write-Host "Metrics Available in Aspire Dashboard:" -ForegroundColor Yellow
Write-Host "  Business Metrics (HardwareStore.Business):"
Write-Host "    - hardwarestore.products.created"
Write-Host "    - hardwarestore.products.updated"
Write-Host "    - hardwarestore.products.deleted"
Write-Host "    - hardwarestore.products.viewed"
Write-Host "    - hardwarestore.products.operation.duration"
Write-Host "    - hardwarestore.orders.created"
Write-Host "    - hardwarestore.orders.completed"
Write-Host ""
Write-Host "  Gateway Metrics (HardwareStore.Gateway):"
Write-Host "    - gateway.requests.total"
Write-Host "    - gateway.request.duration"
Write-Host "    - gateway.errors.total"
Write-Host ""
Write-Host "  Aggregator Metrics (HardwareStore.Aggregator):"
Write-Host "    - aggregator.requests.total"
Write-Host "    - aggregator.request.duration"
Write-Host ""
Write-Host "  Core Metrics (HardwareStore.Metrics):"
Write-Host "    - hardwarestore.api.requests"
Write-Host "    - hardwarestore.cache.hits"
Write-Host "    - hardwarestore.cache.misses"
Write-Host "    - hardwarestore.request.duration"
Write-Host "    - hardwarestore.connections.active"
Write-Host ""
Write-Host "Resilience Patterns:" -ForegroundColor Yellow
Write-Host "  - Standard Resilience Handler with custom configuration"
Write-Host "  - Retry Policy: 3 attempts, exponential backoff, jitter"
Write-Host "  - Circuit Breaker: 50% failure ratio, 30s break duration"
Write-Host "  - Timeout Policy: 10s per attempt, 30s total"
Write-Host ""
Write-Host "To view metrics and traces, open Aspire Dashboard" -ForegroundColor Green
Write-Host "=================================================================" -ForegroundColor Cyan
