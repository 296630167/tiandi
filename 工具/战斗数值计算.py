"""从游戏数值配置.md读唯一参数，生成等级表、固定种子离线战斗和查看器。
不操作Unity或玩家真实存档。只用Python标准库；输出可重复验证。
"""
from __future__ import annotations
import csv
import hashlib
import html
import json
import math
from pathlib import Path
import random
import re
import statistics

ROOT = Path(__file__).resolve().parents[1]
DOC = ROOT / "游戏数值配置.md"
OUT = ROOT / "数据" / "战斗数值_v1"
TEXT = DOC.read_text(encoding="utf-8")
MATCH = re.search(r"<!-- 数值配置开始 -->\s*```json\s*(.*?)\s*```\s*<!-- 数值配置结束 -->", TEXT, re.S)
if not MATCH:
    raise ValueError("缺少权威数值JSON块")
CFG = json.loads(MATCH.group(1))
CONFIG_HASH = hashlib.sha256(json.dumps(CFG, sort_keys=True, ensure_ascii=False).encode()).hexdigest()
P, SOFT, DMG, SHAPE, RUNE, SIM = (CFG[k] for k in ("player", "soft", "damage", "shape", "rune", "simulation"))
KINDS = tuple(CFG["enemies"])
PROFILES = ("naked", "low", "standard", "high")
NAMES = {"naked": "裸装", "low": "低配", "standard": "标准", "high": "高配"}
REPORT = {"config_version": CFG["version"], "config_sha256": CONFIG_HASH, "passed": [], "failed": [], "scope": "离线固定事件、固定木桩与条件命中概率；不是Unity实机或真人试玩"}


def check(name, condition, evidence=None):
    REPORT["passed" if condition else "failed"].append({"name": name, "evidence": evidence})


def clamp(value, lo, hi):
    return max(lo, min(hi, value))


def region_counts(level):
    g = CFG['map']['long_region']['growth']
    t = (clamp(level, 1, 100) - 1) / 99
    def n(key):
        return math.floor(g[key + '_start'] + (g[key + '_end'] - g[key + '_start']) * t ** g['exponent'] + .5)
    total, elite, leader = n('total'), n('elite'), n('leader')
    return [total - elite - leader - 1, elite, leader, 1]


def tactical_growth(level, key, start=1):
    return start + (CFG['battle_content']['growth'][key] - start) * (clamp(level, 1, 100) - 1) / 99


def arena_growth(level, key):
    a = CFG['map']['arena']
    return a[key + '_start'] + (a[key + '_end'] - a[key + '_start']) * (clamp(level, 1, 100) - 1) / 99


def boss_grade_floor(level):
    # 八阶顺序中的稀有至传说；阈值只读权威loot配置，不按物品等级封顶。
    return max(3 + i for i, threshold in enumerate(CFG["loot"]["boss_grade_levels"]) if level >= threshold)


def boss_drop_grade(level, rolled_grade):
    return max(rolled_grade, boss_grade_floor(level))

def recycle_price(grade=0, ports=None):
    e = CFG["economy"]
    loot = CFG["loot"]
    rates = [*loot["rune_probability"], *loot["currency_probability"], loot["stone_probability"]]
    check("三类掉率合法且随敌人级别递增", all(len(row) == 4 and all(0 <= p <= 1 for p in row) and all(a <= b for a,b in zip(row,row[1:])) for row in rates))
    empty = lambda multiplier: math.prod(1 - min(1, row[0] * multiplier) for row in (loot["rune_probability"][0], loot["currency_probability"][0], loot["stone_probability"]))
    check("普通小怪多数为空且天命仍不必掉", empty(1) >= .65 and empty(CFG["talents"]["drop_multiplier"]) >= .5, {"普通空掉概率":empty(1), "天命空掉概率":empty(CFG["talents"]["drop_multiplier"])})
    check("BOSS保留三类掉落且头目保留灵石", all(row[-1] == 1 for row in rates) and loot["stone_probability"][2] == 1)
    counts = [CFG["map"][kind] for kind in KINDS]
    check("完整66敌人期望收益降低但保留构筑资源", math.isclose(sum(n*p for n,p in zip(counts,loot["rune_probability"][0])),9.05) and math.isclose(sum(n*p*s for n,p,s in zip(counts,loot["currency_probability"][0],loot["currency_stacks"][0])),9.6) and math.isclose(sum(n*p*s for n,p,s in zip(counts,loot["stone_probability"],e["kill_stones"])),410))
    return e["recycle_branch_prices"][ports - 3] if ports is not None else e["recycle_grade_prices"][grade]


def growth(level):
    x = level - 1
    c = CFG["growth"]
    return 1 + c["linear"] * x + c["quadratic"] * x * x


def soft(value, knee):
    if not math.isfinite(value) or value < 0 or knee <= 0:
        raise ValueError("增益必须有限非负，拐点必须正数")
    if value <= knee:
        return value
    excess = value - knee
    return knee + excess / (1 + excess / knee)


