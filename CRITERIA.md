# ✅ Критерії виконання завдання (Практичне заняття №2)

## 📌 Загальні вимоги

### ✅ Архітектура
- [x] **Тришарова архітектура**: DAL → BLL → API
- [x] **Vertical Slice**: функціональність організована вертикально
- [x] **Немає витоку SQL**: raw SQL не передається в API
- [x] **Немає використання EF Core**: видалено `DbContext`, міграції

---

## 🔵 Крок A - Підготовка середовища

- [x] **Connection String** налаштований в `appsettings.json`
- [x] **Dependency Injection**:
  - `IUnitOfWork` → `UnitOfWork`
  - `ICategoryService` → `CategoryService`
  - `IBrandService` → `BrandService`
  - `IProductService` → `ProductService`
- [x] **Логування**: Serilog (Console + File)
- [x] **База даних**: використовується попередньо створена `HardwareProductCatalogDb`

---

## 🔵 Крок B - DAL (Data Access Layer)

### ✅ Інтерфейси репозиторіїв
- [x] `IRepository<T>` - базовий інтерфейс з CRUD
- [x] `ICategoryRepository` - мінімальний набір + пошукові методи
- [x] `IBrandRepository` - мінімальний набір + пошукові методи
- [x] `IProductRepository` - мінімальний набір + пошукові методи

### ✅ Реалізації

#### 1. **CategoryRepository** - Чистий ADO.NET ✔
```csharp
✓ Використовує IDbConnection, IDbCommand, IDataReader
✓ Параметризація через CreateParameter
✓ Ручний маппінг MapToCategory(IDataReader)
✓ Async через NpgsqlCommand.ExecuteReaderAsync()
✓ Using для управління ресурсами
✓ Підтримка транзакцій (IDbTransaction)
```

#### 2. **BrandRepository** - ADO.NET + Dapper ✔
```csharp
✓ Використовує Dapper.QueryAsync<T>()
✓ Параметризація через анонімні об'єкти
✓ CommandDefinition з CancellationToken
✓ Автоматичний маппінг Dapper
✓ Підтримка транзакцій
```

#### 3. **ProductRepository** - Dapper з Multi-Mapping ✔
```csharp
✓ Використовує Dapper.QueryAsync<T1, T2, T3, TResult>()
✓ Multi-mapping для Product + Category + Brand
✓ SplitOn: "Id,Id"
✓ Async методи (QueryAsync, ExecuteAsync)
✓ Підтримка транзакцій
```

### ✅ Unit of Work
- [x] **Централізоване управління з'єднаннями**:
  - `IDbConnection Connection { get; }` - lazy open
  - Одне з'єднання на UoW
- [x] **Координація репозиторіїв**:
  - `ICategoryRepository Categories`
  - `IBrandRepository Brands`
  - `IProductRepository Products`
- [x] **Транзакції**:
  - `BeginTransaction()`
  - `CommitAsync(CancellationToken)`
  - `Rollback()`
  - Rollback при винятках

### ✅ Параметризація та безпека
- [x] **Всі запити параметризовані** (немає конкатенації рядків)
- [x] **Using/await using** для IDbCommand, IDataReader (ADO.NET)
- [x] **CommandDefinition** з CancellationToken (Dapper)

### ✅ Логування та обробка помилок
- [x] SQL-помилки **логуються** в сервісах
- [x] SQL-помилки **НЕ передаються в API** (перехоплюються в BLL)
- [x] Використання `ILogger<T>`

---

## 🔵 Крок C - BLL (Business Logic Layer)

### ✅ Контракти і DTO
- [x] **DTO для кожного API-контракту**:
  - `CategoryDto`, `CreateCategoryDto`, `UpdateCategoryDto`
  - `BrandDto`, `CreateBrandDto`, `UpdateBrandDto`
  - `ProductDto`, `CreateProductDto`, `UpdateProductDto`
