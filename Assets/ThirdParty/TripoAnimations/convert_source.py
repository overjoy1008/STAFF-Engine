import bpy, json
from pathlib import Path
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath='/Users/gradient/Downloads/theherta.glb')
armatures=[o for o in bpy.data.objects if o.type=='ARMATURE']
print('ARMATURES',[(a.name,len(a.data.bones),tuple(a.scale)) for a in armatures],flush=True)
print('ACTIONS',len(bpy.data.actions),[(a.name,list(a.frame_range)) for a in bpy.data.actions],flush=True)
assert len(armatures)==1 and len(bpy.data.actions)==97
arm=armatures[0]
for track in arm.animation_data.nla_tracks: track.mute=True
arm.animation_data.action=None
bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);bpy.context.view_layer.objects.active=arm
bpy.ops.export_scene.fbx(filepath='/tmp/staff-motion-extension/deploy/Assets/ThirdParty/TripoAnimations/Tripo97.fbx',use_selection=True,object_types={'ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_force_startend_keying=True,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
Path('/tmp/staff-motion-extension/tripo-actions.json').write_text(json.dumps([{'name':a.name,'frames':list(a.frame_range)} for a in bpy.data.actions],indent=2))
print('TRIPO EXPORT DONE',flush=True)
