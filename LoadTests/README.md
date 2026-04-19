# Load Testing with k6

Load test suite for the .NET Core REST API using **[k6](https://k6.io/)** — a modern, developer-friendly load testing tool by Grafana Labs.

---

## Table of Contents

- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Project Structure](#project-structure)
- [Configuration](#configuration)
- [Generate JWT Token](#generate-jwt-token)
- [Run Load Tests](#run-load-tests)
- [Test Profiles](#test-profiles)
- [Environment Variables](#environment-variables)
- [Reading the Report](#reading-the-report)
- [Export Results](#export-results)
- [Tips](#tips)

---

## Prerequisites

- **Node.js** (for JWT token generation only)
- API running locally (`dotnet run` → `http://localhost:5101`)

---

## Installation

### Windows (winget — recommended)

```powershell
winget install GrafanaLabs.k6
```

> After install, **restart your terminal** so k6 is on PATH.

### Windows (Chocolatey)

```powershell
choco install k6
```

### macOS (Homebrew)

```bash
brew install k6
```

### Linux (Debian/Ubuntu)

```bash
sudo gpg -k
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg \
  --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" \
  | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update && sudo apt-get install k6
```

### Docker

```bash
docker run --rm -i grafana/k6 run - <LoadTests/categories.js
```

### Verify Installation

```powershell
k6 version
# Expected: k6 v1.x.x (...)
```

---

## Project Structure

```
LoadTests/
├── config.js            # Shared config: base URL, headers, 5 test profiles
├── categories.js        # Categories API load test (list, filter, getById, CRUD)
├── sub-categories.js    # SubCategories API load test (list, by-category, CRUD)
├── full-flow.js         # Realistic user flow (health → browse → paginate → CRUD)
├── generate-token.js    # Node.js helper to generate JWT tokens
└── README.md            # This file
```

---

## Configuration

All configuration is in `config.js`:

| Setting | Default | Override |
|---------|---------|---------|
| Base URL | `http://localhost:5101` | `--env BASE_URL=https://your-api.com` |
| Test profile | `average` | `--env PROFILE=smoke` |
| JWT token | _(empty — read-only mode)_ | `--env JWT_TOKEN=eyJ...` |

---

## Generate JWT Token

Authenticated endpoints (POST, PUT, DELETE) require a JWT token. Generate one using Node.js:

```powershell
node LoadTests/generate-token.js
```

Output:

```
JWT Token (valid for 24h):

eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

Usage with k6:

  k6 run --env JWT_TOKEN="eyJ..." --env PROFILE=smoke LoadTests/categories.js
```

For a custom expiry (e.g., 48 hours):

```powershell
node LoadTests/generate-token.js 48
```

---

## Run Load Tests

### 1. Start the API

```powershell
cd d:\Github\catalog-service
dotnet run
```

### 2. Smoke Test (read-only, no JWT needed)

```powershell
k6 run --env PROFILE=smoke LoadTests/categories.js
```

### 3. Average Load (read-only)

```powershell
k6 run --env PROFILE=average LoadTests/categories.js
```

### 4. With CRUD — requires JWT

```powershell
# Generate token first
node LoadTests/generate-token.js

# Run with token
k6 run --env PROFILE=average --env JWT_TOKEN="paste-token-here" LoadTests/full-flow.js
```

### 5. Stress Test

```powershell
k6 run --env PROFILE=stress LoadTests/categories.js
```

### 6. Spike Test

```powershell
k6 run --env PROFILE=spike LoadTests/sub-categories.js
```

### 7. Soak Test (long-running)

```powershell
k6 run --env PROFILE=soak LoadTests/full-flow.js
```

### 8. Custom Base URL (deployed API)

```powershell
k6 run --env BASE_URL=https://your-api.com --env PROFILE=smoke LoadTests/categories.js
```

---

## Test Profiles

| Profile | VUs | Duration | Purpose |
|---------|-----|----------|---------|
| **smoke** | 1 | 30s | Verify the system works under minimal load |
| **average** | 20 (ramp) | 2 min | Simulate typical production traffic |
| **stress** | 50 → 200 (ramp) | 3.5 min | Find the breaking point and max throughput |
| **spike** | 10 → 300 (burst) | 1.5 min | Test sudden traffic bursts |
| **soak** | 30 (sustained) | 12 min | Detect memory leaks, connection pool exhaustion |

### Test Scripts

| Script | Endpoints Tested | Auth Required |
|--------|-----------------|---------------|
| `categories.js` | GET /categories, GET /categories/:id, POST/PUT/DELETE | CRUD only |
| `sub-categories.js` | GET /sub-categories, GET /by-category/:id, POST/PUT/DELETE | CRUD only |
| `full-flow.js` | Health + Categories + SubCategories + Pagination + CRUD | CRUD only |

---

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `BASE_URL` | API base URL | `http://localhost:5101` |
| `PROFILE` | Test profile: `smoke`, `average`, `stress`, `spike`, `soak` | `average` |
| `JWT_TOKEN` | Bearer token for authenticated endpoints | _(empty)_ |

---

## Reading the Report

After a test run, k6 prints a summary like:

```
     ✓ list: status is 200
     ✓ list: has items array
     ✓ getById: status is 200 or 404

     checks.........................: 100.00% ✓ 120  ✗ 0
     data_received..................: 45 kB   1.5 kB/s
     data_sent......................: 12 kB   400 B/s
     http_req_blocked...............: avg=1.2ms  min=0s     p(90)=0s     p(95)=0s
     http_req_duration..............: avg=23ms   min=8ms    p(90)=45ms   p(95)=52ms
   ✓ { p(95)<300 }
   ✓ { p(99)<500 }
     http_req_failed................: 0.00%  ✓ 0    ✗ 120
     http_reqs......................: 120     4/s
     iteration_duration.............: avg=1.5s   min=1.2s   p(90)=1.8s   p(95)=2.0s
     iterations.....................: 40      1.33/s
     vus............................: 20      min=0  max=20
     vus_max........................: 20      min=20 max=20
```

### Key Metrics

| Metric | What It Means |
|--------|--------------|
| `http_req_duration` | Response time — `avg`, `p(90)`, `p(95)`, `p(99)` |
| `http_req_failed` | Percentage of non-2xx/3xx responses |
| `http_reqs` | Total requests and requests per second |
| `checks` | Pass/fail rate of assertions |
| `iterations` | Number of complete test iterations |
| `vus` | Virtual users (concurrent connections) |
| Custom trends | `category_list_duration`, `category_get_by_id_duration`, etc. |

### Thresholds

Thresholds are pass/fail criteria defined per profile. If a threshold fails, k6 exits with code **99** and marks it with `✗`:

```
   ✗ http_req_duration..............: avg=520ms  p(95)=1200ms
     ✗ { p(95)<300 }   ← FAILED
```

---

## Export Results

### JSON output

```powershell
k6 run --env PROFILE=smoke --out json=results.json LoadTests/categories.js
```

### CSV output

```powershell
k6 run --env PROFILE=smoke --out csv=results.csv LoadTests/categories.js
```

### Grafana Cloud (k6 Cloud)

```powershell
k6 cloud LoadTests/categories.js
```

### InfluxDB + Grafana (self-hosted dashboards)

```powershell
k6 run --out influxdb=http://localhost:8086/k6 LoadTests/categories.js
```

---

## Tips

1. **Start with smoke** — always run smoke first to verify endpoints before heavy load
2. **Read-only is safe** — GET endpoints are `[AllowAnonymous]`, no JWT needed for read tests
3. **Watch rate limiting** — the API has rate limiting enabled; if you see 429 errors, that's working correctly
4. **Run from a separate machine** — for production tests, run k6 from a different server to avoid resource contention
5. **Compare with/without cache** — run once with Redis enabled, once with in-memory cache to measure the impact
