import re
import os
import glob

prefab_path = r"C:\Users\vicen\Documents\GitHub\RogueLikeProject\Assets\_Project\Player\Player.prefab"
with open(prefab_path, "r", encoding="utf-8") as f:
    content = f.read()

docs = content.split("--- !u!")

gameobjects = {}
components = {}

for doc in docs:
    if not doc.strip():
        continue
    match_id = re.search(r"&(\d+)", doc)
    if not match_id:
        continue
    doc_id = match_id.group(1)
    
    if "GameObject:" in doc:
        name_match = re.search(r"m_Name:\s*(.*)", doc)
        name = name_match.group(1) if name_match else "Unknown"
        gameobjects[doc_id] = {"name": name, "components": []}
    elif "MonoBehaviour:" in doc:
        script_match = re.search(r"m_Script:\s*({fileID:.*, guid: (.*?), type: \d+})", doc)
        script_guid = script_match.group(2) if script_match else "Unknown"
        go_match = re.search(r"m_GameObject:\s*({fileID: (\d+)})", doc)
        go_id = go_match.group(2) if go_match else "None"
        components[doc_id] = {"type": "MonoBehaviour", "guid": script_guid, "go_id": go_id}
    elif "Animator:" in doc:
        go_match = re.search(r"m_GameObject:\s*({fileID: (\d+)})", doc)
        go_id = go_match.group(2) if go_match else "None"
        components[doc_id] = {"type": "Animator", "go_id": go_id}

for comp_id, comp in components.items():
    go_id = comp["go_id"]
    if go_id in gameobjects:
        gameobjects[go_id]["components"].append(comp)

guid_to_name = {}
for meta_path in glob.glob("Assets/**/*.meta", recursive=True):
    with open(meta_path, "r", encoding="utf-8") as f:
        meta_content = f.read()
    guid_match = re.search(r"guid:\s*(.*)", meta_content)
    if guid_match:
        guid = guid_match.group(1).strip()
        filename = os.path.basename(meta_path)[:-5]
        guid_to_name[guid] = filename

print("GameObject Components in Player Prefab:")
for go_id, go in gameobjects.items():
    print(f"GameObject: {go['name']} (ID: {go_id})")
    for comp in go["components"]:
        if comp["type"] == "Animator":
            print("  - Animator")
        else:
            guid = comp["guid"]
            script_name = guid_to_name.get(guid, f"Unknown (GUID: {guid})")
            print(f"  - Script: {script_name}")
