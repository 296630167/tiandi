import bpy
from pathlib import Path
from mathutils import Vector


ROOT = Path(__file__).resolve().parent
PROJECT = ROOT.parent.parent
ASSET = PROJECT / "Assets" / "天帝" / "美术" / "序章3D" / "P01_书桌"
ASSET.mkdir(parents=True, exist_ok=True)

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)


wood_image = bpy.data.images.load(str(ASSET / "P01_浅灰木纹.png"))


def material(name, tint, textured=True):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*tint, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*tint, 1)
    bsdf.inputs["Roughness"].default_value = 0.85
    if textured:
        tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
        tex.name = "LightAshGrain"
        tex.image = wood_image
        mix = mat.node_tree.nodes.new("ShaderNodeMixRGB")
        mix.blend_type = "MULTIPLY"
        mix.inputs[0].default_value = 1
        mix.inputs[2].default_value = (*tint, 1)
        mat.node_tree.links.new(tex.outputs["Color"], mix.inputs[1])
        mat.node_tree.links.new(mix.outputs["Color"], bsdf.inputs["Base Color"])
    return mat


top_mat = material("P01_MAT_LightAsh_Top", (1.0, 1.0, 1.0))
frame_mat = material("P01_MAT_LightAsh_Frame", (0.86, 0.87, 0.86))
end_mat = material("P01_MAT_EndGrain", (0.78, 0.80, 0.78))

root = bpy.data.objects.new("P01_Desk_Root", None)
bpy.context.collection.objects.link(root)


def beam(name, center, size, mat, bevel=0.003):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    obj.parent = root
    if bevel:
        mod = obj.modifiers.new("Soft_Edge", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
        mod = obj.modifiers.new("Weighted_Normals", "WEIGHTED_NORMAL")
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj


# All dimensions are meters. The root stays at the floor center for Unity placement.
beam("P01_Top", (0, 0, 0.777), (1.8, 0.8, 0.046), top_mat, 0.004)
beam("P01_Top_Front_Edge", (0, -0.398, 0.773), (1.78, 0.008, 0.036), end_mat, 0.002)
beam("P01_Top_Right_Edge", (0.898, 0, 0.773), (0.008, 0.78, 0.036), end_mat, 0.002)

for side_x, label_x in [(-1, "L"), (1, "R")]:
    for side_y, label_y in [(-1, "Front"), (1, "Back")]:
        beam(f"P01_Leg_{label_x}_{label_y}",
             (side_x * 0.826, side_y * 0.324, 0.365),
             (0.066, 0.066, 0.73), frame_mat, 0.003)

beam("P01_Apron_Front", (0, -0.324, 0.696), (1.62, 0.032, 0.11), frame_mat, 0.003)
beam("P01_Apron_Back", (0, 0.324, 0.696), (1.62, 0.032, 0.11), frame_mat, 0.003)
beam("P01_Apron_Left", (-0.826, 0, 0.696), (0.032, 0.61, 0.11), frame_mat, 0.003)
beam("P01_Apron_Right", (0.826, 0, 0.696), (0.032, 0.61, 0.11), frame_mat, 0.003)

mesh_objects = [obj for obj in bpy.data.objects if obj.type == "MESH" and obj.parent == root]
triangles = 0
for obj in mesh_objects:
    obj.data.calc_loop_triangles()
    triangles += len(obj.data.loop_triangles)
print(f"P01 desk: {len(mesh_objects)} mesh parts, {triangles} triangles")

# Export the model alone. Preview lighting and ground remain only in the .blend source.
bpy.ops.object.select_all(action="DESELECT")
root.select_set(True)
for obj in mesh_objects:
    obj.select_set(True)
bpy.context.view_layer.objects.active = root
bpy.ops.export_scene.fbx(
    filepath=str(ASSET / "P01_现代书桌.fbx"), use_selection=True,
    object_types={"EMPTY", "MESH"}, axis_forward="-Z", axis_up="Y",
    apply_unit_scale=True, bake_space_transform=False, path_mode="RELATIVE")

ground = material("Preview_Ground", (0.66, 0.68, 0.67), False)
beam("Preview_Floor", (0, 0, -0.02), (3.1, 2.2, 0.04), ground, 0)
bpy.context.object.parent = None

world = bpy.context.scene.world
world.color = (0.8, 0.8, 0.8)
world.use_nodes = True
world.node_tree.nodes.get("Background").inputs["Color"].default_value = (0.78, 0.81, 0.81, 1)
world.node_tree.nodes.get("Background").inputs["Strength"].default_value = 0.55

def area(name, location, energy, size):
    data = bpy.data.lights.new(name, "AREA")
    data.energy = energy
    data.shape = "DISK"
    data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    obj.rotation_euler = (Vector((0, 0, 0.4)) - obj.location).to_track_quat("-Z", "Y").to_euler()


area("Preview_Warm_Key", (-2.0, -2.8, 4.0), 300, 3.5)
area("Preview_Cool_Fill", (2.4, 0.8, 3.0), 140, 3.0)
camera_data = bpy.data.cameras.new("Preview_Camera")
camera = bpy.data.objects.new("Preview_Camera", camera_data)
bpy.context.collection.objects.link(camera)
camera.location = (2.75, -3.4, 2.05)
camera.rotation_euler = (Vector((0, 0, 0.40)) - camera.location).to_track_quat("-Z", "Y").to_euler()
camera_data.type = "ORTHO"
camera_data.ortho_scale = 2.55
bpy.context.scene.camera = camera
bpy.context.scene.render.engine = "BLENDER_EEVEE_NEXT"
bpy.context.scene.render.resolution_x = 1200
bpy.context.scene.render.resolution_y = 900
bpy.context.scene.render.resolution_percentage = 100
bpy.context.scene.render.image_settings.file_format = "PNG"
bpy.context.scene.render.filepath = str(ROOT / "P01_书桌_预览.png")
bpy.context.scene.view_settings.view_transform = "AgX"
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / "P01_现代书桌.blend"))
bpy.ops.render.render(write_still=True)
