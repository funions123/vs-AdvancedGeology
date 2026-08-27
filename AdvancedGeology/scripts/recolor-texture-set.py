from __future__ import annotations

import argparse
import json
import os
import sys
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageOps


@dataclass(frozen=True)
class Recipe:
    source_root: Path
    target_root: Path
    source_prefix: str
    target_prefix: str
    included_files: tuple[tuple[Path, Path], ...]
    output_overrides: dict[Path, Path]
    shadow: str
    midtone: str
    highlight: str
    midpoint: float
    autocontrast: float
    contrast_strength: float


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Recolor a related texture set while preserving luminance and alpha."
    )
    parser.add_argument("recipe", type=Path, help="Path to a recolor recipe JSON file")
    parser.add_argument(
        "--check",
        action="store_true",
        help="Verify that generated textures exist and match the recipe without writing files",
    )
    parser.add_argument(
        "--force",
        action="store_true",
        help="Replace existing generated textures",
    )
    return parser.parse_args()


def resolve_root(value: object, repository_root: Path) -> Path:
    expanded = Path(os.path.expandvars(str(value)))
    return expanded if expanded.is_absolute() else repository_root / expanded


def load_recipe(recipe_path: Path, repository_root: Path) -> Recipe:
    with recipe_path.open(encoding="utf-8") as stream:
        data = json.load(stream)

    midpoint = float(data.get("midpoint", 0.5))
    if not 0.0 <= midpoint <= 1.0:
        raise ValueError("midpoint must be between 0 and 1")

    autocontrast = float(data.get("autocontrast", 0.0))
    if not 0.0 <= autocontrast < 50.0:
        raise ValueError("autocontrast must be a percentile cutoff between 0 and 50")

    contrast_strength = float(data.get("contrastStrength", 1.0))
    if not 0.0 <= contrast_strength <= 1.0:
        raise ValueError("contrastStrength must be between 0 and 1")
    source_prefix = str(data["sourcePrefix"])
    target_prefix = str(data["targetPrefix"])
    if not source_prefix or not target_prefix or source_prefix == target_prefix:
        raise ValueError("sourcePrefix and targetPrefix must be non-empty and different")

    source_root = resolve_root(data["sourceRoot"], repository_root)
    target_root = resolve_root(data.get("targetRoot", data["sourceRoot"]), repository_root)
    source_files_value = data.get("sourceFiles")
    if "sourceFilesFile" in data:
        manifest_path = recipe_path.parent / str(data["sourceFilesFile"])
        with manifest_path.open(encoding="utf-8") as manifest:
            source_files_value = json.load(manifest)
    if isinstance(source_files_value, dict):
        source_files_value = source_files_value[source_prefix]
    expanded_sources = []
    for entry in source_files_value or ():
        if isinstance(entry, dict):
            source_relative = str(entry["source"])
            target_relative = str(entry["target"])
        else:
            source_relative = str(entry)
            target_relative = source_relative.replace("{sourcePrefix}", "{targetPrefix}")
        source_relative = source_relative.replace("{sourcePrefix}", source_prefix).replace("{targetPrefix}", target_prefix)
        target_relative = target_relative.replace("{sourcePrefix}", source_prefix).replace("{targetPrefix}", target_prefix)
        expanded_sources.append((source_root / source_relative, target_root / target_relative))
    included_files = tuple(expanded_sources)
    output_overrides = {
        source_root / source.replace("{sourcePrefix}", source_prefix): target_root / target.replace("{targetPrefix}", target_prefix)
        for source, target in data.get("outputOverrides", {}).items()
    }

    return Recipe(
        source_root=source_root,
        target_root=target_root,
        source_prefix=source_prefix,
        target_prefix=target_prefix,
        included_files=included_files,
        output_overrides=output_overrides,
        shadow=str(data["shadow"]),
        midtone=str(data["midtone"]),
        highlight=str(data["highlight"]),
        midpoint=midpoint,
        autocontrast=autocontrast,
        contrast_strength=contrast_strength,
    )


