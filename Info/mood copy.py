import json

with open("Info/Blocks2.json", "r", encoding="utf8") as o:
    blocks = json.load(o)

print("Blocks")

for block in blocks:
    if not ("Fanciness" in block):
        print(block["Name"] + " [" + str(block["ID"]) + "]")

blocks = None
with open("Info/Props2.json", "r", encoding="utf8") as o:
    props = json.load(o)

print("Props")

for prop in props:
    if not ("Fanciness" in prop):
        print(prop["Name"] + " [" + str(prop["ID"]) + "]")