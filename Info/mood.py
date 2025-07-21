import json

with open("Info/Blocks2.json", "r", encoding="utf8") as o:
    blocks = json.load(o)
with open("mood_blocks.txt", "r") as o:
    mood_blocks = [tuple([int(x) for x in line.split("\t")]) for line in o.read().splitlines()]

for id, fanciness, normal, cute, cool, natural, flamboyant, cheeky in mood_blocks:
    for block in blocks:
        if block["ID"] == id:
            block["Fanciness"] = fanciness
            block["Normal"] = normal
            block["Cute"] = cute
            block["Cool"] = cool
            block["Natural"] = natural
            block["Flamboyant"] = flamboyant
            block["Cheeky"] = cheeky
            break

with open("Info/Blocks2.json", "w", encoding="utf8") as o:
    json.dump(blocks, o)

blocks = None
mood_blocks = None
with open("Info/Props2.json", "r", encoding="utf8") as o:
    props = json.load(o)
with open("mood_props.txt", "r") as o:
    mood_props = [tuple([int(x) for x in line.split("\t")]) for line in o.read().splitlines()]

for id, fanciness, normal, cute, cool, natural, flamboyant, cheeky in mood_props:
    for prop in props:
        if prop["ID"] == id:
            prop["Fanciness"] = fanciness
            prop["Normal"] = normal
            prop["Cute"] = cute
            prop["Cool"] = cool
            prop["Natural"] = natural
            prop["Flamboyant"] = flamboyant
            prop["Cheeky"] = cheeky
            break

with open("Info/Props2.json", "w", encoding="utf8") as o:
    json.dump(props, o)