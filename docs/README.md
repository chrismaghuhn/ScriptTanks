# ScriptTanks documentation

Navigation hub for technical docs in this folder. For build commands and contribution workflow, see [DEVELOPMENT.md](DEVELOPMENT.md) and [../CONTRIBUTING.md](../CONTRIBUTING.md).

The full game design and planning archive (German) lives in [../ScriptTanks_Document_Package/](../ScriptTanks_Document_Package/). Start with [../ScriptTanks_Document_Package/README_INDEX.md](../ScriptTanks_Document_Package/README_INDEX.md).

**Art & UI (v0.1):** [Target_Look_v0.1.md](Target_Look_v0.1.md) — colors, pixel sizes, arena, terminal UI.

**Module sprites:** [Module_Visual_Pipeline.md](Module_Visual_Pipeline.md) — chassis/weapon/sensor/CPU layers and PixelLab workflow.

---

## Plans vs checkpoints

| Kind | Naming | Use |
|------|--------|-----|
| **Integration checkpoint** | `*_INTEGRATION_CHECKPOINT.md` or `*_CHECKPOINT.md` | Authoritative **implemented** state after a task track |
| **Plan** | `*_PLAN.md` | Design record; may be superseded by a later checkpoint |

When a checkpoint and an older plan disagree, trust the **newer checkpoint**.

---

## Core simulation and match tick

| Document | Summary |
|----------|---------|
| [SCRIPT_RUNTIME_INTEGRATION_CHECKPOINT.md](SCRIPT_RUNTIME_INTEGRATION_CHECKPOINT.md) | Combined script runtime: sensor, turret, fire, movement composer |
| [SCRIPT_RUNTIME_ROADMAP_CHECKPOINT.md](SCRIPT_RUNTIME_ROADMAP_CHECKPOINT.md) | Runtime API inventory and historical roadmap |
| [TANK_MOVEMENT_INTEGRATION_CHECKPOINT.md](TANK_MOVEMENT_INTEGRATION_CHECKPOINT.md) | Script-driven movement in combined tick |
| [TANK_BOUNDS_INTEGRATION_CHECKPOINT.md](TANK_BOUNDS_INTEGRATION_CHECKPOINT.md) | Arena outer bounds after movement |
| [WALL_OBSTACLE_COLLISION_INTEGRATION_CHECKPOINT.md](WALL_OBSTACLE_COLLISION_INTEGRATION_CHECKPOINT.md) | **Authoritative** combined tick order including wall obstacles |

Related plans: [TANK_MOVEMENT_TICK_INTEGRATION_PLAN.md](TANK_MOVEMENT_TICK_INTEGRATION_PLAN.md), [TANK_BOUNDS_ARENA_COLLISION_PLAN.md](TANK_BOUNDS_ARENA_COLLISION_PLAN.md), [WALL_OBSTACLE_COLLISION_PLAN.md](WALL_OBSTACLE_COLLISION_PLAN.md).

---

## Combat and projectiles

| Document | Summary |
|----------|---------|
| [PROJECTILE_SPAWN_TICK_INVENTORY.md](PROJECTILE_SPAWN_TICK_INVENTORY.md) | Spawn-tick and owner self-hit policy inventory |
| [MUZZLE_RADIUS_TUNING_CHECKPOINT.md](MUZZLE_RADIUS_TUNING_CHECKPOINT.md) | Muzzle geometry tuning checkpoint |
| [PROJECTILE_SELF_HIT_MUZZLE_GEOMETRY_PLAN.md](PROJECTILE_SELF_HIT_MUZZLE_GEOMETRY_PLAN.md) | Self-hit / muzzle geometry design |

Fire pipeline plans: [SCRIPT_MAPPED_FIRE_REQUEST_CONSTRUCTION_PLAN.md](SCRIPT_MAPPED_FIRE_REQUEST_CONSTRUCTION_PLAN.md), [SCRIPT_MAPPED_FIRE_REQUEST_APPLICATION_PLAN.md](SCRIPT_MAPPED_FIRE_REQUEST_APPLICATION_PLAN.md), [DETERMINISTIC_FIRE_MUZZLE_RESOLVER_PLAN.md](DETERMINISTIC_FIRE_MUZZLE_RESOLVER_PLAN.md), [DETERMINISTIC_FIRE_VELOCITY_RESOLVER_PLAN.md](DETERMINISTIC_FIRE_VELOCITY_RESOLVER_PLAN.md), [DETERMINISTIC_MUZZLE_GEOMETRY_PLAN.md](DETERMINISTIC_MUZZLE_GEOMETRY_PLAN.md).

---

## Scripting runtime

| Document | Summary |
|----------|---------|
| [SCRIPT_RUNTIME_INTEGRATION_CHECKPOINT.md](SCRIPT_RUNTIME_INTEGRATION_CHECKPOINT.md) | End-to-end combined composer and tick wrapper |
| [COMBINED_RUNTIME_LOGGING_CHECKPOINT.md](COMBINED_RUNTIME_LOGGING_CHECKPOINT.md) | Logged runner/replay and rich tick logs |
| [SCRIPT_RUNTIME_EVALUATION_DEBUG_SURFACE_REVIEW.md](SCRIPT_RUNTIME_EVALUATION_DEBUG_SURFACE_REVIEW.md) | Evaluation / debug surface review |

