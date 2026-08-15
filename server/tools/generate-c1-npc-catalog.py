#!/usr/bin/env python3

import argparse
from pathlib import Path
import xml.etree.ElementTree as ElementTree


def csharp_string(value: str | None) -> str:
    if not value:
        return "null"
    escaped = value.replace("\\", "\\\\").replace('"', '\\"').replace("\r", "\\r").replace("\n", "\\n")
    return f'"{escaped}"'


def csharp_int(value: str | None) -> str:
    return value if value is not None else "null"


def csharp_decimal(value: str | None) -> str:
    return f"{value}m" if value is not None else "null"


def resolved_stats(npc: ElementTree.Element) -> tuple[ElementTree.Element | None, ElementTree.Element | None, ElementTree.Element | None, ElementTree.Element | None, ElementTree.Element | None]:
    stats = npc.find("stats")
    return (
        stats,
        stats.find("vitals") if stats is not None else None,
        stats.find("attack") if stats is not None else None,
        stats.find("defence") if stats is not None else None,
        stats.find("speed") if stats is not None else None,
    )


def definition_expression(npc: ElementTree.Element) -> tuple[str, str, str, str, str]:
    stats, vitals, attack, defence, speed = resolved_stats(npc)
    stat = "null" if stats is None else "new(" + ", ".join(csharp_int(stats.get(key)) for key in ("str", "int", "dex", "wit", "con", "men")) + f", {csharp_int(stats.findtext('hitTime'))})"
    vital = "null" if vitals is None else "new(" + ", ".join(csharp_decimal(vitals.get(key)) for key in ("hp", "hpRegen", "mp", "mpRegen")) + ")"
    atk = "null" if attack is None else "new(" + ", ".join([
        csharp_decimal(attack.get("physical")), csharp_decimal(attack.get("magical")),
        csharp_int(attack.get("random")), csharp_int(attack.get("critical")), csharp_decimal(attack.get("accuracy")),
        csharp_int(attack.get("attackSpeed")), csharp_int(attack.get("reuseDelay")), csharp_string(attack.get("type")),
        csharp_int(attack.get("range")), csharp_int(attack.get("distance")), csharp_int(attack.get("width"))]) + ")"
    defense = "null" if defence is None else "new(" + ", ".join([
        csharp_decimal(defence.get("physical")), csharp_decimal(defence.get("magical")),
        csharp_int(defence.get("evasion")), csharp_int(defence.get("shield")), csharp_int(defence.get("shieldRate"))]) + ")"
    walk = speed.find("walk") if speed is not None else None
    run = speed.find("run") if speed is not None else None
    movement = "null" if speed is None else f"new({csharp_decimal(walk.get('ground') if walk is not None else None)}, {csharp_decimal(run.get('ground') if run is not None else None)})"
    return stat, vital, atk, defense, movement


def read_conversions(path: Path) -> dict[int, int]:
    result: dict[int, int] = {}
    for line in path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        source, target = line.split(";", 1)
        result[int(source)] = int(target)
    return result


def parse_boolean(value: str | None, default: bool) -> bool:
    return default if value is None else value.lower() == "true"


def resolved_status(npc: ElementTree.Element, npc_type: str) -> tuple[bool, bool, bool, bool, bool, bool, bool, bool, bool]:
    attributes = npc.find("status")
    values = attributes.attrib if attributes is not None else {}
    walk = npc.find("./stats/speed/walk")
    walk_speed = float(walk.attrib["ground"]) if walk is not None and "ground" in walk.attrib else 1.0
    can_move = parse_boolean(values.get("canMove"), True)
    if walk_speed <= 0.1:
        can_move = True
    return (
        parse_boolean(values.get("attackable"), True),
        parse_boolean(values.get("targetable"), True),
        parse_boolean(values.get("talkable"), True),
        parse_boolean(values.get("undying"), npc_type not in {"Monster", "RaidBoss", "GrandBoss"}),
        parse_boolean(values.get("showName"), True),
        parse_boolean(values.get("randomWalk"), npc_type != "Guard"),
        can_move,
        parse_boolean(values.get("noSleepMode"), False),
        parse_boolean(values.get("canBeSown"), False),
    )


def main() -> None:
    parser = argparse.ArgumentParser(description="Generate the checked-in C1 NPC lookup snapshot.")
    parser.add_argument("npc_root", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()

    conversions = read_conversions(args.npc_root / "CT0_to_C4_ids.txt")
    paths = sorted(args.npc_root.glob("*.xml")) + sorted((args.npc_root / "custom").glob("*.xml"))
    definitions: dict[int, tuple[int, int, str | None, str, str | None, str, tuple[bool, bool, bool, bool, bool, bool, bool, bool, bool], tuple[str, str, str, str, str]]] = {}
    for path in paths:
        for npc in ElementTree.parse(path).getroot().findall("npc"):
            npc_id = int(npc.attrib["id"])
            if npc_id in definitions:
                raise ValueError(f"Duplicate top-level NPC id {npc_id} in {path}")
            display_id = int(npc.attrib.get("displayId", npc_id))
            appearance_id = conversions.get(display_id, display_id)
            race = npc.findtext("race")
            npc_type = npc.attrib.get("type", "Folk")
            definitions[npc_id] = (
                appearance_id,
                int(npc.attrib.get("level", "85")),
                npc.attrib.get("name") or None,
                npc_type,
                None if not race or race == "NONE" else race,
                npc.findtext("sex") or "ETC",
                resolved_status(npc, npc_type),
                definition_expression(npc),
            )

    lines = [
        "namespace L2.Studio.Worker;",
        "",
        "public sealed partial class C1NpcLookupCatalog",
        "{",
        "    private static readonly NpcDefinition[] Definitions =",
        "    [",
    ]
    for npc_id, (appearance_id, level, name, npc_type, race, sex, status, stats) in sorted(definitions.items()):
        status_arguments = ", ".join(str(value).lower() for value in status)
        lines.append(
            f"        new({npc_id}, {appearance_id}, {level}, {csharp_string(name)}, "
            f"{csharp_string(npc_type)}, {csharp_string(race)}, {csharp_string(sex)}, "
            f"new({status_arguments}), {', '.join(stats)}),"
        )
    lines.extend(["    ];", "}", ""])
    args.output.write_text("\n".join(lines), encoding="utf-8", newline="\n")


if __name__ == "__main__":
    main()
