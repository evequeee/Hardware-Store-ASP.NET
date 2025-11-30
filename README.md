# Hardware Store API - Product Catalog

**Тришарова архітектура** з використанням **ADO.NET** та **Dapper** (без Entity Framework Core)

## 📋 Опис проєкту

Це ASP.NET Core 8.0 Web API для управління каталогом продуктів магазину будівельних матеріалів. Проєкт реалізований згідно з вимогами **Практичного заняття №2** з використанням:

- **Vertical Slice Architecture** (вертикальна архітектура по функціональності)
- **DAL (Data Access Layer)** → репозиторії на базі ADO.NET та Dapper
- **BLL (Business Logic Layer)** → сервіси з бізнес-логікою, DTO, AutoMapper
- **API Layer** → thin controllers з атрибутною маршрутизацією
- **Unit of Work** pattern для управління транзакціями
- **Serilog** для логування
- **ProblemDetails** для уніфікованої обробки помилок

---

## 🏗 Архітектура проєкту

```
WebApplication.asp.net.c3/
│
├── API/                          # Контролери (thin controllers)
│   ├── CategoriesController.cs
│   ├── BrandsController.cs
│   ├── ProductsController.cs
│   └── Middleware/
│       └── GlobalExceptionHandlerMiddleware.cs
│
├── BLL/                          # Business Logic Layer
│   ├── Interfaces/
│   │   ├── ICategoryService.cs
│   │   ├── IBrandService.cs
│   │   └── IProductService.cs
│   ├── Services/
│   │   ├── CategoryService.cs   # Бізнес-логіка категорій
│   │   ├── BrandService.cs      # Бізнес-логіка брендів
│   │   └── ProductService.cs    # Бізнес-логіка продуктів
│   ├── DTOs/                     # Data Transfer Objects
│   ├── Mapping/                  # AutoMapper профілі
│   ├── Exceptions/               # Доменні винятки
│   └── Validators/               # Бізнес-валідація
│
├── DAL/                          # Data Access Layer
│   ├── Interfaces/
│   │   ├── IRepository.cs       # Базовий репозиторій
│   │   ├── IUnitOfWork.cs       # Unit of Work
│   │   ├── ICategoryRepository.cs
│   │   ├── IBrandRepository.cs
│   │   └── IProductRepository.cs
│   └── Repositories/
│       ├── UnitOfWork.cs        # Управління транзакціями
│       ├── CategoryRepository.cs # Чистий ADO.NET
│       ├── BrandRepository.cs    # ADO.NET + Dapper
│       └── ProductRepository.cs  # Dapper з multi-mapping
│
├── Models/                       # Доменні моделі
│   ├── BaseEntity.cs
│   ├── Category.cs
│   ├── Brand.cs
│   └── Product.cs
│
└── Program.cs                    # Конфігурація DI, middleware
```

---

## 🎯 Ключові особливості реалізації

### 1. **DAL - Data Access Layer**

#### ✅ CategoryRepository - **Чистий ADO.NET**
- Ручне управління підключеннями (`IDbConnection`)
- Параметризовані запити
- Підтримка транзакцій
- Async/await через `NpgsqlCommand`

```csharp
public async Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken)
{
    const string sql = @"SELECT id, name, description... WHERE id = @Id";
    
    using var command = CreateCommand(sql);
    AddParameter(command, "@Id", id);
    using var reader = await ExecuteReaderAsync(command, cancellationToken);
    
    if (await reader.ReadAsync(cancellationToken))
        return MapToCategory(reader);
    
    return null;
}
```

#### ✅ BrandRepository - **ADO.NET + Dapper**
- Використання Dapper для маппінгу результатів
- Async методи (`QueryAsync`, `ExecuteAsync`)
- Параметризовані запити через анонімні об'єкти

```csharp
public async Task<Brand?> GetByIdAsync(int id, CancellationToken cancellationToken)
{
    const string sql = @"SELECT id AS Id, name AS Name... FROM brands WHERE id = @Id";
    
    return await _connection.QueryFirstOrDefaultAsync<Brand>(
        new CommandDefinition(sql, new { Id = id }, _transaction, cancellationToken: cancellationToken));
}
```

#### ✅ ProductRepository - **Dapper з Multi-Mapping**
- Складні запити з JOIN
- Multi-mapping для завантаження зв'язаних сутностей

