"""3D chibi kit (Blender): the hero, enemies and bosses modelled from primitives, posed and rendered for pixelize.py.

Same looks as tools/art/hero.py + cast.py, built in 3D instead of painted: a big head (with helmet, hood, hat, cap,
ears or skull), a small body, two-segment arms and legs, weapons, and face features (eyes, blush, mouth) placed on the
face surface. One Blender unit is one art pixel. The body is turned 30 degrees toward the camera and the head another
45 so the face reads in 3/4 view while swings stay sideways. Every material is unlit emission: the surface normal is
dotted with charkit's key light (top-left, toward the viewer) and quantised into the part's 3-tone ramp. Each frame
is rendered at 4x twice: colour, and data (R = object index, G + B/255 = camera depth) for pixelize.py's lines.

Run: blender -b --factory-startup -P tools/art/chibi3d.py -- OUT_DIR ENTITY
Writes OUT_DIR/ENTITY/{clip}_{i}_{color,data}.png and OUT_DIR/ENTITY/meta.json. tools/art/build3d.py runs them all.
"""
import json
import math
import os
import random
import sys

import bmesh
import bpy
from bpy_extras.object_utils import world_to_camera_view
from mathutils import Euler, Vector

ARGS = sys.argv[sys.argv.index("--") + 1:]
OUT_ROOT, ENTITY = ARGS[0], ARGS[1]
SCALE = 4
LIGHT = (-0.55, 0.62, 0.56)  # charkit.LIGHT in camera space (x right, y up, z toward the viewer)
BODY_YAW, HEAD_YAW = -30.0, -45.0
CAM_DIST = 300.0
DEPTH_NEAR, DEPTH_FAR = CAM_DIST - 100.0, CAM_DIST + 100.0
CEL = (0.34, 0.9)  # hero.CEL
ROCK_CEL = (0.2, 0.62)

# ---------------------------------------------------------------- palette (sRGB hex, darkest first)

PAL = {
    "skin": ["#f2a58a", "#ffd6ba", "#fff1e4"],
    "goblin": ["#4f9a3c", "#7cc850", "#bdf08a"],
    "goblin_r": ["#b2463c", "#e8705a", "#ffb59a"],
    "bone": ["#b8aa98", "#eee6d6", "#ffffff"],
    "bone_v": ["#8a78b0", "#c6b6e6", "#f0e8ff"],
    "stem": ["#d8b890", "#f6e2c2", "#fff6e8"],
    "cap_r": ["#c2343e", "#ff5a5a", "#ff9a8a"],
    "cap_b": ["#3a5ac8", "#5a8cff", "#a8c8ff"],
    "shadow_face": ["#1e1430", "#2e2046", "#46345e"],
    "leather": ["#6b3a28", "#8f5236", "#b8744a"],
    "wood": ["#7a4a2a", "#a86a3c", "#d69858"],
    "steel": ["#8aa6cf", "#e2eeff", "#ffffff"],
    "gold": ["#d68a1c", "#ffc434", "#fff2a4"],
    "black": ["#241a30", "#3a2e4a", "#5a4a6a"],
    "red": ["#b8303c", "#ee4a52", "#ff8e86"],
    "straw": ["#b88a3a", "#e6c068", "#fff0a8"],
    "navy": ["#2a2e5a", "#3e4a86", "#6a7cc0"],
    "green_cloth": ["#2e6a3a", "#469a4e", "#7cc878"],
    "dark_purple": ["#2a1a44", "#43306a", "#6a50a0"],
    "brown": ["#6a4428", "#8e6038", "#b8844e"],
    "orb_green": ["#2a9a5a", "#5aff9a", "#dcffe8"],
    "orb_purple": ["#7a2ac8", "#c070ff", "#f4dcff"],
    "orb_fire": ["#e04a1c", "#ffa030", "#fff0a0"],
    "eye_white": ["#c8c0d8", "#ffffff", "#ffffff"],
    "wing": ["#3a2a5a", "#5a4488", "#8a70c0"],
    "wing_r": ["#6a2030", "#a83a48", "#e06a70"],
    # Hero (hero.py)
    "hair": ["#e0702c", "#ffa33c", "#ffd88a"],
    "tunic": ["#3a64d0", "#5c98ff", "#aad2ff"],
    "cape": ["#c2384c", "#ff5a62", "#ff9f8f"],
    "pants": ["#3d3769", "#5a5394", "#8078bf"],
    "boots": ["#8c4f2c", "#bb7342", "#e7a868"],
    "helm": ["#7a88b0", "#c4d2ea", "#f4f8ff"],
    "plume": ["#b8283c", "#ee4454", "#ff8a86"],
    "shield": ["#2e4fb0", "#4c7ae6", "#90b8ff"],
    # Promotion tiers (D-101)
    "helm_gold": ["#c8862a", "#ffd25a", "#fff4b0"],
    "helm_dark": ["#3a3050", "#5c4f80", "#8f80b8"],
    "plume_blue": ["#2a4ab0", "#4a7ae6", "#9ec0ff"],
    "plume_purple": ["#5a2a90", "#8a4ad0", "#c890ff"],
    "plume_white": ["#b8c0d8", "#f0f4ff", "#ffffff"],
    "tunic_white": ["#a8b0c8", "#e8ecf8", "#ffffff"],
    "tunic_purple": ["#3a2a6a", "#5a40a0", "#8a70d0"],
    "tunic_crimson": ["#8a1e2e", "#c83040", "#ff7080"],
    "cape_blue": ["#2a3a90", "#3e5ad0", "#7ea0ff"],
    "cape_purple": ["#3a1e5a", "#5e3290", "#9a6ad0"],
    "cape_gold": ["#b07018", "#f0b030", "#ffe08a"],
    "blade_crystal": ["#3a8ad0", "#8ae0ff", "#ffffff"],
    # Companions (D-102)
    "slime": ["#3a9a4a", "#6ad86a", "#c8ffb0"],
    "wisp": ["#4a8ae0", "#8ad0ff", "#e8fbff"],
    "owl": ["#6a4a30", "#9a6e48", "#c8a070"],
    "owl_face": ["#c8b8a0", "#f0e6d4", "#ffffff"],
    "beak": ["#d07a20", "#ffb040", "#ffe090"],
    "dragon": ["#a02a30", "#e04848", "#ff9a8a"],
    "belly": ["#d8a860", "#ffd890", "#fff4c8"],
    # Pets (D-114)
    "chick": ["#d9a520", "#ffd84a", "#fff3a8"],
    "bunny": ["#c8b8c8", "#f8eef4", "#ffffff"],
    "bunny_in": ["#d86a8a", "#ff9ab4", "#ffd0dc"],
    "frog": ["#3a8a3a", "#6ac85a", "#b8f090"],
    "frog_belly": ["#b8c880", "#e8f4b8", "#ffffff"],
    "bat": ["#5a4a7a", "#8a78b0", "#bcaee0"],
    "bat_wing": ["#3a2a52", "#5a4680", "#8a74b4"],
    "pig": ["#d8768e", "#ffaac0", "#ffdce6"],
    "snout": ["#c45a74", "#ee86a0", "#ffbccc"],
    "fox": ["#c4561c", "#ff8a3a", "#ffc890"],
    "fur_white": ["#c8c4d0", "#f8f6fc", "#ffffff"],
    "fur_dark": ["#4a2a1e", "#6e4030", "#9a6048"],
    "cat": ["#5a4a8a", "#8a78c8", "#c0b0f0"],
    "turtle": ["#2a8a7a", "#5ac8b0", "#a8f4e0"],
    "shell": ["#4e6020", "#86a038", "#c4d86a"],
    "phoenix": ["#c4302a", "#ff6a3a", "#ffc070"],
    "phoenix_wing": ["#d84a1a", "#ffa030", "#ffe68a"],
    "gumiho_red": ["#b0203a", "#ff4a6a", "#ffa0b0"],
    "foxfire": ["#3a8ae0", "#8ad0ff", "#e8fbff"],
    "azure": ["#14707e", "#2ccdbc", "#a6fff0"],
    "cloud": ["#c8d8e8", "#f4faff", "#ffffff"],
    # Golem
    "stone": ["#4e4462", "#8a809e", "#c8c0d2"],
    "stone_dark": ["#3e3652", "#6a6284", "#9e96b4"],
    "moss": ["#2e6a3a", "#5aa84e", "#a6dc70"],
    "crystal": ["#2a5ac8", "#4ac0ff", "#dcf8ff"],
    # D-131 chapter 6-10 bosses and recolours
    "sand": ["#a8743a", "#e0b070", "#fff0c0"],
    "basalt": ["#2e2430", "#54444e", "#86707a"],
    "basalt_dark": ["#22181e", "#3e2e36", "#5e4a52"],
    "lava": ["#c8301c", "#ff7a2a", "#ffd070"],
    "ember": ["#d0401a", "#ff8a30", "#fff0a0"],
    "swamp": ["#2a3e2a", "#46663e", "#76985a"],
    "ice_stone": ["#3a4e7a", "#6a8ac0", "#b8d4f4"],
    "ice_dark": ["#2a3a62", "#4a6094", "#7a94c8"],
    "crystal_pink": ["#a02a90", "#e070ff", "#ffd8ff"],
    "blade_blood": ["#8a1e2e", "#e04050", "#ffb0b8"],
    "goblin_s": ["#8a8a2a", "#c0c050", "#eef09a"],
    "bone_r": ["#b0705a", "#e8b098", "#fff0e8"],
    "cap_g": ["#4a6a2a", "#7aa83a", "#c0e070"],
    "wing_c": ["#2a4a7a", "#4a7ac0", "#8ab8f0"],
    # Flat face colours
    "eye": ["#2b1d3a"],
    "eye_low": ["#5a4a8a"],
    "eye_low_c": ["#3aa8e0"],
    "blush": ["#ff8f9c"],
    "mouth": ["#b8465a"],
    "white": ["#ffffff"],
    "flower": ["#ff8fb8"],
    "flower_mid": ["#ffe066"],
    "iris_b": ["#3a6ae0"],
    "iris_r": ["#e03a3a"],
    "iris_c": ["#2ab0e0"],
    "glow_o": ["#ff8a30"],
    "glow_y": ["#ffe066"],
    "glow_v": ["#e070ff"],
    "glow_g": ["#6affa0"],
    "smear0": ["#8cc4ff"],
    "smear1": ["#d8ecff"],
    "smear2": ["#ffffff"],
}
THRESH = {"stone": ROCK_CEL, "stone_dark": ROCK_CEL, "moss": ROCK_CEL, "crystal": (0.12, 0.5),
          "basalt": ROCK_CEL, "basalt_dark": ROCK_CEL, "lava": ROCK_CEL, "ember": (0.12, 0.5),
          "ice_stone": ROCK_CEL, "ice_dark": ROCK_CEL, "crystal_pink": (0.12, 0.5)}
NOLINES = ["smear0", "smear1", "smear2"]

# ---------------------------------------------------------------- looks (R = head radius in art pixels)

