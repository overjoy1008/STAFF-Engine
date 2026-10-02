"""Run with Blender --background --python convert_robot.py.

Requires Blender 5.1.2. The sibling RobotExpressive.glb is CC0; see the asset's
LICENSE.txt. Export actions individually: glTF's NLA tracks are all muted and
enabling all of them for export can contaminate partially keyed poses.
"""
import pathlib
import bpy

tools = pathlib.Path(__file__).resolve().parent
project = tools.parent.parent
destination = project / 'Assets/ThirdParty/Quaternius/RobotExpressive/RobotExpressive.fbx'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(tools / 'RobotExpressive.glb'))
root = bpy.data.objects['RootNode']
keep = {root, *root.children_recursive}
for obj in list(bpy.data.objects):
    if obj not in keep:
        bpy.data.objects.remove(obj, do_unlink=True)
for obj in keep:
    obj.select_set(True)
    if obj.animation_data:
        obj.animation_data.action = None
        for track in obj.animation_data.nla_tracks:
            track.mute = track.name != 'Idle'
            for strip in track.strips:
                strip.mute = False
bpy.context.scene.frame_set(0)
bpy.context.view_layer.update()
for obj in keep:
    if obj.animation_data:
        for track in obj.animation_data.nla_tracks:
            track.mute = True
bpy.context.view_layer.objects.active = bpy.data.objects['RobotArmature']
bpy.context.scene.render.fps = 30
bpy.context.scene.frame_set(0)
destination.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.export_scene.fbx(
    filepath=str(destination), use_selection=True,
    object_types={'ARMATURE', 'MESH', 'EMPTY'}, add_leaf_bones=False,
    bake_anim=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=True,
    bake_anim_simplify_factor=0, axis_forward='-Z', axis_up='Y', path_mode='AUTO')