```csharp
public async Task<Product?> GetWithDetailsAsync(int id, CancellationToken cancellationToken)
{
    const string sql = @"
        SELECT p.*, c.*, b.*
        FROM products p
        LEFT JOIN categories c ON p.category_id = c.id
        LEFT JOIN brands b ON p.brand_id = b.id
        WHERE p.id = @Id";
    
    var products = await _connection.QueryAsync<Product, Category, Brand, Product>(
        new CommandDefinition(sql, new { Id = id }, _transaction, cancellationToken: cancellationToken),
        (product, category, brand) =>
        {
            product.Category = category;
            product.Brand = brand;
            return product;
        },
        splitOn: "Id,Id");
    
    return products.FirstOrDefault();
}
```

#### ✅ Unit of Work - **Управління транзакціями**
```csharp
try
{
    _unitOfWork.BeginTransaction();
    
    var categoryId = await _unitOfWork.Categories.AddAsync(category, cancellationToken);
    await _unitOfWork.Products.AddAsync(product, cancellationToken);
    
    await _unitOfWork.CommitAsync(cancellationToken);
}
catch
{
    _unitOfWork.Rollback();
    throw;
}
```

### 2. **BLL - Business Logic Layer**

#### Сервіси з бізнес-логікою:
- Валідація на рівні бізнес-правил
- Кидання доменних винятків (`NotFoundException`, `BusinessConflictException`)
- Використання UoW для транзакцій

```csharp
public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto, CancellationToken ct)
{
    // Бізнес-валідація
    var existing = await _unitOfWork.Categories.GetByNameAsync(dto.Name, ct);
    if (existing != null)
        throw new BusinessConflictException($"Category '{dto.Name}' already exists.");
    
    // Маппінг DTO → Entity
    var category = _mapper.Map<Category>(dto);
    
    // Транзакція
    _unitOfWork.BeginTransaction();
    var id = await _unitOfWork.Categories.AddAsync(category, ct);
    await _unitOfWork.CommitAsync(ct);
    
    category.Id = id;
    return _mapper.Map<CategoryDto>(category);
}
```

### 3. **API Layer - Thin Controllers**

Контролери **лише делегують** виклики до сервісів:

```csharp
[HttpPost]
[ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status409Conflict)]
public async Task<ActionResult<CategoryDto>> Create(
    [FromBody] CreateCategoryDto dto, 
    CancellationToken cancellationToken)
{
    var category = await _categoryService.CreateCategoryAsync(dto, cancellationToken);
    return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
}
```

### 4. **Обробка помилок - ProblemDetails (RFC 7807)**

Глобальний middleware перехоплює всі винятки і повертає стандартизовані відповіді:

```json
{
  "type": "about:blank",
  "title": "Resource Not Found",
  "status": 404,
  "detail": "Category with id '999' was not found.",
  "instance": "/api/categories/999",
  "traceId": "00-abc123...",
  "timestamp": "2025-11-30T20:51:12Z"
}
```

---

## 🚀 Запуск проєкту

### Передумови:
- .NET 8.0 SDK
- PostgreSQL 14+
- База даних `HardwareProductCatalogDb` (створена раніше)

### Кроки:

1. **Клонуйте репозиторій:**
```bash
git clone https://github.com/evequeee/Hardware-Store-ASP.NET.git
cd Hardware-Store-ASP.NET
```

2. **Налаштуйте Connection String:**

Відредагуйте `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "ProductCatalogDb": "Host=localhost;Port=5432;Database=HardwareProductCatalogDb;Username=postgres;Password=YOUR_PASSWORD"
  }
}
```

3. **Запустіть проєкт:**
```bash
cd WebApplication.asp.net.c3
dotnet run
```

4. **Відкрийте Swagger UI:**
```
http://localhost:5128
```

---

## 📡 Приклади використання API

### 1. Отримати всі категорії (активні)
```bash
GET http://localhost:5128/api/categories?activeOnly=true

# cURL
curl -X GET "http://localhost:5128/api/categories?activeOnly=true"
```

**Відповідь:**
```json
[
  {
    "id": 1,
    "name": "Інструменти",
    "description": "Ручні та електроінструменти",
    "isActive": true,
    "sortOrder": 1
  }
]
```