HERO = dict(R=13.0, frame=(96, 72), foot_x=40, mirror=False, folder="Hero", name="knight", skin="skin",
            head="helmet", body="tunic", cloth="tunic", legs="pants", boots="boots", weapon="sword", shield=True,
            cape=True, eyes="cute", hair=True)
# D-101 promotion looks: the same knight with tier colours (helm, plume, tunic, cape, shield, blade) and extras.
TIERS = {
    "knight1": dict(plume="plume_blue", cloth="tunic", cape_mat="cape_blue"),
    "knight2": dict(helm="helm_gold", plume="plume_white", cloth="tunic_white", cape_mat="cape", shield_mat="gold"),
    "knight3": dict(helm="helm_dark", plume="plume_purple", cloth="tunic_purple", cape_mat="cape_purple", blade="blade_crystal"),
    "knight4": dict(helm="helm_gold", plume="plume", cloth="tunic_crimson", cape_mat="cape_gold", shield_mat="gold",
                    blade="blade_crystal", wings=True),
}

LOOKS = {
    "knight": HERO,
    "goblin": dict(R=10.0, skin="goblin", head="goblin", body="tunic", cloth="brown", legs="leather", weapon="club", eyes="cute"),
    "goblinr": dict(R=10.0, skin="goblin_r", head="goblin", body="tunic", cloth="black", legs="leather", weapon="club", eyes="cute"),
    "skeleton": dict(R=10.0, skin="bone", head="skull", body="bones", cloth="bone", legs="bone", weapon="sword", eyes="socket", glow="glow_y"),
    "skeletonv": dict(R=10.0, skin="bone_v", head="skull", body="bones", cloth="bone_v", legs="bone_v", weapon="sword", eyes="socket", glow="glow_v"),
    "mushroom": dict(R=8.5, eye_el=-20.0, skin="stem", head="mushroom", cap="cap_r", body="tunic", cloth="stem", legs="stem", weapon="none", eyes="cute"),
    "mushroomb": dict(R=8.5, eye_el=-20.0, skin="stem", head="mushroom", cap="cap_b", body="tunic", cloth="stem", legs="stem", weapon="none", eyes="cute"),
    "flyeye": dict(R=9.0, kind="flyeye", iris="iris_b", wing="wing"),
    "flyeyer": dict(R=9.0, kind="flyeye", iris="iris_r", wing="wing_r"),
    "ronin": dict(R=17.0, skin="skin", head="straw_hat", body="tunic", cloth="navy", legs="black", weapon="katana", eyes="cute", brows=True, boss=True),
    "necro": dict(R=17.0, skin="bone", head="hood", hood="black", body="robe", cloth="black", legs="black", weapon="staff", orb="orb_green", eyes="socket", glow="glow_g", boss=True),
    "ranger": dict(R=17.0, skin="skin", head="hood", hood="green_cloth", body="tunic", cloth="green_cloth", legs="brown", weapon="bow", eyes="cute", brows=True, boss=True),
    "shadowmage": dict(R=17.0, skin="shadow_face", head="wizard", hat="dark_purple", body="robe", cloth="dark_purple", legs="dark_purple", weapon="staff", orb="orb_purple", eyes="glow", glow="glow_y", boss=True),
    "firemage": dict(R=17.0, skin="skin", head="wizard", hat="red", body="robe", cloth="red", legs="red", weapon="staff", orb="orb_fire", eyes="cute", boss=True),
    "golem": dict(kind="golem", boss=True),
    # D-131 chapter 6-10 bosses: existing parts in new materials.
    "sandking": dict(R=17.0, skin="skin", head="hood", hood="sand", body="tunic", cloth="sand", legs="brown", weapon="katana", eyes="cute", brows=True, boss=True),
    "lavagolem": dict(kind="golem", boss=True, mat_stone="basalt", mat_stone_dark="basalt_dark", mat_moss="lava", mat_crystal="ember", flowers=False),
    "swampwitch": dict(R=17.0, skin="goblin", head="wizard", hat="swamp", body="robe", cloth="swamp", legs="swamp", weapon="staff", orb="orb_green", eyes="cute", boss=True),
    "crystalgolem": dict(kind="golem", boss=True, mat_stone="ice_stone", mat_stone_dark="ice_dark", mat_moss="crystal", mat_crystal="crystal_pink", flowers=False),
    "demonknight": dict(R=17.0, skin="shadow_face", head="helmet", helm="helm_dark", plume="plume_purple", body="tunic", cloth="tunic_crimson",
                        legs="black", boots="boots", weapon="sword", blade="blade_blood", shield=True, shield_mat="black", cape=True,
                        cape_mat="cape_purple", eyes="glow", glow="glow_v", boss=True),
    # D-131 enemy recolours for the new chapters.
    "goblins": dict(R=10.0, skin="goblin_s", head="goblin", body="tunic", cloth="sand", legs="leather", weapon="club", eyes="cute"),
    "skeletonr": dict(R=10.0, skin="bone_r", head="skull", body="bones", cloth="bone_r", legs="bone_r", weapon="sword", eyes="socket", glow="glow_o"),
    "mushroomg": dict(R=8.5, eye_el=-20.0, skin="stem", head="mushroom", cap="cap_g", body="tunic", cloth="stem", legs="stem", weapon="none", eyes="cute"),
    "flyeyec": dict(R=9.0, kind="flyeye", iris="iris_c", wing="wing_c"),
}
for _name, _over in TIERS.items():
    LOOKS[_name] = dict(HERO, name=_name, **_over)

# D-104 job looks: hero-sized (frame, pivot and face as the knight), weapon-specific attack clips.
JOB_LOOKS = {
    "jobmage": dict(head="wizard", hat="tunic", body="robe", cloth="tunic", legs="tunic", weapon="staff", orb="orb_purple"),
    "jobpyro": dict(head="wizard", hat="red", body="robe", cloth="tunic_crimson", legs="red", weapon="staff", orb="orb_fire",
                    cape_mat="cape_gold", cape=True),
    "jobcryo": dict(head="wizard", hat="tunic_white", body="robe", cloth="cape_blue", legs="cape_blue", weapon="staff",
                    orb="wisp", cape_mat="plume_white", cape=True),
    "jobarcher": dict(head="hood", hood="green_cloth", body="tunic", cloth="green_cloth", legs="brown", weapon="bow"),
    "jobranger": dict(head="hood", hood="leather", body="tunic", cloth="green_cloth", legs="leather", weapon="bow",
                      cape_mat="green_cloth", cape=True),
    "jobsniper": dict(head="hood", hood="black", body="tunic", cloth="navy", legs="black", weapon="bow", cape_mat="black",
                      cape=True),
}
for _name, _over in JOB_LOOKS.items():
    _look = dict(HERO, name=_name, hair=True, shield=False, brows=False, cape=False)
    _look.update(_over)
    _look["job"] = True
    LOOKS[_name] = _look

# D-102 companions and D-114 pets: small creatures that float behind the hero; they face right like the hero (not
# mirrored). Species are drawn by build_pet.
PET_SPECIES = ("slime", "wisp", "owl", "dragon", "chick", "bunny", "frog", "bat", "piglet", "fox", "penguin", "cat",
               "turtle", "phoenix", "gumiho", "azure")
for _species in PET_SPECIES:
    LOOKS["pet" + _species] = dict(kind="pet", species=_species, R=8.0, frame=(48, 48), foot_x=24, mirror=False, folder="Pets")
LOOK = LOOKS[ENTITY]
if LOOK.get("boss"):
    LOOK.setdefault("frame", (128, 112))
    LOOK.setdefault("k", LOOK.get("R", 15.0) / 13.0 * 1.45)  # bosses get a bigger body as well as a bigger head (D-097)
LOOK.setdefault("frame", (80, 64))
LOOK.setdefault("foot_x", LOOK["frame"][0] // 2)
LOOK.setdefault("mirror", True)
LOOK.setdefault("folder", "Bosses" if LOOK.get("boss") else "Enemies")
LOOK.setdefault("name", ENTITY)
W, H = LOOK["frame"]


def lin(h):
    h = h.lstrip("#")
    c = [int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)]
    return tuple(x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c) + (1.0,)


def rad(d):
    return math.radians(d)


# ---------------------------------------------------------------- scene and camera

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.engine = "CYCLES"
scene.cycles.samples = 1
scene.cycles.use_denoising = False
scene.cycles.max_bounces = 0
scene.cycles.pixel_filter_type = "BOX"
scene.cycles.filter_width = 0.01
scene.render.film_transparent = True
scene.render.resolution_x = W * SCALE
scene.render.resolution_y = H * SCALE
scene.render.resolution_percentage = 100
scene.view_settings.look = "None"
scene.view_settings.exposure = 0.0
scene.view_settings.gamma = 1.0

cam_data = bpy.data.cameras.new("cam")
cam_data.type = "ORTHO"
cam_data.ortho_scale = float(W)
cam_data.clip_start, cam_data.clip_end = 1.0, CAM_DIST * 3
cam = bpy.data.objects.new("cam", cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.rotation_euler = Euler((rad(70.0), 0.0, 0.0))  # 20 degrees down
_rot = cam.rotation_euler.to_matrix()
_fwd, _up, _right = _rot @ Vector((0, 0, -1)), _rot @ Vector((0, 1, 0)), _rot @ Vector((1, 0, 0))
# The feet (world origin) sit on the bottom edge of the frame at column foot_x (charkit Canvas convention).
cam.location = -_fwd * CAM_DIST + _up * (H / 2.0) + _right * (W / 2.0 - LOOK["foot_x"])
LIGHT_WORLD = (_rot @ Vector(LIGHT)).normalized()
bpy.context.view_layer.update()

# ---------------------------------------------------------------- materials

_mats = {}


def mat(name):
    if name in _mats:
        return _mats[name]
    colors = PAL[name]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nodes, links = m.node_tree.nodes, m.node_tree.links
    nodes.clear()
    out = nodes.new("ShaderNodeOutputMaterial")
    em = nodes.new("ShaderNodeEmission")
    links.new(em.outputs["Emission"], out.inputs["Surface"])
    if len(colors) == 1:
        em.inputs["Color"].default_value = lin(colors[0])
    else:
        geo = nodes.new("ShaderNodeNewGeometry")
        dot = nodes.new("ShaderNodeVectorMath")
        dot.operation = "DOT_PRODUCT"
        dot.inputs[1].default_value = LIGHT_WORLD
        links.new(geo.outputs["Normal"], dot.inputs[0])
        ramp = nodes.new("ShaderNodeValToRGB")
        cr = ramp.color_ramp
        cr.interpolation = "CONSTANT"
        th = THRESH.get(name, CEL)
        cr.elements[0].position = 0.0
        cr.elements[0].color = lin(colors[0])
        cr.elements[1].position = th[0]
        cr.elements[1].color = lin(colors[1])
        cr.elements.new(th[1]).color = lin(colors[2])
        links.new(dot.outputs["Value"], ramp.inputs["Fac"])
        links.new(ramp.outputs["Color"], em.inputs["Color"])
    _mats[name] = m
    return m


def data_material():
    m = bpy.data.materials.new("data")
    m.use_nodes = True
    n, l = m.node_tree.nodes, m.node_tree.links
    n.clear()

    def math_node(op, b=None):
        node = n.new("ShaderNodeMath")
        node.operation = op
        if b is not None:
            node.inputs[1].default_value = b
        return node

    out = n.new("ShaderNodeOutputMaterial")
    em = n.new("ShaderNodeEmission")
    info = n.new("ShaderNodeObjectInfo")
    camd = n.new("ShaderNodeCameraData")
    mapr = n.new("ShaderNodeMapRange")
    mapr.inputs["From Min"].default_value = DEPTH_NEAR
    mapr.inputs["From Max"].default_value = DEPTH_FAR
    comb = n.new("ShaderNodeCombineColor")
    idx = math_node("DIVIDE", 255.0)
    v = math_node("MULTIPLY", 255.0)
    hi = math_node("FLOOR")
    lo = math_node("SUBTRACT")
    g = math_node("DIVIDE", 255.0)
    l.new(info.outputs["Object Index"], idx.inputs[0])
    l.new(idx.outputs["Value"], comb.inputs[0])
    l.new(camd.outputs["View Z Depth"], mapr.inputs["Value"])
    l.new(mapr.outputs["Result"], v.inputs[0])
    l.new(v.outputs["Value"], hi.inputs[0])
    l.new(v.outputs["Value"], lo.inputs[0])
    l.new(hi.outputs["Value"], lo.inputs[1])
    l.new(hi.outputs["Value"], g.inputs[0])
    l.new(g.outputs["Value"], comb.inputs[1])
    l.new(lo.outputs["Value"], comb.inputs[2])
    l.new(comb.outputs["Color"], em.inputs["Color"])
    l.new(em.outputs["Emission"], out.inputs["Surface"])
    return m


DATA_MAT = data_material()

# ---------------------------------------------------------------- primitives

rng = random.Random(7)
_next_id = [1]


def new_id():
    _next_id[0] += 1
    return _next_id[0] - 1


def empty(name, parent=None, loc=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1)):
    ob = bpy.data.objects.new(name, None)
    scene.collection.objects.link(ob)
    ob.parent = parent
    ob.location = loc
    ob.rotation_euler = rot
    ob.scale = scale
    return ob


