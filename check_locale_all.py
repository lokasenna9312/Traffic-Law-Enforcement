import re
from pathlib import Path


# Directory containing locale files
locale_dir = Path(r"Traffic Law Enforcement/Localization")
cs_path = Path(r"Traffic Law Enforcement/LocalizationKeys.cs")

# Find all locale files (*.txt)
locale_files = list(locale_dir.glob("*.txt"))

# Extract keys from LocalizationKeys.cs
cs_code = cs_path.read_text(encoding="utf-8")

# map["Key"] = ...
map_keys = re.findall(r'map\["([\w.]+)"\]', cs_code)

# AddOption(map, ..., nameof(Setting.XXX))
option_names = re.findall(r'AddOption\(map, setting, nameof\(Setting\.([A-Za-z0-9_]+)\)\)', cs_code)
option_keys = []
for name in option_names:
    option_keys.append(f"OptionLabel.{name}")
    option_keys.append(f"OptionDesc.{name}")

# Final set of code keys
code_keys = set(map_keys + option_keys)

# Key extraction pattern for locale files
key_pattern = r"^([\w.\[\]-]+)="
def extract_keys(text):
    return [m.group(1) for m in re.finditer(key_pattern, text, re.MULTILINE)]

# Main check for all locale files
def check_locale_file(locale_path):
    text = locale_path.read_text(encoding="utf-8")
    keys = extract_keys(text)
    key_set = set(keys)
    # Duplicate keys
    dupes = sorted(set([k for k in keys if keys.count(k) > 1]))
    # Missing keys
    missing = sorted(code_keys - key_set)
    # Extra keys
    extra = sorted(key_set - code_keys)
    return dupes, missing, extra

print("=== Traffic Law Enforcement Locale Key Check ===\n")
print(f"Reference key count (from code): {len(code_keys)}\n")

for locale_path in sorted(locale_files):
    lang = locale_path.stem
    print(f"--- {lang} ---")
    dupes, missing, extra = check_locale_file(locale_path)
    if dupes:
        print(f"Duplicate keys: {len(dupes)}")
        for k in dupes:
            print(f"  {k}")
    else:
        print("No duplicate keys.")
    if missing:
        print(f"Missing keys: {len(missing)}")
        for k in missing:
            print(f"  {k}")
    else:
        print("No missing keys.")
    if extra:
        print(f"Extra keys: {len(extra)}")
        for k in extra:
            print(f"  {k}")
    else:
        print("No extra keys.")
    print()
print("Check complete.")
