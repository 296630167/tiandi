import bpy
import json
import sys
from pathlib import Path
from mathutils import Vector


stage = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else "序章待检"
root = Path(__file__).resolve().parents[1] / "生成" / "tripo" / stage
report = {}
for code in ("P10", "P11", "W01", "W04", "W08"):
    source = next((root / code).glob("*.fbx"))
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(source), use_anim=False)
    meshes = [obj for obj in bpy.data.objects if obj.type == "MESH"]
    armatures = [obj for obj in bpy.data.objects if obj.type == "ARMATURE"]
    corners = [obj.matrix_world @ Vector(corner) for obj in meshes for corner in obj.bound_box]
    minimum = [min(corner[i] for corner in corners) for i in range(3)]
    maximum = [max(corner[i] for corner in corners) for i in range(3)]
    images = []
    for img in bpy.data.images:
        images.append({"name": img.name, "path": img.filepath, "packed": img.packed_file is not None,
                       "size": list(img.size)})
    report[code] = {
        "source": str(source),
        "mesh_count": len(meshes),
        "triangles": sum(sum(max(0, len(poly.vertices) - 2) for poly in obj.data.polygons) for obj in meshes),
        "dimensions": [round(maximum[i] - minimum[i], 4) for i in range(3)],
        "min": [round(value, 4) for value in minimum],
        "max": [round(value, 4) for value in maximum],
        "armatures": [{"name": obj.name, "bones": len(obj.data.bones),
                       "bone_names": [bone.name for bone in obj.data.bones]} for obj in armatures],
        "skinned_meshes": [obj.name for obj in meshes if any(mod.type == "ARMATURE" for mod in obj.modifiers)],
        "images": images,
        "materials": [material.name for material in bpy.data.materials],
    }
    print(code, report[code]["triangles"], report[code]["dimensions"],
          len(report[code]["armatures"]), len(images), flush=True)

destination = root / "检查结果.json"
destination.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
print(destination, flush=True)