def finish(ob, material, parent, loc, scale=(1, 1, 1), rot=(0, 0, 0), smooth=True, line_id=None):
    ob.data.materials.clear()
    ob.data.materials.append(mat(material))
    if hasattr(ob.data, "polygons"):
        for poly in ob.data.polygons:
            poly.use_smooth = smooth
    ob.parent = parent
    ob.location = loc
    ob.scale = scale
    ob.rotation_euler = rot
    ob.pass_index = new_id() if line_id is None else line_id
    return ob


def ellip(material, parent, loc, radii, rot=(0, 0, 0), line_id=None):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=1.0)
    return finish(bpy.context.active_object, material, parent, loc, radii, rot, line_id=line_id)


def rock(material, parent, loc, radius, scale=(1, 1, 1), subdiv=2, jitter=0.07, line_id=None):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdiv, radius=radius)
    ob = bpy.context.active_object
    for v in ob.data.vertices:
        v.co *= 1.0 + rng.uniform(-jitter, jitter)
    return finish(ob, material, parent, loc, scale, smooth=False, line_id=line_id)


def cone(material, parent, loc, r1, r2, depth, rot=(0, 0, 0), verts=24, line_id=None, smooth=True):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r1, radius2=r2, depth=depth)
    return finish(bpy.context.active_object, material, parent, loc, rot=rot, line_id=line_id, smooth=smooth)


def cyl(material, parent, loc, r, depth, rot=(0, 0, 0), line_id=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=r, depth=depth)
    return finish(bpy.context.active_object, material, parent, loc, rot=rot, line_id=line_id)


def torus(material, parent, loc, major, minor, rot=(0, 0, 0), line_id=None):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, major_segments=32, minor_segments=8)
    return finish(bpy.context.active_object, material, parent, loc, rot=rot, line_id=line_id)


def box(material, parent, loc, size, rot=(0, 0, 0), line_id=None, taper=None):
    """Box of `size`; taper (0..1) narrows the +X end (blade tips)."""
    bpy.ops.mesh.primitive_cube_add(size=1.0)
    ob = bpy.context.active_object
    if taper is not None:
        for v in ob.data.vertices:
            if v.co.x > 0:
                v.co.y *= taper
                v.co.z *= taper
    return finish(ob, material, parent, loc, size, rot, smooth=False, line_id=line_id)


def tube(material, parent, points, radius, line_id=None):
    cu = bpy.data.curves.new("tube", "CURVE")
    cu.dimensions = "3D"
    cu.bevel_depth = radius
    cu.bevel_resolution = 2
    sp = cu.splines.new("POLY")
    sp.points.add(len(points) - 1)
    for p, co in zip(sp.points, points):
        p.co = (co[0], co[1], co[2], 1.0)
    sp.use_smooth = True
    ob = bpy.data.objects.new("tube", cu)
    scene.collection.objects.link(ob)
    return finish(ob, material, parent, (0, 0, 0), line_id=line_id)


def bipyramid(material, parent, loc, length, radius, tilt, line_id=None):
    me = bpy.data.meshes.new("bip")
    bm = bmesh.new()
    top = bm.verts.new((0, 0, length))
    bot = bm.verts.new((0, 0, -length * 0.35))
    ring = [bm.verts.new((math.cos(a) * radius, math.sin(a) * radius, 0)) for a in
            (i / 5 * math.tau + 0.3 for i in range(5))]
    for i in range(5):
        a, b = ring[i], ring[(i + 1) % 5]
        bm.faces.new((a, b, top))
        bm.faces.new((b, a, bot))
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new("bip", me)
    scene.collection.objects.link(ob)
    return finish(ob, material, parent, loc, rot=tuple(rad(t) for t in tilt), smooth=False, line_id=line_id)


def sector(material, parent, loc, a0, a1, r0, r1):
    """Flat ring sector in the parent's XZ plane (angles in degrees, 0 = +X, 90 = +Z)."""
    me = bpy.data.meshes.new("sector")
    bm = bmesh.new()
    n = 16
    inner, outer = [], []
    for i in range(n + 1):
        a = rad(a0 + (a1 - a0) * i / n)
        inner.append(bm.verts.new((math.cos(a) * r0, 0, math.sin(a) * r0)))
        outer.append(bm.verts.new((math.cos(a) * r1, 0, math.sin(a) * r1)))
    for i in range(n):
        bm.faces.new((inner[i], inner[i + 1], outer[i + 1], outer[i]))
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new("sector", me)
    scene.collection.objects.link(ob)
    return finish(ob, material, parent, loc)


# ---------------------------------------------------------------- face


class Face:
    """An ellipsoid surface (in the head pivot's space) that face features sit on; front is +X."""

    def __init__(self, pivot, center, radii, line_id):
        self.pivot, self.c, self.r, self.id = pivot, Vector(center), radii, line_id

    def at(self, az, el, lift=0.0):
        a, e = rad(az), rad(el)
        d = Vector((math.cos(e) * math.cos(a), math.cos(e) * math.sin(a), math.sin(e)))
        p = Vector((d.x * self.r[0], d.y * self.r[1], d.z * self.r[2]))
        n = Vector((d.x / self.r[0], d.y / self.r[1], d.z / self.r[2])).normalized()
        return self.c + p + n * lift, n

    def decal(self, material, az, el, size, lift=0.0):
        pos, n = self.at(az, el, lift)
        q = n.to_track_quat("X", "Z")
        return ellip(material, self.pivot, pos, size, q.to_euler(), line_id=self.id)


class Eyes:
    def __init__(self, face, kind, R, glow=None, low="eye_low", az=22.0, el=-10.0, w=0.19, h=0.3):
        self.face, self.kind, self.R = face, kind, R
        self.items = []
        color = {"cute": "eye", "socket": "eye", "glow": glow}[kind]
        if kind == "socket":
            w, h = w * 1.25, h * 1.0
        if kind == "glow":
            h *= 0.6
        self.w, self.h = w, h
        for sgn in (-1, 1):
            eye = face.decal(color, sgn * az, el, (0.06 * R, w * R, h * R))
            lowp = None
            if kind == "cute":
                lowp = face.decal(low, sgn * az, el - h * 22.0, (0.05 * R, w * R * 0.75, h * R * 0.3), lift=0.03 * R)
            hi, _ = face.at(sgn * az + 4.0, el + h * 26.0, lift=0.2 * R)
            self.items.append((eye, lowp, hi))

    def set(self, state):
        open_ = state in ("open", "angry")
        for eye, low, _ in self.items:
            hh = self.h if state == "open" else (self.h * 0.65 if state == "angry" else 0.07)
            eye.scale = (0.06 * self.R, self.w * self.R * (1.1 if not open_ else 1.0), hh * self.R)
            if low is not None:
                low.hide_render = not open_ or state == "angry"

    def stamps(self, state):
        if state not in ("open", "angry") or self.kind == "glow":
            return []
        out = []
        for i, (_, _, hi) in enumerate(self.items):
            p = to_px(self.face.pivot.matrix_world @ hi)
            if self.kind == "socket":
                out.append([p[0], p[1], PAL[LOOK["glow"]][0], 2 if i == 0 else 1])
            else:
                out.append([p[0], p[1], "#ffffff", 2 if i == 0 else 1])
        return out


# ---------------------------------------------------------------- humanoid

class Rig:
    pass


