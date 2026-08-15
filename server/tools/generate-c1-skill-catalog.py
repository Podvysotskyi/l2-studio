#!/usr/bin/env python3
"""Generate Studio's checked-in C1 skill snapshot from Mobius skill XML."""

import argparse
from pathlib import Path
import xml.etree.ElementTree as ElementTree


OPERATE_TYPES = {"A1", "A2", "A3", "CA1", "CA5", "P", "T"}
TARGET_TYPES = {
    "AREA", "AREA_CORPSE_MOB", "AREA_SUMMON", "AURA", "AURA_CORPSE_MOB", "BEHIND_AURA",
    "CLAN", "CLAN_MEMBER", "CORPSE", "CORPSE_CLAN", "CORPSE_MOB", "ENEMY_SUMMON",
    "FRONT_AREA", "FRONT_AURA", "GROUND", "HOLY", "NONE", "ONE", "OWNER_PET", "PARTY",
    "PARTY_CLAN", "PARTY_MEMBER", "PARTY_NOT_ME", "PC_BODY", "SELF", "SERVITOR", "UNLOCKABLE"
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


def lookup_value(value: str | None, values: set[str], lookup_name: str) -> str:
    if value is None or not value.strip():
        return "null"
    value = value.strip()
    if value not in values:
        raise ValueError(f"Unknown {lookup_name} '{value}'.")
    return csharp_string(value)


def expression(skill: ElementTree.Element) -> str:
    identifier = int(skill.attrib["id"])
    levels = int(skill.attrib["levels"])
    if levels < 1 or levels > 255:
        raise ValueError(f"Skill {identifier} has unsupported level count {levels}.")
    operate_type = lookup_value(skill.findtext("operateType"), OPERATE_TYPES, "SkillOperateType")
    target_type = lookup_value(skill.findtext("targetType"), TARGET_TYPES, "SkillTargetType")
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
    lines = ["namespace L2.Studio.Worker;", "", "public sealed partial class C1SkillCatalog", "{", "    private static readonly SkillDefinition[] Definitions =", "    ["]
    lines.extend(expression(skill) for _, skill in sorted(skills.items()))
    lines.extend(["    ];", "}", ""])
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text("\n".join(lines), encoding="utf-8", newline="\n")


if __name__ == "__main__":
    main()
