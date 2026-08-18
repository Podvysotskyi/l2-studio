#!/usr/bin/env python3
"""Generate Studio's checked-in C1 recipe snapshot from Mobius Recipes.xml."""

import argparse
from pathlib import Path
import xml.etree.ElementTree as ElementTree


def csharp_string(value: str) -> str:
    return '"' + value.replace('\\', '\\\\').replace('"', '\\"') + '"'


def integer(value, description: str) -> int:
    if value is None:
        raise ValueError(f"Missing {description}")
    try:
        parsed = int(value)
    except ValueError as error:
        raise ValueError(f"Invalid {description}: {value}") from error
    if parsed < 1:
        raise ValueError(f"{description} must be positive: {parsed}")
    return parsed


def ingredient_expression(value: ElementTree.Element, recipe_id: int) -> str:
    return f"new({integer(value.get('id'), f'recipe {recipe_id} ingredient id')}, {integer(value.get('count'), f'recipe {recipe_id} ingredient count')})"


def production_expression(value: ElementTree.Element, recipe_id: int) -> str:
    return f"new({integer(value.get('id'), f'recipe {recipe_id} production id')}, {integer(value.get('count'), f'recipe {recipe_id} production count')})"


def expression(value: ElementTree.Element) -> str:
    recipe_id = integer(value.get('id'), 'recipe id')
    name = value.get('name')
    recipe_type = value.get('type')
    if not name or not recipe_type:
        raise ValueError(f"Recipe {recipe_id} must define name and type")
    ingredients = list(value.findall('ingredient'))
    if not ingredients:
        raise ValueError(f"Recipe {recipe_id} must define ingredients")
    ingredient_ids = [integer(ingredient.get('id'), f'recipe {recipe_id} ingredient id') for ingredient in ingredients]
    if len(ingredient_ids) != len(set(ingredient_ids)):
        raise ValueError(f"Recipe {recipe_id} defines duplicate ingredient items")
    productions = list(value.findall('production'))
    if len(productions) != 1:
        raise ValueError(f"Recipe {recipe_id} must define exactly one production")
    stat_uses = list(value.findall('statUse'))
    if not stat_uses:
        raise ValueError(f"Recipe {recipe_id} must define statUse")
    stats: dict[str, int] = {}
    for stat_use in stat_uses:
        name_value = stat_use.get('name')
        if name_value not in {'MP', 'HP'}:
            raise ValueError(f"Recipe {recipe_id} has unsupported statUse '{name_value}'")
        if name_value in stats:
            raise ValueError(f"Recipe {recipe_id} defines duplicate {name_value} statUse")
        stats[name_value] = integer(stat_use.get('value'), f'recipe {recipe_id} {name_value} statUse value')
    values = [
        str(recipe_id), csharp_string(name), csharp_string(recipe_type),
        str(integer(value.get('craftLevel'), f'recipe {recipe_id} craftLevel')),
        str(integer(value.get('successRate'), f'recipe {recipe_id} successRate')),
        '[' + ', '.join(ingredient_expression(ingredient, recipe_id) for ingredient in ingredients) + ']',
        '[' + ', '.join(production_expression(production, recipe_id) for production in productions) + ']',
        f"new({stats.get('MP', 'null')}, {stats.get('HP', 'null')})",
    ]
    return '        new(' + ', '.join(values) + '),'


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument('source', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()

    root = ElementTree.parse(args.source).getroot()
    values: dict[int, ElementTree.Element] = {}
    for item in root.findall('item'):
        recipe_id = integer(item.get('id'), 'recipe id')
        if recipe_id in values:
            raise ValueError(f'Duplicate recipe id {recipe_id}')
        values[recipe_id] = item
    types = sorted({item.get('type') for item in values.values() if item.get('type')})
    lines = [
        'namespace L2.Studio.Worker;', '', 'public sealed partial class C1ItemRecipeCatalog', '{',
        '    private static readonly ItemRecipeTypeDefinition[] TypeDefinitions =', '    ['
    ]
    lines.extend(f'        new({csharp_string(recipe_type)}),' for recipe_type in types)
    lines.extend(['    ];', '', '    private static readonly ItemRecipeDefinition[] Definitions =', '    ['])
    lines.extend(expression(value) for _, value in sorted(values.items()))
    lines.extend(['    ];', '}', ''])
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open('w', encoding='utf-8', newline='\n') as output:
        output.write('\n'.join(lines))


if __name__ == '__main__':
    main()
