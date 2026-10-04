# Catapult Cats — Project Direction

## Purpose

Catapult Cats is a small game-development portfolio project. Its central portfolio message is that the developer can build an Angry-Birds-inspired slingshot and smash mechanic, 2D physics destruction, data-driven levels, and practical custom Unity level-authoring tools. The game will use original assets, presentation, and a Cats vs Mice theme rather than reproducing another game's identity.

The game is a fully 2D, side-view experience designed for a landscape 16:9 frame. A chubby cat is launched from a wooden slingshot toward mice protected by physical structures. The intended balance is approximately 40% puzzle solving and 60% satisfying destruction, with multiple physical solutions allowed.

## Locked Direction

- Bright backyard-cartoon presentation: blue sky, green grass, and simple environmental details.
- Cute and playful, without becoming excessively childish; roughly 40% clean/minimal and 60% stylized.
- Unity 2D architecture using `Rigidbody2D`, `Collider2D`, `PhysicsMaterial2D`, and `SpriteRenderer` when gameplay is implemented.
- No realtime lighting or shadow pipeline, Universal Render Pipeline, post-processing stack, or cinematic rendering complexity.
- One `Gameplay` scene. Individual levels will not be separate Unity scenes.
- Approximately five levels maximum.
- Data-driven level definitions and a piece/prefab catalog are planned for a later milestone.
- A custom Unity Level Editor, validation, and a Playtest Current Level workflow are planned.
- The user will manually author the final levels with that editor.
- Prefer direct, readable solutions. Avoid premature abstraction and overengineering.
- This is a focused portfolio piece, not a commercial-production project.

## Milestone Boundaries

- **R0:** repository, project structure, scene/configuration foundation, assemblies, tests, and direction documentation.
- **R1:** slingshot interaction, cat drag/release, trajectory, and `Rigidbody2D` launch.
- **R2:** mouse targets, breakable materials, heavy objects, impulse damage, and shot lifecycle.
- **R3:** data-driven level format and custom Level Editor.
- **R4:** manual authoring of up to five levels, with design feedback.
- **R5:** Cats vs Mice art, background, HUD, stars, VFX, and audio.
- **R6:** portfolio QA, README, and showcase preparation.

## NON-GOALS

- No large campaign.
- No online or social systems.
- No monetization.
- No 3D gameplay or 3D physics architecture.
- No complex character abilities initially.
- No realistic fracture simulation.
- No procedural level generation.
- No unnecessary service architecture.
- No commercial-production scope creep.

## ENGINEERING PRIORITIES

1. Readable mechanic.
2. Deterministic and testable math where practical.
3. Robust 2D physics.
4. Authoring usability.
5. Portfolio clarity.
6. Visual polish after mechanics.

When a proposed feature, abstraction, or dependency conflicts with this document, prefer the smallest approach that demonstrates the core mechanic clearly and keeps authoring reliable.
