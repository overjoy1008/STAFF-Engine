"""Render the original Studio animation unchanged, with a black background."""
from pathlib import Path
import sys
sys.path.insert(0, str(Path(__file__).resolve().parent))
from TangentStudioReference import StudioScene, SETTINGS
SETTINGS['background'] = '#000000'
class TangentBlack(StudioScene):
    pass
