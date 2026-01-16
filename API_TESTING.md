# API Testing Examples

## Using Swagger UI

1. Запустіть проект:
```powershell
cd HardwareStore.WebUI
dotnet run
```

2. Відкрийте браузер: `https://localhost:5001`

## Using PowerShell

### Get All Products
```powershell
$response = Invoke-RestMethod -Uri "https://localhost:5001/api/products" -Method GET
$response | ConvertTo-Json -Depth 5
```

### Get Product by ID
```powershell
$productId = "6756..." # Replace with actual ID
$response = Invoke-RestMethod -Uri "https://localhost:5001/api/products/$productId" -Method GET
$response | ConvertTo-Json -Depth 5
```

### Create Product
```powershell
$product = @{
    name = "Cordless Drill"
    description = "Professional 20V cordless drill with 2 batteries"
    category = "Power Tools"
    price = 199.99
    currency = "USD"
    stockQuantity = 15
    manufacturer = "DeWalt"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "https://localhost:5001/api/products" `
    -Method POST `
    -ContentType "application/json" `
    -Body $product

Write-Host "Created Product ID: $response"
```

### Update Product
```powershell
$productId = "6756..." # Replace with actual ID

$updatedProduct = @{
    id = $productId
    name = "Cordless Drill - Updated"
    description = "Professional 20V cordless drill with 3 batteries"
    category = "Power Tools"
    price = 219.99
    currency = "USD"
    stockQuantity = 25
    manufacturer = "DeWalt"
    isAvailable = $true
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:5001/api/products/$productId" `
    -Method PUT `
    -ContentType "application/json" `
    -Body $updatedProduct

Write-Host "Product updated successfully"
```

### Delete Product
```powershell
$productId = "6756..." # Replace with actual ID

Invoke-RestMethod -Uri "https://localhost:5001/api/products/$productId" -Method DELETE

Write-Host "Product deleted successfully"
```

## Testing Validation

### Invalid Product (Missing Required Fields)
```powershell
$invalidProduct = @{
    name = ""
    description = ""
    category = "Tools"
    price = -10
    currency = "US"
    stockQuantity = -5
    manufacturer = ""
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "https://localhost:5001/api/products" `
        -Method POST `
        -ContentType "application/json" `
        -Body $invalidProduct
} catch {
    $_.ErrorDetails.Message | ConvertFrom-Json | ConvertTo-Json -Depth 5
}
```

## Testing Error Handling

### Get Non-Existent Product (404)
```powershell
try {
    Invoke-RestMethod -Uri "https://localhost:5001/api/products/000000000000000000000000" -Method GET
} catch {
    Write-Host "Status Code:" $_.Exception.Response.StatusCode
    $_.ErrorDetails.Message | ConvertFrom-Json | ConvertTo-Json -Depth 5
}
```

### Invalid ObjectId Format (400)
```powershell
try {
    Invoke-RestMethod -Uri "https://localhost:5001/api/products/invalid-id" -Method GET
} catch {
    Write-Host "Status Code:" $_.Exception.Response.StatusCode
    $_.ErrorDetails.Message | ConvertFrom-Json | ConvertTo-Json -Depth 5
}
```

## MongoDB Direct Access

### Connect to MongoDB
```powershell
# Using MongoDB Shell
mongosh mongodb://localhost:27017/HardwareStoreDb
```

### View Collections
```javascript
show collections
```

### View Products
```javascript
db.products.find().pretty()
```

### View Customers
```javascript
db.customers.find().pretty()
```

### Count Documents
```javascript
db.products.countDocuments()
```

### Find by Category
```javascript
db.products.find({ Category: "Tools" }).pretty()
```

## Expected Responses

### Successful Product Creation (201 Created)
```json
"6756e8f4a5c3d2e1f0b9a8c7"
```

### Successful Get Product (200 OK)
```json
{
  "id": "6756e8f4a5c3d2e1f0b9a8c7",
  "name": "Hammer",
  "description": "Heavy-duty steel hammer for construction work",
  "category": "Tools",
  "price": 25.99,
  "currency": "USD",
  "stockQuantity": 50,
  "manufacturer": "Stanley",
  "isAvailable": true,
  "isInStock": true,
  "createdAt": "2024-12-06T12:00:00Z",
  "updatedAt": "2024-12-06T12:00:00Z"
}
```

### Validation Error (400 Bad Request)
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "One or more validation failures have occurred.",
  "instance": "/api/products",
  "errors": {
    "Name": ["Name is required"],
    "Price": ["Price must be greater than 0"],
    "Currency": ["Currency must be 3 characters (e.g., USD, EUR)"]
  }
}
```

### Not Found Error (404 Not Found)
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Entity \"Product\" (000000000000000000000000) was not found.",
  "instance": "/api/products/000000000000000000000000"
}
```
