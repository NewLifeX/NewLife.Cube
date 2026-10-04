/** Stable serialization for the inputs that determine a server-side list result. */
function stableValue(value: unknown): unknown {
  if (Array.isArray(value)) return value.map(stableValue);
  if (!value || typeof value !== 'object') return value;
  return Object.fromEntries(
    Object.entries(value as Record<string, unknown>)
      .filter(([, item]) => item !== undefined)
      .sort(([left], [right]) => left.localeCompare(right))
      .map(([key, item]) => [key, stableValue(item)]),
  );
}

/**
 * Produces a comparable key for the exact request that fetched the current raw rows.
 * Rendering-only view settings deliberately do not enter this key.
 */
export function listRequestSignature(input: Record<string, unknown>): string {
  return JSON.stringify(stableValue(input));
}