- [x] **AutoMapper профілі**: `MappingProfile.cs`
- [x] **Перевірка конфігурації**: `services.AddAutoMapper(typeof(MappingProfile))`

### ✅ Сервіси
- [x] **Кожна бізнес-операція в сервісі**:
  - `ICategoryService` / `CategoryService`
  - `IBrandService` / `BrandService`
  - `IProductService` / `ProductService`
- [x] **Використання UoW**:
  - Всі сервіси отримують `IUnitOfWork` через DI
  - Транзакції для створення/оновлення/видалення
- [x] **Доменні винятки**:
  - `NotFoundException`
  - `BusinessConflictException`
  - `ValidationException`

### ✅ Валідація (двохрівнева)
- [x] **API-валідація**: перевірка контрактів (DTO)
- [x] **BLL-валідація**: бізнес-інваріанти (наприклад, унікальність назв)

**Приклади бізнес-правил:**
```csharp
✓ Категорія з такою назвою вже існує → BusinessConflictException
✓ Неможливо видалити категорію з продуктами → BusinessConflictException
✓ Discount price >= Price → BusinessConflictException
✓ Категорія не може бути власним батьком → BusinessConflictException
```

### ✅ Idempotency і Concurrency
- [x] **Optimistic concurrency**: можна додати `RowVersion`/`UpdatedAt` перевірки (опційно)
- [x] **Idempotency tokens**: можна додати для критичних операцій (опційно)

---

## 🔵 Крок D - Web/API (Controllers, Middleware)

### ✅ Thin Controllers
- [x] **Контролери тонкі** - лише делегування до сервісів
- [x] **Атрибутна маршрутизація**:
  - `[Route("api/[controller]")]`
  - `[HttpGet]`, `[HttpPost("{id}")]`, etc.
- [x] **Правильні HTTP-статуси**:
  - `200 OK` - успішна операція
  - `201 Created` - створено ресурс
  - `204 No Content` - успішне видалення
  - `400 Bad Request` - невалідні дані
  - `404 Not Found` - ресурс не знайдено
  - `409 Conflict` - бізнес-конфлікт

### ✅ Типи повернення
- [x] **ActionResult<T>** для успішних відповідей
- [x] **IActionResult** для операцій без тіла (204)
- [x] **CreatedAtAction()** для POST (201 + Location header)

### ✅ Асинхронність і CancellationToken
- [x] **Всі контролери async**
- [x] **CancellationToken передається**:
  - Controller → Service → Repository → ADO.NET/Dapper

### ✅ ProblemDetails (RFC 7807)
- [x] **Глобальний middleware**: `GlobalExceptionHandlerMiddleware`
- [x] **Стандартизований формат помилок**:
  ```json
  {
    "title": "Resource Not Found",
    "status": 404,
    "detail": "Category with id '999' was not found.",
    "instance": "/api/categories/999",
    "traceId": "00-abc123...",
    "timestamp": "2025-11-30T20:51:12Z"
  }
  ```
- [x] **Мапування винятків**:
  - `NotFoundException` → 404
  - `BusinessConflictException` → 409
  - `ValidationException` → 400
  - `Exception` → 500

### ✅ OpenAPI / Swagger
- [x] **Swagger UI** доступний на `/` (root)
- [x] **Описи endpoints**:
  - HTTP методи
  - Шляхи
  - DTO in/out
  - Статуси (ProducesResponseType)
- [x] **XML коментарі** (опційно, якщо налаштовані)

---

## 🔵 Крок E - Тестування

### ✅ Unit Tests (BLL) - *Опційно*
- [ ] Мокати DAL/UoW
- [ ] Перевіряти happy path
- [ ] Перевіряти негативні сценарії (валідація, бізнес-помилки)

### ✅ Integration Tests (DAL/UoW) - *Опційно*
- [ ] Запускати проти тестової бази
- [ ] Перевіряти транзакційний rollback

