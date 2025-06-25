import os
import subprocess
import sys
import venv

venv_dir = os.path.join(os.path.dirname(__file__), "embedding_env")

# Create virtual environment if it doesn't exist
if not os.path.isdir(venv_dir):
    print("Creating virtual environment...")
    venv.create(venv_dir, with_pip=True)

python_exe = os.path.join(venv_dir, "Scripts", "python.exe")

# Ensure dependencies are installed
print("Installing dependencies...")
subprocess.check_call([python_exe, "-m", "pip", "install", "--upgrade", "pip"])
subprocess.check_call([python_exe, "-m", "pip", "install", "uvicorn", "fastapi", "sentence-transformers"])
