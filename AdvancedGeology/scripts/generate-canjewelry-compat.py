from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path


BASE_CODES = {"ruby", "citrine"}
GEOADDONS_CODES = {
    "spinelred",
    "topazblue",
    "tourmalinerubellite",
    "tourmalineschorl",
    "tourmalineverdelite",
}
ALLOWED_BUFFS = {
    "walkspeed",
    "miningSpeedMul",
    "maxhealthExtraPoints",
    "meleeWeaponsDamage",
    "hungerrate",
    "wildCropDropRate",
    "armorDurabilityLoss",
    "oreDropRate",
    "healingeffectivness",
    "rangedWeaponsDamage",
    "animalLootDropRate",
    "vesselContentsDropRate",
    "bowDrawingStrength",
    "animalSeekingRange",
    "rangedWeaponsSpeed",
    "armorWalkSpeedAffectedness",
    "animalHarvestingTime",
    "mechanicalsDamage",
    "rangedWeaponsAcc",
    "candurability",
    "temporalgrasp",
}
PATCH_DEPENDENCY = [{"modid": "canjewelry"}]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Generate AdvancedGeology CAN Jewelry compatibility assets.")
    parser.add_argument("--check", action="store_true", help="Verify generated files without writing them.")
    return parser.parse_args()


def encoded(value: object) -> str:
    return json.dumps(value, indent=2, ensure_ascii=False) + "\n"


def load_json(path: Path) -> object:
    with path.open(encoding="utf-8") as stream:
        return json.load(stream)


def actual_ag_codes(assets: Path) -> set[str]:
    codes: set[str] = set()
    for name in ("gemstone-pilot.json", "gemstone-expansion.json", "alexandrite-gem.json"):
        for operation in load_json(assets / "game" / "patches" / name):
            if (
                operation.get("file") == "game:worldproperties/block/ore-gem-rough.json"
                and operation.get("path") == "/variants/-"
            ):
                codes.add(operation["value"]["Code"])
    return codes


def validate(gems: list[dict[str, object]], assets: Path) -> None:
    codes = [str(gem["code"]) for gem in gems]
    if len(codes) != 56 or len(set(codes)) != 56:
        raise ValueError("canonical table must contain exactly 56 unique gem codes")
    if any(re.fullmatch(r"[a-z0-9]+", code) is None for code in codes):
        raise ValueError("gem codes must be lowercase concatenations without separators")
    if set(codes) != actual_ag_codes(assets):
        raise ValueError("canonical CAN gem codes do not exactly match registered AdvancedGeology gems")

    base = {str(gem["code"]) for gem in gems if gem.get("providedByCanBase") is True}
    geoaddons = {str(gem["code"]) for gem in gems if gem.get("providedByCanGeoAddons") is True}
    if base != BASE_CODES:
        raise ValueError(f"CAN base flags must be exactly {sorted(BASE_CODES)}")
    if geoaddons != GEOADDONS_CODES:
        raise ValueError(f"CAN GeoAddons flags must be exactly {sorted(GEOADDONS_CODES)}")

    for gem in gems:
        code = str(gem["code"])
        if not str(gem.get("displayName", "")).strip():
            raise ValueError(f"missing display name for {code}")
        if gem.get("buff") not in ALLOWED_BUFFS:
            raise ValueError(f"unknown CAN buff for {code}: {gem.get('buff')}")
        groups = gem.get("eligibleGroups")
        if not isinstance(groups, list) or "jewelry" not in groups:
            raise ValueError(f"{code} must include jewelry eligibility")
        texture = assets / "game" / "textures" / "block" / "stone" / "gem" / f"{code}.png"
        if not texture.exists():
            raise ValueError(f"missing AG gem texture: {texture}")


def patch_operation(op: str, path: str, value: object, file: str) -> dict[str, object]:
    return {
        "op": op,
        "path": path,
        "value": value,
        "file": file,
        "dependsOn": PATCH_DEPENDENCY,
    }


def generate_patch(gems: list[dict[str, object]]) -> list[dict[str, object]]:
    added_codes = [str(gem["code"]) for gem in gems if gem.get("providedByCanBase") is not True]
    textures = {
        f"*-{code}": {"gem": {"base": f"game:block/stone/gem/{code}"}}
        for code in added_codes
    }

    operations = [
        patch_operation(
            "addeach",
            "/variantgroups/2/states/-",
            added_codes,
            "canjewelry:itemtypes/resource/gem-rough.json",
        ),
        patch_operation(
            "addeach",
            "/variantgroups/2/states/-",
            added_codes,
            "canjewelry:itemtypes/resource/gem-cut.json",
        ),
        patch_operation(
            "addmerge",
            "/texturesByType",
            textures,
            "canjewelry:itemtypes/resource/gem-rough.json",
        ),
        patch_operation(
            "addmerge",
            "/texturesByType",
            textures,
            "canjewelry:itemtypes/resource/gem-cut.json",
        ),
    ]

    for recipe in ("round_cutting.json", "pear_cutting.json", "baguette_cutting.json"):
        for quality_index in range(3):
            operations.append(
                patch_operation(
                    "addeach",
                    f"/{quality_index}/ingredient/allowedVariants/-",
                    added_codes,
                    f"canjewelry:recipes/gemcutting/{recipe}",
                )
            )
    return operations


def generate_lang(gems: list[dict[str, object]]) -> dict[str, str]:
    result: dict[str, str] = {}
    for gem in gems:
        if gem.get("providedByCanBase") is True or gem.get("providedByCanGeoAddons") is True:
            continue
        code = str(gem["code"])
        name = str(gem["displayName"])
        result[f"item-gem-rough-chipped-{code}"] = f"Small rough {name}"
        result[f"item-gem-rough-flawed-{code}"] = f"Medium rough {name}"
        result[f"item-gem-rough-normal-{code}"] = f"Great rough {name}"
        result[f"item-gem-cut-normal-{code}"] = f"Small cut {name}"
        result[f"item-gem-cut-flawless-{code}"] = f"Medium cut {name}"
        result[f"item-gem-cut-exquisite-{code}"] = f"Great cut {name}"
    return result


def main() -> int:
    args = parse_args()
    root = Path(__file__).resolve().parent.parent
    assets = root / "resources" / "assets"
    source = root / "scripts" / "canjewelry-gems.json"
    gems = load_json(source)["gems"]
    validate(gems, assets)

    outputs = {
        assets / "canjewelry" / "patches" / "advancedgeology-gems.json": encoded(generate_patch(gems)),
        assets / "canjewelry" / "lang" / "en.json": encoded(generate_lang(gems)),
        assets / "advancedgeology" / "config" / "canjewelry-gems.json": encoded({"gems": gems}),
    }

    failures: list[Path] = []
    for path, content in outputs.items():
        if args.check:
            if not path.exists() or path.read_text(encoding="utf-8") != content:
                failures.append(path)
            continue
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(content, encoding="utf-8", newline="\n")

    if failures:
        for path in failures:
            print(f"mismatch: {path.relative_to(root)}")
        return 1

    print(f"{'verified' if args.check else 'wrote'} {len(outputs)} CAN Jewelry compatibility assets")
    return 0


if __name__ == "__main__":
    sys.exit(main())
