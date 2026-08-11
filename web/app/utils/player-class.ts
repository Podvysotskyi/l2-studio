import type {
  PlayerClassNode,
  PlayerClassRecord,
  PlayerClassStage
} from '../types/models/content-directory'

export function buildPlayerClassHierarchy(
  records: PlayerClassRecord[]
): PlayerClassNode[] {
  const nodes = new Map<number, PlayerClassNode>()
  for (const record of [...records].sort((left, right) => left.id - right.id)) {
    nodes.set(record.id, {
      ...record,
      parentName: null,
      depth: 0,
      stage: 'Base',
      children: []
    })
  }

  const roots: PlayerClassNode[] = []
  for (const node of nodes.values()) {
    const parent =
      node.parentClassId === null ? undefined : nodes.get(node.parentClassId)
    if (!parent || parent === node) {
      roots.push(node)
      continue
    }

    node.parentName = parent.name
    parent.children.push(node)
  }

  const assignDepth = (node: PlayerClassNode, depth: number) => {
    node.depth = depth
    node.stage = playerClassStage(depth)
    for (const child of node.children) assignDepth(child, depth + 1)
  }
  for (const root of roots) assignDepth(root, 0)
  return roots
}

export function flattenPlayerClassHierarchy(
  roots: PlayerClassNode[],
  expandedIds: ReadonlySet<number>,
  query = ''
): PlayerClassNode[] {
  const term = query.trim().toLocaleLowerCase()
  const visibleIds = term ? matchingPlayerClassPathIds(roots, term) : undefined
  const visible: PlayerClassNode[] = []

  const visit = (node: PlayerClassNode) => {
    if (visibleIds && !visibleIds.has(node.id)) return
    visible.push(node)
    if (visibleIds || expandedIds.has(node.id)) {
      for (const child of node.children) visit(child)
    }
  }
  for (const root of roots) visit(root)
  return visible
}

function matchingPlayerClassPathIds(
  roots: PlayerClassNode[],
  term: string
): Set<number> {
  const nodes = new Map<number, PlayerClassNode>()
  const visit = (node: PlayerClassNode) => {
    nodes.set(node.id, node)
    for (const child of node.children) visit(child)
  }
  for (const root of roots) visit(root)

  const visible = new Set<number>()
  for (const node of nodes.values()) {
    if (
      !node.name.toLocaleLowerCase().includes(term) &&
      !String(node.id).includes(term) &&
      !node.allowedRaces.some(
        race =>
          race.name.toLocaleLowerCase().includes(term) ||
          race.allowedSexes.some(sex =>
            sex.name.toLocaleLowerCase().includes(term)
          )
      )
    )
      continue

    let current: PlayerClassNode | undefined = node
    while (current && !visible.has(current.id)) {
      visible.add(current.id)
      current =
        current.parentClassId === null
          ? undefined
          : nodes.get(current.parentClassId)
    }
  }
  return visible
}

function playerClassStage(depth: number): PlayerClassStage {
  if (depth === 0) return 'Base'
  if (depth === 1) return 'First'
  if (depth === 2) return 'Second'
  return 'Third'
}
