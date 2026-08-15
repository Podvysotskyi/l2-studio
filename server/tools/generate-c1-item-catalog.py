#!/usr/bin/env python3
"""Generate Studio's checked-in C1 item snapshot from Mobius item XML."""

import argparse
from pathlib import Path
import xml.etree.ElementTree as ElementTree


FIELDS = {
    "default_action": ("ActionName", "string"), "bodypart": ("BodyPartName", "string"),
    "material": ("MaterialName", "string"), "crystal_type": ("CrystalTypeName", "string"),
    "icon": ("Icon", "string"), "weapon_type": ("WeaponType", "string"),
    "armor_type": ("ArmorType", "string"), "etcitem_type": ("EtcItemType", "string"),
    "damage_range": ("DamageRange", "string"), "displayId": ("DisplayId", "int"),
    "crystal_count": ("CrystalCount", "int"), "weight": ("Weight", "int"),
    "price": ("Price", "long"), "soulshots": ("Soulshots", "int"), "spiritshots": ("Spiritshots", "int"),
    "mp_consume": ("MpConsume", "int"), "reduced_mp_consume": ("ReducedMpConsume", "string"),
    "reuse_delay": ("ReuseDelay", "int"), "recipe_id": ("RecipeId", "int"),
    "handler": ("Handler", "string"), "item_skill": ("ItemSkill", "string"), "use_condition": ("UseCondition", "string"),
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
    "lrhand": "hand",
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


def expression(item: ElementTree.Element) -> str:
    sets = {node.attrib["name"]: node.attrib["val"] for node in item.findall("set")}
    if "bodypart" in sets:
        sets["bodypart"] = BODY_PART_NAMES.get(sets["bodypart"], sets["bodypart"])
    values = [f"Id = {item.attrib['id']}", f"Name = {csharp_string(item.attrib['name'])}", f"TypeName = {csharp_string(item.attrib['type'])}"]
    for source, (target, kind) in FIELDS.items():
        values.append(f"{target} = {csharp(sets.get(source), kind)}")
    stat_values = {node.attrib["type"]: (node.text or "").strip() for node in item.findall("./stats/stat")}
    if stat_values:
        values.append("Stats = new(" + ", ".join(decimal(stat_values.get(source)) for source, _ in STATS) + ")")
    else:
        values.append("Stats = null")
    return "        new() { " + ", ".join(values) + " },"


def lookup_expression(name: str) -> str:
    return f"        {csharp_string(name)},"


def lookup_lines(items: dict[int, ElementTree.Element]) -> list[str]:
    definitions = list(items.values())
    lookups = [
        ("TypeNames", {item.attrib["type"] for item in definitions}),
        ("ActionNames", {item.find("set[@name='default_action']").attrib["val"] for item in definitions if item.find("set[@name='default_action']") is not None}),
        ("BodyPartNames", {BODY_PART_NAMES.get(item.find("set[@name='bodypart']").attrib["val"], item.find("set[@name='bodypart']").attrib["val"]) for item in definitions if item.find("set[@name='bodypart']") is not None}),
        ("MaterialNames", {item.find("set[@name='material']").attrib["val"] for item in definitions if item.find("set[@name='material']") is not None}),
        ("CrystalTypeNames", {item.find("set[@name='crystal_type']").attrib["val"] for item in definitions if item.find("set[@name='crystal_type']") is not None}),
    ]
    lines = ["namespace L2.Studio.Worker;", "", "public sealed partial class C1ItemCatalog", "{"]
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
