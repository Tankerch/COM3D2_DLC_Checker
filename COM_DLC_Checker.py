# Version 5,
# - FIX spaghetti code
import msvcrt
import os
import sys
import winreg

import requests
from colorama import init
from termcolor import colored, cprint

# Constants, Vars, URL
# Default: Current Directory of COM3D2_DLC_Checker, will replaced by COM3D2 InstallPath Registery
GAME_DIRECTORY = os.getcwd()

CONNECTION_TIMEOUT = 5  # second
DLC_URL = r"https://raw.githubusercontent.com/Tankerch/COM3D2_DLC_Checker/master/COM_NewListDLC.lst"


def main():
    PRINT_HEADER()

    STATUS_CODE, UPDATE_CONTENT = CONNECT_TO_INTERNET()

    if(STATUS_CODE == 200):
        print("Connected to {}".format(DLC_URL))
        UPDATE_DLC_LIST(UPDATE_CONTENT)
    else:
        print("Can't connect to internet, offline file will be used")

    DLC_LIST = READ_DLC_LIST()
    GAMEDATA_LIST = READ_GAMEDATA()

    INSTALLED_DLC, NOT_INSTALLED_DLC = COMPARE_DLC(DLC_LIST, GAMEDATA_LIST)
    PRINT_DLC(INSTALLED_DLC, NOT_INSTALLED_DLC)

    EXIT_PROGRAM()


def PRINT_HEADER():
    init()
    print(colored("===========================================================================================",
                  'cyan', attrs=['bold']))
    print(colored('COM_DLC_Checker', 'cyan', attrs=['bold']) + " | " + colored(
        ' Github.com/Tankerch/COM3D2_DLC_Checker', 'cyan', attrs=['bold']))
    print(colored("===========================================================================================",
                  'cyan', attrs=['bold']))
    print("Checking internet connection : ")


def CONNECT_TO_INTERNET():
    respond = requests.get(DLC_URL, timeout=CONNECTION_TIMEOUT)
    return respond.status_code, respond.content


def UPDATE_DLC_LIST(UPDATE_CONTENT):
    with open('COM_NewListDLC.lst', 'wb') as file:
        file.write(UPDATE_CONTENT)


def READ_DLC_LIST():
    try:
        temp = []
        with open('COM_NewListDLC.lst', 'r', encoding='utf8') as lines:
            for index, line in enumerate(lines):
                # Remove DLC version in header
                if(index == 0):
                    continue
                # Else, create temp files for DLC
                temp.append(line.rstrip('\n').split(","))
            return temp
    except FileNotFoundError:
        print("COM_NewListDLC.lst file doesn't exist, Connect to the internet to download it automatically")
        EXIT_PROGRAM()


def GET_COM3D2_INSTALLPATH():
    try:
        registry_key = winreg.OpenKey(
            winreg.HKEY_CURRENT_USER, r"SOFTWARE\KISS\カスタムオーダーメイド3D2", 0, winreg.KEY_READ)
        regValue, regType = winreg.QueryValueEx(registry_key, "InstallPath")
        winreg.CloseKey(registry_key)
        if(regType == winreg.REG_SZ):
            return regValue
    except FileNotFoundError:
        print(colored(
            'Warning : COM3D2 installation directory is not set in registry. Will using work directory', 'yellow'))
        return GAME_DIRECTORY


def READ_GAMEDATA():
    GAME_DIRECTORY = GET_COM3D2_INSTALLPATH()

    try:
        GAMEDATA_LIST = set(os.listdir(
            os.path.join(GAME_DIRECTORY, "GameData")))
        GAMEDATA_LIST.update(os.listdir(
            os.path.join(GAME_DIRECTORY, "GameData_20")))
    except Exception:
        print(colored("ERROR : Cannot find 'GameData' Or 'GameData_20'", 'red'))
        print("Make sure to set COM3D2 install directory OR run this program in COM3D2 directories")
        EXIT_PROGRAM()

    return GAMEDATA_LIST


def COMPARE_DLC(DLC_LIST, GAMEDATA_LIST):
    DLC_FILENAMES = set(list(zip(*DLC_LIST))[0])
    DLC_NAMES = set(list(zip(*DLC_LIST))[1])
    # UNIT_DLC_LIST:
    # (DLC_FILENAMES,DLC_NAMES)
    INSTALLED_DLC = set()
    for INSTALLED_DLC_FILENAME in DLC_FILENAMES.intersection(GAMEDATA_LIST):
        for UNIT_DLC_LIST in DLC_LIST:
            if(INSTALLED_DLC_FILENAME == UNIT_DLC_LIST[0]):
                INSTALLED_DLC.add(UNIT_DLC_LIST[1])
                DLC_LIST.remove(UNIT_DLC_LIST)
                break

    NOT_INSTALLED_DLC = DLC_NAMES.difference(INSTALLED_DLC)

    return INSTALLED_DLC, NOT_INSTALLED_DLC


def PRINT_DLC(INSTALLED_DLC, NOT_INSTALLED_DLC):
    print('\n'+colored('Already Installed:', 'cyan', attrs=['bold']))
    for DLC_NAME in sorted(INSTALLED_DLC):
        print(DLC_NAME)

    print(colored("\nNot Installed:", 'cyan', attrs=['bold']))
    for DLC_NAME in sorted(NOT_INSTALLED_DLC):
        print(DLC_NAME)


def EXIT_PROGRAM():
    print("\nPress 'Enter' to end program")
    while True:
        if msvcrt.getch() == b'\r':
            sys.exit(0)


if __name__ == "__main__":
    main()
