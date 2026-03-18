# Contributing

Thank you for your interest in contributing to **Traffic Law Enforcement** — a Cities Skylines 2 mod.

## Branch Strategy

This project follows **Git Flow**:

```
main       ← stable releases only
develop    ← integration branch
feature/*  ← per-task branches
```

### Rules

- **Never** commit directly to `main` or `develop`.
- Always create a `feature/*` branch from `develop`.
- Merge back into `develop` via Pull Request.
- `main` is updated only from `develop` when a release is ready.

### Examples

```
feature/traffic-ai-fix
feature/pathfinding-optimization
feature/bus-lane-enforcement
```

## Commit Conventions

Use [Conventional Commits](https://www.conventionalcommits.org/):

| Prefix      | Usage                    |
|-------------|--------------------------|
| `feat:`     | New feature              |
| `fix:`      | Bug fix                  |
| `refactor:` | Code restructuring       |
| `perf:`     | Performance improvement  |
| `chore:`    | Miscellaneous            |

### Rules

- **One logical change per commit** — avoid large "dump" commits.
- Write clear, descriptive commit messages.

## Pull Request Rules

1. **One feature per PR** — keep PRs focused and reviewable.
2. **Must include an explanation** — describe what changed and why.
3. **Must include test notes** — how was it tested? In-game scenario, edge cases, etc.
4. Follow the PR template provided.

## Code Guidelines

- Follow **ECS architecture** — systems contain logic, components contain data.
- Prefer `SystemBase` / `ISystem` patterns.
- Use `IComponentData` for components.
- Avoid allocations in hot paths (`OnUpdate`).
- Use Jobs and Burst compilation where possible.
- Follow naming conventions: `*System`, `*Component`, `*Job`.
