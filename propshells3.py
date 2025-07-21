import json

with open("Info/Props.json", "r", encoding="utf8") as o:
    props = json.load(o)

for prop in props:
    if not ("Name" in prop):
        continue

    name: str = prop["Name"].lower()
    if name.endswith(" [white]") or\
    name.endswith(" [black]") or\
    name.endswith(" [purple]") or\
    name.endswith(" [pink]") or\
    name.endswith(" [red]") or\
    name.endswith(" [green]") or\
    name.endswith(" [yellow]") or\
    name.endswith(" [blue]") or\
    name.endswith(" (white)") or\
    name.endswith(" (black)") or\
    name.endswith(" (purple)") or\
    name.endswith(" (pink)") or\
    name.endswith(" (red)") or\
    name.endswith(" (green)") or\
    name.endswith(" (yellow)") or\
    name.endswith(" (blue)"):
        filteredname = name\
            .replace(" [white]", "")\
            .replace(" [black]", "")\
            .replace(" [purple]", "")\
            .replace(" [pink]", "")\
            .replace(" [red]", "")\
            .replace(" [green]", "")\
            .replace(" [yellow]", "")\
            .replace(" [blue]", "")\
            .replace(" (white)", "")\
            .replace(" (black)", "")\
            .replace(" (purple)", "")\
            .replace(" (pink)", "")\
            .replace(" (red)", "")\
            .replace(" (green)", "")\
            .replace(" (yellow)", "")\
            .replace(" (blue)", "")
        
        for prop2 in props:
            if not ("Name" in prop2):
                continue
            if not ("PropShell" in prop2):
                continue

            if prop2["Name"].lower() == filteredname:
                prop["PropShell"] = prop2["PropShell"]

with open("Info/Props2.json", "w", encoding="utf8") as o:
    json.dump(props, o)