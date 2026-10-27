import test from "node:test";
import assert from "node:assert/strict";
import { getSafeReturnUrl } from "./safeReturnUrl.js";

test("allows same-origin paths and rejects external or malformed destinations", () => {
  const origin = "https://culinaryblog.example";
  assert.equal(getSafeReturnUrl("/recipes?sort=recent#top", origin), "/recipes?sort=recent#top");
  assert.equal(getSafeReturnUrl("//evil.example/path", origin), "/");
  assert.equal(getSafeReturnUrl("/\\evil.example", origin), "/");
  assert.equal(getSafeReturnUrl("https://evil.example", origin), "/");
});
