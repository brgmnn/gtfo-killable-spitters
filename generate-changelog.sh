#!/bin/bash
set -euo pipefail

releases=$(gh release list --exclude-drafts --json name,publishedAt,tagName --limit 10000)

# CHANGELOG.md ships inside the Thunderstore package (csproj PostBuild copy), so
# an empty release list — rate limit, token scope, outage — must fail here
# rather than let the workflow commit a header-only changelog to main.
if [ -z "$releases" ] || [ "$(echo "$releases" | jq 'length')" -eq 0 ]; then
  echo "error: gh release list returned no releases, CHANGELOG.md left untouched" >&2
  exit 1
fi

# Generate the header
cat <<-EOF > CHANGELOG.md
# Changelog

See more at https://github.com/brgmnn/gtfo-killable-spitters

EOF

# Iterate over the tags and generate their respective change logs
echo "$releases" | jq -c '.[]' | while read -r release; do
  name=$(echo "$release" | jq -r '.name')
  publishedAt=$(echo "$release" | jq -r '.publishedAt')
  # jq's strptime/strftime work the same on GNU and BSD (macOS), unlike date -d
  releasedAt=$(jq -rn --arg d "$publishedAt" '$d | strptime("%Y-%m-%dT%H:%M:%SZ") | strftime("%B %d, %Y")')
  tag=$(echo "$release" | jq -r '.tagName')

  echo "-> $name ($tag)"

  # Fetched outside the heredoc so a failing gh call aborts (set -e) instead
  # of silently writing an empty release body.
  body=$(gh release view "$tag" --json body -q '.body' | tr -d '\r')

  cat <<-EOF >> CHANGELOG.md

## [$name](https://github.com/brgmnn/gtfo-killable-spitters/releases/tag/$tag) — $releasedAt

$body

EOF
done