def build_humanoid(look):
    R = look["R"]
    k = look.get("k", R / 13.0)
    rig = Rig()
    rig.k, rig.R = k, R
    root = empty("root", rot=(0, 0, rad(BODY_YAW)))
    rig.fall = empty("fall", root)
    rig.hip = empty("hip", rig.fall)
    rig.hip_z = 6.0 * k
    cloth, legs = look["cloth"], look["legs"]
    skin = look["skin"]
    bones = look["body"] == "bones"
    limb_r = (1.3 if bones else 2.0) * k

    # Body
    if bones:
        ellip(skin, rig.hip, (0, 0, 4.8 * k), (4.0 * k, 3.6 * k, 4.4 * k))
        cyl(skin, rig.hip, (0, 0, 1.0 * k), 1.0 * k, 3.0 * k)
    else:
        ellip(cloth, rig.hip, (0, 0, 4.4 * k), (5.4 * k, 5.0 * k, 5.4 * k))
        ellip(look.get("belt", "leather"), rig.hip, (0, 0, 1.8 * k), (5.2 * k, 4.8 * k, 1.2 * k))
    if look["body"] == "robe":
        cone(cloth, rig.hip, (0, 0, -rig.hip_z * 0.5 + 2.2 * k), 7.2 * k, 4.6 * k, rig.hip_z + 4.4 * k)
    if look.get("cape"):
        rig.cape = empty("cape", rig.hip, (-2.6 * k, 0, 8.4 * k))
        ellip(look.get("cape_mat", "cape"), rig.cape, (-0.6 * k, 0, -4.6 * k), (1.0 * k, 4.6 * k, 5.6 * k))

    # Legs: thigh + shin from the hip, a boot kept level.
    rig.legs = {}
    leg_len = (rig.hip_z - 1.2 * k) / 2.0
    for side, y in (("front", -2.6 * k), ("back", 2.6 * k)):
        thigh = empty("thigh_" + side, rig.hip, (0.2 * k, y, 0.6 * k))
        ellip(legs, thigh, (leg_len / 2, 0, 0), (leg_len / 2 + limb_r * 0.6, limb_r, limb_r))
        shin = empty("shin_" + side, thigh, (leg_len, 0, 0))
        ellip(legs, shin, (leg_len / 2, 0, 0), (leg_len / 2 + limb_r * 0.6, limb_r, limb_r))
        boot = empty("boot_" + side, shin, (leg_len, 0, 0))
        ellip(look.get("boots", legs if bones else "leather"), boot, (0.8 * k, 0, 0.1 * k), (2.3 * k, 1.7 * k, 1.3 * k))
        rig.legs[side] = (thigh, shin, boot)

    # Arms: upper + forearm from the shoulder, a hand at the end.
    rig.arms = {}
    arm_len = 3.0 * k
    for side, y, x in (("front", -5.2 * k, 0.4 * k), ("back", 5.2 * k, -0.4 * k)):
        up = empty("upper_" + side, rig.hip, (x, y, 7.4 * k))
        ellip(cloth if not bones else skin, up, (arm_len / 2, 0, 0), (arm_len / 2 + limb_r * 0.6, limb_r, limb_r))
        fore = empty("fore_" + side, up, (arm_len, 0, 0))
        ellip(cloth if not bones else skin, fore, (arm_len / 2, 0, 0), (arm_len / 2 + limb_r * 0.6, limb_r, limb_r))
        hand = empty("hand_" + side, fore, (arm_len, 0, 0))
        ellip(skin, hand, (0.4 * k, 0, 0), (2.1 * k, 2.1 * k, 2.1 * k))
        rig.arms[side] = (up, fore, hand)

    # Head
    rig.neck = empty("neck", rig.hip, (0.6 * k, 0, 8.8 * k))
    rig.head = empty("head", rig.neck, rot=(0, 0, rad(HEAD_YAW)))
    hc = (0.0, 0.0, R * 0.92)
    head_r = (R, R * 0.97, R * 0.93)
    head_id = new_id()
    ellip(skin, rig.head, hc, head_r, line_id=head_id)
    face = Face(rig.head, hc, head_r, head_id)
    kind = look["head"]
    if kind in ("helmet", "hood"):
        shell = look.get("helm", "helm") if kind == "helmet" else look["hood"]
        ellip(shell, rig.head, (hc[0] - 0.06 * R, 0, hc[2] + 0.06 * R), (1.1 * R, 1.08 * R, 1.06 * R))
        fc = (hc[0] + 0.3 * R, 0, hc[2] - 0.12 * R)
        fr = (0.9 * R, 0.92 * R, 0.86 * R)
        face_id = new_id()
        ellip(skin, rig.head, fc, fr, line_id=face_id)
        face = Face(rig.head, fc, fr, face_id)
        if kind == "helmet":
            ellip(look.get("plume", "plume"), rig.head, (hc[0] - 0.15 * R, 0, hc[2] + 1.12 * R), (0.62 * R, 0.2 * R, 0.32 * R), (0, rad(-15), 0))
            if look.get("wings"):
                for sgn in (-1, 1):
                    ellip("plume_white", rig.head, (hc[0] - 0.25 * R, sgn * 1.02 * R, hc[2] + 0.45 * R),
                          (0.5 * R, 0.1 * R, 0.24 * R), (sgn * rad(-25), rad(-35), 0))
        if look.get("hair"):
            ellip("hair", rig.head, (fc[0] + 0.25 * R, 0, fc[2] + 0.5 * R), (0.32 * R, 0.55 * R, 0.16 * R), (0, rad(-25), 0), line_id=face_id)
        if kind == "hood":
            cone(shell, rig.head, (hc[0] - 0.75 * R, 0, hc[2] + 0.55 * R), 0.45 * R, 0.0, 0.9 * R, (0, rad(-60), 0))
    elif kind == "goblin":
        for sgn in (-1, 1):
            cone(skin, rig.head, (hc[0] - 0.05 * R, sgn * 0.95 * R, hc[2] + 0.1 * R), 0.32 * R, 0.0, 1.0 * R,
                 (sgn * rad(-70), 0, 0))
    elif kind == "mushroom":
        cap_c = (hc[0] - 0.1 * R, 0, hc[2] + 0.86 * R)
        cap_r = (1.38 * R, 1.38 * R, 0.72 * R)
        cap_id = new_id()
        ellip(look["cap"], rig.head, cap_c, cap_r, line_id=cap_id)
        cap = Face(rig.head, cap_c, cap_r, cap_id)
        for az, el, s in ((20, 30, 0.28), (-35, 45, 0.22), (-80, 25, 0.25), (60, 55, 0.2), (150, 35, 0.25)):
            cap.decal("white", az, el, (0.05 * R, s * R, s * R * 0.8))
    elif kind == "straw_hat":
        base = hc[2] + 0.62 * R
        cone("straw", rig.head, (hc[0] - 0.05 * R, 0, base + 0.35 * R), 1.6 * R, 0.08 * R, 0.7 * R, verts=32)
        torus("red", rig.head, (hc[0] - 0.05 * R, 0, base + 0.24 * R), 1.12 * R, 0.09 * R)
    elif kind == "wizard":
        brim_z = hc[2] + 0.7 * R
        cyl(look["hat"], rig.head, (hc[0] - 0.05 * R, 0, brim_z), 1.55 * R, 0.14 * R, (0, rad(-8), 0))
        tip = empty("hat_tip", rig.head, (hc[0] - 0.1 * R, 0, brim_z), (0, rad(-18), 0))
        cone(look["hat"], tip, (0, 0, 0.85 * R), 0.95 * R, 0.08 * R, 1.7 * R)
        ellip("gold", rig.head, (hc[0] + 0.85 * R, 0, brim_z + 0.3 * R), (0.12 * R, 0.2 * R, 0.2 * R))

    rig.eyes = Eyes(face, look["eyes"], R, glow=look.get("glow"), el=look.get("eye_el", -10.0), w=0.17, h=0.27)
    face.decal("blush", -44, -24, (0.04 * R, 0.17 * R, 0.09 * R))
    face.decal("blush", 44, -24, (0.04 * R, 0.17 * R, 0.09 * R))
    rig.mouth = face.decal("mouth", 0, -30, (0.05 * R, 0.08 * R, 0.05 * R))
    rig.mouth_base = (0.05 * R, 0.08 * R, 0.05 * R)
    if look.get("brows"):
        rig.brows = [face.decal("eye", sgn * 22, 18, (0.04 * R, 0.17 * R, 0.05 * R)) for sgn in (-1, 1)]

    # Weapons
    rig.weapon = None
    hand_f, hand_b = rig.arms["front"][2], rig.arms["back"][2]
    w = look.get("weapon", "none")
    if w in ("sword", "club", "katana", "staff"):
        rig.weapon = empty("weapon", hand_f)
        wp = rig.weapon
        if w == "sword":
            # D-108: a longer, broader blade and guard so the sword reads at game scale.
            cyl("leather", wp, (0, 0, 0), 0.85 * k, 3.4 * k, (0, rad(90), 0))
            box("gold", wp, (2.0 * k, 0, 0), (1.1 * k, 1.4 * k, 5.0 * k))
            box(look.get("blade", "steel"), wp, (8.9 * k, 0, 0), (12.8 * k, 0.7 * k, 3.1 * k), taper=0.35)
        elif w == "club":
            cone("wood", wp, (4.2 * k, 0, 0), 1.0 * k, 2.4 * k, 10.0 * k, (0, rad(90), 0))
        elif w == "katana":
            cyl("black", wp, (0, 0, 0), 0.7 * k, 4.0 * k, (0, rad(90), 0))
            cyl("gold", wp, (2.2 * k, 0, 0), 1.3 * k, 0.4 * k, (0, rad(90), 0))
            box("steel", wp, (9.4 * k, 0, 0.3 * k), (13.5 * k, 0.5 * k, 1.6 * k), (0, rad(-3), 0), taper=0.3)
        else:
            cyl("wood", wp, (3.5 * k, 0, 0), 0.75 * k, 24.0 * k, (0, rad(90), 0))
            rig.orb = ellip(look["orb"], wp, (17.0 * k, 0, 0), (3.1 * k, 3.1 * k, 3.1 * k))
            torus("gold", wp, (14.8 * k, 0, 0), 1.5 * k, 0.45 * k, (0, rad(90), 0))
    if w == "bow":
        bow = empty("bow", hand_b)
        pts = [(1.0 * k + 3.8 * k * math.cos(rad(a)), 0, 10.5 * k * math.sin(rad(a))) for a in range(-90, 91, 10)]
        tube("wood", bow, pts, 0.9 * k)
        tube("white", bow, [(1.0 * k, 0, -10.5 * k), (1.0 * k, 0, 10.5 * k)], 0.22 * k)
        rig.arrow = empty("arrow", bow)
        tube("wood", rig.arrow, [(-10.0 * k, 0, 0), (5.0 * k, 0, 0)], 0.4 * k)
        cone("steel", rig.arrow, (5.6 * k, 0, 0), 1.1 * k, 0.0, 2.2 * k, (0, rad(90), 0), verts=6, smooth=False)
        rig.bow = bow
    if look.get("shield"):
        sh = empty("shield", hand_b)
        cyl(look.get("shield_mat", "shield"), sh, (0, -1.2 * k, 0), 4.6 * k, 1.0 * k, (rad(90), 0, 0))
        torus("gold", sh, (0, -1.8 * k, 0), 4.6 * k, 0.55 * k, (rad(90), 0, 0))
        ellip("gold", sh, (0, -2.0 * k, 0), (1.2 * k, 0.6 * k, 1.2 * k))
        rig.shield = sh
    return rig


def set_seg(chain, a1, a2, keep_level=None):
    """Two-segment limb with absolute angles (degrees, 0 = forward, 90 = up) in the body's plane."""
    first, second, end = chain
    first.rotation_euler = (0, -rad(a1), 0)
    second.rotation_euler = (0, -rad(a2 - a1), 0)
    if keep_level is not None:
        end.rotation_euler = (0, rad(a2) + rad(keep_level), 0)


