#!/usr/bin/env python3
"""Generate Studio's checked-in C1 item snapshot from Mobius item XML."""

import argparse
from pathlib import Path
import xml.etree.ElementTree as ElementTree


FIELDS = {
    "default_action": ("ActionName", "string"), "bodypart": ("BodyPartName", "string"),
    "material": ("MaterialName", "string"), "crystal_type": ("CrystalTypeName", "string"),
    "icon": ("Icon", "string"),
    "displayId": ("DisplayId", "int"),
    "crystal_count": ("CrystalCount", "int"), "weight": ("Weight", "int"),
    "price": ("Price", "long"), "soulshots": ("Soulshots", "int"), "spiritshots": ("Spiritshots", "int"),
    "mp_consume": ("MpConsume", "int"), "reduced_mp_consume": ("ReducedMpConsume", "string"),
    "reuse_delay": ("ReuseDelay", "int"), "recipe_id": ("RecipeId", "int"),
    "handler": ("HandlerName", "string"), "item_skill": ("ItemSkill", "string"), "use_condition": ("UseCondition", "string"),
    "element_enabled": ("ElementEnabled", "bool"), "enchant_enabled": ("EnchantEnabled", "bool"),
    "for_npc": ("ForNpc", "bool"), "immediate_effect": ("ImmediateEffect", "bool"),
    "isAttackWeapon": ("IsAttackWeapon", "bool"), "isForceEquip": ("IsForceEquip", "bool"),
    "is_depositable": ("IsDepositable", "bool"), "is_destroyable": ("IsDestroyable", "bool"),
    "is_dropable": ("IsDropable", "bool"), "is_magic_weapon": ("IsMagicWeapon", "bool"),
    "is_oly_restricted": ("IsOlyRestricted", "bool"), "is_questitem": ("IsQuestItem", "bool"),
    "is_sellable": ("IsSellable", "bool"), "is_stackable": ("IsStackable", "bool"),
    "is_tradable": ("IsTradable", "bool"), "useWeaponSkillsOnly": ("UseWeaponSkillsOnly", "bool"),
}
STATS = [
    ("accCombat", "AccuracyCombat"), ("critRate", "CriticalRate"), ("mAtk", "MagicalAttack"),
    ("mDef", "MagicalDefence"), ("maxMp", "MaximumMp"), ("pAtk", "PhysicalAttack"),
    ("pAtkRange", "PhysicalAttackRange"), ("pAtkSpd", "PhysicalAttackSpeed"),
    ("pDef", "PhysicalDefence"), ("rEvas", "Evasion"), ("rShld", "ShieldRate"),
    ("randomDamage", "RandomDamage"), ("sDef", "ShieldDefence"),
]
BODY_PART_NAMES = {
    "lrhand": "hands",
    "rear;lear": "ear",
    "rfinger;lfinger": "finger",
}


def csharp_string(value: str | None) -> str:
    if value is None:
        return "null"
    return '"' + value.replace("\\", "\\\\").replace('"', '\\"').replace("\r", "\\r").replace("\n", "\\n") + '"'


def csharp(value: str | None, kind: str) -> str:
    if value is None:
        return "null"
    if kind == "string":
        return csharp_string(value)
    if kind == "bool":
        return value.lower()
    return value + ("L" if kind == "long" else "")


def decimal(value: str | None) -> str:
    return "null" if value is None else f"{value}m"


def attack_geometry(item: ElementTree.Element) -> str:
    damage_range = item.find("set[@name='damage_range']")
    if damage_range is None:
        return "null"
    values = damage_range.attrib["val"].split(";")
    if len(values) != 4:
        raise ValueError(f"Item {item.attrib['id']} has an invalid damage_range: {damage_range.attrib['val']}")
    try:
        offset_x, offset_y, radius, length = (int(value) for value in values)
    except ValueError as error:
        raise ValueError(f"Item {item.attrib['id']} has an invalid damage_range: {damage_range.attrib['val']}") from error
    return f"new({offset_x}, {offset_y}, {radius}, {length})"


def skills(item: ElementTree.Element) -> str:
    values: list[str] = []
    keys: set[tuple[int, int]] = set()
    for skill in item.findall("./skills/skill"):
        try:
            skill_id = int(skill.attrib["id"])
            skill_level = int(skill.attrib["level"])
            chance = int(skill.attrib["type_chance"]) if "type_chance" in skill.attrib else None
        except ValueError as error:
            raise ValueError(f"Item {item.attrib['id']} has an invalid skill definition") from error
        if not -(2 ** 15) <= skill_level < 2 ** 15:
            raise ValueError(f"Item {item.attrib['id']} has an out-of-range skill level: {skill_level}")
        if (skill_id, skill_level) in keys:
            raise ValueError(f"Item {item.attrib['id']} defines duplicate skill {skill_id}-{skill_level}")
        keys.add((skill_id, skill_level))
        chance_value = "null" if chance is None else str(chance)
        values.append(f"new({skill_id}, {skill_level}, {csharp_string(skill.attrib.get('type'))}, {chance_value})")
    return "[" + ", ".join(values) + "]"


