import re

def repl(path, p_val, repl_text):
    with open(path, 'r', encoding='utf-8', errors='ignore') as f:
        t = f.read()
    # Replace anything before the variable/field name that is a comment or tooltip
    # But it's easier to just match the public Type varName and replace the [Tooltip] above it.
pass
