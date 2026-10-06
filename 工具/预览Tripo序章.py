import bpy
from mathutils import Vector
from pathlib import Path
import sys


stage = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else "序章待检"
root = Path(__file__).resolve().parents[1] / "生成" / "tripo" / stage
for code in ("P10", "P11", "W01", "W04", "W08"):
    source = next((root / code).glob("*.fbx"))
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(source), use_anim=False)
    if stage == "序章游戏版":
        original = next((root.parent / "序章待检" / code).rglob("tripo_rgb_*.png"))
        original_image = bpy.data.images.load(str(original), check_existing=False)
        for material in bpy.data.materials:
            if material.use_nodes:
                for node in material.node_tree.nodes:
                    if node.type == "TEX_IMAGE":
                        node.image = original_image
    meshes = [obj for obj in bpy.data.objects if obj.type == "MESH"]
    corners = [obj.matrix_world @ Vector(v) for obj in meshes for v in obj.bound_box]
    low = Vector(min(v[i] for v in corners) for i in range(3))
    high = Vector(max(v[i] for v in corners) for i in range(3))
    center = (low + high) / 2
    longest = max(high - low)

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 700
    scene.render.resolution_y = 700
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.world = bpy.data.worlds.new("QC_World")
    scene.world.color = (0.46, 0.49, 0.52)
    scene.view_settings.view_transform = "Standard"

    camera_data = bpy.data.cameras.new("QC_Camera")
    camera = bpy.data.objects.new("QC_Camera", camera_data)
    bpy.context.collection.objects.link(camera)
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = longest * 1.5
    scene.camera = camera

    for name, position, energy in (("Key", Vector((-2, -3, 4)), 450),
                                   ("Fill", Vector((3, 1, 2)), 270)):
        data = bpy.data.lights.new(name, "AREA")
        light = bpy.data.objects.new(name, data)
        bpy.context.collection.objects.link(light)
        light.location = center + position.normalized() * longest * 2.5
        data.energy = energy
        data.shape = "DISK"
        data.size = longest * 2
        light.rotation_euler = (center - light.location).to_track_quat("-Z", "Y").to_euler()

    for view, direction in (("front", Vector((0, -3, 1.2))),
                            ("side", Vector((3, 0, 1.2)))):
        camera.location = center + direction.normalized() * longest * 3
        camera.rotation_euler = (center - camera.location).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(root / code / f"{code}_{view}.png")
        bpy.ops.render.render(write_still=True)
        print(scene.render.filepath, flush=True)
