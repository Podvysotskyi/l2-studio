#!/usr/bin/env python3
"""Generate Studio's checked-in C1 skill snapshot from Mobius skill XML."""

import argparse
from pathlib import Path
import xml.etree.ElementTree as ElementTree


OPERATE_TYPES = {name: f"SkillOperateTypeId.{name}" for name in ("A1", "A2", "A3", "CA1", "CA5", "P", "T")}
TARGET_TYPES = {
    "AREA": "Area", "AREA_CORPSE_MOB": "AreaCorpseMob", "AREA_SUMMON": "AreaSummon",
    "AURA": "Aura", "AURA_CORPSE_MOB": "AuraCorpseMob", "BEHIND_AURA": "BehindAura",
    "CLAN": "Clan", "CLAN_MEMBER": "ClanMember", "CORPSE": "Corpse", "CORPSE_CLAN": "CorpseClan",
    "CORPSE_MOB": "CorpseMob", "ENEMY_SUMMON": "EnemySummon", "FRONT_AREA": "FrontArea",
    "FRONT_AURA": "FrontAura", "GROUND": "Ground", "HOLY": "Holy", "NONE": "None", "ONE": "One",
    "OWNER_PET": "OwnerPet", "PARTY": "Party", "PARTY_CLAN": "PartyClan", "PARTY_MEMBER": "PartyMember",
    "PARTY_NOT_ME": "PartyNotMe", "PC_BODY": "PcBody", "SELF": "Self", "SERVITOR": "Servitor",
    "UNLOCKABLE": "Unlockable"
}


def csharp_string(value: str) -> str:
    return '"' + value.replace("\\", "\\\\").replace('"', '\\"').replace("\r", "\\r").replace("\n", "\\n") + '"'


def icon_definitions(skill: ElementTree.Element, levels: int) -> list[str]:
    icon = skill.findtext("icon")
    if icon is None or not icon.strip():
        return []
    icon = icon.strip()
    tables = {
        table.attrib["name"]: (table.text or "").split()
        for table in skill.findall("table") if "name" in table.attrib
    }
    values = tables[icon] if icon.startswith("#") and icon in tables else [icon]
    if len(values) == 1:
        values *= levels
    if len(values) != levels:
        raise ValueError(f"Skill {skill.attrib['id']} has {levels} levels but {len(values)} icon values.")
    return [f"new({level}, {csharp_string(name)})" for level, name in enumerate(values, 1)]


def enum_value(value: str | None, values: dict[str, str], enum_name: str) -> str:
    if value is None or not value.strip():
        return "null"
    value = value.strip()
    if value not in values:
        raise ValueError(f"Unknown {enum_name} '{value}'.")
    return f"{enum_name}Id.{values[value]}" if enum_name == "SkillTargetType" else values[value]


def expression(skill: ElementTree.Element) -> str:
    identifier = int(skill.attrib["id"])
    levels = int(skill.attrib["levels"])
    if levels < 1 or levels > 255:
        raise ValueError(f"Skill {identifier} has unsupported level count {levels}.")
    operate_type = enum_value(skill.findtext("operateType"), OPERATE_TYPES, "SkillOperateType")
    target_type = enum_value(skill.findtext("targetType"), TARGET_TYPES, "SkillTargetType")
    icons = ", ".join(icon_definitions(skill, levels))
    return f"        new({identifier}, {levels}, {csharp_string(skill.attrib['name'])}, {operate_type}, {target_type}, [{icons}]),"


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("skills_root", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    paths = sorted(args.skills_root.glob("*.xml")) + sorted((args.skills_root / "custom").glob("*.xml"))
    skills: dict[int, ElementTree.Element] = {}
    for path in paths:
        for skill in ElementTree.parse(path).getroot().findall("skill"):
            # Mobius loads custom skill files after the base catalogue; duplicate
            # identifiers therefore resolve to the custom definition.
            skills[int(skill.attrib["id"])] = skill
    lines = ["using L2.Studio.Context.Identifiers;", "", "namespace L2.Studio.Worker;", "", "public sealed partial class C1SkillCatalog", "{", "    private static readonly SkillDefinition[] Definitions =", "    ["]
    lines.extend(expression(skill) for _, skill in sorted(skills.items()))
    lines.extend(["    ];", "}", ""])
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text("\n".join(lines), encoding="utf-8", newline="\n")


if __name__ == "__main__":
    main()
