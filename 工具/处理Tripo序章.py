import bpy
import json
import shutil
from pathlib import Path


root = Path(__file__).resolve().parents[1] / "生成" / "tripo"
source_root = root / "序章待检"
output_root = root / "序章游戏版"
specs = {
    "P10": ("LeftArm", 8000),
    "P11": ("RightArm", 8000),
    "W01": ("Cultivator", 20000),
    "W04": ("Mountain", 18000),
    "W08": ("Hall", 8000),
}


def triangles(meshes):
    for obj in meshes:
        obj.data.calc_loop_triangles()
    return sum(len(obj.data.loop_triangles) for obj in meshes)


report = {}
for code, (name, limit) in specs.items():
    source = next((source_root / code).glob("*.fbx"))
    texture_source = next((source_root / code).rglob("tripo_rgb_*.png"))
    destination = output_root / code
    destination.mkdir(parents=True, exist_ok=True)
    texture = destination / f"{code}_BaseColor.png"
    shutil.copy2(texture_source, texture)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(source), use_anim=False)
    meshes = [obj for obj in bpy.data.objects if obj.type == "MESH"]
    armatures = [obj for obj in bpy.data.objects if obj.type == "ARMATURE"]
    before = triangles(meshes)
    for index, obj in enumerate(meshes):
        obj.name = f"{code}_Mesh_{index:02d}"
        for attempt in range(8):
            if triangles(meshes) <= limit:
                break
            current = triangles(meshes)
            modifier = obj.modifiers.new("GameMesh_Decimate", "DECIMATE")
            modifier.ratio = min(0.96, limit / current * 0.96)
            modifier.use_collapse_triangulate = True
            while obj.modifiers.find(modifier.name) > 0:
                bpy.context.view_layer.objects.active = obj
                bpy.ops.object.modifier_move_up(modifier=modifier.name)
            bpy.context.view_layer.objects.active = obj
            bpy.ops.object.modifier_apply(modifier=modifier.name)
    after = triangles(meshes)
    if after > limit + 16:
        raise RuntimeError(f"{code}: {after} triangles exceeds {limit} by more than 16")

    for obj in armatures:
        obj.name = f"{code}_Armature"
    for material in bpy.data.materials:
        material.name = f"{code}_Base"
    for image in bpy.data.images:
        image.filepath = str(texture)
        image.source = "FILE"

    bpy.ops.object.select_all(action="DESELECT")
    for obj in bpy.data.objects:
        if obj.type in ("MESH", "ARMATURE", "EMPTY"):
            obj.select_set(True)
    model = destination / f"{code}_{name}.fbx"
    bpy.ops.export_scene.fbx(
        filepath=str(model), use_selection=True, axis_forward="-Z", axis_up="Y",
        add_leaf_bones=False, bake_anim=False, path_mode="RELATIVE",
    )
    report[code] = {"source_triangles": before, "game_triangles": after,
                    "limit": limit, "bones": sum(len(obj.data.bones) for obj in armatures),
                    "model": str(model), "texture": str(texture)}
    print(code, before, "->", after, "bones", report[code]["bones"], flush=True)

(output_root / "导出检查.json").write_text(
    json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