def pose_humanoid(rig, look, p):
    k = rig.k
    t = p["fall"] / 90.0
    rig.fall.rotation_euler = (0, -rad(p["fall"]), 0)  # backward
    # Slide forward while falling so the body lying behind the feet stays inside the frame.
    rig.fall.location = (t * 11.0 * k, 0, t * 4.5 * k)
    rig.hip.location = (p["bx"] * k, 0, rig.hip_z + p["by"])
    rig.hip.rotation_euler = (0, rad(p["lean"]), 0)
    sq = p["squash"]
    rig.hip.scale = (1 + sq * 0.03, 1 + sq * 0.03, 1 - sq * 0.05)
    rig.neck.rotation_euler = (0, rad(p.get("nod", 0.0)), 0)
    set_seg(rig.legs["front"], *p["fl"], keep_level=0.0)
    set_seg(rig.legs["back"], *p["bl"], keep_level=0.0)
    set_seg(rig.arms["front"], *p["fa"])
    set_seg(rig.arms["back"], *p["ba"])
    if rig.weapon is not None:
        rig.weapon.rotation_euler = (0, rad(p["fa"][1]) - rad(p["weapon"]), 0)
    if hasattr(rig, "orb"):
        s = 3.1 * k * (1.0 + 0.12 * p["glow"])
        rig.orb.scale = (s, s, s)
    if hasattr(rig, "bow"):
        rig.bow.rotation_euler = (0, rad(p["ba"][1]), 0)  # keep the bow upright
        rig.arrow.hide_render = p["draw"] < 0.2
        rig.arrow.location = (-p["draw"] * 3.0 * k, 0, 0)
        for ob in rig.arrow.children:
            ob.hide_render = p["draw"] < 0.2
    if hasattr(rig, "shield"):
        rig.shield.rotation_euler = (0, rad(p["ba"][1]), 0)
    if hasattr(rig, "cape"):
        rig.cape.rotation_euler = (0, -rad(p["cape"] * 7.0), 0)
    rig.eyes.set(p["eyes"])
    mx, my, mz = rig.mouth_base
    m = p["mouth"]
    rig.mouth.scale = (mx, my * (1.4 if m == "grit" else 1.0), mz * (2.2 if m == "open" else (0.7 if m == "grit" else 1.0)))


# ---------------------------------------------------------------- flying eye

# D-114: pets that sit and hop on the ground (the rest float and flap).
HOPPERS = ("slime", "chick", "bunny", "frog", "piglet", "fox", "penguin")

# Body material and radii (x front, y side, z up, in R); the D-102 four keep their original shapes exactly.
PET_BODY = {
    "slime": ("slime", (1.1, 1.0, 0.85)),
    "wisp": ("wisp", (1.0, 0.95, 1.0)),
    "owl": ("owl", (1.0, 0.95, 1.0)),
    "dragon": ("dragon", (1.0, 0.95, 1.0)),
    "chick": ("chick", (1.0, 0.95, 0.95)),
    "bunny": ("bunny", (1.0, 0.95, 0.92)),
    "frog": ("frog", (1.1, 1.05, 0.78)),
    "bat": ("bat", (0.95, 0.9, 0.95)),
    "piglet": ("pig", (1.05, 1.0, 0.92)),
    "fox": ("fox", (1.0, 0.92, 0.95)),
    "penguin": ("navy", (0.9, 0.85, 1.12)),
    "cat": ("cat", (1.0, 0.95, 0.95)),
    "turtle": ("turtle", (1.0, 0.92, 0.88)),
    "phoenix": ("phoenix", (1.0, 0.95, 1.0)),
    "gumiho": ("fur_white", (1.0, 0.92, 0.95)),
    "azure": ("azure", (1.0, 0.95, 1.0)),
}

# Flapping parts: material, size factor, height offset (in R). Wings on the back, flippers low on the sides.
PET_WINGS = {
    "wisp": ("white", 1.0, 0.0),
    "owl": ("owl", 1.0, 0.0),
    "dragon": ("wing_r", 1.0, 0.0),
    "chick": ("chick", 0.6, -0.15),
    "bat": ("bat_wing", 1.25, 0.05),
    "penguin": ("navy", 0.75, -0.35),
    "turtle": ("turtle", 0.8, -0.3),
    "phoenix": ("phoenix_wing", 1.45, 0.1),
}


def ear_pair(material, body, R, x, y, z, r, depth, tilt_y=-10.0, tilt_x=25.0, inner=None):
    """Two pointed ears (cones) on top of the head; an optional inner colour as a smaller cone in front."""
    for sgn in (-1, 1):
        rot = (sgn * rad(-tilt_x), rad(tilt_y), 0)
        cone(material, body, (x * R, sgn * y * R, z * R), r * R, 0.0, depth * R, rot)
        if inner is not None:
            cone(inner, body, ((x + 0.06) * R, sgn * y * R, (z - 0.02) * R), r * 0.55 * R, 0.0, depth * 0.75 * R, rot)


def bushy_tail(material, tip, body, R, base, angle, steps, size, curl=25.0, step=0.42):
    """
    A tail of overlapping balls from `base` (body frame, in R) toward the viewer's left (body -Y, the pet faces right),
    leaving at `angle` degrees above the horizontal and curling `curl` degrees further up per ball; the last ball in
    the tip colour. A tail along body -X would hide behind the body.
    """
    x, y, z = base
    a = angle
    for i in range(steps):
        r = size * (1.0 - 0.1 * i) * R
        if i > 0:
            x -= 0.12
            y -= step * math.cos(rad(a))
            z += step * math.sin(rad(a))
            a += curl
        ellip(tip if (tip is not None and i == steps - 1) else material, body, (x * R, y * R, z * R), (r, r * 0.9, r))


def build_pet(look):
    """D-102 / D-114 pet: a round body with cute eyes and species parts."""
    R = look["R"]
    sp = look["species"]
    rig = Rig()
    rig.k, rig.R = 1.0, R
    root = empty("root", rot=(0, 0, rad(-78.0)))
    rig.fall = empty("fall", root)
    rig.hip = empty("hip", rig.fall)
    body_mat, f = PET_BODY[sp]
    radii = (R * f[0], R * f[1], R * f[2])
    rig.hip_z = R * f[2] / 0.85 if sp in HOPPERS else R + 6.0
    if sp == "slime":
        rig.hip_z = R
    rig.body = empty("body", rig.hip)
    body_id = new_id()
    ellip(body_mat, rig.body, (0, 0, 0), radii, line_id=body_id)
    face = Face(rig.body, (0, 0, 0), radii, body_id)
    eye_az, eye_el = 24.0, -6.0

    if sp == "owl":
        face_id = new_id()
        fc, fr = (0.45 * R, 0, 0.1 * R), (0.62 * R, 0.8 * R, 0.62 * R)
        ellip("owl_face", rig.body, fc, fr, line_id=face_id)
        face = Face(rig.body, fc, fr, face_id)
        for sgn in (-1, 1):
            cone("owl", rig.body, (-0.1 * R, sgn * 0.55 * R, 0.85 * R), 0.25 * R, 0.0, 0.6 * R, (sgn * rad(-25), rad(-10), 0))
        cone("beak", rig.body, (fc[0] + 0.6 * R, 0, fc[2] - 0.15 * R), 0.16 * R, 0.0, 0.35 * R, (0, rad(110), 0), verts=6, smooth=False)
    if sp == "dragon":
        ellip("belly", rig.body, (0.55 * R, 0, -0.3 * R), (0.45 * R, 0.6 * R, 0.55 * R))
        for sgn in (-1, 1):
            cone("bone", rig.body, (-0.05 * R, sgn * 0.45 * R, 0.85 * R), 0.16 * R, 0.0, 0.55 * R, (sgn * rad(-15), rad(-25), 0))
        cone("dragon", rig.body, (-1.05 * R, 0, -0.35 * R), 0.35 * R, 0.05 * R, 1.0 * R, (0, rad(-110), 0))
    if sp == "wisp":
        for i in range(3):
            ellip("wisp", rig.body, (-(1.1 + i * 0.45) * R, 0, -0.15 * R * i), (0.4 * R * (1 - i * 0.25),) * 3)

    if sp == "chick":
        cone("beak", rig.body, (0.98 * R, 0, -0.12 * R), 0.16 * R, 0.0, 0.32 * R, (0, rad(95), 0), verts=6, smooth=False)
        for i, (dy, tilt) in enumerate(((-0.12, -25.0), (0.0, 0.0), (0.12, 25.0))):
            cone("chick", rig.body, (0.05 * R, dy * R, 0.98 * R), 0.09 * R, 0.0, 0.42 * R, (rad(tilt), rad(-20), 0))
    if sp == "bunny":
        for sgn in (-1, 1):
            rot = (sgn * rad(-12), rad(-22), 0)
            ellip("bunny", rig.body, (-0.12 * R, sgn * 0.32 * R, 1.35 * R), (0.2 * R, 0.17 * R, 0.62 * R), rot)
            ellip("bunny_in", rig.body, (-0.04 * R, sgn * 0.32 * R, 1.38 * R), (0.1 * R, 0.09 * R, 0.48 * R), rot)
        ellip("bunny", rig.body, (-0.98 * R, 0, -0.25 * R), (0.26 * R,) * 3)
    if sp == "frog":
        ellip("frog_belly", rig.body, (0.55 * R, 0, -0.32 * R), (0.55 * R, 0.75 * R, 0.42 * R))
        for sgn in (-1, 1):
            # Big eye bumps on top: the frog silhouette.
            ellip("frog", rig.body, (0.3 * R, sgn * 0.5 * R, 0.72 * R), (0.4 * R, 0.38 * R, 0.38 * R))
            ellip("eye_white", rig.body, (0.58 * R, sgn * 0.52 * R, 0.82 * R), (0.16 * R, 0.24 * R, 0.24 * R))
        eye_el = 2.0
    if sp == "bat":
        ear_pair("bat", rig.body, R, -0.05, 0.42, 0.82, 0.24, 0.62, inner="bunny_in")
    if sp == "piglet":
        # The face turns 3/4 to the right, so the snout must be big to read from the side.
        cyl("snout", rig.body, (1.12 * R, 0, -0.34 * R), 0.46 * R, 0.55 * R, (0, rad(90), 0))
        for sgn in (-1, 1):
            ellip("mouth", rig.body, (1.4 * R, sgn * 0.16 * R, -0.34 * R), (0.05 * R, 0.1 * R, 0.14 * R))
        ear_pair("pig", rig.body, R, 0.05, 0.55, 0.78, 0.26, 0.42, tilt_y=15.0, tilt_x=45.0, inner="snout")
        torus("pig", rig.body, (-0.3 * R, -1.0 * R, -0.1 * R), 0.2 * R, 0.07 * R, (rad(90), 0, 0))
        eye_el = 8.0
    if sp in ("fox", "gumiho"):
        fur = "fox" if sp == "fox" else "fur_white"
        mark = "fur_white" if sp == "fox" else "gumiho_red"
        ellip(mark if sp == "fox" else "fur_white", rig.body, (0.55 * R, 0, -0.32 * R), (0.5 * R, 0.66 * R, 0.5 * R))
        ear_pair(fur, rig.body, R, -0.05, 0.45, 0.8, 0.27, 0.72, inner="fur_dark" if sp == "fox" else "gumiho_red")
        if sp == "fox":
            bushy_tail("fox", "fur_white", rig.body, R, (-0.45, -0.85, -0.35), 20.0, 4, 0.42, curl=28.0)
        else:
            # Five tails fanned out on the far side, red tipped, and a blue fox-fire beside the head.
            for angle in (-10.0, 15.0, 40.0, 65.0, 90.0):
                bushy_tail("fur_white", "gumiho_red", rig.body, R, (-0.5, -0.75, -0.2), angle, 4, 0.3, curl=12.0, step=0.38)
            ellip("foxfire", rig.body, (0.5 * R, 1.25 * R, 0.85 * R), (0.24 * R, 0.24 * R, 0.34 * R))
            face.decal("gumiho_red", 0, 34, (0.04 * R, 0.08 * R, 0.14 * R))
    if sp == "penguin":
        face_id = new_id()
        fc, fr = (0.42 * R, 0, -0.1 * R), (0.55 * R, 0.68 * R, 0.85 * R)
        ellip("fur_white", rig.body, fc, fr, line_id=face_id)
        face = Face(rig.body, fc, fr, face_id)
        cone("beak", rig.body, (fc[0] + 0.55 * R, 0, 0.05 * R), 0.13 * R, 0.0, 0.3 * R, (0, rad(100), 0), verts=6, smooth=False)
        for sgn in (-1, 1):
            ellip("beak", rig.body, (0.2 * R, sgn * 0.32 * R, -0.98 * R), (0.28 * R, 0.16 * R, 0.08 * R))
        eye_el = 18.0
    if sp == "cat":
        ear_pair("cat", rig.body, R, -0.05, 0.45, 0.8, 0.26, 0.55, inner="bunny_in")
        # A little wizard hat between the ears and a tail curling up behind.
        cone("dark_purple", rig.body, (-0.15 * R, 0, 1.35 * R), 0.38 * R, 0.02 * R, 0.9 * R, (0, rad(-18), 0))
        torus("gold", rig.body, (-0.1 * R, 0, 0.95 * R), 0.42 * R, 0.07 * R, (0, rad(-18), 0))
        tube("cat", rig.body, [(-0.85 * R, 0, -0.4 * R), (-1.35 * R, 0, -0.1 * R), (-1.45 * R, 0, 0.5 * R), (-1.2 * R, 0, 0.85 * R)], 0.13 * R)
    if sp == "turtle":
        shell_id = new_id()
        ellip("shell", rig.body, (-0.38 * R, 0, 0.18 * R), (0.95 * R, 1.0 * R, 0.78 * R), line_id=shell_id)
        torus("shell", rig.body, (-0.35 * R, 0, -0.1 * R), 0.92 * R, 0.1 * R, (0, 0, 0))
        for i, (dx, dy) in enumerate(((-0.5, 0.0), (-0.25, 0.42), (-0.25, -0.42), (-0.85, 0.3), (-0.85, -0.3))):
            ellip("turtle", rig.body, ((dx + 0.0) * R, dy * R, 0.88 * R), (0.18 * R, 0.18 * R, 0.06 * R))
    if sp == "phoenix":
        ellip("belly", rig.body, (0.55 * R, 0, -0.3 * R), (0.42 * R, 0.58 * R, 0.5 * R))
        cone("beak", rig.body, (1.0 * R, 0, -0.08 * R), 0.14 * R, 0.0, 0.3 * R, (0, rad(100), 0), verts=6, smooth=False)
        for i, (dy, tilt, h) in enumerate(((-0.14, -22.0, 0.65), (0.0, 0.0, 0.85), (0.14, 22.0, 0.65))):
            cone("phoenix_wing", rig.body, (-0.05 * R, dy * R, 1.05 * R), 0.12 * R, 0.0, h * R, (rad(tilt), rad(-35), 0))
        for i, (dy, h) in enumerate(((-0.25, 1.2), (0.0, 1.5), (0.25, 1.2))):
            cone("phoenix_wing", rig.body, (-1.2 * R, dy * R, -0.35 * R), 0.16 * R, 0.0, h * R, (rad(dy * 40), rad(-120), 0))
    if sp == "azure":
        ellip("belly", rig.body, (0.55 * R, 0, -0.3 * R), (0.42 * R, 0.6 * R, 0.52 * R))
        for sgn in (-1, 1):
            # Golden antler horns, a white mane tuft and whiskers trailing to the sides.
            cone("gold", rig.body, (-0.05 * R, sgn * 0.38 * R, 0.85 * R), 0.13 * R, 0.0, 0.75 * R, (sgn * rad(-18), rad(-30), 0))
            cone("gold", rig.body, (-0.12 * R, sgn * 0.55 * R, 1.15 * R), 0.07 * R, 0.0, 0.32 * R, (sgn * rad(-60), rad(-10), 0))
            ellip("cloud", rig.body, (-0.45 * R, sgn * 0.55 * R, 0.55 * R), (0.3 * R, 0.26 * R, 0.24 * R))
            tube("gold", rig.body, [(0.85 * R, sgn * 0.45 * R, -0.25 * R), (0.7 * R, sgn * 1.05 * R, -0.05 * R),
                                    (0.45 * R, sgn * 1.45 * R, -0.3 * R)], 0.05 * R)
        # A serpent body winding off to the far side in an S, with a cloud puff at its tip.
        tube("azure", rig.body, [(-0.3 * R, -0.75 * R, -0.45 * R), (-0.45 * R, -1.35 * R, -0.75 * R), (-0.55 * R, -1.85 * R, -0.35 * R),
                                 (-0.6 * R, -2.05 * R, 0.3 * R), (-0.65 * R, -1.8 * R, 0.85 * R)], 0.26 * R)
        ellip("cloud", rig.body, (-0.65 * R, -1.75 * R, 1.1 * R), (0.32 * R, 0.32 * R, 0.24 * R))

    rig.eyes = Eyes(face, "cute", R, az=eye_az, el=eye_el, w=0.2, h=0.3)
    face.decal("blush", -48, -22, (0.04 * R, 0.17 * R, 0.09 * R))
    face.decal("blush", 48, -22, (0.04 * R, 0.17 * R, 0.09 * R))
    rig.wings = []
    if sp in PET_WINGS:
        wing_mat, k, dz = PET_WINGS[sp]
        for sgn in (-1, 1):
            pivot = empty("wing", rig.body, (-0.25 * R, sgn * 0.8 * R, (0.25 + dz) * R))
            ellip(wing_mat, pivot, (-0.2 * R * k, sgn * 0.6 * R * k, 0.2 * R * k), (0.5 * R * k, 0.75 * R * k, 0.16 * R * k), (sgn * rad(-25), 0, 0))
            rig.wings.append((pivot, sgn))
    return rig


