import json

with open("Info/Props.json", "r", encoding="utf8") as o:
    props = json.load(o)

for prop in props:
    if not ("PropShell" in prop):
        print(f"{prop['Name']} [{prop['ID']}]")