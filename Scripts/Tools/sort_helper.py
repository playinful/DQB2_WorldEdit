import json

xin = input().lower().split(" ")

if "dqb1" in xin:
    directory: str = "DQB1"
else:
    directory: str = "DQB2"

file: str = None
if "blocks" in xin:
    file = "Blocks"
if "bgparts" in xin:
    file = "BGParts"
if "items" in xin:
    file = "Items"

if (file is None):
    exit()

multiply: bool = False
multiplication_factor: float = 1
if "multiply" in xin and xin.index("multiply") < len(xin) - 1:
    multiply = True
    multiplication_factor = float(xin[xin.index("multiply") + 1])

push_up: bool = False
push_up_from: int = 0
push_up_by: int = 0
if "push_up" in xin and xin.index("push_up") < len(xin) - 2:
    push_up = True
    push_up_from = int(xin[xin.index("push_up") + 1])
    push_up_by = int(xin[xin.index("push_up") + 2])

push_down: bool = False
push_down_from: int = 0
push_down_by: int = 0
if "push_down" in xin and xin.index("push_down") < len(xin) - 2:
    push_down = True
    push_down_from = int(xin[xin.index("push_down") + 1])
    push_down_by = int(xin[xin.index("push_down") + 2])

pull_up: bool = False
pull_up_from: int = 0
pull_up_by: int = 0
if "pull_up" in xin and xin.index("pull_up") < len(xin) - 2:
    pull_up = True
    pull_up_from = int(xin[xin.index("pull_up") + 1])
    pull_up_by = int(xin[xin.index("pull_up") + 2])

pull_down: bool = False
pull_down_from: int = 0
pull_down_by: int = 0
if "pull_down" in xin and xin.index("pull_down") < len(xin) - 2:
    pull_down = True
    pull_down_from = int(xin[xin.index("pull_down") + 1])
    pull_down_by = int(xin[xin.index("pull_down") + 2])

print(f"multiply: {multiply}, {multiplication_factor}")
print(f"push_up: {push_up}, {push_up_from}, {push_up_by}")
print(f"push_down: {push_down}, {push_down_from}, {push_down_by}")
print(f"pull_up: {pull_up}, {pull_up_from}, {pull_up_by}")
print(f"pull_up: {pull_down}, {pull_down_from}, {pull_down_by}")

with open(f"Info/{directory}/{file}.json", "r", encoding="utf8") as o:
    Data = json.load(o)

for datum in Data:
    if (not ("Sort" in datum)) or (datum["Sort"] is None):
        continue

    if multiply:
        datum["Sort"] = (int)(datum["Sort"] * multiplication_factor)

    if (push_up) and (datum["Sort"] >= push_up_from):
        datum["Sort"] = (int)(datum["Sort"] + push_up_by)
    if (push_down) and (datum["Sort"] < push_down_from):
        datum["Sort"] = (int)(datum["Sort"] - push_down_by)
    if (pull_up) and (datum["Sort"] < pull_up_from):
        datum["Sort"] = (int)(datum["Sort"] + pull_up_by)
    if (pull_down) and (datum["Sort"] >= pull_down_from):
        datum["Sort"] = (int)(datum["Sort"] - pull_down_by)

with open(f"Info/{directory}/{file}_out.json", "w", encoding="utf8") as o:
    json.dump(Data, o, ensure_ascii=False, indent=4)