# Hardware Store API - каталог комп'ютерних комплектуючих

API для магазину комп'ютерних комплектуючих. Зроблено на ASP.NET Core 8.0 з ADO.NET та Dapper (Entity Framework не використовується).

## Що тут є

Проєкт для **Лабораторної роботи №2**. Основні штуки:

- Тришарова архітектура (DAL → BLL → API)
- Repository pattern з ADO.NET та Dapper
- Unit of Work для транзакцій
- AutoMapper для маппінгу між моделями та DTO
- Serilog для логів
- Middleware для обробки помилок

---

## Структура проєкту

```
WebApplication.asp.net.c3/
│
├── API/                          # Контролери
│   ├── CategoriesController.cs
│   ├── BrandsController.cs
│   ├── ProductsController.cs
│   └── Middleware/               # для обробки помилок
│
├── BLL/                          # Бізнес-логіка
│   ├── Services/                 # тут вся логіка
│   ├── DTOs/                     # об'єкти для передачі даних
│   ├── Mapping/                  # AutoMapper profiles
│   └── Exceptions/               # свої винятки
│
├── DAL/                          # Робота з БД
│   ├── Repositories/             # CategoryRepo (ADO.NET)
│   │                             # BrandRepo (ADO.NET + Dapper)
│   │                             # ProductRepo (Dapper)
│   └── Interfaces/               # інтерфейси репозиторіїв
│
└── Models/                       # класи моделей
```

---

## Як це працює

### DAL - робота з базою

**CategoryRepository** - написано на чистому ADO.NET
```csharp
// приклад методу з ADO.NET
public async Task<Category?> GetByIdAsync(int id)
{
    const string sql = "SELECT * FROM categories WHERE id = @Id";
    using var command = CreateCommand(sql);
    AddParameter(command, "@Id", id);
    
    using var reader = await ExecuteReaderAsync(command);
    if (await reader.ReadAsync())
        return MapToCategory(reader);
    
    return null;
}
```

**BrandRepository** - ADO.NET + Dapper (трошки простіше)
```csharp
public async Task<Brand?> GetByIdAsync(int id)
{
    const string sql = "SELECT * FROM brands WHERE id = @Id";
    return await _connection.QueryFirstOrDefaultAsync<Brand>(
        new CommandDefinition(sql, new { Id = id }, _transaction));
}
```

**ProductRepository** - повний Dapper з JOIN-ами
```csharp
// тут складніше - завантажуємо продукт разом з категорією і брендом
public async Task<Product?> GetWithDetailsAsync(int id)
{
    const string sql = @"
        SELECT p.*, c.*, b.*
        FROM products p
        LEFT JOIN categories c ON p.category_id = c.id
        LEFT JOIN brands b ON p.brand_id = b.id
        WHERE p.id = @Id";
    
    var products = await _connection.QueryAsync<Product, Category, Brand, Product>(
        sql, 
        (product, category, brand) =>
        {
            product.Category = category;
            product.Brand = brand;
            return product;
        },
        new { Id = id },
        splitOn: "Id,Id");
    
    return products.FirstOrDefault();
}
```

**Unit of Work** - щоб все в одній транзакції
```csharp
try
{
    _unitOfWork.BeginTransaction();
    
    var id = await _unitOfWork.Categories.AddAsync(category);
    await _unitOfWork.Products.AddAsync(product);
    
    await _unitOfWork.CommitAsync();
}
catch
{
    _unitOfWork.Rollback();
    throw;
}
```

### BLL - бізнес-логіка

Тут перевіряємо правила бізнесу, працюємо з DTO
```csharp
public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto)
{
    // перевіряємо чи не існує вже така категорія
    var existing = await _unitOfWork.Categories.GetByNameAsync(dto.Name);
    if (existing != null)
        throw new BusinessConflictException($"Категорія '{dto.Name}' вже є");
    
    var category = _mapper.Map<Category>(dto);
    
    _unitOfWork.BeginTransaction();
    var id = await _unitOfWork.Categories.AddAsync(category);
    await _unitOfWork.CommitAsync();
    
    category.Id = id;
    return _mapper.Map<CategoryDto>(category);
}
```

### API - контролери

Просто викликають сервіси, нічого складного
```csharp
[HttpPost]
public async Task<ActionResult<CategoryDto>> Create([FromBody] CreateCategoryDto dto)
{
    var category = await _categoryService.CreateCategoryAsync(dto);
    return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
}
```

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

4. Відкрий Swagger - `http://localhost:5128`

---

## Як тестувати API

### Приклади через cURL

**Всі категорії:**
```bash
curl -X GET "http://localhost:5128/api/categories?activeOnly=true"
```

**Створити категорію:**
```bash
curl -X POST "http://localhost:5128/api/categories" \
  -H "Content-Type: application/json" \
  -d '{"name":"Сантехніка","description":"Труби та інше","isActive":true}'
```

**Продукт із деталями:**
```bash
curl -X GET "http://localhost:5128/api/products/5?includeDetails=true"
```

**Оновити залишок:**
```bash
curl -X PATCH "http://localhost:5128/api/products/5/stock" \
  -H "Content-Type: application/json" \
  -d '{"quantity": -2}'
```

### Відповіді з API

**Список категорій:**
```json
[
  {
    "id": 1,
    "name": "Інструменти",
    "description": "Ручні та електроінструменти",
    "isActive": true
  }
]
```

**Продукт:**
```json
{
  "id": 5,
  "name": "Дриль Makita",
  "price": 3500.00,
  "stockQuantity": 15,
  "category": { "id": 1, "name": "Інструменти" },
  "brand": { "id": 3, "name": "Makita", "country": "Japan" }
}
```

### Помилки

**404 якщо не знайдено:**
```json
{
  "title": "Resource Not Found",
  "status": 404,
  "detail": "Brand with id '999' was not found."
}
```

**409 якщо порушення правил:**
```json
{
  "title": "Business Rule Violation",
  "status": 409,
  "detail": "Cannot delete category that has products."
}
  "instance": "/api/categories/5"
}
```

---

## Що було зроблено

**DAL (робота з БД):**
- CategoryRepository - чистий ADO.NET
- BrandRepository - ADO.NET + Dapper
- ProductRepository - Dapper з JOIN-ами
- Unit of Work для транзакцій
- Всі запити параметризовані

**BLL (бізнес-логіка):**
- Сервіси з перевіркою правил
- DTO та AutoMapper
- Свої винятки для помилок

**API:**
- Контролери просто викликають сервіси
- Асинхронність
- Правильні HTTP коди (201, 404, 409, тощо)
- Swagger для тестування

---

## Технології

- ASP.NET Core 8.0
- PostgreSQL + Npgsql
- Dapper
- AutoMapper
- Serilog
- Swagger

---

## Автор

GitHub: [evequeee](https://github.com/evequeee)

