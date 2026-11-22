#!/usr/bin/env python3

import requests
import os
from typing import Iterable


PUBLISH_TOKEN = os.environ["PUBLISH_TOKEN"]
ROBUST_CDN_URL = os.environ["ROBUST_CDN_URL"]
FORK_ID = os.environ["FORK_ID"]
RELEASE_DIR = os.environ["RELEASE_DIR"]


def main():
    with open(os.path.join(RELEASE_DIR, "VERSION"), "r") as version_file:
        version = version_file.readline().strip()

    engine_version = ""
    try:
        with open(os.path.join(RELEASE_DIR, "ENGINE_VERSION"), "r") as version_file:
            engine_version = version_file.readline().strip()
    except FileNotFoundError:
        print("No Engine Version file found, omitting")

    session = requests.Session()
    session.headers = {
        "Authorization": f"Bearer {PUBLISH_TOKEN}",
    }

    print(f"Starting publish on Robust.Cdn for version {version}")

    data = {
        "version": version,
        "engineVersion": engine_version,
    }
    headers = {
        "Content-Type": "application/json"
    }
    resp = session.post(f"{ROBUST_CDN_URL}fork/{FORK_ID}/publish/start", json=data, headers=headers)
    resp.raise_for_status()
    print("Publish successfully started, adding files...")

    for file in get_files_to_publish():
        print(f"Publishing {file}")
        with open(file, "rb") as f:
            headers = {
                "Content-Type": "application/octet-stream",
                "Robust-Cdn-Publish-File": os.path.basename(file),
                "Robust-Cdn-Publish-Version": version
            }
            resp = session.post(f"{ROBUST_CDN_URL}fork/{FORK_ID}/publish/file", data=f, headers=headers)

        resp.raise_for_status()

    print("Successfully pushed files, finishing publish...")

    data = {
        "version": version
    }
    headers = {
        "Content-Type": "application/json"
    }
    resp = session.post(f"{ROBUST_CDN_URL}fork/{FORK_ID}/publish/finish", json=data, headers=headers)
    resp.raise_for_status()

    print("SUCCESS!")


def get_files_to_publish() -> Iterable[str]:
    for file in os.listdir(RELEASE_DIR):
        yield os.path.join(RELEASE_DIR, file)


if __name__ == '__main__':
    main()
