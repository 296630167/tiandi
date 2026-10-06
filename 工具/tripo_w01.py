"""Generate and download the W01 cultivator from its local reference image."""

import argparse
import json
import mimetypes
import os
from pathlib import Path
import sys
import time
import urllib.error
import urllib.request
import uuid


ROOT = Path(__file__).resolve().parents[1]
IMAGE = ROOT / "output/imagegen/prologue/W01_reference.png"
OUTPUT = ROOT / "生成/tripo/W01"
STATE = OUTPUT / "task.json"
BASE = "https://openapi.tripo3d.ai/v3"
ESTIMATED_CREDITS = 60
PARAMETERS = {
    "model": "v3.1-20260211",
    "enable_image_autofix": False,
    "texture_alignment": "original_image",
    "orientation": "align_image",
    "face_limit": 20000,
    "texture": True,
    "pbr": True,
    "texture_quality": "detailed",
    "texture_version": "v3.5-20260815",
    "geometry_quality": "detailed",
}


def api_key():
    key = os.environ.get("TRIPO_API_KEY")
    if not key and sys.platform == "win32":
        import winreg

        try:
            with winreg.OpenKey(winreg.HKEY_CURRENT_USER, "Environment") as env:
                key, _ = winreg.QueryValueEx(env, "TRIPO_API_KEY")
        except FileNotFoundError:
            pass
    if not key:
        raise RuntimeError("TRIPO_API_KEY is missing. Set it as a User environment variable.")
    return key.strip()


def request_json(path, key, *, payload=None, body=None, content_type=None):
    headers = {"Authorization": f"Bearer {key}"}
    if payload is not None:
        body = json.dumps(payload).encode("utf-8")
        content_type = "application/json"
    if content_type:
        headers["Content-Type"] = content_type
    req = urllib.request.Request(BASE + path, data=body, headers=headers)
    try:
        with urllib.request.urlopen(req, timeout=90) as response:
            result = json.load(response)
    except urllib.error.HTTPError as error:
        detail = error.read(1000).decode("utf-8", errors="replace")
        raise RuntimeError(f"Tripo HTTP {error.code}: {detail}") from error
    if result.get("code") != 0:
        raise RuntimeError(f"Tripo API error: {result.get('code')} {result.get('message')}")
    return result["data"]


def upload_image(key):
    boundary = "----codex-" + uuid.uuid4().hex
    name = IMAGE.name
    mime = mimetypes.guess_type(name)[0] or "application/octet-stream"
    body = (
        f"--{boundary}\r\n"
        f'Content-Disposition: form-data; name="file"; filename="{name}"\r\n'
        f"Content-Type: {mime}\r\n\r\n"
    ).encode("ascii") + IMAGE.read_bytes() + f"\r\n--{boundary}--\r\n".encode("ascii")
    return request_json("/files", key, body=body,
                        content_type=f"multipart/form-data; boundary={boundary}")["file_token"]


def save_state(state):
    OUTPUT.mkdir(parents=True, exist_ok=True)
    STATE.write_text(json.dumps(state, ensure_ascii=False, indent=2), encoding="utf-8")


def download(url, destination):
    req = urllib.request.Request(url, headers={"User-Agent": "W01-Tripo-Client/1.0"})
    with urllib.request.urlopen(req, timeout=180) as response:
        with destination.open("wb") as file:
            while block := response.read(1024 * 1024):
                file.write(block)
    if destination.stat().st_size == 0:
        destination.unlink()
        raise RuntimeError(f"Downloaded file is empty: {destination}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Check input, settings and key without submitting")
    args = parser.parse_args()
    if not IMAGE.is_file():
        raise RuntimeError(f"Reference image missing: {IMAGE}")
    if IMAGE.stat().st_size > 20 * 1024 * 1024:
        raise RuntimeError("Reference image exceeds Tripo's 20 MB limit")
    key = api_key()
    balance = request_json("/account/balance", key)["balance"]
    print(f"API balance: {balance} credits; estimated W01 cost: {ESTIMATED_CREDITS} credits")
    if args.check:
        print(json.dumps({"reference": str(IMAGE), "output": str(OUTPUT),
                          "parameters": PARAMETERS, "api_key": "present"}, indent=2))
        return

    if STATE.exists():
        state = json.loads(STATE.read_text(encoding="utf-8"))
        if state.get("status") in ("failed", "cancelled", "banned"):
            raise RuntimeError(f"Existing task {state['task_id']} ended as {state['status']}; inspect it before retrying")
        print(f"Resuming task {state['task_id']}")
    else:
        if float(balance) < ESTIMATED_CREDITS:
            raise RuntimeError("API balance is below the estimated W01 cost; no task was submitted")
        token = upload_image(key)
        task = request_json("/generation/image-to-model", key,
                            payload={"input": token, **PARAMETERS})
        state = {"task_id": task["task_id"], "status": "submitted",
                 "reference": str(IMAGE), "parameters": PARAMETERS}
        save_state(state)
        print(f"Submitted W01 task {state['task_id']}")

    for _ in range(180):
        task = request_json(f"/tasks/{state['task_id']}", key)
        state["status"] = task["status"]
        state["progress"] = task.get("progress")
        if "credits_consumed" in task:
            state["credits_consumed"] = task["credits_consumed"]
        save_state(state)
        print(f"{state['status']}: {state['progress']}%", flush=True)
        if state["status"] in ("failed", "cancelled", "banned"):
            raise RuntimeError(f"W01 task ended as {state['status']}: {task.get('message', '')}")
        if state["status"] == "success":
            model_url = task.get("output", {}).get("model_url")
            if not model_url:
                raise RuntimeError("W01 task succeeded without output.model_url")
            model = OUTPUT / "W01_cultivator.glb"
            if not model.exists():
                download(model_url, model)
            preview_url = task.get("output", {}).get("rendered_image_url")
            if preview_url and not (OUTPUT / "W01_preview.png").exists():
                download(preview_url, OUTPUT / "W01_preview.png")
            print(f"Model: {model} ({model.stat().st_size:,} bytes)")
            print(f"Credits: {state.get('credits_consumed', 'not reported')}")
            return
        time.sleep(5)
    raise RuntimeError("Polling timed out; rerun the same command to resume this task")


if __name__ == "__main__":
    try:
        main()
    except (RuntimeError, OSError, ValueError) as error:
        print(f"Error: {error}", file=sys.stderr)
        sys.exit(1)