TYPE_SUBTYPE_FIELDS = {
    "Weapon": "weapon_type",
    "Armor": "armor_type",
    "EtcItem": "etcitem_type",
}

COMMON_FIELDS = {"material", "icon", "weight", "price"}
FAMILY_FIELDS = {
    "Armor": {"default_action", "bodypart", "crystal_type", "crystal_count", "enchant_enabled", "for_npc", "immediate_effect", "is_depositable", "is_destroyable", "is_dropable", "is_sellable", "is_tradable"},
    "Weapon": {"default_action", "bodypart", "crystal_type", "displayId", "crystal_count", "soulshots", "spiritshots", "mp_consume", "reduced_mp_consume", "reuse_delay", "element_enabled", "enchant_enabled", "for_npc", "immediate_effect", "isAttackWeapon", "isForceEquip", "is_depositable", "is_destroyable", "is_dropable", "is_magic_weapon", "is_sellable", "is_tradable", "useWeaponSkillsOnly"},
    "Arrow": {"default_action", "bodypart", "crystal_type", "immediate_effect", "is_stackable"},
    "Material": {"immediate_effect", "is_stackable"},
    "Potion": {"default_action", "reuse_delay", "handler", "for_npc", "immediate_effect", "is_oly_restricted", "is_stackable"},
    "Recipe": {"default_action", "recipe_id", "handler", "immediate_effect", "is_depositable", "is_destroyable", "is_dropable", "is_sellable", "is_stackable", "is_tradable"},
    "Enchant": {"default_action", "handler", "immediate_effect", "is_oly_restricted", "is_stackable"},
    "Scroll": {"default_action", "handler", "for_npc", "is_oly_restricted", "is_stackable"},
    "PetCollar": {"default_action", "handler", "use_condition", "is_oly_restricted"},
    "Etc": {"default_action", "bodypart", "crystal_type", "displayId", "reuse_delay", "handler", "item_skill", "use_condition", "for_npc", "immediate_effect", "is_depositable", "is_destroyable", "is_dropable", "is_oly_restricted", "is_questitem", "is_sellable", "is_stackable", "is_tradable"},
}
SKILL_FAMILIES = {"Weapon", "Potion", "Enchant", "Scroll", "PetCollar", "Etc"}
STATS_FAMILIES = {"Armor", "Weapon", "Etc"}


def item_family(item: ElementTree.Element, sets: dict[str, str]) -> str:
    parent = item.attrib["type"]
    subtype = item_type(item, sets)
    if parent == "Armor":
        return "Armor"
    if parent == "Weapon":
        return "Weapon"
    return {
        "ARROW": "Arrow", "MATERIAL": "Material", "POTION": "Potion", "RECIPE": "Recipe",
        "SCRL_ENCHANT_AM": "Enchant", "SCRL_ENCHANT_WP": "Enchant", "SCROLL": "Scroll",
        "PET_COLLAR": "PetCollar",
    }.get(subtype, "Etc")


def item_type(item: ElementTree.Element, sets: dict[str, str]) -> str:
    parent = item.attrib["type"]
    return sets.get(TYPE_SUBTYPE_FIELDS.get(parent, ""), parent)


def expression(item: ElementTree.Element) -> str:
    sets = {node.attrib["name"]: node.attrib["val"] for node in item.findall("set")}
    if "bodypart" in sets:
        sets["bodypart"] = BODY_PART_NAMES.get(sets["bodypart"], sets["bodypart"])
    family = item_family(item, sets)
    values = [f"Id = {item.attrib['id']}", f"Name = {csharp_string(item.attrib['name'])}", f"TypeName = {csharp_string(item_type(item, sets))}"]
    for source, (target, kind) in FIELDS.items():
        if source in COMMON_FIELDS or source in FAMILY_FIELDS[family]:
            values.append(f"{target} = {csharp(sets.get(source), kind)}")
    if family == "Weapon":
        values.append(f"AttackGeometry = {attack_geometry(item)}")
    if family in SKILL_FAMILIES:
        values.append(f"Skills = {skills(item)}")
    stat_values = {node.attrib["type"]: (node.text or "").strip() for node in item.findall("./stats/stat")}
    if family in STATS_FAMILIES:
        if stat_values:
            values.append("Stats = new(" + ", ".join(decimal(stat_values.get(source)) for source, _ in STATS) + ")")
        else:
            values.append("Stats = null")
    return f"        new Item_{family}Definition() {{ " + ", ".join(values) + " },"


