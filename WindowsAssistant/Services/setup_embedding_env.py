import os
import sys
import subprocess
import urllib.request
import zipfile
import shutil
import platform

REQUIRED_VERSION = "3.10.9"
PYTHON_EMBED_URL = f"https://www.python.org/ftp/python/{REQUIRED_VERSION}/python-{REQUIRED_VERSION}-embed-amd64.zip"
TEMP_PYTHON_DIR = os.path.abspath("py310_embed")
VENV_DIR = os.path.abspath("embedding_env")

def download_and_extract_embeddable_python():
    print(f"Downloading Python {REQUIRED_VERSION} embeddable...")
    zip_path = os.path.join(TEMP_PYTHON_DIR, "python_embed.zip")
    os.makedirs(TEMP_PYTHON_DIR, exist_ok=True)

    urllib.request.urlretrieve(PYTHON_EMBED_URL, zip_path)

    with zipfile.ZipFile(zip_path, 'r') as zip_ref:
        zip_ref.extractall(TEMP_PYTHON_DIR)

    print("Download and extraction complete.")

    # Python embeddable requires a config file to allow site packages
    with open(os.path.join(TEMP_PYTHON_DIR, "python310._pth"), "a") as f:
        f.write("\nimport site\n")

def get_embedded_python_exe():
    return os.path.join(TEMP_PYTHON_DIR, "python.exe")

def ensure_python_310():
    # Check system for python3.10
    try:
        output = subprocess.check_output(["py", "-3.10", "--version"], stderr=subprocess.STDOUT)
        print("Using system Python 3.10")
        return ["py", "-3.10"]
    except:
        print("Python 3.10 not found. Using embeddable zip.")
        if not os.path.exists(get_embedded_python_exe()):
            download_and_extract_embeddable_python()
        return [get_embedded_python_exe()]

def create_virtual_env(python_command):
    print("Creating virtual environment...")
    subprocess.check_call(python_command + ["-m", "venv", VENV_DIR])

def install_dependencies():
    pip_path = os.path.join(VENV_DIR, "Scripts", "pip.exe")
    subprocess.check_call([pip_path, "install", "--upgrade", "pip"])
    subprocess.check_call([pip_path, "install", "uvicorn", "fastapi", "sentence-transformers", "pydantic<2.0", "TTS"])

if __name__ == "__main__":
    if platform.system() != "Windows":
        print("This setup script currently only supports Windows.")
        sys.exit(1)

    try:
        python_cmd = ensure_python_310()
        create_virtual_env(python_cmd)
        install_dependencies()
        print("Setup complete.")
    except Exception as e:
        print(f"Setup failed: {e}")
        sys.exit(1)
