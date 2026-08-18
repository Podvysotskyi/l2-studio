#!/usr/bin/env python3
"""Generate Studio's checked-in C1 NPC spawn snapshot from Mobius spawn XML."""

import argparse
from pathlib import Path
import xml.etree.ElementTree as ElementTree


def csharp_string(value: str) -> str:
    return '"' + value.replace('\\', '\\\\').replace('"', '\\"') + '"'


def integer(value, description: str, minimum=None) -> int:
    if value is None:
        raise ValueError(f'Missing {description}')
    try:
        parsed = int(value)
    except ValueError as error:
        raise ValueError(f'Invalid {description}: {value}') from error
    if minimum is not None and parsed < minimum:
        raise ValueError(f'{description} must be at least {minimum}: {parsed}')
    return parsed


def territory_expression(value: ElementTree.Element, source: Path) -> tuple[int, int, str]:
    min_z = integer(value.get('minZ'), f'{source} territory minZ')
    max_z = integer(value.get('maxZ'), f'{source} territory maxZ')
    if not -32768 <= min_z <= 32767 or not -32768 <= max_z <= 32767:
        raise ValueError(f'{source} territory Z bounds exceed Int16')
    if min_z > max_z:
        raise ValueError(f'{source} territory minZ exceeds maxZ')
    nodes = list(value.findall('node'))
    if len(nodes) < 3:
        raise ValueError(f'{source} territory must have at least three nodes')
    node_values = []
    for sequence, node in enumerate(nodes):
        x = integer(node.get('x'), f'{source} territory node {sequence} x')
        y = integer(node.get('y'), f'{source} territory node {sequence} y')
        node_values.append(f'new({sequence}, {x}, {y})')
    return min_z, max_z, '[' + ', '.join(node_values) + ']'


def zone_expression(value: ElementTree.Element, source: Path) -> tuple[str, str]:
    name = value.get('zone')
    if not name:
        raise ValueError(f'{source} zone spawn is missing zone')
    if value.get('name') is not None:
        raise ValueError(f'{source} zone spawn must not also declare name')
    territories = list(value.findall('territory'))
    if len(territories) != 1:
        raise ValueError(f'{source} zone {name} must declare exactly one territory')
    if value.findall('banned_territory'):
        raise ValueError(f'{source} zone {name} declares unsupported banned territory')
    min_z, max_z, nodes = territory_expression(territories[0], source)
    entities = []
    for sequence, entity in enumerate(value.findall('npc')):
        if any(entity.get(attribute) is not None for attribute in ('x', 'y', 'z', 'heading')):
            raise ValueError(f'{source} zone {name} NPC {sequence} must not declare fixed coordinates')
        npc_id = integer(entity.get('id'), f'{source} zone {name} NPC {sequence} id', 1)
        count = integer(entity.get('count'), f'{source} zone {name} NPC {sequence} count', 1)
        delay = integer(entity.get('respawnDelay'), f'{source} zone {name} NPC {sequence} respawnDelay', 0)
        random = entity.get('respawnRandom')
        random_value = 'null' if random is None else str(integer(random, f'{source} zone {name} NPC {sequence} respawnRandom', 0))
        entities.append(f'new({sequence}, {npc_id}, {count}, {delay}, {random_value})')
    if not entities:
        raise ValueError(f'{source} zone {name} must declare NPC entities')
    return name, f'        new({csharp_string(name)}, {min_z}, {max_z}, {nodes}, [{", ".join(entities)}]),'


def spawn_expression(value: ElementTree.Element, source: Path) -> tuple[str, str]:
    name = value.get('name')
    if not name:
        raise ValueError(f'{source} fixed spawn is missing name')
    if value.get('zone') is not None:
        raise ValueError(f'{source} fixed spawn {name} must not also declare zone')
    if value.findall('territory') or value.findall('banned_territory'):
        raise ValueError(f'{source} fixed spawn {name} must not declare territory')
    entities = []
    for sequence, entity in enumerate(value.findall('npc')):
        if entity.get('count') is not None or entity.get('respawnRandom') is not None:
            raise ValueError(f'{source} fixed spawn {name} NPC {sequence} has unsupported count or respawnRandom')
        npc_id = integer(entity.get('id'), f'{source} fixed spawn {name} NPC {sequence} id', 1)
        x = integer(entity.get('x'), f'{source} fixed spawn {name} NPC {sequence} x')
        y = integer(entity.get('y'), f'{source} fixed spawn {name} NPC {sequence} y')
        z = integer(entity.get('z'), f'{source} fixed spawn {name} NPC {sequence} z')
        heading = integer(entity.get('heading'), f'{source} fixed spawn {name} NPC {sequence} heading', 0)
        delay = integer(entity.get('respawnDelay'), f'{source} fixed spawn {name} NPC {sequence} respawnDelay', 0)
        entities.append(f'new({sequence}, {npc_id}, {x}, {y}, {z}, {heading}, {delay})')
    if not entities:
        raise ValueError(f'{source} fixed spawn {name} must declare NPC entities')
    return name, f'        new({csharp_string(name)}, [{", ".join(entities)}]),'


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument('source', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()

    zones: dict[str, str] = {}
    spawns: dict[str, str] = {}
    for source in sorted(args.source.rglob('*.xml')):
        root = ElementTree.parse(source).getroot()
        if root.tag != 'list' or root.get('enabled') != 'true':
            raise ValueError(f'{source} must be an enabled spawn list')
        for spawn in root.findall('spawn'):
            if spawn.get('zone') is not None:
                name, expression = zone_expression(spawn, source)
                if name in zones:
                    raise ValueError(f'Duplicate spawn zone {name}')
                zones[name] = expression
            elif spawn.get('name') is not None:
                name, expression = spawn_expression(spawn, source)
                if name in spawns:
                    raise ValueError(f'Duplicate fixed spawn {name}')
                spawns[name] = expression
            else:
                raise ValueError(f'{source} spawn must declare zone or name')

    lines = [
        'namespace L2.Studio.Worker;', '', 'public sealed partial class C1NpcSpawnCatalog', '{',
        '    private static readonly NpcSpawnZoneDefinition[] ZoneDefinitions =', '    ['
    ]
    lines.extend(zones[name] for name in sorted(zones))
    lines.extend(['    ];', '', '    private static readonly NpcSpawnDefinition[] SpawnDefinitions =', '    ['])
    lines.extend(spawns[name] for name in sorted(spawns))
    lines.extend(['    ];', '}', ''])
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open('w', encoding='utf-8', newline='\n') as output:
        output.write('\n'.join(lines))


if __name__ == '__main__':
    main()
