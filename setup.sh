#!/usr/bin/env bash
# Clones (or updates) every repository the server depends on, next to this one, then builds the server.
#
# Usage: ./setup.sh [branch]
#   branch: branch to checkout in each dependency (default: the server's current branch, or develop).
#           Falls back to develop when a dependency does not have that branch.
set -euo pipefail

SERVER_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SERVER_DIR")"
FALLBACK_BRANCH="develop"

BRANCH="${1:-$(git -C "$SERVER_DIR" branch --show-current 2>/dev/null || true)}"
BRANCH="${BRANCH:-$FALLBACK_BRANCH}"

# Reuse the protocol of the server's remote (SSH or HTTPS).
case "$(git -C "$SERVER_DIR" remote get-url origin 2>/dev/null || true)" in
    git@github.com:*) BASE_URL="git@github.com:natofgame" ;;
    *) BASE_URL="https://github.com/natofgame" ;;
esac

# "<local folder> <GitHub repository>"
DEPENDENCIES=(
    "Yggdrasilnet.Maths Yggdrasilnet-Math"
    "Yggdrasilnet.Network Yggdrasilnet-Network"
    "Yggdrasilnet.Gameplay Yggdrasilnet-Gameplay"
)

resolve_branch() {
    local url="$1"
    if git ls-remote --exit-code --heads "$url" "$BRANCH" > /dev/null 2>&1; then
        echo "$BRANCH"
    else
        echo "$FALLBACK_BRANCH"
    fi
}

for dependency in "${DEPENDENCIES[@]}"; do
    read -r folder repository <<< "$dependency"
    target="$ROOT_DIR/$folder"
    url="$BASE_URL/$repository.git"
    branch="$(resolve_branch "$url")"

    if [ ! -d "$target/.git" ]; then
        echo "==> Cloning $folder ($branch)"
        git clone --branch "$branch" "$url" "$target"
        continue
    fi

    if [ -n "$(git -C "$target" status --porcelain)" ]; then
        echo "==> Skipping $folder: uncommitted changes" >&2
        continue
    fi

    echo "==> Updating $folder ($branch)"
    git -C "$target" fetch --all --prune --quiet
    git -C "$target" checkout --quiet "$branch"
    git -C "$target" pull --ff-only --quiet
done

if command -v dotnet > /dev/null 2>&1; then
    echo "==> Building server"
    dotnet build "$SERVER_DIR/Yggdrasilnet.Server.csproj"
else
    echo "==> dotnet not found, skipping build" >&2
fi
