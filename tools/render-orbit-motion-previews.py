"""Assemble previews from sampled production-renderer frames; no gameplay is simulated here."""
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "artifacts/orbit-card-motion"
OUTPUT = ROOT / "docs/images"


def preview(sequence, name, fps):
    frames = []
    for path in sorted((SOURCE / sequence).glob(f"{sequence}-*.png")):
        with Image.open(path) as image:
            image = image.convert("RGB")
            image.thumbnail((896, 576), Image.Resampling.LANCZOS)
            frames.append(image.copy())
    if not frames:
        raise RuntimeError(f"No rendered {sequence} frames")
    # One shared palette prevents the colors from changing between GIF frames.
    samples = frames[::max(1, len(frames) // 24)]
    swatches = Image.new("RGB", (224 * 6, 144 * ((len(samples) + 5) // 6)))
    for index, frame in enumerate(samples):
        swatches.paste(frame.resize((224, 144)), (index % 6 * 224, index // 6 * 144))
    palette = swatches.quantize(colors=256)
    indexed = [frame.quantize(palette=palette, dither=Image.Dither.NONE) for frame in frames]
    # GIF delays are whole centiseconds. Alternating delays preserve 30fps timing.
    durations = [round((i + 1) * 100 / fps) * 10 - round(i * 100 / fps) * 10 for i in range(len(frames))]
    indexed[0].save(OUTPUT / name, save_all=True, append_images=indexed[1:],
                    duration=durations, loop=0, optimize=False, disposal=1)
    print(f"{name}: {len(frames)} frames, {sum(durations)} ms, {(OUTPUT / name).stat().st_size:,} bytes")


preview("deal", "orbit-deal.gif", 30)
preview("win", "orbit-win.gif", 20)
