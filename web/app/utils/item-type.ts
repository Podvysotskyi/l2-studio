import type { ItemLookupRecord } from '../types/models/item'

export interface ItemTypeNode extends ItemLookupRecord {
  depth: number
  parentDisplayName: string | null
  children: ItemTypeNode[]
}

export function buildItemTypeHierarchy(
  records: ItemLookupRecord[]
): ItemTypeNode[] {
  const nodes = new Map<string, ItemTypeNode>(
    records.map(record => [
      record.name,
      {
        ...record,
        depth: 0,
        parentDisplayName: record.parentTypeDisplayName ?? null,
        children: []
      }
    ])
  )
  const roots: ItemTypeNode[] = []

  for (const node of nodes.values()) {
    const parent = node.parentTypeName
      ? nodes.get(node.parentTypeName)
      : undefined
    if (parent && parent !== node) parent.children.push(node)
    else roots.push(node)
  }

  const sort = (items: ItemTypeNode[]) => {
    items.sort((left, right) => left.displayName.localeCompare(right.displayName))
    for (const item of items) sort(item.children)
  }
  const assignDepth = (node: ItemTypeNode, depth: number) => {
    node.depth = depth
    for (const child of node.children) assignDepth(child, depth + 1)
  }

  sort(roots)
  for (const root of roots) assignDepth(root, 0)
  return roots
}

export function flattenItemTypeHierarchy(
  roots: ItemTypeNode[],
  expandedNames: Set<string>,
  query = ''
): ItemTypeNode[] {
  const term = query.trim().toLocaleLowerCase()
  const visibleNames = term ? matchingItemTypePathNames(roots, term) : undefined
  const visible: ItemTypeNode[] = []

  const visit = (node: ItemTypeNode) => {
    if (visibleNames && !visibleNames.has(node.name)) return
    visible.push(node)
    if (visibleNames || expandedNames.has(node.name)) {
      for (const child of node.children) visit(child)
    }
  }

  for (const root of roots) visit(root)
  return visible
}

function matchingItemTypePathNames(
  roots: ItemTypeNode[],
  term: string
): Set<string> {
  const nodes = new Map<string, ItemTypeNode>()
  const parents = new Map<string, string>()
  const visit = (node: ItemTypeNode) => {
    nodes.set(node.name, node)
    for (const child of node.children) {
      parents.set(child.name, node.name)
      visit(child)
    }
  }
  for (const root of roots) visit(root)

  const visible = new Set<string>()
  for (const node of nodes.values()) {
    if (!matches(node, term)) continue
    let current: ItemTypeNode | undefined = node
    while (current) {
      visible.add(current.name)
      const parentName = parents.get(current.name)
      current = parentName ? nodes.get(parentName) : undefined
    }
  }
  return visible
}

function matches(node: ItemTypeNode, term: string): boolean {
  return [node.name, node.displayName, node.parentTypeName, node.parentTypeDisplayName]
    .some(value => value?.toLocaleLowerCase().includes(term))
}
