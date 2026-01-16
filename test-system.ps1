# Hardware Store - Тестування мікросервісної системи

Write-Host "🚀 Тестування Hardware Store мікросервісної системи" -ForegroundColor Cyan
Write-Host ""

# Функція для GET запитів
function Test-GetEndpoint {
    param(
        [string]$Url,
        [string]$Description
    )
    
    Write-Host "📡 Тестування: $Description" -ForegroundColor Yellow
    Write-Host "   URL: $Url" -ForegroundColor Gray
    
    try {
        $response = Invoke-WebRequest -Uri $Url -Method Get -ErrorAction Stop
        $correlationId = $response.Headers["X-Correlation-Id"]
        
        Write-Host "   ✅ Status: $($response.StatusCode)" -ForegroundColor Green
        Write-Host "   🔗 CorrelationId: $correlationId" -ForegroundColor Magenta
        Write-Host ""
        
        return $response
    }
    catch {
        Write-Host "   ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        return $null
    }
}

# Отримати порти з Aspire
Write-Host "🔍 Перевіряємо запущені сервіси..." -ForegroundColor Yellow

# Знайти порт Gateway
$gatewayPort = 5000 # За замовчуванням, може змінитися

Write-Host "Gateway працює на порту: $gatewayPort" -ForegroundColor Green
Write-Host ""

# Тест 1: Health Check Gateway
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "TEST 1: Gateway Health Check" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Test-GetEndpoint -Url "http://localhost:$gatewayPort/health" -Description "Gateway Health"

# Тест 2: Отримати всі продукти через Gateway
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "TEST 2: Get All Products через Gateway" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
$productsResponse = Test-GetEndpoint -Url "http://localhost:$gatewayPort/api/products" -Description "Get Products"

if ($productsResponse) {
    $products = $productsResponse.Content | ConvertFrom-Json
    Write-Host "   📦 Знайдено продуктів: $($products.Count)" -ForegroundColor Cyan
    
    if ($products.Count -gt 0) {
        Write-Host "   Перший продукт:" -ForegroundColor Gray
        Write-Host "   - Id: $($products[0].id)" -ForegroundColor Gray
        Write-Host "   - Name: $($products[0].name)" -ForegroundColor Gray
        Write-Host "   - Price: $($products[0].price.amount) $($products[0].price.currency)" -ForegroundColor Gray
    }
    Write-Host ""
}

# Тест 3: Aggregator Dashboard
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "TEST 3: Aggregator Dashboard" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
$dashboardResponse = Test-GetEndpoint -Url "http://localhost:$gatewayPort/api/aggregator/dashboard" -Description "Aggregated Dashboard"

if ($dashboardResponse) {
    $dashboard = $dashboardResponse.Content | ConvertFrom-Json
    Write-Host "   📊 Dashboard Statistics:" -ForegroundColor Cyan
    Write-Host "   - Total Products: $($dashboard.totalProducts)" -ForegroundColor Green
    Write-Host "   - Total Inventory Value: $($dashboard.totalInventoryValue)" -ForegroundColor Green
    Write-Host "   - Products by Category:" -ForegroundColor Yellow
    
    foreach ($category in $dashboard.productsByCategory.PSObject.Properties) {
        Write-Host "     * $($category.Name): $($category.Value)" -ForegroundColor Gray
    }
    Write-Host ""
}

# Тест 4: Перевірка CorrelationId propagation
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "TEST 4: CorrelationId Propagation" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan

$customCorrelationId = [Guid]::NewGuid().ToString()
Write-Host "   Відправляємо custom CorrelationId: $customCorrelationId" -ForegroundColor Yellow

try {
    $headers = @{
        "X-Correlation-Id" = $customCorrelationId
    }
    $response = Invoke-WebRequest -Uri "http://localhost:$gatewayPort/api/products" -Headers $headers -Method Get
    $returnedCorrelationId = $response.Headers["X-Correlation-Id"]
    
    if ($returnedCorrelationId -eq $customCorrelationId) {
        Write-Host "   ✅ CorrelationId правильно propagated!" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  CorrelationId різні:" -ForegroundColor Yellow
        Write-Host "      Sent: $customCorrelationId" -ForegroundColor Gray
        Write-Host "      Received: $returnedCorrelationId" -ForegroundColor Gray
    }
} catch {
    Write-Host "   ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Тест 5: Docker Containers
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "TEST 5: Docker Containers Status" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "   Запущені контейнери:" -ForegroundColor Yellow
docker ps --format "   {{.Names}} - {{.Status}}" | Write-Host -ForegroundColor Green
Write-Host ""

# Підсумок
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ Тестування завершено!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "📊 Aspire Dashboard: https://localhost:17120" -ForegroundColor Magenta
Write-Host "🌐 API Gateway: http://localhost:$gatewayPort" -ForegroundColor Magenta
Write-Host ""
Write-Host "Для детального моніторингу відкрийте Aspire Dashboard у браузері" -ForegroundColor Yellow
