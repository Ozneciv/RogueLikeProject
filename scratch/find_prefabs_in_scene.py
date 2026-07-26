import os
import re

scene_path = r"c:\Users\vicen\Documents\GitHub\RogueLikeProject\Assets\_Project\Scenes\Base.unity"

guid_to_path = {}
# Walk all files in assets to find meta files mapping GUIDs to asset paths
for root, dirs, files in os.walk(r"c:\Users\vicen\Documents\GitHub\RogueLikeProject\Assets"):
    for file in files:
        if file.endswith('.meta'):
            meta_path = os.path.join(root, file)
            asset_path = meta_path[:-5]
            with open(meta_path, 'r', encoding='utf-8', errors='ignore') as f:
                content = f.read()
                guid_match = re.search(r"guid:\s*([a-f0-9]+)", content)
                if guid_match:
                    guid_to_path[guid_match.group(1)] = asset_path

with open(scene_path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

prefab_instances = []
current_prefab = None

for line in lines:
    if line.startswith("--- !u!1001"):
        if current_prefab:
            prefab_instances.append(current_prefab)
        current_prefab = {"id": line.strip(), "modifications": []}
    elif current_prefab:
        m_source = re.search(r"m_SourcePrefab: \{fileID: \d+, guid: ([a-f0-9]+), type: \d+\}", line)
        if m_source:
            current_prefab["guid"] = m_source.group(1)
        name_val = re.search(r"propertyPath: m_Name\s*\n\s*value:\s*(.*)", line)
        if name_val:
            current_prefab["name"] = name_val.group(1)
        # fallback name parsing
        if "value:" in line and len(current_prefab["modifications"]) < 10:
            current_prefab["modifications"].append(line.strip())

if current_prefab:
    prefab_instances.append(current_prefab)

for pi in prefab_instances:
    guid = pi.get("guid", "unknown")
    path = guid_to_path.get(guid, "unknown path")
    print(f"Prefab ID: {pi['id']} | GUID: {guid} | Path: {path}")
