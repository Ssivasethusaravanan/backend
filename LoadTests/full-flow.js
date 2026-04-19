import http from "k6/http";
import { check, sleep } from "k6";
import { Trend, Rate, Counter } from "k6/metrics";
import {
  BASE_URL,
  getPublicHeaders,
  getAuthHeaders,
  smokeTest,
  averageLoad,
  stressTest,
  spikeTest,
  soakTest,
} from "./config.js";

// ───── Warmup (runs once before test, not counted in metrics) ─────
export function setup() {
  const headers = getPublicHeaders();
  http.get(`${BASE_URL}/health`);
  for (let i = 0; i < 3; i++) {
    http.get(`${BASE_URL}/api/v1/categories`, { headers });
    http.get(`${BASE_URL}/api/v1/sub-categories`, { headers });
  }
}

// ───── Custom Metrics ─────
const apiDuration = new Trend("api_duration", true);
const errorRate = new Rate("errors");
const requestCount = new Counter("total_requests");

// ───── Test Profile ─────
const profile = __ENV.PROFILE || "average";

const profiles = {
  smoke: smokeTest,
  average: averageLoad,
  stress: stressTest,
  spike: spikeTest,
  soak: soakTest,
};

export const options = profiles[profile] || profiles.average;

// ───── Full API Flow ─────
// Simulates realistic user behaviour: browse categories → pick one → browse sub-categories → CRUD

export default function () {
  const headers = getPublicHeaders();
  const authHeaders = getAuthHeaders();
  const hasAuth = !!__ENV.JWT_TOKEN;

  // 1. Health check
  const healthRes = http.get(`${BASE_URL}/health`, { tags: { name: "GET /health" } });
  requestCount.add(1);
  apiDuration.add(healthRes.timings.duration);
  check(healthRes, { "health: 200": (r) => r.status === 200 });
  sleep(0.3);

  // 2. List categories
  const catRes = http.get(`${BASE_URL}/api/v1/categories?page=1&limit=10`, {
    headers,
    tags: { name: "GET /categories" },
  });
  requestCount.add(1);
  apiDuration.add(catRes.timings.duration);
  const catOk = check(catRes, { "categories: 200": (r) => r.status === 200 });
  errorRate.add(!catOk);
  sleep(0.3);

  // 3. Get first category by ID (if data exists)
  let categoryId = 1;
  if (catRes.status === 200) {
    const body = catRes.json();
    if (body.data && body.data.length > 0) {
      categoryId = body.data[0].id;
    }
  }

  const catByIdRes = http.get(`${BASE_URL}/api/v1/categories/${categoryId}`, {
    headers,
    tags: { name: "GET /categories/:id" },
  });
  requestCount.add(1);
  apiDuration.add(catByIdRes.timings.duration);
  check(catByIdRes, { "category: 200/404": (r) => r.status === 200 || r.status === 404 });
  sleep(0.3);

  // 4. List sub-categories for that category
  const subRes = http.get(`${BASE_URL}/api/v1/sub-categories/by-category/${categoryId}`, {
    headers,
    tags: { name: "GET /sub-categories/by-category/:id" },
  });
  requestCount.add(1);
  apiDuration.add(subRes.timings.duration);
  const subOk = check(subRes, { "sub-categories: 200": (r) => r.status === 200 });
  errorRate.add(!subOk);
  sleep(0.3);

  // 5. Get a sub-category by ID
  let subCategoryId = 1;
  if (subRes.status === 200) {
    const body = subRes.json();
    if (body.data && body.data.length > 0) {
      subCategoryId = body.data[0].id;
    }
  }

  const subByIdRes = http.get(`${BASE_URL}/api/v1/sub-categories/${subCategoryId}`, {
    headers,
    tags: { name: "GET /sub-categories/:id" },
  });
  requestCount.add(1);
  apiDuration.add(subByIdRes.timings.duration);
  check(subByIdRes, { "sub-category: 200/404": (r) => r.status === 200 || r.status === 404 });
  sleep(0.3);

  // 6. Pagination walk (page 1 → page 2)
  const page2Res = http.get(`${BASE_URL}/api/v1/categories?page=2&limit=5`, {
    headers,
    tags: { name: "GET /categories?page=2" },
  });
  requestCount.add(1);
  apiDuration.add(page2Res.timings.duration);
  check(page2Res, { "page2: 200": (r) => r.status === 200 });
  sleep(0.3);

  // 7. Authenticated CRUD (only if JWT available)
  if (hasAuth) {
    const code = `K6F_${Date.now()}_${Math.floor(Math.random() * 10000)}`;

    // Create category
    const createRes = http.post(
      `${BASE_URL}/api/v1/categories`,
      JSON.stringify({ code, name: `Flow Test ${code}` }),
      { headers: authHeaders, tags: { name: "POST /categories" } }
    );
    requestCount.add(1);
    apiDuration.add(createRes.timings.duration);
    check(createRes, { "create: 201": (r) => r.status === 201 });

    if (createRes.status === 201) {
      const newId = createRes.json().data?.id;
      if (newId) {
        // Read back
        const readRes = http.get(`${BASE_URL}/api/v1/categories/${newId}`, {
          headers,
          tags: { name: "GET /categories/:id (new)" },
        });
        requestCount.add(1);
        apiDuration.add(readRes.timings.duration);
        check(readRes, { "readBack: 200": (r) => r.status === 200 });

        // Update
        const updateRes = http.put(
          `${BASE_URL}/api/v1/categories/${newId}`,
          JSON.stringify({ name: `Updated Flow ${code}` }),
          { headers: authHeaders, tags: { name: "PUT /categories/:id" } }
        );
        requestCount.add(1);
        apiDuration.add(updateRes.timings.duration);
        check(updateRes, { "update: 200": (r) => r.status === 200 });

        // Delete
        const delRes = http.del(`${BASE_URL}/api/v1/categories/${newId}`, null, {
          headers: authHeaders,
          tags: { name: "DELETE /categories/:id" },
        });
        requestCount.add(1);
        apiDuration.add(delRes.timings.duration);
        check(delRes, { "delete: 204": (r) => r.status === 204 });
      }
    }
  }

  sleep(1);
}
