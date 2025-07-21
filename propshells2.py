import json

lines = []
with open("shells.txt", "r", encoding="utf8") as o:
    lines = o.read().splitlines()

shelldic = {int(line.split("\t")[0]): line.split("\t")[1] for line in lines}

with open("Info/Props.json", "r", encoding="utf8") as o:
    props = json.load(o)

for prop in props:
    if not ("ID" in prop):
        continue
    id = prop["ID"]
    if id in shelldic:
        prop["PropShell"] = shelldic[id]

with open("Info/Props2.json", "w", encoding="utf8") as o:
    json.dump(props, o)