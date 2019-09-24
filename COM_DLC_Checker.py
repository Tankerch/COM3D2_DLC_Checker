#Version 4, More user friendly, I guess ¯\_(ツ)_/¯
import time
import requests
import os
from colorama import init
from termcolor import colored, cprint
init()

start = time.time()

#Request function
url='https://raw.githubusercontent.com/Tankerch/COM3D2_DLC_Checker/master/COM_NewListDLC.lst'
def check_internet():
    timeout=2
    try:
        requests.get(url, timeout=timeout)
        return True
    except requests.ConnectionError:
        pass
    return False

#Start
print(colored("===========================================================================================", 'cyan',attrs=['bold']))
print(colored('COM_DLC_Checker', 'cyan',attrs=['bold']) + " | " + colored(' Github.com/Tankerch/COM3D2_DLC_Checker', 'cyan',attrs=['bold']))
print(colored("===========================================================================================", 'cyan',attrs=['bold']))
print("Checking internet connection : ")

#Check connection
#if connection is available, Check Update DLC list from repo
#Write new DLC list if current DLC list is old
if check_internet():
    r = requests.get(url, timeout=2)
    if os.path.isfile('COM_NewListDLC.lst'): #Check offline DLC list
        print("Connected")
        with open('COM_NewListDLC.lst', 'r') as f: #Read offline version header of DLC list
            first_line_DLC = int(f.readline().rstrip('\n'))
        first_line_update = r.text.splitlines()[0]
        if first_line_update != "404: Not Found":
            first_line_update = int(first_line_update) #Read online version header of DLC list
            if first_line_update > first_line_DLC: #Found Update
                print("Found update, updating DLC list")
                with open('COM_NewListDLC.lst', 'wb') as f:
                    f.write(r.content)
            else: #No update
                print("DLC list is up-to-date")
        else: #There's online file, but failed to load the actual content
            input("Failed to download DLC list, please contact author")
            exit()
    else: #No offline DLC list
        if r.text.splitlines()[0] != "404: Not Found":
            with open('COM_NewListDLC.lst', 'wb') as f:
                f.write(r.content)
        else:
            input("Failed to download DLC list, please contact author")
            exit()
else:
    if not os.path.isfile('COM_NewListDLC.lst'): #No connection + offline DLC list
        input("COM_NewListDLC.lst doesn't exist, Connect to the internet redownload it")
        exit()

print("\nBegin sorting, Please wait a moment")
#Open file and removing header for DLC List
line_inform = []
with open('COM_NewListDLC.lst', 'r') as f:
    for _ in range(1):
        next(f)
    for line in f:
        line_inform.append(line.rstrip('\n').split(","))
line_DLC = set(list(zip(*line_inform))[0])
line_informset = set(list(zip(*line_inform))[1])

#Check + make a set from gamedata
error = False
try:
    line_Real = set(os.listdir("GameData"))
    line_Real.update(os.listdir("GameData_20"))
except Exception:
	print("Error : Make sure to run this program in COM3D2 directories")
	error = True

if(not error):
	#Searching with intersection and linear remove searching
	count_p = set()
	for x in line_DLC.intersection(line_Real):
		for y in line_inform:
			if x == y[0]:
				count_p.add(y[1])
				line_inform.remove(y)
				break

	#Separating installed with not installed
	count_n = line_informset.difference(count_p)

	#Show time
	print('\n'+colored('Already Installed:', 'cyan',attrs=['bold']))
	for x in sorted(count_p):
		print(x)

	print(colored("\nNot Installed:", 'cyan',attrs=['bold']))
	for x in sorted(count_n):
		print(x)

#Ending & Note
end = time.time()
print("\n\nElapsed time:", end-start)
text = input("Press Enter to end program")
