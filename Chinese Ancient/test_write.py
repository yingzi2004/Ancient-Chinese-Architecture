import os

def rewrite_file(path, replacements):
    with open(path, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
    
    for old_str, new_str in replacements:
        content = content.replace(old_str, new_str)
        
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)

print('Rewrite script created')
