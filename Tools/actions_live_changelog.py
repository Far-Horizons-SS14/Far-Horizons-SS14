import yaml
import os
import time
import requests
from git import Repo

DISCORD_WEBHOOK_URL = os.environ.get("DISCORD_WEBHOOK_URL", None)
CHANGELOG_FILE = os.environ.get("CHANGELOG_FILE", "Resources/Changelog/ChangelogFarHorizons.yml")

TYPES_TO_EMOJI = {"Fix": "🐛", "Add": "🆕", "Remove": "❌", "Tweak": "⚒️"}
EMBED_DESCRIPTION_LIMIT = 4096
EMBED_TITLE_LIMIT = 256
EMBED_FIELD_NAME_LIMIT = 256
EMBED_FIELD_VALUE_LIMIT = 1024

def main():
    if not DISCORD_WEBHOOK_URL:
        print("No webhook URL; skipping send")
        return

    changelog = get_changelog()
    if not changelog:
        print("No new changelogs")
        return
    
    by_pr = group_entries_by_pr(changelog)

    for pr, changelog in by_pr.items():
        send_changelog(pr, changelog)


def get_changelog():
    repo = Repo(".")

    diffs = repo.head.commit.parents[0].diff("HEAD", create_patch=True)
    patch_text_raw = ""
    for diff in diffs:
        if diff.a_path == diff.b_path == CHANGELOG_FILE:
            patch_text_raw = diff.diff.decode('utf-8')
    
    if patch_text_raw == "":
        return []
    
    patch = ""
    for line in patch_text_raw.splitlines():
        if line.startswith('+') and not line.startswith('+++'):
            patch += line[1:] + "\n"
    
    data = yaml.safe_load(patch) or []

    return data

def group_entries_by_pr(entries):
    groups = {}
    for entry in entries:
        url = entry.get("url", "")
        if url and url.strip():
            pr_number = url.rstrip("/").split("/")[-1]
        else:
            pr_number = "no-pr"
        groups.setdefault(pr_number, []).append(entry)
    return groups

def send_changelog(pr, changelog):
    embed = build_embed(pr, changelog)
    payload = {
        "embeds": [embed],
        "allowed_mentions": {"parse": []},  # no automatic pings
    }
    post_with_retries(payload)

def build_embed(pr, changelog):
    authors = set()
    description_lines = []

    for entry in changelog:
        authors.add(entry.get("author", "Unknown"))
        url = entry.get("url", "").strip() or None
        for change in entry.get("changes", []):
            emoji = TYPES_TO_EMOJI.get(change.get("type", ""), "❓")
            message = change.get("message", "").strip()
            if len(message) > 300:
                message = message[:297].rstrip() + "..."
            line = f"{emoji} {message}"
            if url and pr != "no-pr":
                line += f" ([#{pr}]({url}))"
            description_lines.append(line)

    description = "\n".join(description_lines)
    if len(description) > EMBED_DESCRIPTION_LIMIT:
        description = description[: EMBED_DESCRIPTION_LIMIT - 50].rstrip() + "\n*...truncated...*"

    sorted_authors = sorted(authors)
    authors_str = ", ".join(sorted_authors)
    title = authors_str
    if len(title) > EMBED_TITLE_LIMIT:
        # truncate authors part to fit
        overflow = len(title) - EMBED_TITLE_LIMIT + 3  # for "..."
        # remove overflow chars from authors_str
        truncated_authors = authors_str
        if overflow < len(authors_str):
            truncated_authors = authors_str[: -overflow].rstrip()
            # avoid cutting mid-comma: optionally rstrip to last comma-space
            if "," in truncated_authors:
                truncated_authors = truncated_authors.rsplit(",", 1)[0]
            truncated_authors = truncated_authors.rstrip() + "..."
        title = truncated_authors
        if len(title) > EMBED_TITLE_LIMIT:
            title = title[:EMBED_TITLE_LIMIT]

    author_field = ", ".join(sorted_authors)
    embed: dict[str, Any] = {
        "title": title,
        "description": description,
  #      "fields": [
  #          {"name": "Author(s)", "value": author_field[:EMBED_FIELD_VALUE_LIMIT], "inline": False}
  #      ],
        "footer": {"text": "Far Horizons changelog"},
        "timestamp": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
    }
    if pr != "no-pr":
        embed["url"] = changelog[0].get("url", "")
    return embed

def post_with_retries(payload):
    attempt = 0
    while True:
        try:
            resp = requests.post(DISCORD_WEBHOOK_URL, json=payload, timeout=10)
            if resp.status_code == 429:
                attempt += 1
                if attempt > 20:
                    print("Too many rate limit retries; giving up", file=sys.stderr)
                    sys.exit(1)
                retry_after = resp.json().get("retry_after", 5)
                print(f"Rate limited; sleeping {retry_after}s (attempt {attempt})")
                time.sleep(retry_after)
                continue
            resp.raise_for_status()
            return
        except requests.exceptions.RequestException as e:
            attempt += 1
            if attempt > 5:
                print(f"Failed after retries: {e}", file=sys.stderr)
                return
            backoff = 2 ** attempt
            print(f"Request failed ({e}), backing off {backoff}s and retrying")
            time.sleep(backoff)


if __name__ == "__main__":
    main()