def source_targets(recipe: Recipe) -> list[tuple[Path, Path]]:
    if recipe.included_files:
        pairs = list(recipe.included_files)
    else:
        pairs = [
            (source, output_path(source, recipe))
            for source in sorted(recipe.source_root.rglob(f"{recipe.source_prefix}*.png"))
        ]
    if not pairs:
        raise FileNotFoundError(
            f"no {recipe.source_prefix!r} PNG textures found below {recipe.source_root}"
        )
    sources = {source for source, _ in pairs}
    seen_targets: dict[Path, Path] = {}
    for source, target in pairs:
        if not source.is_file():
            raise FileNotFoundError(f"source texture does not exist: {source}")
        if not source.name.startswith(recipe.source_prefix):
            raise ValueError(
                f"source texture name must start with {recipe.source_prefix!r}: {source}"
            )
        if not target.name.startswith(recipe.target_prefix):
            raise ValueError(
                f"refusing to write {target}: output name must start with {recipe.target_prefix!r}"
            )
        if target == source:
            raise ValueError(f"refusing to overwrite the source texture in place: {source}")
        if target in sources:
            raise ValueError(
                f"refusing to write {target}: that path is a source texture for this recipe"
            )
        if target in seen_targets:
            raise ValueError(
                f"two sources map to the same output {target}: "
                f"{seen_targets[target]} and {source}"
            )
        seen_targets[target] = source
    return pairs


def output_path(source: Path, recipe: Recipe) -> Path:
    if source in recipe.output_overrides:
        return recipe.output_overrides[source]
    relative_source = source.relative_to(recipe.source_root)
    suffix = source.name[len(recipe.source_prefix) :]
    return recipe.target_root / relative_source.parent / f"{recipe.target_prefix}{suffix}"


def recolor(source: Path, recipe: Recipe) -> Image.Image:
    with Image.open(source) as image:
        rgba = image.convert("RGBA")

    alpha = rgba.getchannel("A")
    luminance = ImageOps.grayscale(rgba.convert("RGB"))
    if recipe.autocontrast > 0 and recipe.contrast_strength > 0:
        stretched = ImageOps.autocontrast(luminance, cutoff=recipe.autocontrast)
        if recipe.contrast_strength < 1.0:
            stretched = Image.blend(luminance, stretched, recipe.contrast_strength)
        luminance = stretched
    colored = ImageOps.colorize(
        luminance,
        black=recipe.shadow,
        mid=recipe.midtone,
        white=recipe.highlight,
        midpoint=int(round(recipe.midpoint * 255)),
    ).convert("RGBA")
    colored.putalpha(alpha)
    return colored


def pixels_match(expected: Image.Image, output: Path) -> bool:
    if not output.exists():
        return False
    with Image.open(output) as actual:
        return actual.convert("RGBA").tobytes() == expected.tobytes()


def run(recipe: Recipe, check: bool, force: bool) -> int:
    pairs = source_targets(recipe)
    failures: list[Path] = []
    written = 0

    for source, output in pairs:
        expected = recolor(source, recipe)

        if check:
            if not pixels_match(expected, output):
                failures.append(output)
            continue

        if output.exists() and not force:
            raise FileExistsError(f"output already exists: {output}; use --force to replace it")

        output.parent.mkdir(parents=True, exist_ok=True)
        expected.save(output, format="PNG", optimize=False)
        written += 1

    if check and failures:
        for output in failures:
            print(f"mismatch: {output}")
        print(f"{len(failures)} of {len(pairs)} textures do not match", file=sys.stderr)
        return 1

    action = "verified" if check else "wrote"
    print(f"{action} {len(pairs) if check else written} textures")
    return 0


def main() -> int:
    args = parse_args()
    repository_root = Path(__file__).resolve().parent.parent
    recipe_path = args.recipe.resolve()
    recipe = load_recipe(recipe_path, repository_root)
    return run(recipe, check=args.check, force=args.force)


if __name__ == "__main__":
    raise SystemExit(main())
