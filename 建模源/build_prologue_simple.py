import bpy
import math
import random
from pathlib import Path
from mathutils import Vector


PROJECT = Path(__file__).resolve().parent.parent
SOURCE = PROJECT / "建模源" / "序章简单物件"
ASSETS = PROJECT / "Assets" / "天帝" / "美术" / "序章3D"
SOURCE.mkdir(parents=True, exist_ok=True)

SPECS = {
    "P02": ("现代显示器", (1.30, 1.10, 0.23), 5000),
    "P03": ("键盘", (0.44, 0.05, 0.15), 4000),
    "P04": ("鼠标", (0.07, 0.05, 0.12), 2000),
    "P05": ("电脑椅", (0.60, 1.20, 0.60), 5000),
    "P06": ("马克杯", (0.13, 0.12, 0.09), 3000),
    "P07": ("窗框", (1.40, 1.60, 0.10), 2000),
    "P08": ("书柜", (0.90, 2.00, 0.35), 4000),
    "P09": ("台灯", (0.22, 0.47, 0.20), 3000),
    "W02": ("飞剑", (0.42, 0.08, 1.90), 5000),
    "W03": ("山间岩石", (2.40, 1.30, 1.80), 4000),
    "W06": ("起身石台", (3.40, 0.24, 3.40), 3000),
    "W07": ("源道纹", (0.38, 0.38, 0.10), 3000),
    "W09": ("矮草", (0.35, 0.35, 0.20), 1000),
}

COLORS = {
    "plastic_dark": (0.105, 0.145, 0.155),
    "plastic_mid": (0.18, 0.225, 0.23),
    "screen": (0.68, 0.735, 0.73),
    "key": (0.69, 0.72, 0.70),
    "ivory": (0.68, 0.70, 0.66),
    "fabric": (0.18, 0.245, 0.265),
    "fabric_side": (0.12, 0.175, 0.19),
    "wood": (0.86, 0.86, 0.84),
    "wood_dark": (0.69, 0.71, 0.68),
    "sage": (0.31, 0.46, 0.43),
    "sage_light": (0.45, 0.56, 0.51),
    "coffee": (0.105, 0.07, 0.048),
    "metal": (0.50, 0.59, 0.61),
    "metal_edge": (0.78, 0.82, 0.80),
    "metal_dark": (0.17, 0.27, 0.29),
    "stone": (0.36, 0.46, 0.46),
    "stone_light": (0.46, 0.55, 0.53),
    "stone_dark": (0.27, 0.36, 0.37),
    "moss": (0.23, 0.39, 0.34),
    "leaf": (0.27, 0.47, 0.38),
    "leaf_light": (0.39, 0.57, 0.45),
    "root": (0.30, 0.27, 0.21),
}


def reset(code):
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    globals()["code"] = code
    globals()["root"] = bpy.data.objects.new(f"{code}_Root", None)
    bpy.context.collection.objects.link(root)
    globals()["materials"] = {}


def mat(key):
    if key in materials:
        return materials[key]
    color = COLORS[key]
    m = bpy.data.materials.new(f"{code}_{key}")
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Roughness"].default_value = 0.88 if key not in ("metal", "metal_edge") else 0.48
    if key.startswith("wood"):
        tex = m.node_tree.nodes.new("ShaderNodeTexImage")
        tex.image = bpy.data.images.load(str(ASSETS / "P01_书桌" / "P01_浅灰木纹.png"), check_existing=True)
        mix = m.node_tree.nodes.new("ShaderNodeMixRGB")
        mix.blend_type = "MULTIPLY"
        mix.inputs[0].default_value = 1
        mix.inputs[2].default_value = (*color, 1)
        m.node_tree.links.new(tex.outputs["Color"], mix.inputs[1])
        m.node_tree.links.new(mix.outputs["Color"], bsdf.inputs["Base Color"])
    materials[key] = m
    return m


def finish_object(obj, name, key):
    obj.name = f"{code}_{name}"
    obj.parent = root
    obj.data.materials.append(mat(key))
    return obj


