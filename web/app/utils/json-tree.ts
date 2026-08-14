export function jsonTreeEntries(value: unknown): [string, unknown][] {
  if (Array.isArray(value)) return value.map((item, index) => [String(index), item])
  if (!value || typeof value !== 'object') return []
  return Object.entries(value)
}

export function isJsonTreeBranch(value: unknown) {
  return Array.isArray(value) || Boolean(value && typeof value === 'object')
}

export function jsonTreeBranchLabel(value: unknown) {
  const count = jsonTreeEntries(value).length
  return Array.isArray(value)
    ? `Array(${count})`
    : `Object(${count})`
}

export function jsonTreePrimitiveLabel(value: unknown) {
  if (value === null) return 'null'
  if (typeof value === 'string') return JSON.stringify(value)
  if (typeof value === 'boolean' || typeof value === 'number') return String(value)
  return JSON.stringify(value)
}