def lookup_expression(name: str) -> str:
    return f"        {csharp_string(name)},"


def display_name(name: str) -> str:
    if name == "EtcItem":
        return "Etc Item"
    return name.replace("_", " ").title()


def type_definition_expression(name: str, parent: str | None) -> str:
    arguments = f"{csharp_string(name)}, {csharp_string(display_name(name))}"
    if parent is not None:
        arguments += f", {csharp_string(parent)}"
    return f"        new({arguments}),"


def lookup_lines(items: dict[int, ElementTree.Element]) -> list[str]:
    definitions = list(items.values())
    parents = {item.attrib["type"] for item in definitions}
    children: dict[str, set[str]] = {parent: set() for parent in parents}
    for item in definitions:
        parent = item.attrib["type"]
        subtype = item_type(item, {node.attrib["name"]: node.attrib["val"] for node in item.findall("set")})
        if subtype != parent:
            children[parent].add(subtype)
    child_parents: dict[str, set[str]] = {}
    for parent, values in children.items():
        for child in values:
            child_parents.setdefault(child, set()).add(parent)
    ambiguous_children = [child for child, value_parents in child_parents.items() if len(value_parents) > 1]
    if ambiguous_children:
        raise ValueError(f"Ambiguous item subtype names: {', '.join(sorted(ambiguous_children))}")
    lookups = [
        ("ActionNames", {item.find("set[@name='default_action']").attrib["val"] for item in definitions if item.find("set[@name='default_action']") is not None}),
        ("BodyPartNames", {BODY_PART_NAMES.get(item.find("set[@name='bodypart']").attrib["val"], item.find("set[@name='bodypart']").attrib["val"]) for item in definitions if item.find("set[@name='bodypart']") is not None}),
        ("MaterialNames", {item.find("set[@name='material']").attrib["val"] for item in definitions if item.find("set[@name='material']") is not None}),
        ("CrystalTypeNames", {item.find("set[@name='crystal_type']").attrib["val"] for item in definitions if item.find("set[@name='crystal_type']") is not None}),
        ("HandlerNames", {item.find("set[@name='handler']").attrib["val"] for item in definitions if item.find("set[@name='handler']") is not None}),
        ("SkillTypeNames", {skill.attrib["type"] for item in definitions for skill in item.findall("./skills/skill") if "type" in skill.attrib}),
    ]
    lines = ["namespace L2.Studio.Worker;", "", "public sealed partial class C1ItemCatalog", "{"]
    lines.extend(["    private static readonly ItemLookupDefinition[] TypeDefinitions =", "    ["])
    lines.extend(type_definition_expression(parent, None) for parent in sorted(parents))
    lines.extend(type_definition_expression(child, parent) for parent in sorted(parents) for child in sorted(children[parent]))
    lines.extend(["    ];", ""])
    for field, names in lookups:
        lines.extend([f"    private static readonly string[] {field} =", "    ["])
        lines.extend(lookup_expression(name) for name in sorted(names))
        lines.extend(["    ];", ""])
    return [*lines, "}", ""]


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("items_root", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("lookups_output", type=Path)
    args = parser.parse_args()
    paths = sorted(args.items_root.glob("*.xml")) + sorted((args.items_root / "custom").glob("*.xml"))
    items: dict[int, ElementTree.Element] = {}
    for path in paths:
        for item in ElementTree.parse(path).getroot().findall("item"):
            identifier = int(item.attrib["id"])
            # Mobius loads custom item files after the base catalogue; an identical
            # id in custom therefore overrides the base definition.
            items[identifier] = item
    lines = ["namespace L2.Studio.Worker;", "", "public sealed partial class C1ItemCatalog", "{", "    private static readonly ItemDefinition[] Definitions =", "    ["]
    lines.extend(expression(item) for _, item in sorted(items.items()))
    lines.extend(["    ];", "}", ""])
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text("\n".join(lines), encoding="utf-8", newline="\n")
    args.lookups_output.parent.mkdir(parents=True, exist_ok=True)
    args.lookups_output.write_text("\n".join(lookup_lines(items)), encoding="utf-8", newline="\n")


if __name__ == "__main__":
    main()
