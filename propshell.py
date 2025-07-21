import os

lines: list[str] = []
with open("result_stgdat01.txt", "r", encoding="utf8") as o:
    lines.extend(o.read().splitlines())
with open("result_stgdat02.txt", "r", encoding="utf8") as o:
    lines.extend(o.read().splitlines())
with open("result_stgdat03.txt", "r", encoding="utf8") as o:
    lines.extend(o.read().splitlines())
with open("result_stgdat04.txt", "r", encoding="utf8") as o:
    lines.extend(o.read().splitlines())
with open("result_stgdat05.txt", "r", encoding="utf8") as o:
    lines.extend(o.read().splitlines())
with open("result_stgdat13.txt", "r", encoding="utf8") as o:
    lines.extend(o.read().splitlines())

lines = [(int(line.split("\t")[1]), int(line.split("\t")[3])) for line in lines]
#for line in lines:
#    print(line)

propdic = {}
for prop, shell in lines:
    if prop in propdic:
        if not (shell in propdic[prop]):
            propdic[prop].append(shell)
    else:
        propdic[prop] = [shell]

go_out = []
dupes = []
for prop, shell in propdic.items():
    if len(shell) == 1:
        go_out.append(f"{prop}\t{shell[0]}")
    else:
        dupes.append(f"{prop}\t{shell}")

with open("shells.txt", "w", encoding="utf8") as o:
    o.write("\n".join(go_out))
with open("dupes.txt", "w", encoding="utf8") as o:
    o.write("\n".join(dupes))