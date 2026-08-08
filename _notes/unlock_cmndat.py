path = input("Type the path to your CMNDAT file: ")

with open(path, "rb") as o:
    cmndat = o.read()

if (len(cmndat) <= 0x2A41C):
    input("Not a valid CMNDAT file.")

cmndat_list = list(cmndat)

cmndat_list[0x2A415] = 0
cmndat_list[0x2A416] = 0
cmndat_list[0x2A417] = 0
cmndat_list[0x2A418] = 0
cmndat_list[0x2A419] = 0
cmndat_list[0x2A41A] = 0
cmndat_list[0x2A41B] = 0
cmndat_list[0x2A41C] = 0

cmndat = bytes(cmndat_list)

with open(path, "wb") as o:
    o.write(cmndat)