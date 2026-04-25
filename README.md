# Catalog Service

A production-ready **REST API** built with **.NET 10** for managing product categories and sub-categories. Designed following clean architecture principles with the **Repository → Service → Controller** pattern, the API includes JWT authentication, cursor-based pagination, distributed caching, rate limiting, and comprehensive observability — all backed by **PostgreSQL**.

The project also ships with a complete **unit & integration testing** suite (xUnit + Testcontainers) and a **load testing** suite (Grafana k6) for performance benchmarking.

---

## Table of Contents

- [Features](#features)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
  - [1. Clone the Repository](#1-clone-the-repository)
  - [2. Configure Environment Variables](#2-configure-environment-variables)
  - [3. Set Up the Database](#3-set-up-the-database)
  - [4. Run the API](#4-run-the-api)
- [API Endpoints](#api-endpoints)
- [Project Creation Reference](#project-creation-reference)
  - [Creating the Main Project](#creating-the-main-project)
  - [Installing NuGet Dependencies](#installing-nuget-dependencies)
- [Architecture & Key Components](#architecture--key-components)
  - [Middleware Pipeline](#middleware-pipeline)
  - [Standardized API Response](#standardized-api-response)
  - [Authentication & Authorization](#authentication--authorization)
  - [Rate Limiting](#rate-limiting)
  - [Caching Strategy](#caching-strategy)
  - [Logging & Observability](#logging--observability)
- [Testing](#testing)
  - [Test Project Setup](#test-project-setup)
  - [Installing Test Dependencies](#installing-test-dependencies)
  - [Running Unit Tests](#running-unit-tests)
  - [Running Integration Tests](#running-integration-tests)
  - [Code Coverage Report](#code-coverage-report)
- [Load Testing (Grafana k6)](#load-testing-grafana-k6)
  - [k6 Installation](#k6-installation)
  - [Load Test Project Structure](#load-test-project-structure)
  - [Generate JWT Token](#generate-jwt-token)
  - [Running Load Tests](#running-load-tests)
  - [Test Profiles](#test-profiles)
  - [Exporting Results](#exporting-results)
- [Docker](#docker)
- [License](#license)

---

## Features

- **Full CRUD** — Create, Read, Update, Delete for Categories & Sub-Categories
- **Cursor-Based Pagination** — Efficient keyset pagination with `page`, `limit`, `sort`, and `cursor` support
- **Filtering & Sorting** — Query by `name`, `code`, `categoryId`; sort by any field (asc/desc)
- **JWT Authentication** — Secure mutating endpoints (POST/PUT/DELETE) with Bearer tokens
- **Rate Limiting** — Token bucket algorithm with Standard (100 req/min) and Premium (1000 req/min) tiers
- **Distributed Caching** — Upstash Redis (REST API) with automatic fallback to in-memory cache
- **Output Caching** — Server-side response caching for public GET endpoints
- **Response Compression** — Brotli + Gzip compression for all responses
- **Structured Logging** — Serilog with async file + console sinks and per-request context
- **Health Checks** — `/health` endpoint with PostgreSQL connectivity check
- **Request Timeouts** — 30-second global request timeout policy
- **Standardized Responses** — RFC 7807 error format with consistent success/error envelopes
- **Security Headers** — `X-Content-Type-Options`, `X-Frame-Options`, `X-XSS-Protection`, `Referrer-Policy`
- **Docker Support** — Multi-stage Dockerfile included
- **OpenAPI** — Auto-generated API documentation in development mode

---

## Technology Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| **Runtime** | .NET | 10.0 |
| **Language** | C# | 14 |
| **Database** | PostgreSQL | 15+ |
| **ORM** | Entity Framework Core (Npgsql) | 10.0.1 |
| **Authentication** | JWT Bearer (HS256) | 10.0.5 |
| **Caching** | Upstash Redis REST API / In-Memory | — |
| **Logging** | Serilog (Async + Console + File) | 9.0.0 |
| **Health Checks** | AspNetCore.HealthChecks.NpgSql | 9.0.0 |
| **API Docs** | Microsoft.AspNetCore.OpenApi | 10.0.4 |
| **Containerization** | Docker (multi-stage build) | — |
| **Unit Testing** | xUnit + Moq + EF Core InMemory | 2.9.3 |
| **Integration Testing** | Testcontainers.PostgreSql + WebApplicationFactory | 4.11.0 |
| **Code Coverage** | Coverlet (MSBuild + Collector) | 8.0.1 |
| **Load Testing** | Grafana k6 | 1.x |

---

## Project Structure

```
catalog-service/
├── Constants/
│   └── ErrorTypes.cs                # Standardized RFC 7807 error type URIs
├── Controllers/
│   ├── BaseApiController.cs         # Shared response helpers (ApiOk, ApiCreated, ApiNotFound)
│   ├── CategoriesController.cs      # /api/v1/categories endpoints
│   └── SubCategoriesController.cs   # /api/v1/sub-categories endpoints
├── Data/
│   ├── AppDbContext.cs              # EF Core DbContext
│   ├── Configurations/             # Fluent API entity configurations
│   ├── IUnitOfWork.cs              # Unit of Work contract
│   └── UnitOfWork.cs               # UoW implementation (transaction management)
├── Dtos/
│   ├── CategoryDtos.cs             # Create/Update request DTOs for categories
│   └── SubCategoryDtos.cs          # Create/Update request DTOs for sub-categories
├── Entities/
│   ├── Category.cs                 # Category domain entity
│   └── SubCategory.cs              # SubCategory domain entity (FK → Category)
├── Helpers/
│   ├── CursorHelper.cs             # Base64 cursor encoding/decoding
│   └── SortHelper.cs               # Dynamic sort expression builder
├── Middleware/
│   ├── GlobalExceptionMiddleware.cs # Catches unhandled exceptions → RFC 7807
│   ├── RequestIdMiddleware.cs       # Injects X-Request-Id header
│   └── RequestLoggingMiddleware.cs  # Logs method, path, status, duration
├── Migrations/
│   ├── 000_create_categories.sql    # Categories table DDL
│   └── 001_create_sub_categories.sql# Sub-categories table DDL
├── Models/
│   ├── ApiResponse.cs              # Success/Error response envelopes
│   ├── PagedResult.cs              # Generic paged result wrapper
│   └── QueryParameters.cs          # Pagination, filter, sort query models
├── Properties/
│   └── launchSettings.json         # HTTP/HTTPS/Docker launch profiles
├── Repositories/
│   ├── ICategoryRepository.cs      # Category repository contract
│   ├── CategoryRepository.cs       # Category repository (EF Core + Distributed Cache)
│   ├── ISubCategoryRepository.cs   # SubCategory repository contract
│   └── SubCategoryRepository.cs    # SubCategory repository (EF Core + Distributed Cache)
├── Services/
│   ├── ICategoryService.cs         # Category service contract
│   ├── CategoryService.cs          # Category business logic
│   ├── ISubCategoryService.cs      # SubCategory service contract
│   ├── SubCategoryService.cs       # SubCategory business logic
│   └── UpstashDistributedCache.cs  # IDistributedCache implementation for Upstash REST API
├── Tests/                          # Unit & Integration test project
│   ├── catalog-service.Tests.csproj
│   ├── IntegrationTestFactory.cs   # WebApplicationFactory with Testcontainers (PostgreSQL)
│   ├── CategoriesControllerTests.cs
│   ├── CategoriesIntegrationTests.cs
│   ├── CategoryRepositoryTests.cs
│   ├── CategoryServiceTests.cs
│   ├── SubCategoriesControllerTests.cs
│   ├── SubCategoriesIntegrationTests.cs
│   ├── SubCategoryRepositoryTests.cs
│   ├── SubCategoryServiceTests.cs
│   └── ... (more test files)
├── LoadTests/                      # Grafana k6 load test scripts
│   ├── config.js                   # Shared config, test profiles, headers
│   ├── categories.js               # Categories API load test
│   ├── sub-categories.js           # SubCategories API load test
│   ├── full-flow.js                # End-to-end user flow load test
│   ├── generate-token.js           # Node.js JWT token generator
│   └── README.md                   # Detailed load testing documentation
├── Program.cs                      # Application entry point & DI configuration
├── catalog-service.csproj          # Main project file
├── catalog-service.slnx            # Solution file
├── catalog-service.http            # REST Client HTTP requests
├── Dockerfile                      # Multi-stage Docker build
├── appsettings.json                # Production configuration
├── appsettings.Development.json    # Development configuration overrides
├── .env.example                    # Environment variable template
└── .env                            # Local secrets (git-ignored)
```

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL 15+](https://www.postgresql.org/download/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (required for integration tests — Testcontainers)
- [Node.js](https://nodejs.org/) (optional — for load test JWT token generation only)
- [Grafana k6](https://k6.io/) (optional — for load testing only)

---

## Getting Started

### 1. Clone the Repository

```powershell
git clone https://github.com/TechMindrsg/catalog-service.git
cd catalog-service
```

### 2. Configure Environment Variables

Copy the example file and fill in your values:

```powershell
cp .env.example .env
```

Edit `.env` with your actual credentials:

```env
POSTGRES_DB_URL=Host=localhost;Port=5432;Database=catalogdb;Username=postgres;Password=secret
JWT_KEY=your-jwt-signing-key-at-least-32-characters-long
UPSTASH_REDIS_REST_URL=https://your-endpoint.upstash.io
UPSTASH_REDIS_REST_TOKEN=your-upstash-rest-token
```

> **Note:** If Upstash Redis is not configured, the API automatically falls back to in-memory caching.

### 3. Set Up the Database

Run the SQL migration scripts against your PostgreSQL database in order:

```powershell
# Using psql
psql -h localhost -U postgres -d catalogdb -f Migrations/000_create_categories.sql
psql -h localhost -U postgres -d catalogdb -f Migrations/001_create_sub_categories.sql
```

### 4. Run the API

```powershell
dotnet run
```

The API starts on:
- **HTTP:** `http://localhost:5101`
- **HTTPS:** `https://localhost:7268`

Verify it's running:

```powershell
curl http://localhost:5101/health
```

---

## API Endpoints

### Categories (`/api/v1/categories`)

| Method | Endpoint | Auth | Description |
|--------|---------|------|-------------|
| `GET` | `/api/v1/categories` | No | List all categories (paginated, filterable, sortable) |
| `GET` | `/api/v1/categories/{id}` | No | Get a category by ID |
| `POST` | `/api/v1/categories` | Yes | Create a new category |
| `PUT` | `/api/v1/categories/{id}` | Yes | Update an existing category |
| `DELETE` | `/api/v1/categories/{id}` | Yes | Delete a category |

### Sub-Categories (`/api/v1/sub-categories`)

| Method | Endpoint | Auth | Description |
|--------|---------|------|-------------|
| `GET` | `/api/v1/sub-categories` | No | List all sub-categories (paginated, filterable, sortable) |
| `GET` | `/api/v1/sub-categories/by-category/{categoryId}` | No | List sub-categories by category |
| `GET` | `/api/v1/sub-categories/{id}` | No | Get a sub-category by ID |
| `POST` | `/api/v1/sub-categories` | Yes | Create a new sub-category |
| `PUT` | `/api/v1/sub-categories/{id}` | Yes | Update an existing sub-category |
| `DELETE` | `/api/v1/sub-categories/{id}` | Yes | Delete a sub-category |

### Other

| Method | Endpoint | Description |
|--------|---------|-------------|
| `GET` | `/health` | Health check (PostgreSQL connectivity) |
| `GET` | `/openapi/v1.json` | OpenAPI spec (development only) |

### Query Parameters

```
GET /api/v1/categories?page=1&limit=10&sort=name:asc&name=Electronics&code=ELEC
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `page` | int | Page number (default: 1) |
| `limit` | int | Items per page (default: 20, max: 100) |
| `sort` | string | Sort field and direction (e.g., `name:asc`, `createdAt:desc`) |
| `name` | string | Filter by name (partial match) |
| `code` | string | Filter by code (partial match) |
| `cursor` | string | Cursor for keyset pagination |

---

## Project Creation Reference

### Creating the Main Project

```powershell
# Create a new .NET Web API project
dotnet new webapi -n catalog-service --no-openapi

# Navigate to the project directory
cd catalog-service
```

### Installing NuGet Dependencies

```powershell
# PostgreSQL + Entity Framework Core
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

# JWT Authentication
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer

# Structured Logging
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Async

# Health Checks
dotnet add package AspNetCore.HealthChecks.NpgSql

# OpenAPI Documentation
dotnet add package Microsoft.AspNetCore.OpenApi

# Validation Annotations
dotnet add package System.ComponentModel.Annotations

# Docker Tooling (Visual Studio)
dotnet add package Microsoft.VisualStudio.Azure.Containers.Tools.Targets
```

---

## Architecture & Key Components

### Middleware Pipeline

Requests flow through the middleware in this exact order:

```
Request → RequestIdMiddleware → GlobalExceptionMiddleware → ResponseCompression
        → RequestLoggingMiddleware → Security Headers → HTTPS Redirection
        → CORS → RateLimiter → RequestTimeouts → OutputCache
        → Authentication → Authorization → Controller
```

| Middleware | Purpose |
|-----------|---------|
| **RequestIdMiddleware** | Generates a unique `X-Request-Id` for every request (used for log correlation) |
| **GlobalExceptionMiddleware** | Catches all unhandled exceptions and returns RFC 7807 error responses |
| **RequestLoggingMiddleware** | Logs HTTP method, path, status code, and response duration |

### Standardized API Response

All responses follow a consistent envelope format:

**Success Response:**
```json
{
  "success": true,
  "data": { ... },
  "meta": {
    "page": 1,
    "limit": 20,
    "total": 42,
    "cursor": "eyJpZCI6MjB9",
    "hasMore": true
  },
  "timestamp": "2026-04-12T07:30:00.000Z",
  "requestId": "0HN8..."
}
```

**Error Response (RFC 7807):**
```json
{
  "success": false,
  "error": {
    "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
    "title": "Not Found",
    "status": 404,
    "detail": "Category with id 999 was not found.",
    "instance": "/api/v1/categories/999"
  },
  "timestamp": "2026-04-12T07:30:00.000Z",
  "requestId": "0HN8..."
}
```

### Authentication & Authorization

- **Algorithm:** HMAC-SHA256 (HS256)
- **Public Endpoints:** All `GET` routes are `[AllowAnonymous]`
- **Protected Endpoints:** `POST`, `PUT`, `DELETE` require a valid `Authorization: Bearer <token>` header
- **Policies:** `AdminOnly` (role-based), `ReadAccess` (any authenticated user)
- JWT settings are configured in `appsettings.json` under the `Jwt` section

### Rate Limiting

Token bucket algorithm with two tiers:

| Tier | Token Limit | Replenishment | Use Case |
|------|------------|---------------|----------|
| **Standard** | 100 req/min | 100 tokens/min | Applied to all controller endpoints |
| **Premium** | 1000 req/min | 1000 tokens/min | Available for premium clients |

Rate limit violations return `429 Too Many Requests` with an RFC 7807 response body.

### Caching Strategy

**Two-Layer Caching:**

1. **Distributed Cache (Upstash Redis)** — Repository-level cache for database query results. Configurable TTL per operation (`GetAll`: 60s, `GetById`: 120s). Falls back to in-memory if Redis is not configured.
2. **Output Cache** — Server-side HTTP response caching for public `GET` endpoints (60s TTL, tagged `public`).

### Logging & Observability

- **Serilog** with async console + rolling file sinks
- Per-request `X-Request-Id` correlation
- Request/response logging (method, path, status, duration)
- Log files: `Logs/log-YYYYMMDD.txt` (30-day retention, 10 MB max per file)

---

## Testing

The project includes a comprehensive testing suite located in the `Tests/` directory, covering unit tests, integration tests, and code coverage reporting.

### Test Project Setup

To create a test project from scratch:

```powershell
# Create xUnit test project inside Tests/ directory
dotnet new xunit -n catalog-service.Tests -o Tests

# Add reference to the main project
dotnet add Tests/catalog-service.Tests.csproj reference catalog-service.csproj
```

### Installing Test Dependencies

```powershell
cd Tests

# Mocking framework
dotnet add package Moq

# EF Core In-Memory provider (for unit tests)
dotnet add package Microsoft.EntityFrameworkCore.InMemory

# Integration testing with WebApplicationFactory
dotnet add package Microsoft.AspNetCore.Mvc.Testing

# Testcontainers for real PostgreSQL in integration tests
dotnet add package Testcontainers.PostgreSql

# JWT token generation for auth tests
dotnet add package System.IdentityModel.Tokens.Jwt

# Code coverage
dotnet add package coverlet.msbuild
dotnet add package coverlet.collector

cd ..
```

### Test Coverage Summary

| Test Type | Files | What It Tests |
|-----------|-------|---------------|
| **Unit — Controller** | `CategoriesControllerTests.cs`, `SubCategoriesControllerTests.cs`, `BaseApiControllerTests.cs` | Controller action logic, response codes, response format |
| **Unit — Service** | `CategoryServiceTests.cs`, `SubCategoryServiceTests.cs` | Business logic, DTO mapping, validation |
| **Unit — Repository** | `CategoryRepositoryTests.cs`, `SubCategoryRepositoryTests.cs` | EF Core queries with InMemory provider |
| **Unit — Data** | `DataTests.cs`, `UnitOfWorkTests.cs` | DbContext config, UoW transaction handling |
| **Unit — Helpers** | `SortHelperTests.cs`, `CursorHelperTests.cs` | Sorting expressions, cursor encoding/decoding |
| **Unit — Middleware** | `MiddlewareTests.cs` | Exception handling, request ID, request logging |
| **Unit — Other** | `DtoTests.cs`, `QueryParametersTests.cs`, `ErrorTypesTests.cs`, `UpstashDistributedCacheTests.cs` | DTOs, query models, constants, cache |
| **Integration** | `CategoriesIntegrationTests.cs`, `SubCategoriesIntegrationTests.cs` | Full HTTP pipeline with real PostgreSQL (Testcontainers) |

### Running Unit Tests

```powershell
# Run all tests
dotnet test Tests/catalog-service.Tests.csproj

# Run with detailed output
dotnet test Tests/catalog-service.Tests.csproj --logger "console;verbosity=detailed"

# Run a specific test class
dotnet test Tests/catalog-service.Tests.csproj --filter "FullyQualifiedName~CategoryServiceTests"

# Run only unit tests (exclude integration tests)
dotnet test Tests/catalog-service.Tests.csproj --filter "FullyQualifiedName~Tests&FullyQualifiedName!~Integration"
```

### Running Integration Tests

> **⚠️ Prerequisite:** Docker Desktop must be running. Integration tests use **Testcontainers** to spin up a real PostgreSQL container automatically.

```powershell
# Run only integration tests
dotnet test Tests/catalog-service.Tests.csproj --filter "FullyQualifiedName~IntegrationTests"
```

Integration tests use `WebApplicationFactory<Program>` together with a Testcontainers-backed PostgreSQL instance. The `IntegrationTestFactory` class:
- Spins up a fresh PostgreSQL container per test run
- Applies EF Core migrations automatically
- Replaces the app's `DbContext` with one pointed at the container
- Configures JWT authentication for testing protected endpoints
- Tears down the container when tests complete

### Code Coverage Report

Generate an HTML code coverage report:

```powershell
# Run tests with coverage collection (Coverlet MSBuild)
dotnet test Tests/catalog-service.Tests.csproj `
  /p:CollectCoverage=true `
  /p:CoverletOutputFormat=cobertura `
  /p:CoverletOutput=./coverage/

# Generate HTML report (requires ReportGenerator global tool)
dotnet tool install -g dotnet-reportgenerator-globaltool

reportgenerator `
  -reports:Tests/coverage/coverage.cobertura.xml `
  -targetdir:Tests/coverage/report `
  -reporttypes:Html

# Open the report in browser
start Tests/coverage/report/index.html
```

**What's excluded from coverage** (configured in `.csproj`):
- Auto-generated code (`GeneratedCodeAttribute`, `CompilerGeneratedAttribute`)
- OpenAPI generated classes
- `Program.cs` (application bootstrap)
- `SubCategoryRepository.cs` (raw SQL, tested via integration tests)

---

## Load Testing (Grafana k6)

The `LoadTests/` directory contains a complete load testing suite using **[Grafana k6](https://k6.io/)** — a modern, developer-friendly load testing tool.

### k6 Installation

**Windows (recommended):**
```powershell
winget install GrafanaLabs.k6
```

**macOS:**
```bash
brew install k6
```

**Verify:**
```powershell
k6 version
```

### Load Test Project Structure

```
LoadTests/
├── config.js            # Shared config: base URL, headers, 5 test profiles
├── categories.js        # Categories API load test (list, filter, getById, CRUD)
├── sub-categories.js    # SubCategories API load test (list, by-category, CRUD)
├── full-flow.js         # Realistic user flow (health → browse → paginate → CRUD)
├── generate-token.js    # Node.js helper to generate JWT tokens
└── README.md            # Detailed load testing documentation
```

### Generate JWT Token

Authenticated endpoints (POST, PUT, DELETE) require a JWT token:

```powershell
# Generate token (valid for 24 hours)
node LoadTests/generate-token.js

# Custom expiry (e.g., 48 hours)
node LoadTests/generate-token.js 48
```

### Running Load Tests

```powershell
# 1. Start the API first
dotnet run

# 2. Smoke test (read-only, no JWT needed)
k6 run --env PROFILE=smoke LoadTests/categories.js

# 3. Average load test
k6 run --env PROFILE=average LoadTests/categories.js

# 4. Full flow with CRUD (requires JWT)
k6 run --env PROFILE=average --env JWT_TOKEN="paste-token-here" LoadTests/full-flow.js

# 5. Stress test
k6 run --env PROFILE=stress LoadTests/categories.js

# 6. Spike test
k6 run --env PROFILE=spike LoadTests/sub-categories.js

# 7. Soak test (long-running — 12 min)
k6 run --env PROFILE=soak LoadTests/full-flow.js
```

### Test Profiles

| Profile | VUs | Duration | Purpose |
|---------|-----|----------|---------|
| **smoke** | 1 | 30s | Verify the system works under minimal load |
| **average** | 20 (ramp) | 2 min | Simulate typical production traffic |
| **stress** | 50 → 200 (ramp) | 3.5 min | Find the breaking point and max throughput |
| **spike** | 10 → 300 (burst) | 1.5 min | Test sudden traffic bursts |
| **soak** | 30 (sustained) | 12 min | Detect memory leaks, connection pool exhaustion |

### Exporting Results

```powershell
# JSON output
k6 run --env PROFILE=smoke --out json=results.json LoadTests/categories.js

# CSV output
k6 run --env PROFILE=smoke --out csv=results.csv LoadTests/categories.js

# Grafana Cloud (k6 Cloud)
k6 cloud LoadTests/categories.js

# InfluxDB + Grafana (self-hosted dashboards)
k6 run --out influxdb=http://localhost:8086/k6 LoadTests/categories.js
```

### Reading the k6 Report

Key metrics to watch after each test run:

| Metric | What It Means |
|--------|---------------|
| `http_req_duration` | Response time — `avg`, `p(90)`, `p(95)`, `p(99)` |
| `http_req_failed` | Percentage of non-2xx/3xx responses |
| `http_reqs` | Total requests and requests per second |
| `checks` | Pass/fail rate of assertions |
| `iterations` | Number of complete test iterations |

For comprehensive load testing documentation, see [LoadTests/README.md](LoadTests/README.md).

---

## Docker

Build and run with Docker:

```powershell
# Build the image
docker build -t catalog-service .

# Run the container
docker run -d -p 8080:8080 -p 8081:8081 \
  -e POSTGRES_DB_URL="Host=host.docker.internal;Port=5432;Database=catalogdb;Username=postgres;Password=secret" \
  -e JWT_KEY="your-jwt-signing-key-at-least-32-characters-long" \
  --name catalog-service \
  catalog-service
```

---

## License

This project is open source and available under the [MIT License](LICENSE).
#   b a c k e n d  
 