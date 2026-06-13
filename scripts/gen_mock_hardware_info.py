# -*- coding: utf-8 -*-
"""为指定会话生成 hardwareInfo_ 临时数据（CPU频率/网络收发/low memory 事件）。

正式数据需 Unity 端上传，见 todo.md「hardwareInfo_」条目。
"""
import json
import math
import os
import random
import sys

SESSION = sys.argv[1] if len(sys.argv) > 1 else "2026_06_09_14_54_35"
FRAME_COUNT = int(sys.argv[2]) if len(sys.argv) > 2 else 748

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
random.seed(8115)

samples = []
low_memory_frames = {221, 581}  # 需与采样帧 range(1, N, 5) 对齐
for frame in range(1, FRAME_COUNT + 1, 5):
    # CPU 频率围绕 1.8GHz 波动，中段模拟降频
    base = 1804.8
    if 300 <= frame <= 420:
        base = 1420.0
    freq = base + 120 * math.sin(frame / 23.0) + random.uniform(-40, 40)

    recv = max(0.0, 8 + 6 * math.sin(frame / 17.0) + random.uniform(0, 4))
    sent = max(0.0, 3 + 2 * math.sin(frame / 31.0) + random.uniform(0, 2))
    if frame % 150 == 0:
        recv += random.uniform(180, 420)  # 模拟突发下载

    samples.append({
        "frameIndex": frame,
        "cpuFreqMHz": round(freq, 2),
        "netSentKB": round(sent, 2),
        "netRecvKB": round(recv, 2),
        "lowMemory": frame in low_memory_frames,
    })

payload = {
    "targetFrameRate": 60,
    "networkType": "WIFI",
    "samples": samples,
}

for flavor in ("Release", "Debug"):
    session_dir = os.path.join(ROOT, "UProfiler-Server", "bin", flavor, "net8.0", "uploads", SESSION)
    if not os.path.isdir(session_dir):
        continue
    out = os.path.join(session_dir, f"hardwareInfo_{SESSION}.txt")
    with open(out, "w", encoding="utf-8") as fp:
        json.dump(payload, fp, ensure_ascii=False)
    print(out, len(samples), "samples")