Composer and tick plans: [COMBINED_SCRIPT_RUNTIME_COMPOSER_PLAN.md](COMBINED_SCRIPT_RUNTIME_COMPOSER_PLAN.md), [COMBINED_SCRIPT_RUNTIME_TICK_PLAN.md](COMBINED_SCRIPT_RUNTIME_TICK_PLAN.md), [COMBINED_RUNTIME_RUNNER_REPLAY_PLAN.md](COMBINED_RUNTIME_RUNNER_REPLAY_PLAN.md), [LOGGED_COMBINED_RUNTIME_RUNNER_REPLAY_PLAN.md](LOGGED_COMBINED_RUNTIME_RUNNER_REPLAY_PLAN.md).

Translator / commands: [SCRIPT_COMMAND_TRANSLATOR_V2_PLAN.md](SCRIPT_COMMAND_TRANSLATOR_V2_PLAN.md), [SCRIPT_TRANSLATED_COMMAND_DOMAIN_MAPPING_PLAN.md](SCRIPT_TRANSLATED_COMMAND_DOMAIN_MAPPING_PLAN.md), [FIRST_PURE_SCRIPT_INTENT_INTEGRATION_COMPOSER_PLAN.md](FIRST_PURE_SCRIPT_INTENT_INTEGRATION_COMPOSER_PLAN.md).

---

## Turret aim, turn-rate, full-angle

| Document | Summary |
|----------|---------|
| [FULL_ANGLE_TURRET_AIM_INTEGRATION_CHECKPOINT.md](FULL_ANGLE_TURRET_AIM_INTEGRATION_CHECKPOINT.md) | Full-angle aim resolver integration |
| [TURRET_TURN_RATE_INTEGRATION_CHECKPOINT.md](TURRET_TURN_RATE_INTEGRATION_CHECKPOINT.md) | Gradual turret rotation and fire-while-turning policy |
| [FULL_ANGLE_AIM_API_COMPATIBILITY_AUDIT.md](FULL_ANGLE_AIM_API_COMPATIBILITY_AUDIT.md) | API compatibility audit |

Plans: [FULL_ANGLE_TURRET_AIM_RESOLVER_PLAN.md](FULL_ANGLE_TURRET_AIM_RESOLVER_PLAN.md), [TURRET_TURN_RATE_GRADUAL_ROTATION_PLAN.md](TURRET_TURN_RATE_GRADUAL_ROTATION_PLAN.md), [SCRIPT_TURRET_APPLICATION_PLAN.md](SCRIPT_TURRET_APPLICATION_PLAN.md), [DETERMINISTIC_ROTATION_FORWARD_HELPER_PLAN.md](DETERMINISTIC_ROTATION_FORWARD_HELPER_PLAN.md).

---

## Aim status and alignment (script-visible)

| Document | Summary |
|----------|---------|
| [SCRIPT_VISIBLE_TURRET_ALIGNMENT_STATUS_INTEGRATION_CHECKPOINT.md](SCRIPT_VISIBLE_TURRET_ALIGNMENT_STATUS_INTEGRATION_CHECKPOINT.md) | **Authoritative** — `TurretAligned`, `TurretTurning`, aim target conditions |
| [SCRIPT_VISIBLE_TURRET_ALIGNMENT_STATUS_PLAN.md](SCRIPT_VISIBLE_TURRET_ALIGNMENT_STATUS_PLAN.md) | Original plan (superseded by checkpoint above) |

---

## Replay, logging, runner

| Document | Summary |
|----------|---------|
| [COMBINED_RUNTIME_LOGGING_CHECKPOINT.md](COMBINED_RUNTIME_LOGGING_CHECKPOINT.md) | Logged combined runner and replay |
| [COMBINED_RUNTIME_RUNNER_REPLAY_PLAN.md](COMBINED_RUNTIME_RUNNER_REPLAY_PLAN.md) | Runner/replay design |
| [LOGGED_COMBINED_RUNTIME_RUNNER_REPLAY_PLAN.md](LOGGED_COMBINED_RUNTIME_RUNNER_REPLAY_PLAN.md) | Logged variants |

---

## Future plans

| Document | Summary |
|----------|---------|
| [MULTI_CONDITION_SCRIPT_ROUTINE_PLAN.md](MULTI_CONDITION_SCRIPT_ROUTINE_PLAN.md) | AND multi-condition routines (next scripting track) |
| [SCRIPT_RUNTIME_ROADMAP_CHECKPOINT.md](SCRIPT_RUNTIME_ROADMAP_CHECKPOINT.md) | Broader runtime gaps and sequencing |

Other active plans: [SCRIPT_TICK_COMBAT_EVENT_DESIGN_PLAN.md](SCRIPT_TICK_COMBAT_EVENT_DESIGN_PLAN.md), [MATCH_SCRIPT_RUNTIME_STATE_PAIRING_PLAN.md](MATCH_SCRIPT_RUNTIME_STATE_PAIRING_PLAN.md), [SCRIPT_RUNTIME_CONTEXT_BUILDER_PLAN.md](SCRIPT_RUNTIME_CONTEXT_BUILDER_PLAN.md).

---

## Misc

| Document | Summary |
|----------|---------|
| [chat-session-sensor-layer.md](chat-session-sensor-layer.md) | Sensor layer session notes |
| [GITHUB_PRESENTATION_TODO.md](GITHUB_PRESENTATION_TODO.md) | GitHub repo presentation checklist |