def box(name, center, size, key, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = bpy.context.object
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    finish_object(obj, name, key)
    if bevel:
        mod = obj.modifiers.new("Soft_Edge", "BEVEL")
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
        mod = obj.modifiers.new("Weighted_Normals", "WEIGHTED_NORMAL")
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj


def cylinder(name, center, radius, depth, key, vertices=16, rotation=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=center)
    obj = bpy.context.object
    if rotation:
        obj.rotation_euler = rotation
    return finish_object(obj, name, key)


def sphere(name, center, scale, key, segments=16, rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=center)
    obj = bpy.context.object
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish_object(obj, name, key)


def mesh(name, vertices, faces, key, face_keys=None):
    data = bpy.data.meshes.new(f"{code}_{name}_Mesh")
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(f"{code}_{name}", data)
    bpy.context.collection.objects.link(obj)
    obj.parent = root
    data.materials.append(mat(key))
    if face_keys:
        for extra in sorted(set(face_keys)):
            if extra != key:
                data.materials.append(mat(extra))
        slots = {m.name: i for i, m in enumerate(data.materials)}
        for poly, color_key in zip(data.polygons, face_keys):
            poly.material_index = slots[mat(color_key).name]
    return obj


def line(name, points, radius, key):
    curve = bpy.data.curves.new(f"{code}_{name}_Curve", "CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth = radius
    curve.bevel_resolution = 1
    spline = curve.splines.new("POLY")
    spline.points.add(len(points) - 1)
    for p, co in zip(spline.points, points):
        p.co = (*co, 1)
    obj = bpy.data.objects.new(f"{code}_{name}", curve)
    bpy.context.collection.objects.link(obj)
    bpy.ops.object.select_all(action="DESELECT")
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.convert(target="MESH")
    obj = bpy.context.object
    obj.select_set(False)
    return finish_object(obj, name, key)


def p02():
    box("Base", (0, 0.005, 0.018), (.42, .23, .036), "plastic_dark", .006)
    box("Stand", (0, .045, .292), (.078, .084, .53), "plastic_mid", .008)
    box("Bezel", (0, 0, .725), (1.30, .075, .74), "plastic_dark", .007)
    box("Screen_Blank", (0, -.040, .725), (1.236, .003, .684), "screen", .001)
    box("Lower_Lip", (0, -.043, .372), (1.28, .004, .018), "plastic_mid", .001)


def p03():
    box("Housing", (0, 0, .016), (.44, .15, .032), "plastic_dark", .004)
    for row in range(4):
        for col in range(12):
            if row == 0 and 3 <= col <= 8:
                if col != 3:
                    continue
                width = .203
            else:
                width = .027
            x = -.192 + col * .035 + (width - .027) / 2
            y = -.054 + row * .035
            box(f"Key_{row:02d}_{col:02d}", (x, y, .037), (width, .028, .014), "key", .0015)


def p04():
    # A low-profile shell with a flat bottom, split buttons and a separate wheel.
    count = 16
    verts = []
    for h, r in ((.006, 1.0), (.027, .98), (.047, .65)):
        for i in range(count):
            a = 2 * math.pi * i / count
            verts.append((.035 * r * math.cos(a), .06 * r * math.sin(a), h))
    faces = []
    for ring in range(2):
        for i in range(count):
            j = (i + 1) % count
            faces.append((ring * count + i, ring * count + j, (ring + 1) * count + j, (ring + 1) * count + i))
    faces.append(tuple(range((2 * count), 3 * count)))
    mesh("Shell", verts, faces, "ivory")
    box("Left_Button", (-.012, -.023, .043), (.026, .043, .006), "ivory", .002)
    box("Right_Button", (.012, -.023, .043), (.026, .043, .006), "ivory", .002)
    cylinder("Wheel", (0, -.029, .047), .006, .008, "plastic_mid", 12, (0, math.pi / 2, 0))


def p05():
    box("Seat", (0, -.01, .48), (.55, .52, .105), "fabric", .028)
    box("Back", (0, .218, .865), (.52, .09, .65), "fabric", .035)
    box("Back_Inner", (0, .166, .86), (.44, .012, .54), "fabric_side", .015)
    for side in (-1, 1):
        x = side * .27
        box(f"Arm_{side}", (x, -.035, .68), (.065, .31, .045), "plastic_mid", .008)
        box(f"Arm_Post_{side}", (x, -.03, .56), (.035, .035, .20), "plastic_dark", .004)
    cylinder("Gas_Lift", (0, 0, .28), .037, .36, "plastic_dark", 12)
    for i in range(5):
        angle = i * 2 * math.pi / 5
        dx, dy = math.cos(angle), math.sin(angle)
        arm = box(f"Star_Leg_{i}", (dx * .14, dy * .14, .115), (.28, .055, .045), "plastic_dark", .008)
        arm.rotation_euler.z = angle
        sphere(f"Caster_{i}", (dx * .27, dy * .27, .044), (.045, .043, .044), "plastic_mid", 12, 6)


def p06():
    segments = 24
    verts = []
    for z, r in ((0, .041), (.116, .044), (.116, .037), (.012, .036)):
        for i in range(segments):
            a = i * 2 * math.pi / segments
            verts.append((r * math.cos(a) + .014, r * math.sin(a), z))
    faces = []
    for ring in range(3):
        for i in range(segments):
            j = (i + 1) % segments
            faces.append((ring * segments + i, ring * segments + j, (ring + 1) * segments + j, (ring + 1) * segments + i))
    faces.append(tuple(range(3 * segments, 4 * segments)))
    mesh("Ceramic_Cup", verts, faces, "ivory")
    cylinder("Coffee", (.014, 0, .095), .0355, .002, "coffee", 24)
    # Open handle, placed to the left of the cup.
    points = []
    for i in range(13):
        a = -math.pi / 2 + i * math.pi / 12
        points.append((-.025 - .032 * math.cos(a), 0, .060 + .040 * math.sin(a)))
    line("Open_Handle", points, .006, "ivory")


def p07():
    for x in (-.675, 0, .675):
        box(f"Vertical_{x}", (x, 0, .8), (.05, .10, 1.6), "ivory", .002)
    for z in (.025, .8, 1.575):
        box(f"Horizontal_{z}", (0, 0, z), (1.4, .10, .05), "ivory", .002)
    for x in (-.335, .335):
        for z in (.39, 1.19):
            box(f"Inner_Trim_{x}_{z}", (x, -.052, z), (.62, .008, .012), "wood_dark")


def p08():
    box("Back", (0, .163, 1), (.9, .024, 2), "wood_dark", .002)
    for side in (-1, 1):
        box(f"Side_{side}", (side * .433, 0, 1), (.034, .35, 2), "wood", .003)
    for z in (.02, .48, .86, 1.25, 1.63, 1.98):
        box(f"Shelf_{z}", (0, 0, z), (.90, .35, .035), "wood", .003)
    for i, z in enumerate((.62, 1.05, 1.43, 1.78)):
        for j in range(3 if i % 2 else 5):
            width = .038 + ((i + j) % 3) * .01
            x = -.28 + j * .12
            height = min(.28, .15 + ((i * 3 + j) % 4) * .03)
            box(f"Book_{i}_{j}", (x, -.045, z + height / 2), (width, .20, height), "fabric" if j % 3 == 0 else "sage", .001)
    for side in (-1, 1):
        box(f"Lower_Door_{side}", (side * .215, -.179, .245), (.42, .015, .41), "wood", .003)
        sphere(f"Door_Knob_{side}", (side * .06, -.19, .26), (.012, .012, .012), "metal_dark", 8, 4)


def p09():
    cylinder("Base", (0, 0, .025), .105, .05, "sage", 24)
    line("Bent_Arm", ((0, 0, .045), (0, 0, .34), (-.03, 0, .43)), .012, "sage")
    verts = []
    segments = 20
    for z, radius in ((.36, .075), (.465, .040)):
        for i in range(segments):
            a = i * 2 * math.pi / segments
            verts.append((-.03 + radius * math.cos(a), radius * math.sin(a), z))
    faces = []
    for i in range(segments):
        j = (i + 1) % segments
        faces.append((i, j, segments + j, segments + i))
    mesh("Shade", verts, faces, "sage_light")
    cylinder("Shade_Inner", (-.03, 0, .362), .06, .003, "ivory", 20)


def w02():
    # Unity's +Z sword tip is Blender -Y with the chosen FBX axis conversion.
    verts = [(-.09, -.83, 0), (.09, -.83, 0), (0, -.95, 0),
             (-.09, .30, 0), (.09, .30, 0), (0, -.30, .034), (0, -.30, -.034)]
    faces = [(0, 5, 3), (5, 1, 4), (0, 2, 5), (2, 1, 5),
             (3, 6, 0), (6, 4, 1), (0, 6, 2), (2, 6, 1)]
    mesh("Tapered_Blade", verts, faces, "metal")
    line("Blade_Centerline", ((0, -.88, .006), (0, .23, .037)), .002, "metal_edge")
    box("Guard", (0, .34, 0), (.42, .065, .065), "metal_dark", .01)
    cylinder("Grip", (0, .59, 0), .030, .44, "metal_dark", 12, (math.pi / 2, 0, 0))
    cylinder("Pommel", (0, .89, 0), .045, .055, "metal_edge", 12, (math.pi / 2, 0, 0))


def w03():
    rng = random.Random(20261004)
    count = 14
    rings = ((.0, 1.0, .85), (.35, 1.0, .90), (.92, .84, .74), (1.30, .42, .38))
    verts = []
    for h, sx, sy in rings:
        for i in range(count):
            a = i * 2 * math.pi / count
            bump = .88 + rng.random() * .20
            verts.append((1.20 * sx * bump * math.cos(a), .90 * sy * bump * math.sin(a), h + (.04 * rng.random() if h else 0)))
    verts.append((0, 0, 1.22))
    faces, colors = [], []
    palette = ("stone", "stone_light", "stone", "stone_dark", "stone")
    for ring in range(3):
        for i in range(count):
            j = (i + 1) % count
            faces.append((ring * count + i, ring * count + j, (ring + 1) * count + j))
            colors.append(palette[(i + ring) % len(palette)])
            faces.append((ring * count + i, (ring + 1) * count + j, (ring + 1) * count + i))
            colors.append(palette[(i + ring + 1) % len(palette)])
    for i in range(count):
        faces.append((3 * count + i, 3 * count + (i + 1) % count, 4 * count))
        colors.append("stone_light" if i % 3 else "stone")
    mesh("Faceted_Rock", verts, faces, "stone", colors)
    for i in range(4):
        x = -.45 + i * .25
        line(f"Moss_Seam_{i}", ((x, -.78, .48), (x + .09, -.69, .66)), .009, "moss")


def w06():
    count = 24
    rng = random.Random(606)
    verts = []
    for z, r in ((0, 1.65), (.19, 1.70), (.24, 1.63)):
        for i in range(count):
            a = i * 2 * math.pi / count
            radius = r * (.987 + rng.random() * .024)
            verts.append((radius * math.cos(a), radius * math.sin(a), z))
    faces, colors = [], []
    for layer in range(2):
        for i in range(count):
            j = (i + 1) % count
            faces.append((layer * count + i, layer * count + j, (layer + 1) * count + j, (layer + 1) * count + i))
            colors.append("stone_dark" if i % 5 == 0 else "stone")
    faces.append(tuple(range(2 * count, 3 * count)))
    colors.append("stone_light")
    mesh("Stone_Disc", verts, faces, "stone", colors)
    for i, pts in enumerate((
        ((-.7, -.20, .243), (-.18, -.12, .243), (.32, -.30, .243)),
        ((.20, .38, .243), (.48, .12, .243), (.66, -.14, .243)),
        ((-.35, .70, .243), (-.26, .41, .243), (-.07, .20, .243)),
    )):
        line(f"Shallow_Crack_{i}", pts, .006, "stone_dark")


def w07():
    count = 8
    verts = []
    radius = .19 / math.cos(math.pi / 8)
    for y in (-.05, .05):
        for i in range(count):
            a = math.pi / 8 + i * 2 * math.pi / count
            verts.append((radius * math.sin(a), y, radius * math.cos(a)))
    faces = [tuple(range(count - 1, -1, -1)), tuple(range(count, 2 * count))]
    for i in range(count):
        faces.append((i, (i + 1) % count, count + (i + 1) % count, count + i))
    mesh("Octagonal_Stone", verts, faces, "stone_light", ["stone", "stone_light"] + ["stone_dark"] * 8)
    # The runtime camera sees the +Y side after FBX axis conversion.
    for i, points in enumerate((
        ((0, .056, .13), (0, .056, .07)),
        ((-.11, .056, .06), (-.065, .056, .02), (-.065, .056, -.06)),
        ((.11, .056, .06), (.065, .056, .02), (.065, .056, -.06)),
        ((0, .056, -.04), (0, .056, -.13)),
    )):
        line(f"Original_Mark_{i}", points, .006, "stone_dark")
    cylinder("Mark_Center", (0, .056, -.004), .022, .003, "stone_dark", 16, (math.pi / 2, 0, 0))


def w09():
    rng = random.Random(909)
    for i in range(7):
        angle = 2 * math.pi * i / 7
        dx, dy = math.cos(angle), math.sin(angle)
        length = .13 + (i % 3) * .024
        verts = []
        for t, width, z in ((0, .008, .02), (.36, .026, .14), (.70, .022, .28), (1, .001, .35)):
            x = dx * length * t
            y = dy * length * t
            verts.append(((x - dy * width) * 1.25, (y + dx * width) * .55, z))
            verts.append(((x + dy * width) * 1.25, (y - dx * width) * .55, z))
        faces = [(0, 1, 3, 2), (2, 3, 5, 4), (4, 5, 7, 6)]
        obj = mesh(f"Leaf_{i}", verts, faces, "leaf" if i % 2 else "leaf_light")
        obj.data.materials.append(mat("leaf"))
        for poly in obj.data.polygons:
            poly.use_smooth = False
        # A thin second side avoids camera-angle disappearance without alpha cards.
        mod = obj.modifiers.new("Solid_Leaf", "SOLIDIFY")
        mod.thickness = .005
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    cylinder("Root_Cluster", (0, 0, .025), .038, .05, "root", 10)


BUILD = {"P02": p02, "P03": p03, "P04": p04, "P05": p05, "P06": p06,
         "P07": p07, "P08": p08, "P09": p09, "W02": w02, "W03": w03,
         "W06": w06, "W07": w07, "W09": w09}


def preview(code, label, size):
    ground = bpy.data.materials.new("Preview_Gray_Ground")
    ground.diffuse_color = (.65, .68, .67, 1)
    if code not in ("W02", "W07"):
        bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, -.045))
        floor = bpy.context.object
        floor.name = "Preview_Floor_Not_Exported"
        floor.dimensions = (max(size[0] * 2, .5), max(size[2] * 2, .5), .09)
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        floor.data.materials.append(ground)
    world = bpy.context.scene.world
    world.use_nodes = True
    world.node_tree.nodes.get("Background").inputs["Color"].default_value = (.72, .76, .75, 1)
    world.node_tree.nodes.get("Background").inputs["Strength"].default_value = .45
    for name, pos, power in (("Warm_Key", (-2.5, -3.0, 4.0), 260), ("Cool_Fill", (2.8, 1.0, 3.0), 100)):
        data = bpy.data.lights.new(name, "AREA")
        data.energy = power
        data.size = 4
        light = bpy.data.objects.new(name, data)
        bpy.context.collection.objects.link(light)
        light.location = pos
        light.rotation_euler = (Vector((0, 0, size[1] * .42)) - light.location).to_track_quat("-Z", "Y").to_euler()
    cam_data = bpy.data.cameras.new("Preview_Camera")
    cam = bpy.data.objects.new("Preview_Camera", cam_data)
    bpy.context.collection.objects.link(cam)
    span = max(size)
    cam.location = (span * .9, -span * 1.9, max(size[1] * 1.35, span * .7))
    cam.rotation_euler = (Vector((0, 0, size[1] * .48)) - cam.location).to_track_quat("-Z", "Y").to_euler()
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = max(size[0] * 1.7, size[1] * 1.55, size[2] * 1.7)
    if code in ("P07", "W07"):
        cam.location = (size[0] * .6, -max(size[2], .2) * 4, size[1] * .75)
        cam.rotation_euler = (Vector((0, 0, size[1] * .5)) - cam.location).to_track_quat("-Z", "Y").to_euler()
    if code == "W02":
        cam.location = (.65, -.85, 1.6)
        cam.rotation_euler = (Vector((0, 0, 0)) - cam.location).to_track_quat("-Z", "Y").to_euler()
        cam_data.ortho_scale = 2.3
    bpy.context.scene.camera = cam
    bpy.context.scene.render.engine = "BLENDER_EEVEE_NEXT"
    bpy.context.scene.render.resolution_x = 800
    bpy.context.scene.render.resolution_y = 800
    bpy.context.scene.render.resolution_percentage = 100
    bpy.context.scene.render.image_settings.file_format = "PNG"
    bpy.context.scene.render.filepath = str(SOURCE / f"{code}_{label}_预览.png")
    bpy.context.scene.view_settings.view_transform = "AgX"
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / f"{code}_{label}.blend"))
    bpy.ops.render.render(write_still=True)


def build(code):
    label, target_size, limit = SPECS[code]
    reset(code)
    BUILD[code]()
    parts = [obj for obj in bpy.data.objects if obj.parent == root and obj.type == "MESH"]
    tris = 0
    points = []
    for obj in parts:
        obj.data.calc_loop_triangles()
        tris += len(obj.data.loop_triangles)
        points.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
    low = [min(p[i] for p in points) for i in range(3)]
    high = [max(p[i] for p in points) for i in range(3)]
    actual = (high[0] - low[0], high[2] - low[2], high[1] - low[1])
    if tris > limit:
        raise RuntimeError(f"{code} exceeds triangle limit: {tris} > {limit}")
    print(f"{code} {label}: {len(parts)} parts, {tris} triangles, Blender bounds {actual}")
    asset_dir = ASSETS / f"{code}_{label}"
    asset_dir.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for obj in parts:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.fbx(filepath=str(asset_dir / f"{code}_{label}.fbx"), use_selection=True,
                             object_types={"EMPTY", "MESH"}, axis_forward="-Z", axis_up="Y",
                             apply_unit_scale=True, path_mode="RELATIVE")
    preview(code, label, target_size)


for code in SPECS:
    build(code)
