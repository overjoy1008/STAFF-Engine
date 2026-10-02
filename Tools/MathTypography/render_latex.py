#!/usr/bin/env python3
"""Compile a math expression with real LaTeX into outlined SVG + transparent PNG.

Use the Python environment installed alongside STAFF/Manim (Pillow required).
Example: python render_latex.py --formula O --output Assets/MathSpace/Mathematics/Origin
No ordinary font substitution is used for mathematical glyphs.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
from PIL import Image, ImageOps


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--formula', required=True, help='LaTeX math body without dollar delimiters')
    parser.add_argument('--output', type=Path, required=True, help='Output directory')
    parser.add_argument('--tex-bin', type=Path)
    parser.add_argument('--rasterizer', type=Path)
    args = parser.parse_args()
    project = Path(__file__).resolve().parents[2]
    tex_bin = args.tex_bin or project.parent / 'STAFF/Manim/.tools/TinyTeX/bin/universal-darwin'
    rasterizer = args.rasterizer or shutil.which('pdftoppm') or (
        Path.home() / '.cache/codex-runtimes/codex-primary-runtime/dependencies/bin/override/pdftoppm')
    for name in ('latex', 'pdflatex', 'dvisvgm'):
        if not (tex_bin / name).is_file():
            raise SystemExit(f'Missing {name}; pass --tex-bin pointing to the existing Manim TeX installation.')
    if not Path(rasterizer).is_file():
        raise SystemExit('Pass --rasterizer pointing to pdftoppm.')
    source = ('\\documentclass[preview,border=1pt]{standalone}\n'
              '\\usepackage{amsmath,amssymb}\n'
              '\\begin{document}\n$' + args.formula + '$\n\\end{document}\n')
    args.output.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix='staff-latex-') as scratch:
        folder = Path(scratch)
        tex = folder / 'symbol.tex'
        tex.write_text(source)
        env = dict(os.environ)
        env['PATH'] = str(tex_bin) + os.pathsep + env.get('PATH', '')
        env['TEXMFVAR'] = str(folder / 'texmf-var')
        env['TEXMFCONFIG'] = str(folder / 'texmf-config')
        def run(command):
            result = subprocess.run([str(x) for x in command], cwd=folder, env=env,
                                    text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
            if result.returncode:
                raise RuntimeError(result.stdout)
        for executable in ('latex', 'pdflatex'):
            run([tex_bin / executable, '-no-shell-escape', '-interaction=nonstopmode', '-halt-on-error', tex.name])
        run([tex_bin / 'dvisvgm', '--no-fonts', '--exact', '--output=symbol.svg', 'symbol.dvi'])
        run([rasterizer, '-png', '-singlefile', '-scale-to', '1024', 'symbol.pdf', 'glyph'])
        # TeX renders black ink on white. Preserve coverage as alpha and use white ink in Unity.
        coverage = ImageOps.invert(Image.open(folder / 'glyph.png').convert('L'))
        bounds = coverage.getbbox()
        if not bounds:
            raise ValueError('LaTeX produced an empty symbol')
        coverage = coverage.crop(bounds)
        coverage = ImageOps.expand(coverage, border=32, fill=0)
        image = Image.new('RGBA', coverage.size, (255, 255, 255, 0))
        image.putalpha(coverage)
        image.save(args.output / 'symbol.png')
        shutil.copy2(tex, args.output / 'symbol.tex')
        shutil.copy2(folder / 'symbol.svg', args.output / 'symbol.svg')
        metadata = {'formula': args.formula, 'renderer': 'TinyTeX LaTeX + dvisvgm (Manim toolchain)',
                    'source_sha256': hashlib.sha256(source.encode()).hexdigest(),
                    'texture_sha256': hashlib.sha256((args.output / 'symbol.png').read_bytes()).hexdigest(),
                    'size': list(image.size), 'color': 'white', 'background': 'transparent'}
        (args.output / 'symbol.json').write_text(json.dumps(metadata, indent=2) + '\n')
    print(f'Rendered actual LaTeX ${args.formula}$ to {args.output}')


if __name__ == '__main__':
    main()
