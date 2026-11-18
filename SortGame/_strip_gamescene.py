# -*- coding: utf-8 -*-
"""Remove tile-play objects from GameScene. One-shot."""
import re
from pathlib import Path

path = Path(r"f:\SelfMaj\AIdeaProj\SortGame\SortClient\Assets\CABRes\Scenes\GameScene.unity")
raw = path.read_bytes()
newline = b"\r\n" if b"\r\n" in raw else b"\n"
text = raw.decode("utf-8")
text_n = text.replace("\r\n", "\n")

header, _, rest = text_n.partition("--- !u!")
chunks = rest.split("\n--- !u!")
docs = []
for i, chunk in enumerate(chunks):
    body = ("--- !u!" if i == 0 else "--- !u!") + chunk
    if i > 0:
        body = "--- !u!" + chunk
    else:
        body = "--- !u!" + chunk
    docs.append(body)

parsed = []
for body in docs:
    m = re.match(r"--- !u!(\d+) &(-?\d+)", body)
    if not m:
        raise SystemExit("bad doc: " + body[:80])
    cls = int(m.group(1))
    did = int(m.group(2))
    go = None
    gm = re.search(r"^  m_GameObject: \{fileID: (-?\d+)\}", body, re.M)
    if gm:
        go = int(gm.group(1))
    prefab = None
    pm = re.search(r"^  m_PrefabInstance: \{fileID: (-?\d+)\}", body, re.M)
    if pm:
        prefab = int(pm.group(1))
    father = None
    fm = re.search(r"^  m_Father: \{fileID: (-?\d+)\}", body, re.M)
    if fm:
        father = int(fm.group(1))
    parent = None
    tm = re.search(r"^    m_TransformParent: \{fileID: (-?\d+)\}", body, re.M)
    if tm:
        parent = int(tm.group(1))
    children = [int(x) for x in re.findall(r"^  - \{fileID: (-?\d+)\}", body, re.M)]
    # only transform children live under m_Children; the regex also hits m_Component.
    # Re-parse children from the m_Children block only.
    cm = re.search(r"^  m_Children:\n((?:  - \{fileID: -?\d+\}\n)*)", body, re.M)
    if cm:
        children = [int(x) for x in re.findall(r"fileID: (-?\d+)", cm.group(1))]
    elif re.search(r"^  m_Children: \[\]", body, re.M):
        children = []
    else:
        children = []
    name = None
    nm = re.search(r"^  m_Name: (.*)$", body, re.M)
    if nm:
        name = nm.group(1)
    target = None
    tgm = re.search(r"^  mTarget: \{fileID: (-?\d+)\}", body, re.M)
    if tgm:
        target = int(tgm.group(1))
    parsed.append({
        "id": did,
        "class": cls,
        "body": body,
        "go": go,
        "prefab": prefab,
        "father": father,
        "parent": parent,
        "children": children,
        "name": name,
        "target": target,
    })

by_id = {d["id"]: d for d in parsed}
transform_to_go = {}
for d in parsed:
    if d["class"] in (4, 224) and d["go"]:
        transform_to_go[d["id"]] = d["go"]

delete_gos = {774289891, 262644659, 1892285866}
delete_prefabs = {
    529591109,  # Dock
    3451539243946123828,  # UI Game Play
    1556912071741374067,  # UI Game Over
    7685625500847839237,  # UI Complete
}
delete_ids = {
    1060784204,  # LevelDirector
    1060784205,  # PUDirector
    1060784207,  # LevelFitter
    1060784212,  # ParticlesDirector
    1060784213,  # FloatingTextDirector
    1060784214,  # InputDirector
    1060784215,  # leftover tile component
}

changed = True
while changed:
    changed = False
    deleted_transforms = {i for i in delete_ids if i in by_id and by_id[i]["class"] in (4, 224)}
    for d in parsed:
        if d["id"] in delete_ids:
            continue
        hit = False
        if d["id"] in delete_prefabs:
            hit = True
        if d["go"] in delete_gos:
            hit = True
        if d["prefab"] in delete_prefabs and d["prefab"] != 0:
            hit = True
        if d["class"] == 1001 and d["parent"] in deleted_transforms:
            delete_prefabs.add(d["id"])
            hit = True
        if d["target"] is not None and d["target"] in delete_ids and (d["go"] in (None, 0)):
            hit = True
        if not hit:
            continue
        delete_ids.add(d["id"])
        changed = True
        if d["class"] == 1:
            delete_gos.add(d["id"])
        if d["class"] in (4, 224):
            for child in d["children"]:
                go = transform_to_go.get(child)
                if go and go not in delete_gos:
                    delete_gos.add(go)
                    changed = True

# one more sweep so children added at the end get their docs
changed = True
while changed:
    changed = False
    for d in parsed:
        if d["id"] in delete_ids:
            continue
        if d["go"] in delete_gos or (d["prefab"] in delete_prefabs and d["prefab"] != 0) or d["id"] in delete_prefabs:
            delete_ids.add(d["id"])
            changed = True
            if d["class"] == 1:
                delete_gos.add(d["id"])
            if d["class"] in (4, 224):
                for child in d["children"]:
                    go = transform_to_go.get(child)
                    if go and go not in delete_gos:
                        delete_gos.add(go)
                        changed = True

kept = []
removed_names = []
for d in parsed:
    if d["id"] in delete_ids:
        if d["name"]:
            removed_names.append(d["name"])
        continue
    body = d["body"]
    if d["id"] == 1060784196:
        body = body.replace(
            """  m_Component:
  - component: {fileID: 1060784197}
  - component: {fileID: 1060784199}
  - component: {fileID: 1060784204}
  - component: {fileID: 1060784205}
  - component: {fileID: 1060784207}
  - component: {fileID: 1060784212}
  - component: {fileID: 1060784213}
  - component: {fileID: 1060784214}
  - component: {fileID: 1060784215}
""",
            """  m_Component:
  - component: {fileID: 1060784197}
  - component: {fileID: 1060784199}
""",
        )
        body = body.replace(
            "  captureNode: {fileID: 286165251}",
            "  captureNode: {fileID: 0}",
        )
    if d["id"] == 5178066047858781992:
        body = body.replace(
            """  m_Children:
  - {fileID: 7921812722490943923}
  - {fileID: 3962967672951719357}
  - {fileID: 9148071814000728274}
  - {fileID: 3896966791145870289}
  - {fileID: 4756381618353047150}
  - {fileID: 262644660}
  - {fileID: 1892285867}
""",
            """  m_Children:
  - {fileID: 3962967672951719357}
  - {fileID: 4756381618353047150}
""",
        )
    if d["id"] == 9223372036854775807:
        body = body.replace("  - {fileID: 774289892}\n", "")
    kept.append(body)

out = header + "\n".join(kept)
if not out.endswith("\n"):
    out += "\n"

# dangling local fileIDs
kept_ids = {d["id"] for d in parsed if d["id"] not in delete_ids}
dangling = []
for i, line in enumerate(out.split("\n"), 1):
    if "guid:" in line:
        continue
    for fid in re.findall(r"fileID: (-?\d+)", line):
        n = int(fid)
        if n != 0 and n not in kept_ids:
            dangling.append((i, n, line.strip()))

path.write_bytes(out.replace("\n", newline.decode("ascii")).encode("utf-8"))

print("removed objects:", ", ".join(removed_names))
print("removed docs", len(delete_ids), "kept docs", len(kept_ids))
print("dangling", len(dangling))
for item in dangling[:40]:
    print(item)
