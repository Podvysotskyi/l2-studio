#!/usr/bin/env python3
"""Generate Studio's checked-in C1 player class snapshot from Mobius data."""

import argparse
import re
from pathlib import Path
import xml.etree.ElementTree as ElementTree


RACES = {
    "HUMAN": "PlayerRaceId.Human", "ELF": "PlayerRaceId.Elf",
    "DARK_ELF": "PlayerRaceId.DarkElf", "ORC": "PlayerRaceId.Orc",
    "DWARF": "PlayerRaceId.Dwarf"
}
ENUM = re.compile(r"^\s*[A-Z_]+\((\d+), (true|false), (?:true, )?Race\.([A-Z_]+),")


def csharp_string(value: str) -> str:
    return '"' + value.replace("\\", "\\\\").replace('"', '\\"').replace("\r", "\\r").replace("\n", "\\n") + '"'


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("class_list", type=Path)
    parser.add_argument("player_class_enum", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    class_info: dict[int, tuple[bool, str]] = {}
    for line in args.player_class_enum.read_text(encoding="utf-8").splitlines():
        match = ENUM.match(line)
        if match:
            identifier, mage, race = match.groups()
            class_info[int(identifier)] = (mage == "true", RACES[race])

    definitions = []
    for node in ElementTree.parse(args.class_list).getroot().findall("class"):
        identifier = int(node.attrib["classId"])
        if identifier not in class_info:
            raise ValueError(f"Class {identifier} does not have a matching PlayerClass declaration.")
        is_mage, race = class_info[identifier]
        parent = node.attrib.get("parentClassId")
        parent_value = "null" if parent is None else f"(PlayerClassId){int(parent)}"
        definitions.append(
            f"        new((PlayerClassId){identifier}, {race}, {str(is_mage).lower()}, {parent_value}, {csharp_string(node.attrib['name'])}),"
        )
    lines = ["using L2.Studio.Context.Identifiers;", "", "namespace L2.Studio.Worker;", "", "public sealed partial class C1PlayerCatalog", "{", "    private static readonly PlayerClassDefinition[] ClassDefinitions =", "    [", *definitions, "    ];", "}", ""]
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text("\n".join(lines), encoding="utf-8", newline="\n")


if __name__ == "__main__":
    main()