def pose_pet(rig, look, p):
    rig.hip.location = (p["bx"], 0, rig.hip_z + p["by"])
    rig.body.rotation_euler = (0, rad(p["lean"]), 0)
    sq = p["squash"]
    rig.body.scale = (1 + sq * 0.15, 1 + sq * 0.15, 1 - sq * 0.2)
    for pivot, sgn in rig.wings:
        pivot.rotation_euler = (sgn * rad(-40.0 * p["wing"]), 0, 0)
    rig.eyes.set(p["eyes"])


def pet_clips(look):
    hop = look["species"] in HOPPERS
    idle = []
    for i in range(N):
        s = math.sin(i / N * math.tau)
        if hop:
            idle.append(pose(by=max(0.0, s) * 2.0, squash=max(0.0, -s) * 0.6, wing=math.sin(2 * i / N * math.tau) * 0.5,
                             eyes="closed" if i in BLINK else "open"))
        else:
            idle.append(pose(by=s * 1.5, wing=math.sin(2 * i / N * math.tau), eyes="closed" if i in BLINK else "open"))
    attack = [pose(bx=-1, lean=-10, squash=0.3), pose(bx=-2, lean=-15, squash=0.5, eyes="angry"),
              pose(bx=4, lean=20, by=1, squash=-0.2, wing=1.0, eyes="angry"), pose(bx=5, lean=18, wing=-1.0),
              pose(bx=2, lean=6), pose()]
    return {"idle": idle, "attack": resample(attack, 8)}


def build_flyeye(look):
    R = look["R"]
    rig = Rig()
    rig.k, rig.R = 1.0, R
    root = empty("root", rot=(0, 0, rad(BODY_YAW)))
    rig.fall = empty("fall", root)
    rig.hip = empty("hip", rig.fall)
    rig.hip_z = R + 9.0
    rig.head = empty("head", rig.hip, rot=(0, 0, rad(HEAD_YAW * 0.6)))
    ball_id = new_id()
    ellip("eye_white", rig.head, (0, 0, 0), (R, R, R), line_id=ball_id)
    face = Face(rig.head, (0, 0, 0), (R, R, R), ball_id)
    rig.iris = face.decal(look["iris"], 0, -2, (0.08 * R, 0.55 * R, 0.6 * R), lift=0.0)
    rig.pupil = face.decal("eye", 0, -2, (0.08 * R, 0.25 * R, 0.36 * R), lift=0.05 * R)
    rig.hi = face.at(10, 14, lift=0.3 * R)[0]
    rig.face = face
    rig.wings = []
    for sgn in (-1, 1):
        pivot = empty("wing", rig.head, (-0.3 * R, sgn * 0.75 * R, 0.2 * R))
        ellip(look["wing"], pivot, (-0.2 * R, sgn * 0.75 * R, 0.25 * R), (0.55 * R, 0.9 * R, 0.22 * R), (sgn * rad(-20), 0, 0))
        rig.wings.append((pivot, sgn))
    ellip(look["wing"], rig.head, (-1.05 * R, 0, -0.35 * R), (0.5 * R, 0.25 * R, 0.25 * R), (0, rad(25), 0))
    return rig


def pose_flyeye(rig, look, p):
    t = p["fall"] / 90.0
    rig.fall.rotation_euler = (0, -rad(p["fall"]), 0)
    rig.hip.location = (p["bx"], 0, rig.hip_z + p["by"] - t * (rig.hip_z - rig.R * 0.9) * min(1.0, p["fall"] / 60.0))
    for pivot, sgn in rig.wings:
        pivot.rotation_euler = (sgn * rad(-35.0 * p["wing"]), 0, 0)
    closed = p["eyes"] in ("hurt", "dead", "closed")
    rig.iris.scale = (0.08 * rig.R, 0.55 * rig.R, (0.12 if closed else 0.6) * rig.R)
    rig.pupil.hide_render = closed


def flyeye_stamps(rig, p):
    if p["eyes"] in ("hurt", "dead", "closed"):
        return []
    q = to_px(rig.head.matrix_world @ rig.hi)
    return [[q[0], q[1], "#ffffff", 2]]


# ---------------------------------------------------------------- golem (stone boss, 1 golem unit = 17 px)

G = 17.0


