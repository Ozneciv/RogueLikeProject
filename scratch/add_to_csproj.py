import os

csproj_path = r"C:\Users\vicen\Documents\GitHub\RogueLikeProject\Assembly-CSharp.csproj"
if not os.path.exists(csproj_path):
    print("CSProj file not found!")
    exit(1)

with open(csproj_path, "r", encoding="utf-8") as f:
    lines = f.readlines()

new_compile_lines = [
    '    <Compile Include="Assets\\_Project\\Scripts\\Player\\PlayerSkinReferences.cs" />\n',
    '    <Compile Include="Assets\\_Project\\Scripts\\Player\\PlayerSkinManager.cs" />\n',
    '    <Compile Include="Assets\\_Project\\Scripts\\Player\\PlayerSkinSelectorUI.cs" />\n'
]

# Let's find where to insert them. We can find a line containing PlayerModelOffset.cs or PlayerM.cs
insert_idx = -1
for i, line in enumerate(lines):
    if "PlayerModelOffset.cs" in line:
        insert_idx = i
        break

if insert_idx != -1:
    # Insert right after PlayerModelOffset.cs
    for line in reversed(new_compile_lines):
        # Avoid duplicate insertion
        if not any(line.strip() in l for l in lines):
            lines.insert(insert_idx + 1, line)
    
    with open(csproj_path, "w", encoding="utf-8") as f:
        f.writelines(lines)
    print("Successfully added new C# files to Assembly-CSharp.csproj!")
else:
    print("Could not find a place to insert in CSProj!")
