#Version 3, Using update from repo
import time
import requests
import os
from colorama import init
from termcolor import colored, cprint
init()

start = time.time()

#Check the internet connection
#if the connection is available, check update DLC list from repositories.
#Overwrite new DLC list if current DLC list is the old version
url='https://raw.githubusercontent.com/Tankerch/COM3D2_DLC_Checker/master/COM_NewListDLC.lst'
def check_internet():
    timeout=3
    try:
        requests.get(url, timeout=timeout)
        return True
    except requests.ConnectionError:
        pass
    return False

if check_internet():
    r = requests.get(url, timeout=3)
    if os.path.isfile('COM_NewListDLC.lst'):
        with open('COM_NewListDLC.lst', 'r') as f:
            first_line_DLC = int(f.readline().rstrip('\n'))
        first_line_update = r.text.splitlines()[0]
        if first_line_update != "404: Not Found":
            first_line_update = int(first_line_update)
            if first_line_update > first_line_DLC:
                with open('COM_NewListDLC.lst', 'wb') as f:
                    f.write(r.content)
        else:
            input("Failed to download DLC list, please contact author")
            exit()
    else:
        if r.text.splitlines()[0] != "404: Not Found":
            with open('COM_NewListDLC.lst', 'wb') as f:
                f.write(r.content)
        else:
            input("Failed to download DLC list, please contact author")
            exit()
else:
    if not os.path.isfile('COM_NewListDLC.lst'):
        input("COM_NewListDLC.lst doesn't exist, please connect to the internet redownload it")
        exit()

#Start
print(colored("===========================================================================================", 'cyan',attrs=['bold']))
print(colored('COM_DLC_Checker', 'cyan',attrs=['bold']))
print(colored("===========================================================================================", 'cyan',attrs=['bold']))

#Open file and removing header in DLC List
line_inform = []
with open('COM_NewListDLC.lst', 'r') as f:
    for _ in range(1):
        next(f)
    for line in f:
        line_inform.append(line.rstrip('\n').split(","))
line_DLC = set(list(zip(*line_inform))[0])
line_informset = set(list(zip(*line_inform))[1])

#Make a set from gamedata folder
line_Real = set(os.listdir("GameData"))
line_Real.update(os.listdir("GameData_20"))

#Searching with intersection and linear methods
count_p = set()
for x in line_DLC.intersection(line_Real):
    for y in line_inform:
        if x == y[0]:
            count_p.add(y[1])
            line_inform.remove(y)
            break

#Separating DLC that installed with not installed
count_n = line_informset.difference(count_p)

#Show time
print(colored('Already Installed:', 'cyan',attrs=['bold']))
for x in sorted(count_p):
    print(x)

print(colored("\nNot Installed:", 'cyan',attrs=['bold']))
for x in sorted(count_n):
    print(x)

#End
end = time.time()
print("\n\nElapsed time:", end-start)
text = input("Press Enter to end program")