def build_golem(look):
    # D-131: a golem look may swap its materials (mat_stone, mat_stone_dark, mat_moss, mat_crystal).
    def m(name):
        return look.get("mat_" + name, name)

    rig = Rig()
    root = empty("root", rot=(0, 0, rad(-70.0)), scale=(G, G, G))
    rig.fall = empty("fall", root)
    rig.hip = empty("hips", rig.fall, (0, 0, 0.55))
    rock(m("stone"), rig.hip, (0, 0, 0.62), 0.85, (0.95, 0.85, 0.82))
    for lp, sz, tl in [((-0.15, -0.72, 1.25), (0.62, 0.16), (35, -15, 0)),
                       ((-0.2, 0.72, 1.25), (0.72, 0.17), (-35, -15, 0)),
                       ((-0.45, 0.35, 1.45), (0.55, 0.13), (-20, -35, 0))]:
        bipyramid(m("crystal"), rig.hip, lp, sz[0], sz[1], tl)
    rig.legs, rig.arms = {}, {}
    for side, y in (("front", -0.42), ("back", 0.42)):
        pivot = empty("leg_" + side, rig.hip, (0, y, 0.0))
        rock(m("stone_dark"), pivot, (0.02, 0, -0.2), 0.33, (1.0, 1.0, 1.1), subdiv=1, jitter=0.05)
        rig.legs[side] = pivot
    for side, y in (("front", -0.88), ("back", 0.88)):
        pivot = empty("arm_" + side, rig.hip, (0.08, y, 1.02))
        rock(m("stone_dark"), pivot, (0, 0, -0.22), 0.3, subdiv=1, jitter=0.05)
        rock(m("stone"), pivot, (0.1, 0, -0.8), 0.5, (1.0, 0.95, 0.92), subdiv=2, jitter=0.08)
        rig.arms[side] = pivot
    rig.neck = empty("neck", rig.hip, (0.05, 0, 1.3))
    hc, hr = (0.08, 0.0, 0.78), (1.0, 0.95, 0.88)
    head_id = new_id()
    rock(m("stone"), rig.neck, hc, 1.0, hr, subdiv=3, jitter=0.03, line_id=head_id)
    moss_id = new_id()
    rock(m("moss"), rig.neck, (hc[0] - 0.1, 0, hc[2] + 0.48), 0.96, (1.0, 1.0, 0.45), subdiv=3, jitter=0.04, line_id=moss_id)
    if look.get("flowers", True):
        ellip("flower", rig.neck, (hc[0] + 0.25, -0.35, hc[2] + 0.92), (0.16, 0.16, 0.09), line_id=moss_id)
        ellip("flower_mid", rig.neck, (hc[0] + 0.27, -0.37, hc[2] + 0.99), (0.07, 0.07, 0.07), line_id=moss_id)
    face = Face(rig.neck, hc, hr, head_id)
    rig.eyes = Eyes(face, "cute", 1.0, low="eye_low_c", az=23.0, el=-8.0, w=0.2, h=0.3)
    face.decal("blush", -40, -20, (0.04, 0.14, 0.08))
    face.decal("blush", 40, -20, (0.04, 0.14, 0.08))
    face.decal("mouth", 0, -24, (0.04, 0.09, 0.06))
    return rig


def pose_golem(rig, look, p):
    t = p["fall"] / 90.0
    rig.fall.rotation_euler = (0, -rad(p["fall"]), 0)
    rig.fall.location = (0, 0, t * 0.82)
    rig.hip.location = (p["bx"], 0, 0.55 + p["by"])
    rig.hip.rotation_euler = (0, rad(p["lean"]), 0)
    sq = p["squash"]
    rig.hip.scale = (1 + sq * 0.6, 1 + sq * 0.6, 1 - sq)
    rig.neck.rotation_euler = (0, rad(p["nod"]), 0)
    rig.arms["front"].rotation_euler = (0, -rad(p["ga"]), 0)
    rig.arms["back"].rotation_euler = (0, -rad(p["gb"]), 0)
    rig.legs["front"].rotation_euler = (0, -rad(p["gl"]), 0)
    rig.legs["back"].rotation_euler = (0, -rad(p["gr"]), 0)
    rig.eyes.set(p["eyes"])


# ---------------------------------------------------------------- clips (pose values follow cast.py / hero.py)

def pose(**kw):
    base = dict(bx=0.0, by=0.0, lean=0.0, squash=0.0, nod=0.0, fa=(-60.0, -20.0), ba=(-110.0, -90.0),
                fl=(-90.0, -90.0), bl=(-90.0, -90.0), weapon=40.0, eyes="open", mouth="smile", flash=False,
                smear=None, fall=0.0, glow=0.0, wing=0.0, draw=0.0, cape=1.0, ga=8.0, gb=-6.0, gl=0.0, gr=0.0)
    base.update(kw)
    return base


STAND = dict(fa=(-55.0, -15.0), ba=(-115.0, -95.0), fl=(-82.0, -92.0), bl=(-98.0, -88.0))

# Frames per clip (D-096): twice the old counts, played at 20 fps so clip lengths stay the same. Loops are generated
# at this count; one-shot clips are keyframes resampled to it.
FRAMES = {"idle": 12, "run": 12, "attack": 12, "hit": 6, "dead": 12}
N = 12
BLINK = (8, 9)


def _mix(a, b, t):
    if isinstance(a, bool) or isinstance(b, bool) or a is None or b is None:
        return a if t < 0.5 else b
    if isinstance(a, (int, float)) and isinstance(b, (int, float)):
        return a + (b - a) * t
    if isinstance(a, tuple) and isinstance(b, tuple):
        return tuple(x + (y - x) * t for x, y in zip(a, b))
    return a if t < 0.5 else b


def resample(keys, n):
    """In-betweens for a one-shot clip: numbers and angle pairs are interpolated, the rest snaps to the nearer key.
    A swing smear stays on the single frame nearest its key."""
    out = []
    for j in range(n):
        u = j * (len(keys) - 1) / (n - 1)
        i = min(int(u), len(keys) - 2)
        t = u - i
        a, b = keys[i], keys[i + 1]
        p = {k: _mix(a[k], b[k], t) for k in a}
        p["smear"] = None
        p["flash"] = False
        out.append(p)
    for i, k in enumerate(keys):
        if k.get("smear"):
            out[int(round(i * (n - 1) / (len(keys) - 1)))]["smear"] = k["smear"]
    return out


def finish_clips(clips):
    return {name: (frames if name in ("idle", "run") else resample(frames, FRAMES[name])) for name, frames in clips.items()}


def run_cycle(i, rest, bow=False):
    """Enemy run (D-096): the hero's run with the weapon held low, used while a wave runs in."""
    t = i / N * math.tau
    s, c = math.sin(t), math.cos(t)
    arms = dict(fa=(-70.0 - s * 25.0, -30.0 - s * 15.0), ba=(-110.0 + s * 30.0, -85.0 + s * 25.0))
    if bow:
        arms["ba"] = (-40.0, -20.0)
    return pose(by=round(abs(s) * 1.2), lean=9.0, weapon=rest - 10.0 - s * 8.0,
                fl=(-90.0 + s * 38.0, -90.0 + s * 38.0 - max(0.0, -c) * 45.0),
                bl=(-90.0 - s * 38.0, -90.0 - s * 38.0 - max(0.0, c) * 45.0),
                mouth="open" if i in (3, 9) else "smile", **arms)


def hero_clips():
    idle, run = [], []
    for i in range(N):
        t = i / N * math.tau
        kk = (math.sin(t) + 1.0) / 2.0
        idle.append(pose(squash=kk, lean=-2.0, fa=(-55.0 + kk * 6, -15.0 + kk * 6), ba=(-115.0, -95.0 + kk * 6),
                         fl=(-82.0, -92.0), bl=(-98.0, -88.0), weapon=40.0 + kk * 4, cape=math.sin(t),
                         eyes="closed" if i in BLINK else "open"))
        s, c = math.sin(t), math.cos(t)
        run.append(pose(by=round(abs(s) * 1.5), lean=10.0,
                        fl=(-90.0 + s * 40.0, -90.0 + s * 40.0 - max(0.0, -c) * 50.0),
                        bl=(-90.0 - s * 40.0, -90.0 - s * 40.0 - max(0.0, c) * 50.0),
                        fa=(-70.0 - s * 30.0, -25.0 - s * 20.0), ba=(-110.0 + s * 35.0, -80.0 + s * 30.0),
                        weapon=18.0 - s * 10.0, cape=3.0 + math.sin(t * 2.0) * 1.2,
                        mouth="open" if i in (3, 9) else "smile"))
    attack = [
        pose(lean=-6, bx=-1, squash=1, fa=(-20.0, 50.0), ba=(-125.0, -100.0), fl=(-75.0, -95.0), bl=(-105.0, -92.0), weapon=125.0),
        pose(lean=-12, bx=-2, squash=1, fa=(30.0, 100.0), ba=(-135.0, -110.0), fl=(-72.0, -95.0), bl=(-108.0, -92.0), weapon=155.0, mouth="grit", cape=0.5),
        pose(lean=12, bx=3, fa=(-5.0, -10.0), ba=(-150.0, -140.0), fl=(-60.0, -92.0), bl=(-118.0, -98.0), weapon=-15.0, cape=3.0, mouth="open", smear=(155.0, -30.0)),
        pose(lean=15, bx=4, fa=(-35.0, -45.0), ba=(-155.0, -140.0), fl=(-58.0, -92.0), bl=(-120.0, -98.0), weapon=-50.0, cape=3.5, mouth="open"),
        pose(lean=12, bx=4, fa=(-45.0, -55.0), ba=(-150.0, -130.0), fl=(-60.0, -92.0), bl=(-118.0, -98.0), weapon=-60.0, cape=2.5, mouth="grit"),
        pose(lean=5, bx=2, fa=(-55.0, -30.0), ba=(-125.0, -105.0), fl=(-70.0, -92.0), bl=(-110.0, -90.0), weapon=10.0, cape=1.5),
        pose(lean=-2, weapon=40.0, **STAND),
    ]
    hit = [
        pose(lean=-14, bx=-2, squash=1, fa=(-30.0, 10.0), ba=(-140.0, -120.0), fl=(-72.0, -90.0), bl=(-108.0, -92.0), weapon=25.0, cape=-1.0, eyes="closed", mouth="grit", flash=True),
        pose(lean=-12, bx=-2, fa=(-35.0, 5.0), ba=(-135.0, -115.0), fl=(-72.0, -90.0), bl=(-108.0, -92.0), weapon=25.0, cape=-0.5, eyes="closed", mouth="grit"),
        pose(lean=-6, bx=-1, fa=(-45.0, -5.0), ba=(-120.0, -100.0), fl=(-78.0, -92.0), bl=(-102.0, -90.0), weapon=30.0, cape=0.5),
        pose(lean=-2, weapon=40.0, **STAND),
    ]
    dead = []
    for i, f in enumerate((0, 0, 10, 25, 55, 80, 90, 90)):
        dead.append(pose(lean=-12 if i < 2 else -8, bx=-2 - min(i, 4) * 0.5, squash=1 if i == 1 else 0,
                         fa=(-30.0 - i * 10, 10.0 - i * 18), ba=(-140.0, -120.0), fl=(-72.0 + i * 8, -90.0 + i * 6),
                         bl=(-108.0 - i * 6, -92.0 - i * 9), weapon=60.0 - i * 20, eyes="closed",
                         mouth="open" if i < 3 else "smile", fall=f, flash=i == 0, cape=-min(i, 3)))
    return {"idle": idle, "run": run, "attack": attack, "hit": hit, "dead": dead}


