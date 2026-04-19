// Generates a JWT token for k6 load testing
// Usage: node generate-token.js [expiry-hours]
//
// Reads JWT config from ../.env or uses defaults from ../appsettings.json

const crypto = require("crypto");
const fs = require("fs");
const path = require("path");

// Load .env file
const envPath = path.join(__dirname, "..", ".env");
const env = {};
if (fs.existsSync(envPath)) {
  fs.readFileSync(envPath, "utf8")
    .split("\n")
    .forEach((line) => {
      const [key, ...rest] = line.split("=");
      if (key && rest.length) env[key.trim()] = rest.join("=").trim();
    });
}

const key = env.JWT_KEY || "REPLACE_WITH_ENV_VAR_JWT_SECRET_KEY_MIN_32_CHARS";
const issuer = "identity-service";
const audience = "identity-service-clients";
const expiryHours = parseInt(process.argv[2] || "24", 10);

// Base64url encode
function b64url(obj) {
  return Buffer.from(JSON.stringify(obj))
    .toString("base64")
    .replace(/=/g, "")
    .replace(/\+/g, "-")
    .replace(/\//g, "_");
}

const header = b64url({ alg: "HS256", typ: "JWT" });
const now = Math.floor(Date.now() / 1000);
const payload = b64url({
  sub: "k6-load-test",
  iss: issuer,
  aud: audience,
  iat: now,
  exp: now + expiryHours * 3600,
});

const signature = crypto
  .createHmac("sha256", key)
  .update(`${header}.${payload}`)
  .digest("base64")
  .replace(/=/g, "")
  .replace(/\+/g, "-")
  .replace(/\//g, "_");

const token = `${header}.${payload}.${signature}`;

console.log(`\nJWT Token (valid for ${expiryHours}h):\n`);
console.log(token);
console.log(`\nUsage with k6:\n`);
console.log(
  `  k6 run --env JWT_TOKEN="${token}" --env PROFILE=smoke LoadTests/categories.js\n`
);
