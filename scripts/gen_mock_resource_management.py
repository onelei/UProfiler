# -*- coding: utf-8 -*-
"""为指定会话生成 resourceManagement_ 与 moduleFuncStack_ 临时数据。

正式数据需 Unity 端上传，见 todo.md 对应条目。
"""
import json
import os
import random
import sys

SESSION = sys.argv[1] if len(sys.argv) > 1 else "2026_06_09_14_54_35"
FRAME_COUNT = int(sys.argv[2]) if len(sys.argv) > 2 else 748

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
random.seed(8115)

AB_PATHS = [
    "ab/ui/login_atlas.ab", "ab/ui/main_atlas.ab", "ab/scene/city_env.ab",
    "ab/char/hero_body.ab", "ab/char/npc_common.ab", "ab/effect/fx_common.ab",
]
RES_PATHS = [
    "Prefabs/UI/LoginPanel", "Prefabs/UI/MainPanel", "Textures/Icon/item_001",
    "Audio/BGM/main_theme", "Materials/Char/hero_skin",
]
PREFABS = [
    "Hero_Knight", "NPC_Vendor", "FX_Explosion", "UI_DamageNumber", "Monster_Slime",
]
SCENES = ["Login", "MainCity", "Battle"]


def scene_of(frame):
    if frame < 200:
        return SCENES[0]
    if frame < 480:
        return SCENES[1]
    return SCENES[2]


def make_events(actions, paths, count, ms_range):
    events = []
    for _ in range(count):
        frame = random.randint(1, FRAME_COUNT)
        path = random.choice(paths)
        events.append({
            "frame": frame,
            "action": random.choice(actions),
            "name": os.path.basename(path),
            "path": path,
            "scene": scene_of(frame),
            "durationMs": round(random.uniform(*ms_range), 2),
        })
    return sorted(events, key=lambda e: e["frame"])


ab_events = make_events(
    ["AssetBundle.LoadFromFile", "AssetBundle.LoadFromFileAsync", "AssetBundle.Unload"],
    AB_PATHS, 90, (0.4, 28.0))
res_events = make_events(
    ["Resources.Load", "Resources.LoadAsync", "Resources.UnloadAsset"],
    RES_PATHS, 120, (0.1, 12.0))
inst_events = make_events(
    ["Object.Instantiate", "GameObject.SetActive(true)", "GameObject.SetActive(false)", "Object.Destroy"],
    PREFABS, 160, (0.05, 9.0))


def top(events, action_kw, n=10):
    stat = {}
    for e in events:
        if action_kw.lower() not in e["action"].lower():
            continue
        key = e["path"]
        stat.setdefault(key, {"name": e["name"], "path": key, "loadMode": e["action"], "count": 0})
        stat[key]["count"] += 1
    return sorted(stat.values(), key=lambda x: -x["count"])[:n]


per1k = lambda evts, kw: round(sum(1 for e in evts if kw.lower() in e["action"].lower()) * 1000.0 / FRAME_COUNT, 2)

payload = {
    "resourcesLoadPer1k": per1k(res_events, "Load"),
    "abLoadPer1k": per1k(ab_events, "Load"),
    "instantiatePer1k": per1k(inst_events, "Instantiate"),
    "activatePer1k": per1k(inst_events, "SetActive"),
    "abLoadTop": top(ab_events, "Load"),
    "resourceLoadTop": top(res_events, "Load"),
    "instantiateTop": top(inst_events, "Instantiate"),
    "unloadTop": top(ab_events, "Unload"),
    "assetBundle": ab_events,
    "resource": res_events,
    "instantiate": inst_events,
}

MODULE_STACKS = {
    "rendering": ("渲染模块", [
        "Camera.Render", "RenderPipeline.Render", "Shadows.RenderShadowMap",
        "RenderLoop.Draw", "PostProcessing.Render", "Culling.SceneCull",
    ]),
    "ui": ("UI模块", [
        "Canvas.SendWillRenderCanvases", "CanvasRenderer.SyncTransform",
        "Graphic.Rebuild", "LayoutRebuilder.Rebuild", "EventSystem.Update",
    ]),
    "loading": ("加载模块", [
        "Resources.Load", "AssetBundle.LoadAsset", "Loading.ReadObject",
        "Texture2D.AwakeFromLoad", "Shader.Parse",
    ]),
}


def module_stack(key, title, funcs):
    total = 100.0
    rows = []
    remain = total
    for i, name in enumerate(funcs):
        pct = round(remain * random.uniform(0.25, 0.45), 2) if i < len(funcs) - 1 else round(remain, 2)
        remain = round(remain - pct, 2)
        avg = round(pct * 0.16, 2)
        frames = random.randint(300, FRAME_COUNT)
        calls = random.randint(frames, frames * 4)
        rows.append({
            "name": name,
            "avgMs": avg,
            "totalMs": round(avg * frames, 2),
            "selfMs": round(avg * random.uniform(0.3, 0.9), 2),
            "totalPct": pct,
            "selfPct": round(pct * random.uniform(0.3, 0.9), 2),
            "callCount": calls,
            "callsPerFrame": round(calls / frames, 2),
            "frameCount": frames,
        })
    return {
        "module": key,
        "scope": "overview",
        "stackMode": "module",
        "order": "forward",
        "metrics": [
            {"label": title + " CPU耗时", "avgMs": round(sum(r["avgMs"] for r in rows), 2),
             "peakMs": round(sum(r["avgMs"] for r in rows) * 2.4, 2), "peakFrame": random.randint(1, FRAME_COUNT),
             "unit": "ms", "statLabel": "均值"},
            {"label": "调用次数", "avgMs": round(sum(r["callsPerFrame"] for r in rows), 2),
             "peakMs": round(sum(r["callsPerFrame"] for r in rows) * 3, 2), "peakFrame": random.randint(1, FRAME_COUNT),
             "unit": "次", "statLabel": "每帧均值"},
        ],
        "functions": rows,
        "aiDiagnosis": [
            {"title": title + "耗时偏高", "severity": "Medium",
             "suggestion": "关注 " + rows[0]["name"] + " 的峰值帧，确认是否可分帧或缓存。"},
        ],
    }


for flavor in ("Release", "Debug"):
    session_dir = os.path.join(ROOT, "UProfiler-Server", "bin", flavor, "net8.0", "uploads", SESSION)
    if not os.path.isdir(session_dir):
        continue
    out = os.path.join(session_dir, f"resourceManagement_{SESSION}.txt")
    with open(out, "w", encoding="utf-8") as fp:
        json.dump(payload, fp, ensure_ascii=False)
    print(out)
    for key, (title, funcs) in MODULE_STACKS.items():
        out = os.path.join(session_dir, f"moduleFuncStack_{key}_{SESSION}.txt")
        with open(out, "w", encoding="utf-8") as fp:
            json.dump(module_stack(key, title, funcs), fp, ensure_ascii=False)
        print(out)
