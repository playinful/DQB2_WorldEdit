import json

with open("BGParts.json", "r", encoding="utf8") as o:
	BGParts = json.load(o)

mostfancy = 0
mostfancyids = []

for bgparts in BGParts:
	if not ("Fanciness" in bgparts):
		continue
	if not ("ID" in bgparts):
		continue
	if not ("SizeX" in bgparts and bgparts["SizeX"] > 0):
		continue
	if not ("SizeY" in bgparts and bgparts["SizeY"] > 0):
		continue
	if not ("SizeZ" in bgparts and bgparts["SizeZ"] > 0):
		continue
	if not ("Block" in bgparts and (bgparts["Block"] == "Fixture" or bgparts["Block"] == "Door" or bgparts["Block"] == "Fence")):
		continue
	fanciness = bgparts["Fanciness"] / (bgparts["SizeX"] * bgparts["SizeY"] * bgparts["SizeZ"])
	if fanciness > mostfancy:
		mostfancy = fanciness
		mostfancyids = []
	if fanciness == mostfancy:
		mostfancyids.append(bgparts["ID"])
print(mostfancy)
print(mostfancyids)
input()