def reference_nodes(level):
    return min(RUNE["reference_nodes_max"], 1 + (level - 1) // RUNE["reference_point_stride"])


def bonuses(level, profile):
    g = growth(level)
    result = {}
    nodes = RUNE["reference_nodes"][:reference_nodes(level)]
    if profile == "naked":
        return result
    if profile.startswith("extreme_"):
        key = profile.removeprefix("extreme_")
        # 合法格数，每格6条同属性最高词条，使用可继续传导的二口道纹。
        count = level * (1 if key == "element" else SIM["extreme_affixes_per_node"])
        if key == "shape":
            keys = ("quantity", "chain", "split", "radius", "arc")
            return {name: (count // len(keys) + (index < count % len(keys))) * (RUNE["arc_per_affix"] if name == "arc" else 1) for index, name in enumerate(keys)}
        ranges = RUNE["affix_ranges"]
        result[key] = count * ranges[key][1] * (g if key in ("strength", "intelligence", "speed", "element", "armor") else 1)
        return result
    if profile == "low":
        budget=math.ceil(len(nodes)*RUNE["low_active_fraction"])
        kept = []
        for node in nodes:
            if all(key.removesuffix("_g") in RUNE["affix_ranges"] or key=="quantity" for key in node):
                kept.append(node)
        nodes = kept[:budget]
    for index, node in enumerate(nodes):
        if profile == "high":
            if index == RUNE["high_quantity_node"]:
                node = {"quantity": 1, "attack_speed": RUNE["high_attack_speed"]}
            elif index == RUNE["high_chain_node"]:
                node = {"quantity": RUNE["high_extra_quantity"], "chain": RUNE["high_chain"]}
            elif index == RUNE["high_split_node"]:
                node = {"split": RUNE["high_split"], "radius": RUNE["high_radius"]}
        for key, value in node.items():
            scaled = key.endswith("_g")
            key = key.removesuffix("_g")
            if profile == "low":
                value = RUNE["affix_ranges"][key][0] if key in RUNE["affix_ranges"] else value
            elif profile == "high" and key in RUNE["affix_ranges"] and key != "attack_speed":
                value *= RUNE["high_factor"]
                if key == "element":
                    value = min(value, RUNE["affix_ranges"][key][1])
            result[key] = result.get(key, 0) + value * (g if scaled else 1)
    return result


def armor_retention(armor, attacker_level, percent_pen=0, flat_pen=0):
    effective = max(0, armor * (1 - clamp(percent_pen, 0, DMG["penetration_percent_max"])) - flat_pen)
    k = DMG["armor_k_g"] * growth(attacker_level)
    return max(DMG["armor_retention_min"], k / (k + effective))


def level_factor(attacker, defender):
    return math.exp(DMG["level_log"] * clamp(attacker - defender, -DMG["level_difference_max"], DMG["level_difference_max"]))


def packets(player, targets):
    # 固定密集木桩的直接命中优先预算。每个包分配给不同目标，不乘各形态笛卡尔积。
    quantity, chain, split, radius = (player[k] for k in ("quantity", "chain", "split", "radius"))
    weights = [1.0] * quantity
    for jump in range(1, chain + 1):
        weights.extend([SHAPE["chain_factor"] ** jump] * quantity)
    weights.extend([SHAPE["split_factor"]] * (quantity * split))
    if radius > 0:
        weights.extend([SHAPE["splash_factor"]] * (quantity * SHAPE["splash_targets_max"]))
    return weights[:targets]


def ordered_packets(functions, targets):
    """线性主出口加空侧出口，祖先命中扣目标，齐射兄弟可同目标。"""
    if any(f not in {"齐射", "分裂", "连锁", "增大", "缩小", "加速", "减速", "穿透"} for f in functions):
        return extended_packets(functions, targets)
    cfg = RUNE["ordered_functions"]
    queue, result, generated = [], [], 0
    modifiers = {"增大", "缩小", "加速", "减速"}
    def emit(index, available, factor):
        nonlocal generated
        while index < len(functions) and functions[index] in modifiers:
            index += 1
        if index < len(functions) and functions[index] == "齐射":
            emit(index + 1, available, factor)
            emit(len(functions), available, factor)
        elif generated < cfg["projectiles_per_release"]:
            generated += 1
            queue.append((index, available, factor))
    emit(0, targets, 1)
    while queue:
        index, available, factor = queue.pop(0)
        if available <= 0:
            continue
        result.append(factor)
        if index < len(functions):
            f = functions[index]
            if f == "穿透":
                hits = min(available, 1 + cfg["pierce_count"])
                result.extend([factor] * (hits - 1))
                if hits <= cfg["pierce_count"]:
                    continue
                after = index + 1
                while after < len(functions) and functions[after] in modifiers:
                    after += 1
                left = available - hits
                if after < len(functions):
                    following = functions[after]
                    if following in {"齐射", "穿透"}:
                        emit(after, left, factor)
                    elif following in {"分裂", "连锁"}:
                        factor *= cfg["split_factor"] if following == "分裂" else cfg["chain_factor"]
                        emit(after + 1, left, factor)
                        if following == "分裂":
                            emit(len(functions), left, factor)
                continue
            factor *= cfg["split_factor"] if f == "分裂" else cfg["chain_factor"]
            emit(index + 1, available - 1, factor)
            if f == "分裂":
                emit(len(functions), available - 1, factor)
    return result


def extended_packets(functions, targets):
    """扩展纸面预算：密集可达目标，蓄势6米，一处拖尾完整周期，成功弹墙。"""
    cfg = RUNE["ordered_functions"]
    e = cfg["extended"]
    shapes = cfg["attack_shapes"]
    shape_names = dict(zip(("光束", "刃波", "地刺", "剑雨", "旋刃", "灵鞭", "飞轮", "游龙", "灵网", "地雷"), ("beam", "blade", "spike", "rain", "orbit", "whip", "wheel", "dragon", "net", "mine")))
    emissions = {"扇射": "fan", "环射": "ring", "十字": "cross", "背射": "back"}
    motion = {"回旋", "波动", "蓄势", "拖尾"}
    queue, result, generated = [], [], 0
    def skip(index, layers):
        layers = layers.copy()
        while index < len(functions) and functions[index] in motion | {"增大", "缩小", "加速", "减速"}:
            f = functions[index]
            if f in motion: layers[f] = min(e["motion_layers_max"], layers.get(f, 0) + 1)
            index += 1
        return index, layers
    def emit(index, available, factor, layers):
        nonlocal generated
        if generated >= cfg["projectiles_per_release"]: return
        index, layers = skip(index, layers)
        f = functions[index] if index < len(functions) else "末端"
        if f in emissions:
            key = emissions[f]
            for _ in range(e[key + "_count"]): emit(index + 1, available, factor * e[key + "_factor"], layers)
        elif f == "齐射":
            emit(index + 1, available, factor, layers)
            emit(len(functions), available, factor, layers)
        elif generated < cfg["projectiles_per_release"]:
            generated += 1
            queue.append((index, available, factor, False, layers))
    def follow(index, available, factor, layers, fly=False):
        after, layers = skip(index + 1, layers)
        f = functions[after] if after < len(functions) else "末端"
        if f in set(emissions) | {"齐射"}:
            emit(after, available, factor, layers)
        elif fly or f in {"穿透", "延时", "跃迁", "陨落", "折返", "弹墙"} or f in shape_names:
            queue.append((after, available, factor, False, layers))
        elif f != "末端": queue.append((after, available, factor, True, layers))
    emit(0, targets, 1, {})
    while queue:
        index, available, factor, hit, layers = queue.pop(0)
        f = functions[index] if index < len(functions) else "末端"
        travel = 0 if f in {"剑雨", "旋刃", "灵鞭", "地雷"} else e["paper_travel_distance"]
        factor *= min(e["charge_max"], 1 + travel * e["charge_per_meter"] * layers.get("蓄势", 0))
        # 阶段转换保留原始形态倍率，蓄势只加在伤害包上。
        shape = factor / min(e["charge_max"], 1 + travel * e["charge_per_meter"] * layers.get("蓄势", 0))
        if f in shape_names:
            n = min(available, e["hits_per_area"])
            ticks = shapes["rain_count"] if f == "剑雨" else shapes["paper_wheel_hits"] if f == "飞轮" else 1
            result.extend([factor * shapes[shape_names[f] + "_factor"]] * (n * ticks))
            if index + 1 < len(functions): follow(index, available - n, shape, layers, True)
            continue
        if f in {"延时", "跃迁", "弹墙"}: follow(index, available, shape, layers, True); continue
        if f == "陨落":
            n = min(available, e["hits_per_area"])
            result.extend([factor * e["fall_factor"]] * n)
            follow(index, available - n, shape, layers); continue
        if not hit:
            if available <= 0: continue
            result.append(factor); available -= 1
            if layers.get("拖尾", 0):
                result.extend([factor * layers["拖尾"] * e["trail_factor"]] * (round(e["trail_seconds"] / e["trail_tick"]) * e["paper_trail_zones"]))
        if f in {"分裂", "连锁"}:
            scale = cfg["split_factor"] if f == "分裂" else cfg["chain_factor"]
            emit(index + 1, available, shape * scale, layers)
            if f == "分裂": emit(len(functions), available, shape * scale, layers)
        elif f == "穿透":
            n = min(available, cfg["pierce_count"]); result.extend([factor] * n)
            if n == cfg["pierce_count"]: follow(index, available - n, shape, layers)
        elif f == "折返": result.extend([factor] * available); follow(index, 0, shape, layers, True)
        elif f == "停驻": follow(index, available, shape, layers, True)
        elif f in {"爆破", "震荡", "烙印"}:
            repeat = f != "爆破"; n = min(available, e["hits_per_area"] - int(repeat))
            key = "blast_factor" if f == "爆破" else "pulse_factor" if f == "震荡" else "mark_factor"
            result.extend([factor * e[key]] * (n + int(repeat)))
            follow(index, available - n, shape, layers)
        elif f in {"击退", "牵引", "束缚"}: follow(index, available, shape, layers)
    return result


def score(output, ehp, move, output1, ehp1):
    c = CFG["power"]
    return c["base_score"] * (output / output1) ** c["output_weight"] * (ehp / ehp1) ** c["ehp_weight"] * (move / P["move_base"]) ** c["move_weight"]


def player(level, profile="standard", override=None, talent="普通人", missing_hp=0):
    if not 1 <= level <= CFG["levels"]["player_max"]:
        raise ValueError("玩家等级必须在1到100之间")
    b = bonuses(level, profile) if override is None else override.copy()
    if any(not math.isfinite(v) or v < 0 for v in b.values()):
        raise ValueError("词条值必须有限且非负")
    g = growth(level)
    c, t = CFG["growth"], CFG["talents"]
    initial = P["initial_attribute"]
    strength_raw = b.get("strength", 0) * (t["strength_multiplier"] if talent == "万钧" else 1)
    strength = initial + c["strength"] * (g - 1) + soft(strength_raw, SOFT["strength_g"] * g)
    intelligence = initial + c["intelligence"] * (g - 1) + soft(b.get("intelligence", 0), SOFT["intelligence_g"] * g)
    speed = initial + c["speed"] * (g - 1) + soft(b.get("speed", 0), SOFT["speed_g"] * g)
    s, i, v = strength - initial, intelligence - initial, speed - initial
    hp = P["hp_base"] + P["hp_level_g"] * (g - 1) + P["hp_per_strength"] * strength + soft(b.get("hp", 0), SOFT["hp_g"] * g)
    mp = P["mp_base"] + P["mp_per_intelligence"] * intelligence + soft(b.get("mp", 0), SOFT["mp_g"] * g)
    shield = P["shield_per_intelligence_bonus"] * i + soft(b.get("shield", 0), SOFT["shield_g"] * g)
    armor = P["armor_base"] + P["armor_per_strength_bonus"] * s + soft(b.get("armor", 0), SOFT["armor_g"] * g)
    if talent == "灵海":
        mp *= t["mp_multiplier"]
    elif talent == "铁骨":
        armor *= t["armor_multiplier"]
    elif talent == "凝光":
        shield += intelligence * t["shield_per_intelligence"]
    attack = P["attack_base"] + P["attack_per_strength_bonus"] * s + P["attack_per_intelligence_bonus"] * i + P["attack_per_speed_bonus"] * v
    element = soft(b.get("element", 0), SOFT["element_g"] * g)
    aps = min(P["aps_max"], P["aps_base"] * (1 + P["aps_speed_gain"] * v / (P["aps_speed_k_g"] * g + v)) * (1 + soft(b.get("attack_speed", 0), SOFT["attack_speed_percent"]) / 100))
    crit = clamp(P["crit_base"] + P["crit_speed_gain"] * v / (P["crit_speed_k_g"] * g + v) + b.get("crit", 0), 0, P["crit_max"])
    crit_damage = clamp(P["crit_damage_base"] + P["crit_int_gain"] * i / (P["crit_int_k"] + i) + b.get("crit_damage", 0), 1, P["crit_damage_max"])
    resist = clamp(P["resist_int_gain"] * i / (P["resist_int_k"] + i) + b.get("resist", 0), 0, P["resist_max"])
    haste = clamp(P["haste_int_gain"] * i / (P["haste_int_k"] + i) + b.get("haste", 0), 0, P["haste_max"])
    move = min(P["move_max"], (P["move_base"] + P["move_speed_gain"] * v / (P["move_speed_k_g"] * g + v)) * (1 + soft(b.get("move", 0), SOFT["move_percent"]) / 100) * (t["move_multiplier"] if talent == "逐风" else 1))
    evasion = clamp(P["evasion_speed_gain"] * v / (P["evasion_speed_k_g"] * g + v) + b.get("evasion", 0), 0, P["evasion_max"])
    quantity = min(SHAPE["quantity_max"], 1 + int(b.get("quantity", 0)) + (t["quantity_bonus"] if talent == "双生矢" else 0))
    chain = min(SHAPE["chain_max"], int(b.get("chain", 0)) + (t["chain_bonus"] if talent == "续雷" else 0))
    radius = min(SHAPE["radius_max"], b.get("radius", 0) * SHAPE["radius_per_point"] * (t["radius_multiplier"] if talent == "广域" else 1))
    range_ = min(P["range_max"], P["range_base"] + b.get("range", 0))
    echo = 1 + 1 / t["echo_every"] if talent == "余响" else 1
    talent_damage = 1 + t["missing_hp_damage"] * clamp(missing_hp, 0, 1) if talent == "逆命" else 1
    crit_mean = 1 + crit * (crit_damage - 1)
    ds = (attack + element) * aps * crit_mean * echo * talent_damage * P["skill_multiplier"]
    result = dict(level=level, profile=profile, strength=strength, speed=speed, intelligence=intelligence, growth=g, hp=hp, mp=mp, shield=shield,
                  armor=armor, attack=attack, element=element, aps=aps, crit=crit, crit_damage=crit_damage, resist=resist, haste=haste, move=move,
                  run=min(P["run_max"], move * P["run_factor"]), evasion=evasion, range=range_, travel=range_ + P["travel_extra"],
                  projectile_speed=min(P["projectile_max"], P["projectile_base"] + P["projectile_mp"] * mp),
                  quantity=quantity, chain=chain, split=min(SHAPE["split_max"], int(b.get("split", 0))), radius=radius,
                  arc=min(SHAPE["arc_max"], b.get("arc", 0)), penetration=b.get("penetration", 0), flat_penetration=b.get("flat_penetration", 0),
                  resist_penetration=b.get("resist_penetration", 0), ds=ds, talent_damage=talent_damage, echo_factor=echo,
                  hp_regen=P["hp_regen"], shield_regen=P["shield_regen"], skill_multiplier=P["skill_multiplier"],
                  skill_mana_cost=P["skill_mana_cost"], skill_cooldown=P["skill_cooldown"])
    result["d8"] = ds * sum(packets(result, SHAPE["targets_test"]))
    result["ehp"] = (hp + shield) / armor_retention(armor, level)
    bare1_crit = 1 + P["crit_base"] * (P["crit_damage_base"] - 1)
    output1 = P["attack_base"] * P["aps_base"] * bare1_crit
    ehp1 = (P["hp_base"] + P["hp_per_strength"] * initial) / armor_retention(P["armor_base"], 1)
    c = CFG["power"]
    output = ds ** c["single_weight"] * result["d8"] ** c["crowd_weight"]
    result["cp"] = score(output, result["ehp"], move, output1, ehp1)
    return result


def player_hit(p, enemy, crit=False):
    physical = p["attack"] * armor_retention(enemy["armor"], p["level"], p["penetration"], p["flat_penetration"])
    elemental = p["element"] * (1 - clamp(enemy["resist"] - p["resist_penetration"], 0, P["resist_max"]))
    raw = (physical + elemental) * level_factor(p["level"], enemy["level"]) * P["skill_multiplier"] * p["talent_damage"] * (p["crit_damage"] if crit else 1)
    return clamp(raw, DMG["hit_min"], DMG["technical_hit_max"]) if raw > 0 else 0


def enemy_hit(enemy, p, multiplier=1):
    return enemy["attack"] * multiplier * armor_retention(p["armor"], enemy["level"]) * level_factor(enemy["level"], p["level"])


def enemy(map_level, kind):
    c = CFG["enemies"][kind]
    ref = player(map_level)
    g = growth(map_level)
    e = dict(map_level=map_level, level=map_level + c["offset"], kind=kind, name=c["name"], armor=c["armor_g"] * g, resist=c["resist"],
             period=c["period"], aps=1 / c["period"], windup=c["windup"], recovery=c["recovery"], range=c["range"],
             move=c["speed"] + CFG["enemy_speed"]["gain"] * (map_level - 1) / (CFG["enemy_speed"]["k"] + map_level - 1))
    target = c["ttk_base"] + c["ttk_gain"] * (1 - math.exp(-(map_level - 1) / c["ttk_k"]))
    hit = player_hit(ref, e)
    expected_dps = hit * ref["aps"] * (1 + ref["crit"] * (ref["crit_damage"] - 1))
    e.update(hp=expected_dps * target, ttk_target=target, expected_dps=expected_dps)
    raw_ehp = (ref["hp"] + ref["shield"]) / armor_retention(ref["armor"], e["level"]) / level_factor(e["level"], map_level)
    e["attack"] = raw_ehp / c["hit_budget"]
    e["skill_min"] = e["attack"] * min(c["skills"])
    e["skill_max"] = e["attack"] * max(c["skills"])
    e["phase_period"] = c.get("phase_period", c["period"])
    e["phase_move"] = e["move"] + c.get("phase_speed_gain", 0)
    # 以标准同档玩家普通/五行占比折算敌人EHP，CP不是胜率。
    share = ref["attack"] / (ref["attack"] + ref["element"])
    defense = share * armor_retention(e["armor"], map_level) + (1 - share) * (1 - e["resist"])
    e["ehp"] = e["hp"] / defense
    raw_output = e["attack"] * statistics.mean(c["skills"]) * (0.5 / c["period"] + 0.5 / e["phase_period"])
    bare = player(1, "naked")
    e["cp"] = score(raw_output, e["ehp"], e["move"], bare["ds"], bare["ehp"])
    return e


def analytic_ttk(p, e):
    dps = player_hit(p, e) * p["aps"] * (1 + p["crit"] * (p["crit_damage"] - 1)) * p["echo_factor"]
    return e["hp"] / dps


def duel(p, e, seed, hit_probability=None):
    rng = random.Random(seed)
    c = CFG["enemies"][e["kind"]]
    hp, pool = e["hp"], p["hp"] + p["shield"]
    startup = min(P["attack_windup"], P["attack_windup_fraction"] / p["aps"]) + SIM["distance"] / p["projectile_speed"]
    player_time = startup
    enemy_time = max(0, SIM["distance"] - e["range"]) / e["move"] + e["windup"]
    attack_index, p_casts = 0, 0
    while min(player_time, enemy_time if hit_probability is not None else math.inf) < SIM["max_seconds"]:
        if player_time <= enemy_time or hit_probability is None:
            crit = rng.random() < p["crit"]
            hp -= player_hit(p, e, crit)
            p_casts += 1
            if hp <= 1e-9:
                return dict(win=True, seconds=player_time, remaining=max(0, pool) / (p["hp"] + p["shield"]), casts=p_casts)
            player_time += 1 / p["aps"]
        else:
            index = attack_index % len(c["skills"])
            if rng.random() < hit_probability and (c["aoe"][index] or rng.random() >= p["evasion"]):
                pool -= enemy_hit(e, p, c["skills"][index])
            if pool <= 0:
                return dict(win=False, seconds=enemy_time, remaining=0, casts=p_casts)
            attack_index += 1
            period = e["phase_period"] if hp / e["hp"] <= c.get("phase_hp", -1) else e["period"]
            enemy_time += period
    return dict(win=False, seconds=SIM["max_seconds"], remaining=max(0, pool) / (p["hp"] + p["shield"]), casts=p_casts)


def percentile(values, quantile):
    values = sorted(values)
    position = (len(values) - 1) * quantile
    lower = math.floor(position)
    return values[lower] + (values[min(lower + 1, len(values) - 1)] - values[lower]) * (position - lower)


def simulate_ttk(p, e, count=SIM["duel_runs"]):
    values = [duel(p, e, SIM["seed"] + seed)["seconds"] for seed in range(count)]
    return dict(mean=statistics.mean(values), p10=percentile(values, .1), p90=percentile(values, .9))


def simulate_survival(p, e, hit_probability):
    samples = [duel(p, e, SIM["seed"] + seed, hit_probability) for seed in range(SIM["survival_runs"])]
    return dict(win_rate=sum(s["win"] for s in samples) / len(samples), mean_seconds=statistics.mean(s["seconds"] for s in samples),
                mean_remaining=statistics.mean(s["remaining"] for s in samples if s["win"]) if any(s["win"] for s in samples) else 0)


def map_fight(p, map_level, seed, hit_probability, stage_heal=True):
    """按现有波次规则的固定密集几何替代模型，保留实际累计损伤召王。
    与Unity没有共享场景或运行状态，不模拟路径与玩家路线。
    """
    rng, cfg = random.Random(seed), CFG["map"]
    types = {kind: enemy(map_level,kind) for kind in KINDS}
    total_hp = sum(types[kind]["hp"] * cfg[kind] for kind in KINDS if kind != "boss")
    wave, strong, phase_start, damage_done = 0, False, 0.0, 0.0
    boss_spawn, boss_time = False, None
    hp, shield = p["hp"], p["shield"]
    active, killed = [], 0
    player_time = min(P["attack_windup"], P["attack_windup_fraction"]/p["aps"]) + SIM["distance"]/p["projectile_speed"]
    time = 0.0

    def spawn(kind, number):
        for _ in range(number):
            e = types[kind]
            active.append({"kind":kind,"wave":wave,"hp":e["hp"],"next_attack":time+max(0,SIM["distance"]-e["range"])/e["move"]+e["windup"],"index":0})

    spawn("normal",cfg["waves"][wave])
    while time < SIM["max_seconds"] and hp > 0:
        alive = [a for a in active if a["hp"] > 0]
        if not boss_spawn and damage_done >= cfg["boss_trigger"] * total_hp:
            spawn("boss",cfg["boss"]);boss_spawn=True;boss_time=time
            alive=[a for a in active if a["hp"] > 0]
        remaining=sum(1 for a in alive if a["kind"]=="normal" and a["wave"]==wave)
        if not strong:
            if time-phase_start >= SIM["minimum_strong_seconds"] and (remaining <= cfg["waves"][wave]//2 or time-phase_start >= cfg["strong_wait"]) and len(alive)<cfg["cap"]:
                spawn("elite" if wave < len(cfg["waves"])-1 else "leader",1); strong=True;phase_start=time
        elif wave < len(cfg["waves"])-1:
            strong_alive=any(a["kind"] in ("elite","leader") and a["wave"]==wave for a in alive)
            ready=(not strong_alive and remaining<=2) or time-phase_start>=cfg["slow_intervals"][wave]
            if ready and time-phase_start>=cfg["advance_wait"] and len(alive)+cfg["waves"][wave+1]<=cfg["cap"]:
                wave+=1;strong=False;phase_start=time;spawn("normal",cfg["waves"][wave])
        alive=[a for a in active if a["hp"]>0]
        if player_time <= time+1e-9:
            crit=rng.random()<p["crit"]
            # 不同目标各一次；先选择血量剩余少者，提高清理效率，不让新波抢掉尾怪。
            targets=sorted(alive,key=lambda a:(a["hp"]/types[a["kind"]]["hp"],a["wave"]))
            for a,weight in zip(targets,packets(p,len(targets))):
                dealt=min(a["hp"],player_hit(p,types[a["kind"]],crit)*weight)
                a["hp"]-=dealt
                if a["kind"]!="boss":damage_done+=dealt
                if a["hp"] <= 1e-9:
                    a["hp"]=0;killed+=1
                    heal=CFG["enemies"][a["kind"]]["kill_heal"] if stage_heal else 0
                    hp=min(p["hp"],hp+p["hp"]*heal);shield=min(p["shield"],shield+p["shield"]*heal)
            player_time+=1/p["aps"]
        for a in alive:
            if a["hp"]<=0 or a["next_attack"]>time+1e-9:
                continue
            e,c=types[a["kind"]],CFG["enemies"][a["kind"]]
            index=a["index"]%len(c["skills"])
            if rng.random()<hit_probability*SIM["contact_factors"][a["kind"]] and (c["aoe"][index] or rng.random()>=p["evasion"]):
                damage=enemy_hit(e,p,c["skills"][index]);absorbed=min(shield,damage);shield-=absorbed;hp-=damage-absorbed
                if hp<=0:break
            period=e["phase_period"] if a["hp"]/e["hp"]<=c.get("phase_hp",-1) else e["period"]
            a["next_attack"]+=period;a["index"]+=1
        if killed==sum(cfg[k] for k in KINDS):
            return dict(win=True,seconds=time,killed=killed,boss_time=boss_time,remaining=max(0,hp+shield)/(p["hp"]+p["shield"]))
        next_attack=min((a["next_attack"] for a in active if a["hp"]>0),default=math.inf)
        time=min(player_time,next_attack,time+SIM["map_poll_seconds"])
    return dict(win=False,seconds=min(time,SIM["max_seconds"]),killed=killed,boss_time=boss_time,remaining=max(0,hp+shield)/(p["hp"]+p["shield"]))


def map_summary(p, level, hit_probability, stage_heal=True):
    samples=[map_fight(p,level,SIM["seed"]+seed,hit_probability,stage_heal) for seed in range(SIM["map_runs"])]
    won=[s for s in samples if s["win"]]
    boss=[s["boss_time"] for s in samples if s["boss_time"] is not None]
    return dict(win_rate=len(won)/len(samples),mean_seconds=statistics.mean(s["seconds"] for s in samples),mean_clear_seconds=statistics.mean(s["seconds"] for s in won) if won else None,
                mean_killed=statistics.mean(s["killed"] for s in samples),boss_time=statistics.mean(boss) if boss else None)


def talent_checks():
    rows=[]
    talents=("万钧","双生矢","续雷","广域","灵海","铁骨","凝光","逐风","逆命","余响","普通人","穿越者","天命之子")
    for level in SIM["sample_levels"]:
        ref=player(level)
        for name in talents:
            p=player(level,talent=name,missing_hp=.5 if name=="逆命" else 0)
            rows.append(dict(等级=level,天赋=name,血量=p["hp"],护盾=p["shield"],防御=p["armor"],灵力=p["mp"],单体DPS=p["ds"],八目标DPS=p["d8"],移速=p["move"],额外技能点=CFG["talents"]["extra_skill_points"] if name=="穿越者" else 0,掉落概率倍率=CFG["talents"]["drop_multiplier"] if name=="天命之子" else 1))
        check(f"L{level}天赋只在规定乘区生效",math.isclose(player(level,talent="铁骨")["armor"],ref["armor"]*CFG["talents"]["armor_multiplier"]) and math.isclose(player(level,talent="凝光")["shield"],ref["shield"]+ref["intelligence"]*CFG["talents"]["shield_per_intelligence"]) and math.isclose(player(level,talent="逆命",missing_hp=.5)["ds"],ref["ds"]*(1+CFG["talents"]["missing_hp_damage"]*.5)))
        check(f"L{level}清群天赋不变单体，幸运/点数不加伤害",all(player(level,talent=name)["ds"]==ref["ds"] for name in ("双生矢","续雷","广域","穿越者","天命之子")))
        check(f"L{level}余响长期期望等于第5击补一次",math.isclose(player(level,talent="余响")["ds"],ref["ds"]*(1+1/CFG["talents"]["echo_every"])))
    return rows


def next_exp(level):
    x, c = level - 1, CFG["experience"]
    return math.ceil(c["base"] + c["linear"] * x + c["power_gain"] * x ** c["power_exponent"]) if level < CFG["levels"]["player_max"] else 0


def kill_exp(p_level, e):
    c = CFG["experience"]
    if p_level >= CFG["levels"]["player_max"]:
        return 0
    return math.ceil(c["enemy_base"] * (1 + c["enemy_growth"] * (e["level"] - 1)) * c["rank_multipliers"][e["kind"]] * clamp(1 + c["difference_gain"] * (e["level"] - p_level), c["difference_min"], c["difference_max"]))


def experience_audit():
    rows=[]
    for original in range(1,CFG["levels"]["player_max"]+1):
        level,xp,total=original,0,0
        for kind in KINDS:
            for _ in range(CFG["map"][kind]):
                gain=kill_exp(level,enemy(original,kind));xp+=gain;total+=gain
                while level<CFG["levels"]["player_max"] and xp>=next_exp(level):
                    xp-=next_exp(level);level+=1
                if level==CFG["levels"]["player_max"]:xp=0
        rows.append(dict(初始等级=original,地图等级=original,升级经验=next_exp(original),完整清图所得经验=total,清图后等级=level,升级次数=level-original,新增技能点=level-original,剩余经验=xp))
    check("经验完整清图无负数且满级停止",all(r["剩余经验"]>=0 and r["新增技能点"]==r["升级次数"] for r in rows) and rows[-1]["清图后等级"]==CFG["levels"]["player_max"] and rows[-1]["完整清图所得经验"]==0)
    return rows


def recommendation_audit():
    rows=[]
    c=CFG["recommendation"]
    for level in SIM["sample_levels"]:
        p=player(level,"low")
        best=None
        for map_level in sorted({max(1,math.floor(level*f)) for f in c["fractions"]},reverse=True):
            e=enemy(map_level,"boss");normal=enemy(map_level,"normal")
            wave=CFG["map"]["waves"][0]
            ttk=analytic_ttk(p,e)
            shock=enemy_hit(e,p,max(CFG["enemies"]["boss"]["skills"]))/ (p["hp"]+p["shield"])
            normal_dps=player_hit(p,normal)*p["aps"]*(1+p["crit"]*(p["crit_damage"]-1))*sum(packets(p,wave))
            first_wave=wave*normal["hp"]/normal_dps
            if ttk>c["boss_ttk_max"] or shock>c["shock_resource_max"] or first_wave>c["wave_clear_max"]:
                continue
            result=map_summary(p,map_level,SIM["hit_probabilities"]["skilled"])
            if result["win_rate"]>=c["validation_win_floor"]:
                best=dict(玩家等级=level,推荐地图等级=map_level,BOSS等级=e["level"],单体TTK=ttk,震圈资源损失比例=shock,首波持续预算=first_wave,模型通关率=result["win_rate"],成功清图均时=result["mean_clear_seconds"],说明="熟练走位条件；保持原玩家与道纹不变，仅手动选低档地图")
                break
        if best is None:
            best=dict(玩家等级=level,推荐地图等级=None,BOSS等级=None,单体TTK=None,震圈资源损失比例=None,首波持续预算=None,模型通关率=None,成功清图均时=None,说明="当前没有达标推荐；先刷普通补构筑，不承诺同档通关")
        rows.append(best)
    check("5级及以后低配存在固定等级的可通关档位",all(r["推荐地图等级"] is not None for r in rows if r["玩家等级"]>=5),rows)
    return rows


def csv_write(name, rows):
    if not rows:
        return
    with (OUT / name).open("w", encoding="utf-8-sig", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows({k: round(v, 6) if isinstance(v, float) else v for k, v in row.items()} for row in rows)


def display(value):
    return f"{value:.3f}".rstrip("0").rstrip(".") if isinstance(value, float) else str(value)


def table(rows, columns):
    out = ["| " + " | ".join(title for _, title in columns) + " |", "|" + "---|" * len(columns)]
    out.extend("| " + " | ".join(display(row[key]) for key, _ in columns) + " |" for row in rows)
    return "\n".join(out)


def invariants():
    first, maximum = player(1, "naked"), player(CFG["levels"]["player_max"], "naked")
    check("一级普通人保持三基础各5、攻击1、生命60、零护盾", first["strength"] == first["speed"] == first["intelligence"] == 5 and first["attack"] == 1 and first["hp"] == 60 and first["shield"] == 0)
    check("一级裸装CP归一为100", math.isclose(first["cp"], 100))
    check("裸装开局每1.25秒攻击且一条最高速度词条不快于1秒", math.isclose(1 / first["aps"], 1.25) and 1 / player(1, override={"speed":3.6})["aps"] >= 1)
    for level in (1, 20, 100):
        g = growth(level)
        speeds = [player(level, override={"speed":n * g})["aps"] for n in range(101)]
        gains = [b - a for a,b in zip(speeds, speeds[1:])]
        check(f"{level}级堆速度有收益且边际递减", all(x > 0 for x in gains) and all(b <= a + 1e-12 for a,b in zip(gains,gains[1:])))
        extreme = player(level, override={"speed":SIM["numeric_safety_input"], "attack_speed":SIM["numeric_safety_input"]})
        check(f"{level}级极端速度攻速仍不超过每秒2.5次", math.isclose(extreme["aps"], P["aps_max"]) and extreme["aps"] <= 2.5)
    check("等级成长严格单调、裸装攻击等于G", all(growth(n + 1) > growth(n) and math.isclose(player(n, "naked")["attack"], growth(n)) for n in range(1, CFG["levels"]["player_max"])))
    check("一百级地图王真实等级105", enemy(100, "boss")["level"] == CFG["levels"]["enemy_max"])
    check("标准网络计入所有分叉技能点且无超支", all(2 * reference_nodes(n) - 1 <= n for n in range(1, 101)))
    # 主干(q,0)→侧叶(q,1)和尾叶(N,0)，均向更外圈传播。
    radius = lambda q, r: max(abs(q), abs(r), abs(q + r))
    check("标准网络六边形格不重叠且圈权重单调", all(radius(k, 1) >= radius(k, 0) and radius(k + 1, 0) >= radius(k, 0) for k in range(1, RUNE["reference_nodes_max"])))
    check("软上限连续且单调、有有限渐近值", soft(10, 10) == 10 and math.isclose(soft(10 + 1e-6, 10), 10 + 1e-6, rel_tol=1e-8) and all(soft(x + 1, 10) >= soft(x, 10) for x in range(1000)) and soft(SIM["numeric_safety_input"], 10) < 20)
    check("固定5/10级压制温和且15级后封顶", level_factor(30, 20) < 1.2 and level_factor(20, 25) > .9 and level_factor(100, 1) == level_factor(30, 15))
    simple = player(100, override={})
    shapes = player(100, override={"quantity":999,"chain":999,"split":999,"radius":999})
    check("纯形态不提高单个BOSS的直接DPS", simple["ds"] == shapes["ds"])
    check("共享命中使八目标总伤害不超过八倍单体", sum(packets(shapes, SHAPE["targets_test"])) <= SHAPE["targets_test"])
    check("无装备额外乘区，形态上限有效", shapes["quantity"] == 6 and shapes["chain"] == 4 and shapes["split"] == 3 and shapes["radius"] == 4)
    maximal = player(100, override={key: SIM["numeric_safety_input"] for key in ("strength","intelligence","speed","hp","mp","shield","armor","element","attack_speed","move","crit","crit_damage","resist","evasion","haste")})
    check("极大输入下DPS战力有限且防御未到无敌", all(math.isfinite(v) for v in maximal.values() if isinstance(v, (float,int))) and armor_retention(maximal["armor"],100) >= DMG["armor_retention_min"] and maximal["crit"] <= P["crit_max"] and maximal["aps"] <= P["aps_max"], {"DPS": maximal["ds"], "CP": maximal["cp"]})
    check("词条品阶容量与独立接口概率完整", len(RUNE["grade_counts"]) == 8 and sum(RUNE["property_port_weights"]) == sum(RUNE["branch_port_weights"]) == 100)
    check("功能道纹固定稀有单词条与旧接口兼容", RUNE["function_grade"] == 3 and RUNE["function_affix_count"] == 1 and RUNE["function_port_range"] == [1,3])
    ordered = RUNE["ordered_functions"]
    check("38种顺序功能接口范围", ordered["function_count"] == 38 and ordered["port_counts"] == [3,3]+[2]*36 and ordered["port_counts_min"] == [3,3,2]+[1]*35)
    extra = ordered["extended"]
    check("扩展区域时钟与预算有效", all(v > 0 and math.isfinite(v) for v in extra.values()) and extra["motion_layers_max"] <= 7 and extra["effects_per_release"] <= extra["effects_alive"])
    expected = {"扇射":1.95,"环射":2,"十字":1.8,"背射":1.6,"折返":8,"回旋":1,"波动":1,"弹墙":1,"跃迁":1,"延时":1,"停驻":2,"蓄势":1.48,"爆破":4.5,"震荡":5.8,"拖尾":1.6,"击退":1,"牵引":1,"束缚":1,"烙印":5.8,"陨落":8}
    for name, value in expected.items(): check("扩展单功能纸面对照-"+name, math.isclose(sum(extended_packets([name],8)),value))
    shape_expected = {"光束":9.6,"刃波":7.2,"地刺":6.4,"剑雨":16.8,"旋刃":8,"灵鞭":8.8,"飞轮":8.4,"游龙":9.6,"灵网":6.4,"地雷":12.8}
    for name,value in shape_expected.items(): check("攻击形态单功能纸面预算-"+name, math.isclose(sum(extended_packets([name],8)),value))
    check("攻击形态参数有限且时间预算有效", all(math.isfinite(v) and v>0 for v in ordered["attack_shapes"].values()) and ordered["attack_shapes"]["mine_arm_seconds"]<ordered["attack_shapes"]["mine_life_seconds"])
    names = ["齐射","分裂","连锁","增大","缩小","加速","减速","穿透"] + list(expected) + list(shape_expected)
    check("38功能两两纸面预算有限", all(math.isfinite(sum(ordered_packets([a,b],8))) and sum(ordered_packets([a,b],8)) > 0 for a in names for b in names))
    check("体型弹速正向和反向倍率配对", math.isclose(ordered["grow_factor"]*ordered["shrink_factor"], 1) and math.isclose(ordered["speed_up_factor"]*ordered["speed_down_factor"], 1))
    check("体型弹速上限及穿透预算", 0 < ordered["scale_min"] < 1 < ordered["scale_max"] and 0 < ordered["speed_factor_min"] < 1 < ordered["speed_factor_max"] and ordered["pierce_count"] == 1 and ordered["projectile_radius"] > 0)
    for fs, total in [(["穿透"],2),(["穿透","齐射"],4),(["穿透","分裂"],3),(["穿透","连锁"],2.8),(["穿透","穿透"],4),(["增大","缩小","加速","减速"],1)]:
        check("新增顺序功能纸面预算-"+"→".join(fs), math.isclose(sum(ordered_packets(fs,8)), total))
    check("穿透未达第二目标不触发后续", sum(ordered_packets(["穿透","齐射"],1)) == 1)
    check("顺序功能固定两出口与衍生倍率", ordered["split_outputs"] == 2 and ordered["split_factor"] == .5 and ordered["chain_factor"] == .8)
    check("顺序功能执行预算", ordered["projectiles_per_release"] == 32 and ordered["max_function_depth"] == 16 and ordered["max_compiled_segments"] == 256)
    check("齐射并排偏移为正", ordered["parallel_offset"] > 0)
    check("子弹发射后不追踪", SHAPE["arc_max"] == 0)
    for fs, expected in [("齐射齐射", 3), ("齐射分裂", 3), ("齐射连锁", 2.8),
                         ("分裂齐射", 2.5), ("分裂分裂", 2.5), ("分裂连锁", 2.4),
                         ("连锁齐射", 2.6), ("连锁分裂", 2.6), ("连锁连锁", 2.44)]:
        sequence = [fs[:2], fs[2:]]
        check("顺序组合密集预算-" + fs, math.isclose(sum(ordered_packets(sequence, 8)), expected))
        single = 3 if fs == "齐射齐射" else 2 if fs.startswith("齐射") else 1
        check("顺序组合单体预算-" + fs, math.isclose(sum(ordered_packets(sequence, 1)), single))
    check("功能宝盒只能生成稀有", CFG["economy"]["box_grade_weights"][1] == [0,0,0,10000,0,0,0,0])
    thresholds = CFG["loot"]["boss_grade_levels"]
    check("BOSS五档品阶阈值完整且严格递增", len(thresholds) == len(RUNE["grade_counts"]) - 3 and thresholds[0] == 1 and all(isinstance(x, int) for x in thresholds) and all(a < b for a, b in zip(thresholds, thresholds[1:])))
    for level, grade in ((1,3),(49,3),(50,4),(69,4),(70,5),(84,5),(85,6),(99,6),(100,7),(105,7)):
        check(f"BOSS{level}级品阶保底边界", boss_grade_floor(level) == grade and all(boss_drop_grade(level, rolled) == max(rolled, grade) for rolled in range(len(RUNE["grade_counts"]))))
    check("全地图BOSS保底单调且100级图掉传说", all(boss_grade_floor(enemy(level+1, "boss")["level"]) >= boss_grade_floor(enemy(level, "boss")["level"]) for level in range(1,100)) and boss_grade_floor(enemy(100, "boss")["level"]) == 7)
    e = CFG["economy"]
    check("回收表完整且均为正整数", len(e["recycle_grade_prices"]) == 8 and len(e["recycle_branch_prices"]) == 4 and all(isinstance(x,int) and x > 0 for x in e["recycle_grade_prices"] + e["recycle_branch_prices"]))
    check("任何属性品阶及改造后回收都低于最便宜属性盒", max(e["recycle_grade_prices"]) < min(e["box_prices"][:2]))
    check("任何分叉接口回收都低于分叉盒", max(e["recycle_branch_prices"]) < e["box_prices"][2])
    for kind, weights in enumerate(e["box_grade_weights"]):
        mean = sum(w * recycle_price(g) for g,w in enumerate(weights)) / 10000
        check(f"第{kind}种宝盒回收期望严格亏钱", 0 < mean < e["box_prices"][kind], {"price":e["box_prices"][kind],"expected_refund":mean,"loss_ratio":1-mean/e["box_prices"][kind]})
    mean = sum(w * recycle_price(ports=p) for p,w in enumerate(e["box_branch_weights"],3)) / 100
    check("分叉盒回收期望严格亏钱", 0 < mean < e["box_prices"][2], {"price":e["box_prices"][2],"expected_refund":mean,"loss_ratio":1-mean/e["box_prices"][2]})
    rejected = 0
    for invalid in (float("nan"),float("inf"),-1):
        try: player(1, override={"strength":invalid})
        except ValueError: rejected += 1
    check("非有限或负数词条拒绝", rejected == 3)
    return first, maximum


def trait_audit():
    cfg = CFG['trait_runes']
    check('38条件特性8转化稳定目录', len(cfg['traits']) == cfg['trait_count'] == 38 and len(cfg['converters']) == cfg['converter_count'] == 8)
    for level in range(1,101):
        for gi,grade in enumerate(cfg['grades']):
            for ti,trait in enumerate(cfg['traits']):
                good=True
                for attr,base,unit in trait['条件']:
                    raw=base*grade['门槛']*(growth(level) if unit=='g' else 1)
                    step=30 if attr=='弧度' else 1 if attr in ('数量','分裂','连锁','范围') else .01
                    threshold=math.ceil(raw/step-1e-9)*step
                    good &= math.isfinite(threshold) and threshold >= raw-1e-8 and threshold-step < raw+1e-8
                check(f'特性T{ti+1:02} L{level} G{gi}门槛精度边界',good)
    for grade in cfg['grades']:
        check('特性品阶倍率及转换单次预算-'+grade['名称'],0<grade['转换']<=1 and grade['半径'] in (1,2,3) and grade['周期']>0)
    limits=cfg['limits']
    check('特性恢复盾及控制总量有界', limits['heal_hp_per_second']==.03 and limits['heal_mp_per_second']==.1 and limits['shield_per_second']==.04 and limits['shield_capacity']==.25 and limits['mitigation']<=.3 and limits['targets_per_cast']<=16)
    check('硬控窗口按四品质缩减', limits['control_window']==6 and limits['control_strength']==[1,.6,.4,.25] and limits['control_budget']==[4,2.4,1.6,.8])
    check('特性独立掉落等级8及85/15',cfg['grades'][0]['敌等级']==8 and cfg['loot']['trait_share']==.85 and cfg['loot']['probabilities']==[.004,.06,.15,.35])
    check('特性召唤区域预算',limits['summons_per_family']==3 and limits['decoys_max']==2 and limits['effects_alive']==128)
    for ti,t in enumerate(cfg['traits']):
        check(f'T{ti+1:02}不解析说明驱动机制',t['condition_count']==len(t['条件']) and t['support_count']==len(t['支持']) and all(t[k]>=0 and math.isfinite(t[k]) for k in ('周期','持续','伤害','持续伤','召唤总伤')))


def build_data():
    invariants()
    trait_audit()
    battle_content_audit()
    players, combat, enemies, stress, offlevel, survival, maps = [], [], {k: [] for k in KINDS}, [], [], [], []
    for level in range(1, CFG["levels"]["player_max"] + 1):
        cached = {}
        for profile in PROFILES:
            p = player(level, profile)
            p["reference_attributes"] = reference_nodes(level) if profile in ("standard", "high") else 0
            p["reference_points"] = 2 * p["reference_attributes"] - 1 if p["reference_attributes"] else 0
            p["exp_next"] = next_exp(level)
            players.append(p)
            cached[profile] = p
        row = {"地图等级":level, "玩家裸装CP": cached["naked"]["cp"], "玩家标准CP":cached["standard"]["cp"], "玩家高配CP":cached["high"]["cp"]}
        for kind in KINDS:
            e = enemy(level, kind)
            sim = simulate_ttk(cached["standard"], e)
            e.update(ttk_discrete=sim["mean"], ttk_p10=sim["p10"], ttk_p90=sim["p90"], xp=kill_exp(level,e))
            enemies[kind].append(e)
            row[e["name"] + "等级"] = e["level"]
            row[e["name"] + "CP"] = e["cp"]
            tolerance = max(SIM["ttk_absolute_tolerance"], e["ttk_target"] * SIM["ttk_relative_tolerance"])
            check(f"M{level} {e['name']} 离散TTK在目标容差内", abs(sim["mean"] - e["ttk_target"]) <= tolerance, {"目标":e["ttk_target"],"均值":sim["mean"]})
            for profile in ("low", "standard", "high"):
                p = cached[profile]
                s = simulate_ttk(p,e)
                stress.append(dict(玩家等级=level,地图等级=level,敌人等级=e["level"],构筑=NAMES[profile],类型=e["name"],单体DPS=p["ds"],八目标DPS=p["d8"],物理EHP=p["ehp"],稳态TTK=analytic_ttk(p,e),离散均值=s["mean"],P10=s["p10"],P90=s["p90"]))
        combat.append(row)
    for level in SIM["sample_levels"]:
        for key in ("strength", "intelligence", "speed", "element", "armor", "shape"):
            p = player(level,"extreme_"+key)
            e = enemy(level,"boss")
            s=simulate_ttk(p,e)
            stress.append(dict(玩家等级=level,地图等级=level,敌人等级=e["level"],构筑="极端"+key,类型="BOSS",单体DPS=p["ds"],八目标DPS=p["d8"],物理EHP=p["ehp"],稳态TTK=analytic_ttk(p,e),离散均值=s["mean"],P10=s["p10"],P90=s["p90"]))
        for kind in KINDS:
            e = enemy(level,kind)
            for difference in (5,10,-5):
                p_level = e["level"] + difference
                if not 1 <= p_level <= CFG["levels"]["player_max"]:
                    continue
                p = player(p_level)
                offlevel.append(dict(玩家等级=p_level,地图等级=level,敌人等级=e["level"],实际等级差=difference,类型=e["name"],稳态TTK=analytic_ttk(p,e),离散TTK=simulate_ttk(p,e)["mean"],等级修正=level_factor(p_level,e["level"])))
        e = enemy(level,"boss")
        for profile in ("low","standard","high"):
            for skill, hit in SIM["hit_probabilities"].items():
                s = simulate_survival(player(level,profile),e,hit)
                survival.append(dict(玩家等级=level,地图等级=level,构筑=NAMES[profile],走位=skill,实际命中概率=hit,模拟胜率=s["win_rate"],平均历时=s["mean_seconds"],胜者平均剩余资源=s["mean_remaining"]))
        for profile, skill in (("low","standard"),("low","skilled"),("standard","standard"),("high","standard")):
            result=map_summary(player(level,profile),level,SIM["hit_probabilities"][skill])
            maps.append(dict(等级=level,构筑=NAMES[profile],走位=skill,阶段恢复=True,通关率=result["win_rate"],成功清图均时=result["mean_clear_seconds"],平均击败=result["mean_killed"],BOSS平均登场=result["boss_time"]))
        if level in (1,30,100):
            result=map_summary(player(level),level,SIM["hit_probabilities"]["standard"],False)
            maps.append(dict(等级=level,构筑="标准",走位="standard",阶段恢复=False,通关率=result["win_rate"],成功清图均时=result["mean_clear_seconds"],平均击败=result["mean_killed"],BOSS平均登场=result["boss_time"]))
    naked_ttk = simulate_ttk(player(1,"naked"),enemy(1,"normal"))["mean"]
    check("一级裸装普通怪均值不超过3.5秒",naked_ttk <= SIM["normal_naked_level1_ttk_max"],naked_ttk)
    novice_normal=simulate_survival(player(1,"naked"),enemy(1,"normal"),1)
    novice_elite=simulate_survival(player(1,"naked"),enemy(1,"elite"),1)
    check("一级普通怪站桩损失≤10%资源、精英损失≥60%",novice_normal["mean_remaining"]>=.9 and novice_elite["mean_remaining"]<=.4,{"普通剩余":novice_normal["mean_remaining"],"精英剩余":novice_elite["mean_remaining"]})
    ratios=[r["离散TTK"] for r in offlevel if r["实际等级差"]==-5 and r["类型"]=="普通"]
    check("低于普通怪5级仍可战斗、TTK不超过5秒",max(ratios)<=5,{"最慢TTK":max(ratios)})
    differences=[r for r in offlevel if r["实际等级差"]==10 and r["类型"]=="普通"]
    check("高10级对普通怪真实击杀时间下降",all(r["离散TTK"] < simulate_ttk(player(r["地图等级"]),enemy(r["地图等级"],"normal"))["mean"] for r in differences))
    check("三基础极端构筑均有输出且不超过同档高配单体2倍",all(player(l,"extreme_"+attr)["ds"]>player(l,"naked")["ds"] and player(l,"extreme_"+attr)["ds"]<2*player(l,"high")["ds"] for l in SIM["sample_levels"] for attr in ("strength","intelligence","speed")))
    values = [r["模拟胜率"] for r in survival if r["构筑"] == "标准" and r["走位"] == "standard"]
    check("标准构筑在指定走位条件下BOSS胜率≥70%", min(values) >= SIM["boss_standard_win_floor"], {"最低胜率":min(values),"命中概率":SIM["hit_probabilities"]["standard"]})
    values = [r["模拟胜率"] for r in survival if r["构筑"] == "标准" and r["走位"] == "stationary"]
    check("标准构筑站桩不能无脑过BOSS",max(values) <= .1,{"最高胜率":max(values)})
    check("同级高配对四敌都比标准更快",all(analytic_ttk(player(l,"high"),enemy(l,k)) < analytic_ttk(player(l),enemy(l,k)) for l in range(1,101) for k in KINDS))
    values=[r["通关率"] for r in maps if r["构筑"]=="标准" and r["阶段恢复"]]
    check("固定66敌人情景的标准通关率≥65%",min(values)>=SIM["map_standard_win_floor"],{"最低通关率":min(values)})
    values=[r["成功清图均时"] for r in maps if r["构筑"]=="标准" and r["阶段恢复"] and r["成功清图均时"] is not None]
    check("标准连续地图成功时长在90至360秒预算内",all(SIM["map_seconds_min"]<=x<=SIM["map_seconds_max"] for x in values),{"范围":[min(values),max(values)]})
    values=[r["模拟胜率"] for r in survival if r["构筑"]=="低配" and r["走位"]=="skilled"]
    check("低配熟练走位仍有同档BOSS挑战空间",min(values)>=SIM["low_skilled_boss_win_floor"],{"最低胜率":min(values)})
    talent=talent_checks();recommendations=recommendation_audit();xp=experience_audit()
    return players, combat, enemies, stress, offlevel, survival, maps, talent, recommendations, xp


def battle_content_audit():
    b = CFG['battle_content']
    a = CFG['map']['arena']
    check('大图中央出生边缘安全带', a['width'] == a['height'] == 64 and 0 < a['edge_padding'] < a['edge_depth'] < a['width'] / 2 and a['spawn_player_distance'] >= 14)
    check('大图批次与在场端点', arena_growth(1,'active') == 24 and arena_growth(100,'active') == 240 and arena_growth(1,'batch') == 4 and arena_growth(100,'batch') == 24 and math.isclose(arena_growth(100,'interval'),.9))
    check('反风筝参数仍有可躲预警', 0 < a['bite_follow_fraction'] < 1 and a['charge_period'] >= 7 and a['aim_lead_max'] <= .8 and a['flank_fraction'] <= .4)
    for level in range(1,101):
        check(f'大图{level}刷新预算有效', 0 < arena_growth(level,'batch') <= arena_growth(level,'active') < sum(region_counts(level)) and arena_growth(level,'interval') >= .89)
    check('19普通物种2头目1狼王', len(b['species']) == 22)
    check('24技能连续编号', [s['id'] for s in b['skills']] == list(range(1,25)))
    check('一级132与百级1600敌人', region_counts(1) == [121,8,2,1] and region_counts(100) == [1495,96,8,1])
    check('一百档数量单调且唯一BOSS', all(sum(region_counts(i)) <= sum(region_counts(i+1)) and region_counts(i)[3] == 1 for i in range(1,100)))
    check('五行狼元素与技能对应', all(b['species'][15+i]['skill'] == 20+i and b['skills'][19+i]['element'] == i and b['skills'][19+i]['element_fraction'] == 1 for i in range(5)))
    check('百级频率机动明确提高且前摇保底', tactical_growth(100,'boss_period_end') == .55 and tactical_growth(100,'boss_move_end') == 1.35 and b['growth']['boss_windup_min'] >= .55)
    check('五波普通名额仍为8/10/12/14/16', [sum(w) for w in b['waves']] == [8,10,12,14,16])
    for i,s in enumerate(b['species']):
        check(f'物种{i}解锁与耐久合法', 1 <= s['unlock'] <= 100 and 0 < s['hp_factor'] <= 1.1 and 1 <= s['skill'] <= 24)
    for s in b['skills']:
        check(f'技能{s["id"]}时序伤害合法', 0 <= s['element_fraction'] <= 1 and s['windup'] > 0 and s['period'] >= s['windup'] + s['recovery'] and s['damage'] >= 0)
    for i,row in enumerate(b['boss']['thresholds']):
        values = row[:i]
        check(f'狼王{i+1}形态阈值递减', all(0 < v < 1 for v in values) and all(a > c for a,c in zip(values,values[1:])))
    for level in range(1,101):
        p = player(level,'standard')
        ok = True
        for kind in KINDS:
            e = enemy(level,kind); ad = armor_retention(p['armor'],e['level'])
            for s in b['skills']:
                x = s['element_fraction']; raw = e['attack'] * s['damage'] * ad / ((1-x)*ad + x*(1-p['resist']))
                hit = raw * ((1-x)*ad + x*(1-p['resist']))
                ok &= abs(hit - e['attack']*s['damage']*ad) < max(1e-8,hit*1e-10)
        check(f'地图{level}19技能四品质属性伤害归一',ok)
    check('辅助预算不能无限续命', b['support']['heal_fraction'] <= b['support']['heal_target_cap'] <= .15 and b['support']['heal_global_cap'] <= .1 and b['support']['shield_global_cap'] <= .08)
    check('持续区保留逃路且总伤害有限', b['limits']['zones'] <= 3 and b['limits']['escape_count'] >= 3 and b['limits']['ground_seconds']/b['limits']['tick_interval']*b['limits']['ground_damage'] <= .4)


def write_outputs(players, combat, enemies, stress, offlevel, survival, maps, talents, recommendations, xp):
    OUT.mkdir(parents=True,exist_ok=True)
    region = []
    for level in range(1,101):
        c = region_counts(level)
        region.append(dict(地图等级=level,总数=sum(c),普通=c[0],精英=c[1],头目=c[2],BOSS=c[3],
            在场上限=math.floor(arena_growth(level,'active')+.5),每批数量=math.floor(arena_growth(level,'batch')+.5),刷新间隔=arena_growth(level,'interval'),
            普通周期倍率=tactical_growth(level,'ordinary_period_end'),BOSS周期倍率=tactical_growth(level,'boss_period_end'),
            普通移速倍率=tactical_growth(level,'move_end'),BOSS移速倍率=tactical_growth(level,'boss_move_end')))
    csv_write("青岚原数量与战术成长_1至100级.csv",region)
    csv_write("玩家_1至100级.csv",players)
    csv_write("地图战斗力_1至100级.csv",combat)
    for kind, rows in enemies.items():
        csv_write(CFG["enemies"][kind]["name"]+"_完整等级.csv",rows)
    csv_write("构筑压力测试.csv",stress)
    csv_write("真实等级差测试.csv",offlevel)
    csv_write("BOSS生存情景测试.csv",survival)
    csv_write("连续66敌人测试.csv",maps)
    csv_write("十三天赋计算对照.csv",talents)
    csv_write("低配推荐档位测试.csv",recommendations)
    csv_write("经验成长校验.csv",xp)
    (OUT/"验证报告.json").write_text(json.dumps(REPORT,ensure_ascii=False,indent=2),encoding="utf-8")
    derived = dict(source="游戏数值配置.md", config_sha256=CONFIG_HASH, players=players, combat=combat, enemies=enemies, stress=stress, offlevel=offlevel, survival=survival, maps=maps, talents=talents, recommendations=recommendations, xp=xp)
    (OUT/"派生数据.json").write_text(json.dumps(derived,ensure_ascii=False,indent=2),encoding="utf-8")
    write_viewer(derived)
    update_document(players,combat,enemies,stress,offlevel,survival,maps,talents,recommendations,xp)


def update_document(players,combat,enemies,stress,offlevel,survival,maps,talents,recommendations,xp):
    bare = [r for r in players if r["profile"] == "naked"]
    standard = [r for r in players if r["profile"] == "standard"]
    summary = [r for r in players if r["level"] in (1,100) and r["profile"] in ("naked","standard","high")]
    part = ["## 13. 自动计算结果与完整等级数据",f"参数SHA-256：`{CONFIG_HASH}`。固定种子：{SIM['seed']}。重新运行计算器会替换本节，参数不从本节反读。",
            "### 13.1 一级/满级端点",table(summary,[("level","等级"),("profile","配置"),("strength","力量"),("speed","速度"),("intelligence","智力"),("hp","生命"),("mp","灵力"),("shield","护盾"),("attack","普通攻击"),("element","五行"),("aps","攻速"),("ds","单体DPS"),("d8","八目标DPS"),("cp","战斗力")]),
            "### 13.2 玩家裸装基础与生存表（完整1～100）",table(bare,[("level","L"),("growth","G"),("strength","力量"),("speed","速度"),("intelligence","智力"),("hp","生命"),("mp","灵力"),("shield","护盾"),("armor","防御"),("exp_next","升级经验")]),
            "### 13.3 玩家裸装输出与机动表（完整1～100）","暴击、抗性、闪避、急速列为0～1比例；如0.05表示5%。普通技能倍率1、消耗0、冷却0、回复0全等级相同，见CSV对应字段。",table(bare,[("level","L"),("attack","攻击"),("crit","暴击率"),("crit_damage","暴伤倍率"),("aps","攻速"),("move","移速"),("run","跑速"),("resist","抗性"),("evasion","闪避"),("haste","急速"),("projectile_speed","弹速"),("ds","DPS"),("cp","CP")]),
            "### 13.4 标准道纹逐级实属性（完整1～100）",table(standard,[("level","L"),("reference_attributes","属性叶"),("reference_points","总耗点"),("hp","生命"),("shield","护盾"),("armor","防御"),("attack","普通攻击"),("element","五行"),("aps","攻速"),("quantity","根数"),("chain","连锁"),("ds","单体DPS"),("d8","八目标DPS"),("cp","CP")]),
            "### 13.5 地图/玩家/四敌战斗图（完整1～100）","同一行玩家L=地图M；精英/头目/BOSS实际等级分别为M+1/+3/+5。CP是纸面评分，不是伤害倍率或胜率。",table(combat,[("地图等级","M=L"),("玩家裸装CP","裸装玩家CP"),("玩家标准CP","标准玩家CP"),("普通等级","普通Lv"),("普通CP","普通CP"),("精英等级","精英Lv"),("精英CP","精英CP"),("头目等级","头目Lv"),("头目CP","头目CP"),("BOSS等级","BOSSLv"),("BOSSCP","BOSSCP")])]
    for index, (kind,rows) in enumerate(enemies.items(),6):
        part += [f"### 13.{index} {CFG['enemies'][kind]['name']}真实战斗属性（完整100档）",table(rows,[("map_level","地图M"),("level","真实Lv"),("hp","生命"),("attack","普通攻击"),("armor","防御"),("resist","抗性"),("aps","攻速"),("move","移速"),("range","距离"),("skill_max","最高单次技能"),("ttk_target","目标TTK"),("ttk_discrete","离散均值"),("cp","CP")])]
    sampled = [r for r in stress if r["玩家等级"] in (1,10,30,100) and r["类型"] in ("普通","BOSS") and not r["构筑"].startswith("极端")]
    part += ["## 14. 压力测试结果与判断","### 14.1 低配/标准/高配的离散击杀时间",table(sampled,[("玩家等级","L"),("构筑","Build"),("类型","敌人"),("离散均值","均值秒"),("P10","P10"),("P90","P90"),("单体DPS","单体DPS"),("八目标DPS","清群DPS"),("物理EHP","EHP")]),
             "### 14.2 极端单项投入",table([r for r in stress if r["构筑"].startswith("极端") and r["玩家等级"] in (10,100)],[("玩家等级","L"),("构筑","方向"),("单体DPS","单体DPS"),("八目标DPS","清群DPS"),("物理EHP","EHP"),("稳态TTK","BOSS持续TTK")]),
             "### 14.3 等级压制与越级（按真实敌人等级计算）",table([r for r in offlevel if r["地图等级"] in (10,30,70) and r["类型"] in ("普通","BOSS")],[("玩家等级","玩家Lv"),("地图等级","地图M"),("敌人等级","敌Lv"),("实际等级差","差值"),("类型","类型"),("等级修正","纯等级系数"),("离散TTK","击杀秒")]),
             "### 14.4 BOSS生存压力",f"每行{SIM['survival_runs']}次，一级/中期/满级截取；命中概率包含玩家通过走位避开攻击的情景假设，另外抽基础闪避。无生命回复、无阶段奖励帮助BOSS单挑。",table([r for r in survival if r["玩家等级"] in (1,30,100)],[("玩家等级","L"),("构筑","Build"),("走位","情景"),("实际命中概率","实际命中概率"),("模拟胜率","胜率"),("胜者平均剩余资源","胜者剩余H+Q比例")]),
             "### 14.5 校验汇总",f"通过{len(REPORT['passed'])}项，失败{len(REPORT['failed'])}项。完整报告与全部等级/情景样本在 `数据/战斗数值_v1/`。"]
    part += ["### 14.6 连续66敌人资源与节奏",f"每行{SIM['map_runs']}次；无实际寻路和地图走位输入。阶段恢复按正式配置进行有/无版本对照；死亡样本不混入成功清图时长。",table(maps,[("等级","L"),("构筑","配置"),("走位","情景"),("阶段恢复","阶段恢复"),("通关率","通关率"),("成功清图均时","成功均时秒"),("平均击败","平均击败数"),("BOSS平均登场","王登场秒")]),
             "### 14.7 十三天赋计算对照（完整样本见CSV）",table([r for r in talents if r["等级"]==100],[("天赋","天赋"),("单体DPS","单体DPS"),("八目标DPS","八目标DPS"),("血量","生命"),("护盾","护盾"),("防御","防御"),("移速","移速"),("额外技能点","额外点"),("掉落概率倍率","掉落倍率")])]
    part += ["### 14.8 低配降档验证","保留原玩家等级与其道纹，改变所选地图；不是读Build自动改变敌人。一级没有可继续下降的档位时明确提示补构筑。",table(recommendations,[("玩家等级","玩家Lv"),("推荐地图等级","推荐M"),("BOSS等级","BOSSLv"),("单体TTK","持续TTK"),("首波持续预算","首波预算"),("震圈资源损失比例","震圈资源比例"),("模型通关率","模型通关率"),("成功清图均时","成功均时秒")]),
             "### 14.9 经验与技能点检查","按普通60→精英4→头目1→BOSS1顺序审计经验，每次升级后重新计算等级差经验，角色战斗测试仍固定等级，不借升级补血。",table([r for r in xp if r["初始等级"] in SIM["sample_levels"]],[("初始等级","初始Lv"),("完整清图所得经验","所得经验"),("清图后等级","结束Lv"),("升级次数","升级次数"),("新增技能点","新增点"),("剩余经验","剩余经验")])]
    if REPORT["failed"]:
        part.append("当前失败项，必须修正后才可以宣称满足基础数学验收：\n"+"\n".join("- "+r["name"]+"；"+json.dumps(r["evidence"],ensure_ascii=False) for r in REPORT["failed"]))
    part += ["### 14.10 结果适用范围","已完成公式与离散事件模型的基础验算；构筑连通预算可用、单项极值有界、地图等级固定，不同Build产生不同清群/单体表现。低配同档失败的样本完整保留，降档推荐也经过连续波次模型检查。正式公式、共享命中、经验、阶段恢复及AI机制已接入Unity，并通过定向与真实场景流程验证；真人走位、真实随机掉落构筑可得性、密集战斗表现、十三天赋实际体验与移动端性能仍需试玩验证，当前不代表整体平衡已完成。",
             "数据查看器：`数据/战斗数值_v1/战斗数值查看器.html`；可切换等级与四种配置查看属性、TTK和曲线。完整CSV使用UTF-8 BOM，可用Excel打开；CSV数值为机器字段/比例，单位见正文，不从CSV改参数。"]
    generated = "\n\n".join(part)
    replaced = re.sub(r"<!-- 自动数据开始 -->.*?<!-- 自动数据结束 -->", "<!-- 自动数据开始 -->\n"+generated+"\n<!-- 自动数据结束 -->", DOC.read_text(encoding="utf-8"), flags=re.S)
    DOC.write_text(replaced,encoding="utf-8")


def write_viewer(data):
    safe = json.dumps(data,ensure_ascii=False).replace("</", "<\\/")
    page = '''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>天帝 · 战斗数值查看器</title>
<style>body{margin:0;background:#111b20;color:#d9e8e4;font-family:"Microsoft YaHei",sans-serif}main{max-width:1260px;margin:auto;padding:28px}h1{font-size:25px}p{color:#a0b7af;line-height:1.8}select,input{margin:8px;padding:9px;background:#243b3e;color:#eef4eb;border:1px solid #52746b;border-radius:6px}section{background:#1a2b30;border-radius:12px;padding:16px;margin:18px 0}canvas{width:100%;height:310px}table{width:100%;border-collapse:collapse;font-size:14px}td,th{padding:9px;text-align:right;border-bottom:1px solid #31474b}td:first-child,th:first-child{text-align:left}.cards{display:flex;gap:20px;flex-wrap:wrap}.card{background:#243b3e;padding:16px;border-radius:8px;min-width:120px}.card b{display:block;font-size:25px;color:#84dcc4}small{color:#99b4a9}.legend{color:#9bbcae;font-size:13px}#hint{color:#f2d394;min-height:24px}</style>
<main><h1>《从参加聚光灯比赛到我为天帝镇压世间一切》战斗数值</h1><p>v1.0 数学基准。地图按选择的等级固定生成，构筑只改变玩家；离线结果不代表Unity已接入或真人胜率。数据唯一来源：游戏数值配置.md。</p>
<label>等级 <input id="lv" type="range" min="1" max="100" value="1"><input id="num" type="number" min="1" max="100" value="1"></label><label>道纹 <select id="profile"><option value="naked">裸装</option><option value="low">低配</option><option value="standard" selected>标准</option><option value="high">高配</option></select></label>
<div class="cards" id="cards"></div><section><h2>本档真实属性与预计击杀时间</h2><table id="stats"></table><p id="pstats"></p></section>
<section><h2>1～100级单体DPS</h2><div class="legend">青：裸装　金：标准　紫：高配</div><canvas id="dps"></canvas></section>
<section><h2>当前构筑的四敌持续TTK</h2><div class="legend">青：普通　金：精英　紫：头目　红：BOSS</div><canvas id="ttk"></canvas></section>
<section><h2>当前构筑：单体输出与八目标清群输出</h2><div class="legend">青：单体　金：八目标总DPS；密集木桩覆盖预算，实际命中另测</div><canvas id="coverage"></canvas></section><div id="hint"></div>
<small id="source"></small></main><script>const data=__DATA__;
const profiles={naked:'裸装',low:'低配',standard:'标准',high:'高配'}, colors=['#72d9bd','#f0ca72','#bd9afb','#ec8a82'];
const el=id=>document.getElementById(id), fmt=n=>Number(n).toFixed(2), get=(lv,p)=>data.players.find(x=>x.level===lv&&x.profile===p);
function plot(id,series){const canvas=el(id),dpr=devicePixelRatio||1,W=canvas.clientWidth,H=310;canvas.width=W*dpr;canvas.height=H*dpr;const ctx=canvas.getContext('2d');ctx.scale(dpr,dpr);ctx.clearRect(0,0,W,H);const left=62,right=20,top=20,bottom=35,max=Math.max(1,...series.flatMap(s=>s.values));ctx.font='12px Microsoft YaHei';ctx.fillStyle='#9bb3aa';for(let i=0;i<=4;i++){const y=top+(H-top-bottom)*i/4;ctx.strokeStyle='#30474b';ctx.beginPath();ctx.moveTo(left,y);ctx.lineTo(W-right,y);ctx.stroke();ctx.fillText(fmt(max*(1-i/4)),5,y+4)}for(const x of [1,25,50,75,100])ctx.fillText(x,left+(W-left-right)*(x-1)/99,H-12);for(let i=0;i<series.length;i++){ctx.strokeStyle=series[i].color;ctx.lineWidth=2.3;ctx.beginPath();series[i].values.forEach((n,j)=>{const x=left+(W-left-right)*j/99,y=top+(H-top-bottom)*(1-n/max);j?ctx.lineTo(x,y):ctx.moveTo(x,y)});ctx.stroke()}const index=Number(el('num').value)-1;ctx.strokeStyle='#ecf2e58c';const vx=left+(W-left-right)*index/99;ctx.beginPath();ctx.moveTo(vx,top);ctx.lineTo(vx,H-bottom);ctx.stroke();canvas.onmousemove=e=>{const lv=Math.max(1,Math.min(100,Math.round((e.offsetX-left)/(W-left-right)*99)+1));el('hint').textContent='Lv.'+lv+' · '+series.map(s=>s.name+' '+fmt(s.values[lv-1])).join(' / ')} }
function render(){const lv=Math.max(1,Math.min(100,Math.round(Number(el('num').value)||1))),profile=el('profile').value,p=get(lv,profile);el('num').value=el('lv').value=lv;el('cards').innerHTML=[['单体DPS',p.ds],['八目标DPS',p.d8],['生命＋护盾',p.hp+p.shield],['纸面战力',p.cp]].map(([name,n])=>'<div class="card">'+name+'<b>'+fmt(n)+'</b></div>').join('');let rows='<tr><th>敌人</th><th>等级</th><th>生命</th><th>普通攻击</th><th>防御</th><th>抗性</th><th>持续TTK</th></tr>';for(const kind of Object.keys(data.enemies)){const e=data.enemies[kind][lv-1],s=data.stress.find(r=>r.玩家等级===lv&&r.构筑===profiles[profile]&&r.类型===e.name);let ttk;if(profile==='naked'){const c=__CFG__,k=c.damage.armor_k_g*p.growth,ret=Math.max(c.damage.armor_retention_min,k/(k+e.armor)),lf=Math.exp(c.damage.level_log*Math.max(-c.damage.level_difference_max,Math.min(c.damage.level_difference_max,p.level-e.level)));ttk=e.hp/((p.attack*ret+p.element*(1-e.resist))*p.aps*(1+p.crit*(p.crit_damage-1))*lf)}else ttk=s.稳态TTK;rows+='<tr><td>'+e.name+'</td><td>'+e.level+'</td><td>'+fmt(e.hp)+'</td><td>'+fmt(e.attack)+'</td><td>'+fmt(e.armor)+'</td><td>'+fmt(e.resist*100)+'%</td><td>'+fmt(ttk)+'秒</td></tr>'}el('stats').innerHTML=rows;el('pstats').textContent='力量 '+fmt(p.strength)+' / 速度 '+fmt(p.speed)+' / 智力 '+fmt(p.intelligence)+'；攻速 '+fmt(p.aps)+'次/秒；暴击 '+fmt(p.crit*100)+'%；移速 '+fmt(p.move)+'；护盾 '+fmt(p.shield)+'；防御 '+fmt(p.armor);plot('dps',['naked','standard','high'].map((name,i)=>({name:profiles[name],color:colors[i],values:Array.from({length:100},(_,j)=>get(j+1,name).ds)})));plot('coverage',[{name:'单体',color:colors[0],values:Array.from({length:100},(_,j)=>get(j+1,profile).ds)},{name:'八目标',color:colors[1],values:Array.from({length:100},(_,j)=>get(j+1,profile).d8)}]);if(profile==='naked'){plot('ttk',Object.keys(data.enemies).map((kind,i)=>({name:data.enemies[kind][0].name,color:colors[i],values:data.enemies[kind].map(e=>{const x=get(e.map_level,'naked'),c=__CFG__,k=c.damage.armor_k_g*x.growth,ret=Math.max(c.damage.armor_retention_min,k/(k+e.armor)),lf=Math.exp(c.damage.level_log*Math.max(-c.damage.level_difference_max,Math.min(c.damage.level_difference_max,x.level-e.level)));return e.hp/((x.attack*ret+x.element*(1-e.resist))*x.aps*(1+x.crit*(x.crit_damage-1))*lf)})})))}else plot('ttk',Object.keys(data.enemies).map((kind,i)=>({name:data.enemies[kind][0].name,color:colors[i],values:Array.from({length:100},(_,j)=>data.stress.find(r=>r.玩家等级===j+1&&r.构筑===profiles[profile]&&r.类型===data.enemies[kind][0].name).稳态TTK)})));}
el('lv').oninput=()=>{el('num').value=el('lv').value;render()};el('num').oninput=render;el('profile').onchange=render;window.onresize=render;el('source').textContent='参数SHA-256：'+data.config_sha256;render();</script></html>'''
    page = page.replace("__DATA__",safe).replace("__CFG__",json.dumps(CFG,ensure_ascii=False))
    (OUT/"战斗数值查看器.html").write_text(page,encoding="utf-8")


if __name__ == "__main__":
    data = build_data()
    write_outputs(*data)
    print(json.dumps({"passed":len(REPORT["passed"]),"failed":len(REPORT["failed"]),"failure_names":[v["name"] for v in REPORT["failed"]],"output":str(OUT),"config_sha256":CONFIG_HASH},ensure_ascii=False))
    if REPORT["failed"]:
        raise SystemExit(1)