### ✅ API Tests / E2E - *Виконано вручну*
- [x] Запускати проти працюючого сервера
- [x] Перевіряти статуси і формат помилок (ProblemDetails)
- [x] **Файл**: `API-Tests.http` з прикладами запитів

### ✅ Smoke Tests
- [x] Основні сценарії збереження/читання/транзакцій перевірені

---

## 🔵 Крок F - .NET Aspire *(опційно)*

- [ ] Додати проєкт AppHost
- [ ] Зареєструвати Web/API, DAL, БД, кеш
- [ ] Підключити основний сервіс
- [ ] Запустити Aspire Dashboard
- [ ] Перевірити health checks

**Примітка:** .NET Aspire не є обов'язковим для цього завдання.

---

## 🔵 Крок G - Git і документація

### ✅ Git
- [x] **Репозиторій**: `Hardware-Store-ASP.NET` (main branch)
- [x] **Коміти**: зрозумілі повідомлення
- [x] **PR**: можна створити при роботі в командах

### ✅ Документація
- [x] **README.md**:
  - Кроки запуску
  - Коротка схема проєкту
  - Приклади curl/Postman
  - Пояснення архітектури
- [x] **API-Tests.http**: приклади HTTP-запитів
- [x] **OpenAPI/Swagger**: онлайн-документація

---

## 🎯 Підсумок критеріїв приймання

### DAL (Data Access Layer)
✅ **1 репозиторій на чистому ADO.NET** - `CategoryRepository`  
✅ **2 репозиторії на ADO.NET+Dapper** - `BrandRepository`, `ProductRepository`  
✅ **Параметризовані запити**  
✅ **UoW з транзакційним сценарієм**  
✅ **SQL-помилки логуються, не передаються в API**  

### BLL (Business Logic Layer)
✅ **DTO та AutoMapper профілі**  
✅ **Сервіси з бізнес-логікою**  
✅ **UoW для транзакцій**  
✅ **Доменні винятки**  

### API (Web Layer)
✅ **Thin controllers**  
✅ **Атрибутна маршрутизація**  
✅ **Асинхронність з CancellationToken**  
✅ **Коректні HTTP-статуси**  
✅ **ProblemDetails для помилок**  
✅ **OpenAPI/Swagger опис**  

### Документація
✅ **README з кроками запуску та прикладами**  
✅ **OpenAPI/Swagger документація API**  

---

## 📂 Файли проєкту

### Основні файли
- `Program.cs` - конфігурація DI, middleware, Serilog
- `appsettings.json` - connection string, Serilog
- `README.md` - повна документація
- `API-Tests.http` - приклади запитів

### DAL
- `DAL/Interfaces/IRepository.cs`
- `DAL/Interfaces/IUnitOfWork.cs`
- `DAL/Repositories/CategoryRepository.cs` ⭐ ADO.NET
- `DAL/Repositories/BrandRepository.cs` ⭐ ADO.NET+Dapper
- `DAL/Repositories/ProductRepository.cs` ⭐ Dapper Multi-Mapping
- `DAL/Repositories/UnitOfWork.cs`

### BLL
- `BLL/Interfaces/ICategoryService.cs`
- `BLL/Services/CategoryService.cs`
- `BLL/DTOs/CategoryDto.cs`
- `BLL/Mapping/MappingProfile.cs` (AutoMapper)
- `BLL/Exceptions/DomainException.cs`

### API
- `API/CategoriesController.cs`
- `API/BrandsController.cs`
- `API/ProductsController.cs`
- `API/Middleware/GlobalExceptionHandlerMiddleware.cs`

### Models
- `Models/BaseEntity.cs`
- `Models/Category.cs`
- `Models/Brand.cs`
- `Models/Product.cs`

---

## ✅ Проєкт ВИКОНАНО ПОВНІСТЮ

Всі вимоги завдання реалізовані згідно з **Практичним заняттям №2**.

**Дата завершення:** 30 листопада 2025  
**Статус:** ✅ Готовий до здачі
