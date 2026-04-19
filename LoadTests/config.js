// k6 Load Test Configuration
// Shared settings for all load test scripts

export const BASE_URL = __ENV.BASE_URL || "http://localhost:5101";

// JWT config — must match appsettings.json values
const JWT_KEY = __ENV.JWT_KEY || "REPLACE_WITH_ENV_VAR_JWT_SECRET_KEY_MIN_32_CHARS";
const JWT_ISSUER = __ENV.JWT_ISSUER || "identity-service";
const JWT_AUDIENCE = __ENV.JWT_AUDIENCE || "identity-service-clients";

export function getAuthHeaders() {
  // k6 doesn't have native JWT signing, so we use a pre-generated token
  // or the /api/auth endpoint if available.
  // For load testing, set JWT_TOKEN env var with a valid long-lived token.
  const token = __ENV.JWT_TOKEN || "";
  return {
    "Content-Type": "application/json",
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  };
}

export function getPublicHeaders() {
  return {
    "Content-Type": "application/json",
  };
}

// ───── Load Test Profiles ─────

// Smoke test: minimal load to verify the system works
// Note: p(99) is unreliable with <100 requests (it equals the single worst request).
// Use p(95) for smoke; higher-traffic profiles use p(99).
export const smokeTest = {
  vus: 1,
  duration: "30s",
  thresholds: {
    http_req_duration: ["p(95)<500"],
    http_req_failed: ["rate<0.01"],
  },
};

// Average load: typical production traffic
export const averageLoad = {
  stages: [
    { duration: "30s", target: 20 },  // ramp up
    { duration: "1m", target: 20 },   // steady
    { duration: "30s", target: 0 },   // ramp down
  ],
  thresholds: {
    http_req_duration: ["p(95)<300", "p(99)<500"],
    http_req_failed: ["rate<0.01"],
  },
};

// Stress test: find the breaking point
export const stressTest = {
  stages: [
    { duration: "30s", target: 50 },
    { duration: "1m", target: 100 },
    { duration: "30s", target: 200 },
    { duration: "1m", target: 200 },
    { duration: "30s", target: 0 },
  ],
  thresholds: {
    http_req_duration: ["p(95)<1000"],
    http_req_failed: ["rate<0.05"],
  },
};

// Spike test: sudden burst of traffic
export const spikeTest = {
  stages: [
    { duration: "10s", target: 10 },
    { duration: "10s", target: 300 },  // spike
    { duration: "30s", target: 300 },
    { duration: "10s", target: 10 },
    { duration: "30s", target: 10 },
  ],
  thresholds: {
    http_req_duration: ["p(95)<2000"],
    http_req_failed: ["rate<0.10"],
  },
};

// Soak test: sustained load over time
export const soakTest = {
  stages: [
    { duration: "1m", target: 30 },
    { duration: "10m", target: 30 },
    { duration: "1m", target: 0 },
  ],
  thresholds: {
    http_req_duration: ["p(95)<500"],
    http_req_failed: ["rate<0.01"],
  },
};
