import os
import re
from github import Github

# Reusing your logic for consistency
def parse_changelog(pr_body):
    changelog_entries = []
    pattern = r"(?<!", "", pr_body, flags=re.DOTALL)
    matches = list(re.finditer(pattern, cleaned_body, re.MULTILINE))

    for match in matches:
        author = match.group(1).strip()
        changes_block = match.group(2).strip().splitlines()
        for change in changes_block:
            change_match = re.match(r"-\s+(add|remove|tweak|fix):\s+(.+)", change)
            if change_match:
                changelog_entries.append({
                    "author": author,
                    "type": change_match.group(1).capitalize(),
                    "message": change_match.group(2).strip()
                })
    return changelog_entries

def generate_markdown(entries):
    if not entries:
        return "### ⚠️ No Changelog Detected\nAdd a `:cl: AuthorName` block to your description to include changes in the next update."
    
    lines = ["### 📝 Changelog Preview", "", "| Type | Author | Description |", "| :--- | :--- | :--- |"]
    for e in entries:
        lines.append(f"| **{e['type']}** | {e['author']} | {e['message']} |")
    return "\n".join(lines)

if __name__ == "__main__":
    github_token = os.getenv("GITHUB_TOKEN")
    repo_name = os.getenv("GITHUB_REPOSITORY")
    pr_number = os.getenv("PR_NUMBER")

    g = Github(github_token)
    repo = g.get_repo(repo_name)
    pr = repo.get_pull(int(pr_number))

    entries = parse_changelog(pr.body or "")
    print(generate_markdown(entries))