### 2. Створити нову категорію
```bash
POST http://localhost:5128/api/categories
Content-Type: application/json

{
  "name": "Сантехніка",
  "description": "Труби, змішувачі, ванни",
  "isActive": true,
  "sortOrder": 10
}

# cURL
curl -X POST "http://localhost:5128/api/categories" \
  -H "Content-Type: application/json" \
  -d '{"name":"Сантехніка","description":"Труби, змішувачі","isActive":true,"sortOrder":10}'
```

**Відповідь (201 Created):**
```json
{
  "id": 15,
  "name": "Сантехніка",
  "description": "Труби, змішувачі, ванни",
  "isActive": true,
  "sortOrder": 10
}
```

### 3. Отримати продукт із деталями (Category + Brand)
```bash
GET http://localhost:5128/api/products/5?includeDetails=true

# cURL
curl -X GET "http://localhost:5128/api/products/5?includeDetails=true"
```

**Відповідь:**
```json
{
  "id": 5,
  "name": "Дриль акумуляторний Makita",
  "sku": "DRILL-MAK-001",
  "price": 3500.00,
  "discountPrice": 2999.00,
  "stockQuantity": 15,
  "isAvailable": true,
  "isFeatured": true,
  "category": {
    "id": 1,
    "name": "Інструменти"
  },
  "brand": {
    "id": 3,
    "name": "Makita",
    "country": "Japan"
  }
}
```

### 4. Оновити запас продукту
```bash
PATCH http://localhost:5128/api/products/5/stock
Content-Type: application/json

{
  "productId": 5,
  "quantity": -2
}

# cURL
curl -X PATCH "http://localhost:5128/api/products/5/stock" \
  -H "Content-Type: application/json" \
  -d '{"productId":5,"quantity":-2}'
```

### 5. Пошук брендів
```bash
GET http://localhost:5128/api/brands/search?query=mak

# cURL
curl -X GET "http://localhost:5128/api/brands/search?query=mak"
```

---

## 🧪 Тестування

### Використання Swagger UI:
1. Відкрийте `http://localhost:5128`
2. Розгорніть endpoint (наприклад, `POST /api/categories`)
3. Натисніть **"Try it out"**
4. Заповніть JSON-тіло
5. Натисніть **"Execute"**

### Приклади помилок:

**404 Not Found:**
```json
{
  "title": "Resource Not Found",
  "status": 404,
  "detail": "Brand with id '999' was not found.",
  "instance": "/api/brands/999"
}
```

**409 Conflict (бізнес-правило):**
```json
{
  "title": "Business Rule Violation",
  "status": 409,
  "detail": "Cannot delete category that has products. Remove or reassign products first.",
  "instance": "/api/categories/5"
}
```

---

## 📊 Критерії приймання (виконано)

✅ **DAL:**
- 1 репозиторій на чистому ADO.NET (`CategoryRepository`)
- 2 репозиторії на ADO.NET + Dapper (`BrandRepository`, `ProductRepository`)
- Всі запити параметризовані
- UoW з транзакційним сценарієм
- SQL-помилки логуються, не передаються в API

✅ **BLL:**
- DTO і AutoMapper профілі
- Сервіси з чіткою поверхневою бізнес-логікою
- UoW для транзакцій
- Доменні винятки

✅ **API:**
- Thin controllers
- Атрибутна маршрутизація
- Асинхронність з CancellationToken
- Коректні HTTP-статуси (201, 204, 404, 409)
- ProblemDetails для помилок
- OpenAPI/Swagger опис

---

## 📚 Технології

- **ASP.NET Core 8.0** - Web API framework
- **PostgreSQL** - база даних
- **Npgsql** - PostgreSQL драйвер для ADO.NET
- **Dapper** - мікро-ORM для маппінгу
- **AutoMapper 12** - маппінг DTO ↔ Entity
- **Serilog** - структуроване логування
- **Swashbuckle (Swagger)** - документація API

---

## 👤 Автор

**GitHub:** [evequeee](https://github.com/evequeee)  
**Репозиторій:** [Hardware-Store-ASP.NET](https://github.com/evequeee/Hardware-Store-ASP.NET)

---

## 📝 Ліцензія

Проєкт створено для навчальних цілей (Практичне заняття №2).