def cast_clips(look):
    if look.get("kind") == "flyeye":
        idle = [pose(by=math.sin(i / N * math.tau) * 1.5, wing=math.sin(i / N * math.tau)) for i in range(N)]
        run = [pose(by=math.sin(i / N * math.tau) * 1.0, bx=1.0, wing=math.sin(2 * i / N * math.tau)) for i in range(N)]
        attack = [pose(bx=-2, by=1, wing=1.0), pose(bx=-3, by=2, wing=0.6), pose(bx=5, by=-1, wing=-0.6),
                  pose(bx=6, by=-1, wing=-1.0), pose(bx=3, wing=0.0), pose(bx=0, wing=0.6)]
        hit = [pose(bx=-3, eyes="hurt", flash=True, wing=0.8), pose(bx=-2, eyes="hurt", wing=0.2), pose(bx=-1, wing=-0.4)]
        dead = [pose(eyes="dead", flash=True), pose(eyes="dead", fall=20, wing=-0.5), pose(eyes="dead", fall=45, wing=-1.0),
                pose(eyes="dead", fall=70, wing=-1.0), pose(eyes="dead", fall=90, wing=-1.0), pose(eyes="dead", fall=90, wing=-1.0)]
        return {"idle": idle, "run": run, "attack": attack, "hit": hit, "dead": dead}
    if look.get("kind") == "golem":
        idle = []
        for i in range(N):
            s = math.sin(i / N * math.tau)
            idle.append(pose(by=s * 0.07, squash=max(0.0, -s) * 0.04, nod=s * 2.0, ga=8 + s * 5, gb=-6 - s * 5,
                             eyes="closed" if i in BLINK else "open"))
        run = []
        for i in range(N):
            s = math.sin(i / N * math.tau)
            run.append(pose(by=abs(s) * 0.08, lean=6.0, nod=s * 3.0, ga=8 + s * 28, gb=-6 - s * 28, gl=s * 28, gr=-s * 28))
        attack = [pose(lean=-8, by=0.06, ga=140, gb=130, nod=-6), pose(lean=-14, by=0.14, ga=172, gb=165, nod=-10, squash=-0.04),
                  pose(lean=26, by=-0.16, ga=58, gb=52, nod=8, squash=0.08, eyes="angry"),
                  pose(lean=22, by=-0.2, ga=52, gb=46, nod=6, squash=0.1, eyes="angry"),
                  pose(lean=10, by=-0.06, ga=30, gb=18, nod=2), pose(lean=3, ga=12, gb=-2)]
        hit = [pose(lean=-14, nod=-12, ga=-20, gb=-28, bx=-0.12, eyes="closed", flash=True),
               pose(lean=-9, nod=-7, ga=-10, gb=-18, bx=-0.08, eyes="closed"), pose(lean=-3, nod=-2, ga=2, gb=-10, bx=-0.02)]
        dead = [pose(fall=f, nod=-6, ga=10 + f * 0.4, gb=-6 + f * 0.3, gl=f * 0.25, gr=-f * 0.1, eyes="closed", flash=f == 0)
                for f in (0.0, 12.0, 35.0, 62.0, 84.0, 90.0)]
        return {"idle": idle, "run": run, "attack": attack, "hit": hit, "dead": dead}

    weapon = look["weapon"]
    rest = {"club": 55.0, "sword": 40.0, "katana": 35.0, "staff": 80.0, "bow": 0.0, "none": 0.0}[weapon]
    idle = []
    for i in range(N):
        s = (math.sin(i / N * math.tau) + 1.0) / 2.0
        idle.append(pose(squash=s, lean=-2.0, weapon=rest + s * 4, glow=s, eyes="closed" if i in BLINK else "open", **STAND))
    run = [run_cycle(i, rest, bow=weapon == "bow") for i in range(N)]
    if weapon in ("club", "sword", "katana"):
        attack = [pose(lean=-6, bx=-1, squash=1, fa=(-20.0, 50.0), ba=(-125.0, -100.0), weapon=125.0),
                  pose(lean=-12, bx=-2, squash=1, fa=(30.0, 100.0), ba=(-135.0, -110.0), weapon=155.0, mouth="grit"),
                  pose(lean=12, bx=3, fa=(-5.0, -10.0), ba=(-150.0, -140.0), weapon=-15.0, mouth="open", smear=(155.0, -30.0)),
                  pose(lean=15, bx=4, fa=(-35.0, -45.0), ba=(-155.0, -140.0), weapon=-50.0, mouth="open"),
                  pose(lean=6, bx=2, fa=(-50.0, -30.0), weapon=5.0),
                  pose(lean=-2, weapon=rest, **STAND)]
    elif weapon == "staff":
        attack = [pose(lean=-6, fa=(-10.0, 40.0), weapon=95.0, glow=0.5),
                  pose(lean=-10, fa=(10.0, 60.0), weapon=100.0, glow=1.0, mouth="grit"),
                  pose(lean=10, bx=2, fa=(-20.0, 0.0), weapon=40.0, glow=1.6, mouth="open"),
                  pose(lean=12, bx=3, fa=(-25.0, -5.0), weapon=35.0, glow=1.2, mouth="open"),
                  pose(lean=4, fa=(-40.0, -10.0), weapon=60.0, glow=0.5),
                  pose(lean=-2, weapon=rest, **STAND)]
    elif weapon == "bow":
        bowarm = dict(ba=(-10.0, -5.0), fa=(-160.0, -170.0))
        attack = [pose(lean=-4, draw=0.3, **bowarm), pose(lean=-8, draw=0.8, mouth="grit", **bowarm),
                  pose(lean=-10, draw=1.0, mouth="grit", **bowarm), pose(lean=6, bx=1, draw=0.0, mouth="open", **bowarm),
                  pose(lean=2, draw=0.0, **bowarm), pose(lean=-2, **STAND)]
        idle = [dict(q, ba=(-40.0, -20.0)) for q in idle]
    else:  # mushroom headbutt
        attack = [pose(lean=-12, bx=-1, squash=1, **STAND), pose(lean=-18, bx=-2, squash=2, mouth="grit", **STAND),
                  pose(lean=22, bx=4, by=1, mouth="open", **STAND), pose(lean=18, bx=4, mouth="open", **STAND),
                  pose(lean=6, bx=2, **STAND), pose(lean=-2, **STAND)]
    hit = [pose(lean=-14, bx=-2, squash=1, weapon=rest - 20, eyes="closed", mouth="grit", flash=True, **STAND),
           pose(lean=-12, bx=-2, weapon=rest - 20, eyes="closed", mouth="grit", **STAND),
           pose(lean=-5, bx=-1, weapon=rest - 10, **STAND)]
    dead = [pose(lean=-14, bx=-2, weapon=rest, eyes="closed", mouth="open", flash=True, **STAND),
            pose(lean=-18, bx=-3, squash=1, fa=(-10.0, 30.0), ba=(-150.0, -130.0), fl=(-60.0, -110.0), bl=(-115.0, -120.0),
                 weapon=rest - 30, eyes="closed", mouth="open"),
            pose(lean=-10, bx=-4, by=-1, fa=(-80.0, -100.0), ba=(-130.0, -130.0), fl=(-60.0, -100.0), bl=(-100.0, -120.0),
                 weapon=-30.0, eyes="closed", fall=30.0),
            pose(lean=-10, bx=-5, by=-1, fa=(-90.0, -110.0), ba=(-140.0, -140.0), fl=(-60.0, -80.0), bl=(-100.0, -110.0),
                 weapon=-70.0, eyes="closed", fall=60.0),
            pose(lean=-10, bx=-6, by=-1, fa=(-100.0, -120.0), ba=(-150.0, -150.0), fl=(-70.0, -80.0), bl=(-100.0, -100.0),
                 weapon=-95.0, eyes="closed", fall=85.0),
            pose(lean=-8, bx=-6, by=-1, fa=(-105.0, -125.0), ba=(-150.0, -150.0), fl=(-75.0, -85.0), bl=(-100.0, -100.0),
                 weapon=-100.0, eyes="closed", fall=88.0)]
    return {"idle": idle, "run": run, "attack": attack, "hit": hit, "dead": dead}


# ---------------------------------------------------------------- render


def to_px(world):
    u, v, _ = world_to_camera_view(scene, cam, world)
    return [u * W, (1.0 - v) * H]


def render(path, data):
    vl = bpy.context.view_layer
    s = scene.render.image_settings
    if data:
        vl.material_override = DATA_MAT
        scene.view_settings.view_transform = "Raw"
        s.color_mode = "RGB"
    else:
        vl.material_override = None
        scene.view_settings.view_transform = "Standard"
        s.color_mode = "RGBA"
    s.file_format = "PNG"
    s.color_depth = "8"
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


def smear_objects(rig, arc):
    """White-blue swing arc around the front shoulder, behind the body so the face stays clear."""
    k = rig.k
    up = rig.arms["front"][0]
    holder = empty("smear", rig.hip, (up.location.x, 7.0 * k, up.location.z))
    a0, a1 = arc
    obs = [holder]
    for i, (r0, r1) in enumerate(((6.0, 9.5), (9.5, 13.0), (13.0, 16.0))):
        obs.append(sector("smear%d" % i, holder, (0, -0.1 * i, 0), a1, a0, r0 * k, r1 * k))
    return obs


def main():
    kind = LOOK.get("kind")
    if ENTITY.startswith("knight"):
        rig, clips, poser = build_humanoid(LOOK), hero_clips(), pose_humanoid
    elif LOOK.get("job"):
        # Mage and archer jobs swing a staff or draw a bow, so they take the enemy clip set with its run cycle.
        rig, clips, poser = build_humanoid(LOOK), cast_clips(LOOK), pose_humanoid
    elif kind == "flyeye":
        rig, clips, poser = build_flyeye(LOOK), cast_clips(LOOK), pose_flyeye
    elif kind == "golem":
        rig, clips, poser = build_golem(LOOK), cast_clips(LOOK), pose_golem
    elif kind == "pet":
        rig, clips, poser = build_pet(LOOK), pet_clips(LOOK), pose_pet
    else:
        rig, clips, poser = build_humanoid(LOOK), cast_clips(LOOK), pose_humanoid
    if kind != "pet":
        clips = finish_clips(clips)
    out = os.path.join(OUT_ROOT, ENTITY)
    os.makedirs(out, exist_ok=True)
    meta = {"entity": ENTITY, "name": LOOK["name"], "folder": LOOK["folder"], "frame": [W, H], "scale": SCALE,
            "mirror": LOOK["mirror"], "depth": [DEPTH_NEAR, DEPTH_FAR], "palette": PAL, "nolines": NOLINES,
            "clips": {}}
    for clip, poses in clips.items():
        entries = []
        for i, p in enumerate(poses):
            poser(rig, LOOK, p)
            extra = smear_objects(rig, p["smear"]) if p["smear"] and kind is None else []
            bpy.context.view_layer.update()
            stem = os.path.join(out, "%s_%d" % (clip, i))
            render(stem + "_color.png", False)
            render(stem + "_data.png", True)
            stamps = flyeye_stamps(rig, p) if kind == "flyeye" else rig.eyes.stamps(p["eyes"])
            entries.append({"stamps": stamps})
            for ob in extra:
                bpy.data.objects.remove(ob, do_unlink=True)
        meta["clips"][clip] = entries
    with open(os.path.join(out, "meta.json"), "w") as f:
        json.dump(meta, f, indent=1)
    print("chibi3d done", ENTITY)


main()
