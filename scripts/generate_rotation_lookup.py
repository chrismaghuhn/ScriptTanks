"""One-off offline generator for FixedRotationDirectionLookup. Not used at build/runtime."""
import math
from pathlib import Path

SCALE = 1000
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "src" / "ScriptTanks.Core" / "Math" / "FixedRotationDirectionLookup.cs"


def forward_raw(step: int) -> tuple[int, int]:
    if step == 0:
        return 1000, 0
    if step == 250:
        return 0, 1000
    if step == 500:
        return -1000, 0
    if step == 750:
        return 0, -1000
    angle = 2 * math.pi * step / SCALE
    return int(round(math.cos(angle) * SCALE)), int(round(math.sin(angle) * SCALE))


def main() -> None:
    lines = [
        "namespace ScriptTanks.Core.Math;",
        "",
        "/// <summary>",
        "/// Committed turn-fraction forward lookup for FixedRotationDirectionResolver.",
        "/// </summary>",
        "/// <remarks>",
        "/// One entry per Fixed.Scale step (1000 steps per full turn). Values are",
        "/// generated offline and must not be recomputed at runtime with trigonometry.",
        "/// </remarks>",
        "internal static class FixedRotationDirectionLookup",
        "{",
        "    public const int StepCount = 1000;",
        "",
        "    private static readonly FixedVec2[] Forwards =",
        "    [",
    ]
    for i in range(SCALE):
        x, y = forward_raw(i)
        lines.append(f"        new FixedVec2(Fixed.FromRaw({x}), Fixed.FromRaw({y})),")
    lines.extend(
        [
            "    ];",
            "",
            "    public static FixedVec2 GetForward(int index) => Forwards[index];",
            "}",
            "",
        ]
    )
    OUT.write_text("\n".join(lines), encoding="utf-8")
    print(f"Wrote {OUT} ({SCALE} entries)")


if __name__ == "__main__":
    main()
