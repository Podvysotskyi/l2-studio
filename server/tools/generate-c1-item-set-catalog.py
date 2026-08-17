#!/usr/bin/env python3
"""Generate Studio's checked-in C1 item-set snapshot from Mobius armor-set XML."""

import argparse
from pathlib import Path
import xml.etree.ElementTree as ElementTree


BODY_PART_NAMES = {"lrhand": "hands", "rear;lear": "ear", "rfinger;lfinger": "finger"}
STAT_NAMES = ["str", "dex", "con", "int", "wit", "men"]
FALLBACK_BODY_PARTS = {"gloves", "feet"}


def csharp_string(value: str) -> str:
    return '"' + value.replace("\\", "\\\\").replace('"', '\\"') + '"'


def csharp_nullable(value: int | None) -> str:
    return "null" if value is None else str(value)


def item_body_parts(items_root: Path) -> dict[int, str]:
    values: dict[int, str] = {}
    paths = sorted(items_root.glob("*.xml")) + sorted((items_root / "custom").glob("*.xml"))
    for path in paths:
        for item in ElementTree.parse(path).getroot().findall("item"):
            body_part = item.find("set[@name='bodypart']")
            if body_part is not None:
                values[int(item.attrib["id"])] = BODY_PART_NAMES.get(body_part.attrib["val"], body_part.attrib["val"])
    return values


def expression(value: ElementTree.Element, body_parts: dict[int, str]) -> str:
    set_id = int(value.attrib["id"])
    members: list[str] = []
    seen: set[str] = set()
    skills = []
    stats: dict[str, int] = {}
    for child in value:
        if child.tag == "skill":
            skills.append((int(child.attrib["id"]), int(child.attrib["level"])))
        elif child.tag in STAT_NAMES:
            stats[child.tag] = int(child.attrib["val"])
        elif child.tag in {"chest", "legs", "head", "gloves", "feet", "shield"}:
            item_id = int(child.attrib["id"])
            body_part = body_parts.get(item_id)
            if body_part is None:
                if child.tag not in FALLBACK_BODY_PARTS:
                    raise ValueError(f"Set {set_id} member {item_id} cannot resolve body part from '{child.tag}'")
                body_part = child.tag
            if body_part in seen:
                raise ValueError(f"Set {set_id} defines duplicate body part '{body_part}'")
            seen.add(body_part)
            members.append(f"new({csharp_string(body_part)}, {item_id})")
        else:
            raise ValueError(f"Set {set_id} has unsupported child '{child.tag}'")
    if len(skills) != 1:
        raise ValueError(f"Set {set_id} must define exactly one skill")
    skill_id, skill_level = skills[0]
    stat_value = "null" if not stats else "new(" + ", ".join(csharp_nullable(stats.get(name)) for name in STAT_NAMES) + ")"
    return f"        new({set_id}, [{', '.join(members)}], new({skill_id}, {skill_level}), {stat_value}),"


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("items_root", type=Path)
    parser.add_argument("armor_sets_root", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    body_parts = item_body_parts(args.items_root)
    values: dict[int, ElementTree.Element] = {}
    for path in sorted(args.armor_sets_root.glob("*.xml")):
        for item_set in ElementTree.parse(path).getroot().findall("set"):
            set_id = int(item_set.attrib["id"])
            if set_id in values:
                raise ValueError(f"Duplicate item-set id {set_id}")
            values[set_id] = item_set
    lines = ["namespace L2.Studio.Worker;", "", "public sealed partial class C1ItemSetCatalog", "{", "    private static readonly ItemSetDefinition[] Definitions =", "    ["]
    lines.extend(expression(value, body_parts) for _, value in sorted(values.items()))
    lines.extend(["    ];", "}", ""])
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text("\n".join(lines), encoding="utf-8", newline="\n")


if __name__ == "__main__":
    main()
