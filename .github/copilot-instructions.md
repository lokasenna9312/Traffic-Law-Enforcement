# Copilot Instructions

> This is a Cities Skylines 2 mod project using ECS (Entity Component System).

## Architecture

- Follow ECS-based architecture at all times.
- Prefer `SystemBase` / `ISystem` patterns for systems.
- Use `IComponentData` for components.
- All data should live in components; systems contain logic only.

## Performance

- **Avoid allocations in hot paths** — no `new`, LINQ, or boxing in `OnUpdate`.
- Use **Jobs** (`IJobEntity`, `IJobChunk`) instead of heavy main-thread logic.
- **Cache `EntityQuery`** — never create queries inside `OnUpdate`.
- Prefer `NativeArray` / `NativeList` over managed collections in job code.
- Use `Burst`-compiled jobs wherever possible.

## Naming Conventions

| Type       | Suffix       | Example                        |
|------------|-------------|--------------------------------|
| System     | `*System`   | `LaneTransitionViolationSystem` |
| Component  | `*Component`| `VehicleLaneHistory`            |
| Job        | `*Job`      | `ProcessViolationJob`           |

## Code Style

- One system per file.
- Keep systems focused — single responsibility.
- Prefer composition over inheritance.
- Use descriptive names; avoid abbreviations.

## Commit Messages

Follow conventional commits:

- `feat:` new feature
- `fix:` bug fix
- `refactor:` code restructuring
- `perf:` performance improvement
- `chore:` miscellaneous

## Branching

- Never commit directly to `main` or `develop`.
- Always work on `feature/*` branches.
- One logical change per commit.
