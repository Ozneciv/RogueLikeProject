import re

prefab_path = r"C:\Users\vicen\Documents\GitHub\RogueLikeProject\Assets\_Project\Player\Player.prefab"
with open(prefab_path, "r", encoding="utf-8") as f:
    content = f.read()

# Let's find GameObjects and their names
names = re.findall(r"m_Name:\s*(.*)", content)
print("GameObjects in Player.prefab:")
for name in set(names):
    print(f" - {name}")
