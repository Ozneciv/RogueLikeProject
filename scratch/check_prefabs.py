import os
import re

prefab_paths = [
    r"c:\Users\vicen\Documents\GitHub\RogueLikeProject\Assets\_Project\UI\Canvas\Inventory Canvas\InventorySystem.prefab",
    r"c:\Users\vicen\Documents\GitHub\RogueLikeProject\Assets\_Project\UI\InventorySystem.prefab",
    r"c:\Users\vicen\Documents\GitHub\RogueLikeProject\Assets\_Project\Player\Player.prefab"
]

for path in prefab_paths:
    if os.path.exists(path):
        print(f"Checking {path}...")
        with open(path, 'r', encoding='utf-8') as f:
            content = f.read()
            # Find any MonoBehaviour with m_Script fileID 0
            mono_behaviours = re.findall(r"MonoBehaviour:.*?\n(.*?)(?=\n---|Element|$)", content, re.DOTALL)
            for mb in mono_behaviours:
                if "m_Script: {fileID: 0}" in mb:
                    print("  Found MonoBehaviour with missing/null script reference!")
                # check for GUIDs that are missing
                script_guid_match = re.search(r"m_Script: \{fileID: \d+, guid: ([a-f0-9]+), type: \d+\}", mb)
                if script_guid_match:
                    guid = script_guid_match.group(1)
                    # We could check if meta file for this guid exists, but let's just print it if it looks suspicious
    else:
        print(f"Path does not exist: {path}")
