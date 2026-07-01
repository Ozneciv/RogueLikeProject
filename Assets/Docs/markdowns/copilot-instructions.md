# Unity Project Rules
- ALWAYS reuse existing architecture. Do NOT create new damage, health, or movement systems.
- Before creating a new enemy script, inspect existing enemy scripts in `Assets/_Project/Scripts/Enemy/`.
- For enemy health and damage, use `DummyHealth` and call `TakeDamage(...)`.
- Do not create a custom damage/health system or a new `IDamageable` interface unless it already exists in the repository.
- For buff/status changes, use existing `DummyHealth.SetBuffedStatus(bool)` logic and existing buff systems like `CrystalTuner`, `TotemSpawner`, and `MagicStone`.
- Do not create custom timers for status modifications; reuse the current project patterns.
- Add `RequireComponent(typeof(DummyHealth))` on new enemy AI scripts when appropriate.
- Use existing enemy AI patterns from `Spider_AI.cs`, `Golem_AI.cs`, `CrystalWatcher_AI.cs`, and `ShardSwarm_AI.cs`.