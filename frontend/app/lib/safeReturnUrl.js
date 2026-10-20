export function getSafeReturnUrl(rawUrl, origin) {
  if (!rawUrl || !rawUrl.startsWith("/") || rawUrl.startsWith("//")) return "/";

  try {
    const parsed = new URL(rawUrl, origin);
    return parsed.origin === origin ? `${parsed.pathname}${parsed.search}${parsed.hash}` : "/";
  } catch {
    return "/";
  }
}
